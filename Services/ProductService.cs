using ProvaPub.Models;
using ProvaPub.Repository;

namespace ProvaPub.Services
{
	public interface IProductService
	{
		ProductList ListProducts(int page);
	}

	public class ProductService : BaseService<Product>, IProductService
	{
		public ProductService(TestDbContext ctx) : base(ctx)
		{
		}

		public ProductList ListProducts(int page)
		{
			var pagedResult = GetPagedList(_ctx.Products, page);
			return new ProductList() 
			{  
				HasNext = pagedResult.HasNext, 
				TotalCount = pagedResult.TotalCount, 
				Products = pagedResult.Items 
			};
		}
	}
}
