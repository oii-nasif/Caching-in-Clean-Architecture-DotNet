namespace Application.ViewModels;

public class CartItemVm
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal SubTotal => Price * Quantity;
}

public class CartVm
{
    public string CartId { get; set; } = string.Empty;
    public List<CartItemVm> Items { get; set; } = new();
    public int TotalItems => Items.Sum(x => x.Quantity);
    public decimal TotalAmount => Items.Sum(x => x.SubTotal);
}

public class AddToCartResponseVm
{
    public bool Success { get; set; }
    public int CartItemCount { get; set; }
    public decimal CartTotal { get; set; }
    public string Message { get; set; } = string.Empty;
}