using ECommerce.Catalog.Api.Models;

namespace ECommerce.Catalog.Api.Endpoints;

public static class RequestLog
{
    private static readonly List<string> _entries = new();
    private static int _requestCount;
    private static string _lastError = "";

    public static int IncrementRequestCount() => ++_requestCount;

    public static void Add(string message) =>
        _entries.Add($"[{DateTime.Now}] {message}");

    public static void LogDiscount(int productId, decimal price, DiscountResult discount)
    {
        if (discount.Type == DiscountType.None)
            return;
        Add($"Product {productId}: discount type {(int)discount.Type}, " +
            $"{discount.Percent}% off, was {price} now {discount.DiscountedPrice}, status={discount.Status}");
    }

    public static void SetLastError(string error) => _lastError = error;

    public static object GetSnapshot() => new
    {
        Logs = _entries.ToList(),
        RequestCount = _requestCount,
        LastError = _lastError
    };

    public static void Clear()
    {
        _entries.Clear();
        _requestCount = 0;
        _lastError = "";
    }
}
