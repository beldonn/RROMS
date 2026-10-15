namespace RROMS.ViewModels;

/// <summary>
/// A single line in the shopping cart. The whole cart is serialized to JSON
/// and kept in the customer's session between "Add to cart" and checkout.
/// </summary>
public class CartItemVM
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? SpecialInstructions { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
}
