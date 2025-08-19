using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;
using ProvaPub.Repository;

namespace ProvaPub.Services
{
	public abstract class BaseService<T> where T : class
	{
		protected readonly TestDbContext _ctx;

		protected BaseService(TestDbContext ctx)
		{
			_ctx = ctx;
		}

		protected virtual PagedList<T> GetPagedList(IQueryable<T> query, int page, int pageSize = 10)
		{
			var totalCount = query.Count();
			var skip = (page - 1) * pageSize;
			var items = query.Skip(skip).Take(pageSize).ToList();
			
			return new PagedList<T>
			{
				Items = items,
				TotalCount = totalCount,
				HasNext = skip + pageSize < totalCount,
				CurrentPage = page,
				PageSize = pageSize
			};
		}
	}
}