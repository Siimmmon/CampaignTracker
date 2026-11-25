using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Assessment.Part2;

public sealed record OrderTotalDto(int OrderId, decimal Total);

public sealed record OrderTotalsPage(IReadOnlyList<OrderTotalDto> Orders, int? NextAfterOrderId);

public sealed class OrderQueries(DbContext database)
{
    private const int MaxPageSize = 100;

    public async Task<OrderTotalsPage> GetOrdersWithTotalAsync(
        int? afterOrderId = null,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (pageSize is < 1 or > MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"Enter a page size from 1 to {MaxPageSize}.");

        if (afterOrderId is <= 0)
            throw new ArgumentOutOfRangeException(nameof(afterOrderId), "The order ID must be positive.");

        var query = database.Set<Order>().AsNoTracking();
        if (afterOrderId is int cursor)
            query = query.Where(order => order.Id > cursor);

        var orders = await query
            .OrderBy(order => order.Id)
            .Take(pageSize + 1)
            .Select(order => new OrderTotalDto(
                order.Id,
                order.Items.Sum(item => (decimal?)(item.Price * item.Quantity)) ?? 0m))
            .ToListAsync(cancellationToken);

        int? nextAfterOrderId = null;
        if (orders.Count > pageSize)
        {
            orders.RemoveAt(pageSize);
            nextAfterOrderId = orders[^1].OrderId;
        }

        return new OrderTotalsPage(orders.AsReadOnly(), nextAfterOrderId);
    }
}
