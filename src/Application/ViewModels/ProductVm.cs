namespace Application.ViewModels;

public class ProductVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockLevel { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ProductDetailsVm : ProductVm
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool InStock => StockLevel > 0;
    public string StockStatus => StockLevel switch
    {
        0 => "Out of Stock",
        < 10 => "Low Stock",
        _ => "In Stock"
    };
}