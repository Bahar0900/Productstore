namespace ProductStore.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal SubTotal => Items.Sum(i => i.Quantity * i.Product.Price);
        public decimal ShippingFee => Items.Any() ? 50.00m : 0.00m;
        public decimal TotalAmount => SubTotal + ShippingFee;
    }
}
