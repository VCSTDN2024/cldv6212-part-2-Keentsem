// Models/Category.cs
namespace CLDV6212POE.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public int ProductCount { get; set; }
    }
}

// Models/Product.cs
namespace KhumaloCraft.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal PriceZAR { get; set; }
        public int StockQuantity { get; set; }
        public bool Availability { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = "";
    }
}
