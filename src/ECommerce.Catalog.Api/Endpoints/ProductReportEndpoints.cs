using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ECommerce.Catalog.Api.Endpoints;

public static class ProductReportEndpoints
{
    public static RouteGroupBuilder MapProductReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/report", HandleReport).WithName("GenerateReport");
        return group;
    }

    private static async Task<IResult> HandleReport(
        string? type, string? startDate, string? endDate,
        bool? sendEmail, string? emailTo, CatalogDbContext db)
    {
        RequestLog.IncrementRequestCount();
        var products = await db.Products.AsNoTracking().ToListAsync();
        var report = new StringBuilder();

        AppendHeader(report, type, products.Count);
        AppendPriceAnalysis(report, products);
        AppendStockAnalysis(report, products);
        AppendProductListing(report, products);
        AppendDiscountSummary(report, products);
        HandleEmailNotification(sendEmail, emailTo, report);

        RequestLog.Add($"Report generated: {report.Length} chars, type={type ?? "summary"}");
        return Results.Text(report.ToString(), "text/plain");
    }

    private static void AppendHeader(StringBuilder report, string? type, int productCount)
    {
        report.AppendLine("=== PRODUCT CATALOG REPORT ===");
        report.AppendLine($"Generated: {DateTime.Now}");
        report.AppendLine($"Report Type: {type ?? "summary"}");
        report.AppendLine($"Total Products: {productCount}");
        report.AppendLine();
    }

    private static void AppendPriceAnalysis(StringBuilder report, List<Product> products)
    {
        if (products.Count == 0)
            return;
        report.AppendLine("--- Price Analysis ---");
        report.AppendLine($"Average: {products.Average(p => p.Price):C}");
        report.AppendLine($"Highest: {products.Max(p => p.Price):C}");
        report.AppendLine($"Lowest: {products.Min(p => p.Price):C}");
        var median = products.OrderBy(p => p.Price).Skip(products.Count / 2).First().Price;
        report.AppendLine($"Median: {median:C}");
        report.AppendLine();
    }

    private static void AppendStockAnalysis(StringBuilder report, List<Product> products)
    {
        report.AppendLine("--- Stock Analysis ---");
        report.AppendLine($"In Stock: {products.Count(p => p.AvailableStock > 0)}");
        report.AppendLine($"Out of Stock: {products.Count(p => p.AvailableStock <= 0)}");
        report.AppendLine($"Low Stock (<10): {products.Count(p => p.AvailableStock > 0 && p.AvailableStock < 10)}");
        report.AppendLine($"Total Units: {products.Sum(p => p.AvailableStock)}");
        report.AppendLine();
    }

    private static void AppendProductListing(StringBuilder report, List<Product> products)
    {
        report.AppendLine("--- Product Details ---");
        foreach (var p in products)
        {
            report.AppendLine($"[{p.Id}] {p.Name} | {p.Price:C} | Stock: {p.AvailableStock} | " +
                $"{p.Description ?? "No description"}");
        }
        report.AppendLine();
    }

    private static void AppendDiscountSummary(StringBuilder report, List<Product> products)
    {
        report.AppendLine("--- Discount Eligibility ---");
        foreach (var p in products)
        {
            var discount = DiscountResult.Calculate(p.Price, p.AvailableStock);
            report.AppendLine($"  [{p.Id}] {p.Name}: type={(int)discount.Type}, " +
                $"discount={discount.Percent}%, status={discount.Status}");
        }
    }

    private static void HandleEmailNotification(bool? sendEmail, string? emailTo, StringBuilder report)
    {
        if (sendEmail != true || string.IsNullOrEmpty(emailTo))
            return;
        try
        {
            RequestLog.Add($"Attempting to send report email to {emailTo}");
            RequestLog.Add($"Email would be sent to {emailTo} with report ({report.Length} chars)");
        }
        catch (Exception ex)
        {
            RequestLog.SetLastError(ex.Message);
            RequestLog.Add($"Email failed: {ex.Message}");
        }
    }
}
