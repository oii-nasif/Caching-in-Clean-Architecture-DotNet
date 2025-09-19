namespace Application.Common;

public static class CacheKeys
{
    // User-related keys
    public static string UserProfile(int userId)
        => $"user:{userId}:profile";

    public static string UserSession(string sessionId)
        => $"session:{sessionId}:data";

    // Product-related keys
    public static string ProductDetails(int productId)
        => $"product:{productId}:details";

    public static string ProductInventory(int productId)
        => $"product:{productId}:inventory";

    public static string ProductsByCategory(string category)
        => $"products:category:{category}";

    public static string ProductBySku(string sku)
        => $"product:sku:{sku}";

    // Order-related keys
    public static string OrderSummary(int orderId)
        => $"order:{orderId}:summary";

    public static string OrderByNumber(string orderNumber)
        => $"order:number:{orderNumber}";

    public static string OrdersByCustomer(int customerId)
        => $"orders:customer:{customerId}";

    public static string CartItems(string cartId)
        => $"cart:{cartId}:items";

    // Report keys
    public static string DailyReport(DateTime date)
        => $"report:daily:{date:yyyy-MM-dd}";

    public static string ReportData(string reportType, DateTime startDate, DateTime endDate)
        => $"report:{reportType}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
}