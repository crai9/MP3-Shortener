namespace ProductStoreClient
{
    internal class Product
    {
        public Product()
        {
        }

        public string Category { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int ProductId { get; internal set; }
    }
}