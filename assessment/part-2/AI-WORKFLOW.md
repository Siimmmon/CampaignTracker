# Part 2 — AI workflow

The prompt and raw AI draft below document the starting point for the review. The final implementation and its SQL Server verification are described in the [Part 2 notes](README.md).

## Prompt sent to the AI

````text
Review this AI-generated EF Core method for a .NET 10 technical assessment and give a first-pass C# rewrite for EF Core 10 with SQL Server. The existing order model has a positive integer Order.Id, an Order.Items navigation, decimal item.Price, and integer item.Quantity. The goal is to read each order's calculated total. Identify performance and architectural problems, state assumptions, and propose a concise implementation. You may change the return contract as long as you explain the change. Return your answer as Markdown with one C# code block and brief review notes. Work only from this prompt and return text without calling tools or editing files.

```csharp
public async Task<List<Order>> GetOrdersWithTotalAsync() {
    var orders = await _context.Orders.ToListAsync();
    foreach (var order in orders)
    {
        order.Total = order.Items.Sum(i => i.Price * i.Quantity);
    }
    return orders;
}
```
````

## Raw AI output

````text
The original implementation has several performance and architectural problems:
- Potential N+1 queries: If lazy loading is enabled, accessing order.Items may trigger an additional SQL query for every order.
- Incorrect results: If lazy loading is disabled and the navigation property is not loaded, the calculated total may be incorrect.
- Unnecessary memory consumption: Entire Order entities are retrieved when only the order ID and total are required.
- Client-side computation: Calculations are performed in C# instead of leveraging SQL Server's aggregation capabilities.
- Unnecessary change tracking: EF Core tracks entities by default, despite this being a read-only operation.
- Unintended persistence: Modifying order.Total could result in unintended database updates if it is a mapped property.
- Scalability: Retrieving all orders without pagination could become expensive for large datasets.

Proposed implementation (.NET 10 / EF Core 10)
```csharp
public sealed record OrderTotalDto(int OrderId, decimal Total);

public Task<List<OrderTotalDto>> GetOrdersWithTotalAsync(
    CancellationToken cancellationToken = default)
{
    return _context.Orders
        .Select(order => new OrderTotalDto(
            order.Id,
            order.Items.Sum(item => (decimal?)(item.Price * item.Quantity)) ?? 0m))
        .ToListAsync(cancellationToken);
}
```

Implementation review
Aspect	Improvement
Database queries	Single SQL query instead of potential N+1 queries
Calculation	Database-side aggregation using SUM
Data transfer	Only order IDs and calculated totals
Empty orders	Returns 0m
Change tracking	No tracked entities or entity modifications
Separation of concerns	DTO separates the read model from persistence entities
Cancellation	Supports cancellation of asynchronous database operations

AsNoTracking() is technically redundant because the projection contains no entity instances, but it explicitly communicates the read-only intention.
Assumptions and design decisions
- Order.Items is a correctly configured EF Core relationship.
- Price represents the unit price.
- Orders without items have a total of zero.
- SQL Server decimal precision and scale are sufficient to prevent overflow or unintended precision loss.
- Taxes, discounts and shipping charges are outside the calculation's scope.
- The return contract changes to List<OrderTotalDto> because callers only require the calculated totals, not complete entities.
- Pagination is omitted to satisfy the requirement to retrieve every order. For large production datasets, consider keyset pagination using the positive integer Order.Id.
Conclusion: Server-side aggregation with DTO projection is preferable because it improves database efficiency, reduces memory consumption and avoids unnecessary entity mutations.

Validation limitation: This is a first-pass rewrite based solely on the supplied model. SQL translation, decimal precision and execution-plan performance have not been tested against EF Core 10 and SQL Server.
````

## Three changes from the AI draft

- **Introduced keyset pagination.** Added ascending order ID sorting and cursor-based filtering using `order.Id > cursor`. This makes page order deterministic and lets each request continue after the last order returned.
- **Enforced page size limits and added next-page detection.** The default page size is 25 and the maximum is 100. The query fetches one extra row to detect another page in the same SQL query. It removes that extra row and uses the last returned order ID as the next cursor only when another page exists.
- **Improved separation of concerns and input validation.** Moved the read into `OrderQueries` and validated the cursor and page size before querying. `OrderTotalsPage` returns a read-only list of DTOs and an optional next-page cursor. The final query retains SQL-side aggregation and cancellation support and uses `AsNoTracking()` to signal its read-only intent.
