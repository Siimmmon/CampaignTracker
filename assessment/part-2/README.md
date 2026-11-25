# Part 2 — AI code review

The assessment method reads orders and calculates `Price × Quantity` for their line items. Its results depend on how the navigation is loaded, and it changes EF entities during a read.

## Findings

| Issue | Impact |
| --- | --- |
| `Items` is accessed after `Orders.ToListAsync()` | With lazy loading enabled, accessing `Items` can execute a query per order. With lazy loading disabled, an unloaded navigation can produce incomplete totals. |
| The query materializes every order | The read buffers the entire set. EF Core tracks the returned entities by default, which adds memory and change-tracking work. |
| The calculation runs in a loop on the application server | Every line item used in the calculation must be loaded into the process. SQL Server can calculate the totals and return only the order IDs and totals. |
| The method assigns `Order.Total` and returns entities | A later save on the same context can persist the assignment when `Total` is mapped and tracked. Returning entities also couples the caller to the data model. |
| The method has no cancellation token or stable ordering | Callers cannot pass request cancellation to EF, and pagination would require an explicit sort order. |

References for navigation loading, bounded reads, and tracking are in the EF Core documentation. [Efficient querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying) · [Tracking and no-tracking queries](https://learn.microsoft.com/en-us/ef/core/querying/tracking)

## Final rewrite

`GetOrdersWithTotalAsync()` returns an `OrderTotalsPage` containing read-only order totals and a cursor for the next page. Callers can retrieve the complete list by following `NextAfterOrderId` until it is `null`. [OrderQueries.cs](OrderQueries.cs#L18)

- **Database-side totals.** The LINQ projection translates to SQL `SUM(Price * Quantity)` in one query per page. `?? 0m` gives orders with zero items a total of zero. [Aggregate projection](OrderQueries.cs#L33)
- **Read-only response.** `OrderTotalDto` carries just the order ID and total. The query projects those values directly and marks the read with `AsNoTracking()`. It returns the page as a read-only list. [Query setup](OrderQueries.cs#L29) · [Page response](OrderQueries.cs#L48)
- **Bounded, stable pages.** The query orders by `Order.Id` and uses the previous page’s last returned ID as an exclusive cursor. It defaults to 25 results, accepts up to 100, and fetches one extra row to decide whether to return a next-page cursor. For example, three orders requested in pages of two produce a two-order page with a cursor and a one-order final page. [Argument validation](OrderQueries.cs#L18) · [Cursor and page logic](OrderQueries.cs#L29)
- **Cancellation.** `ToListAsync(cancellationToken)` passes the caller’s cancellation request through to EF Core. [Async query](OrderQueries.cs#L39)

The sample targets an EF Core 10 SQL Server context that maps `Order` and its `Items` relationship. It assumes a decimal unit `Price`, integer `Quantity`, and one currency per order. The result is the line-item subtotal. Taxes, discounts, and shipping are separate calculations. Use a decimal mapping that accommodates the stored prices and totals, and index the order item foreign key used by the aggregate.

The prompt, unedited AI response, and three changes from the draft are recorded in the [AI workflow](AI-WORKFLOW.md).

## Verification

The rewrite compiled with .NET 10 and ran against SQL Server LocalDB with three test orders. The generated SQL used `TOP` to bound the page and `SUM` to calculate each total. Checks passed for decimal totals, orders with zero items, cursor boundaries, one SQL command per page, an empty EF change tracker, invalid arguments, and cancellation.
