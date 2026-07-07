namespace IotModel
{
    public sealed partial class ProductCommandDAO : DbContext<ProductCommand>
    {
        private static ProductCommandDAO instance;
        public static ProductCommandDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ProductCommandDAO();
                }
                return instance;
            }
        }
    }
}
