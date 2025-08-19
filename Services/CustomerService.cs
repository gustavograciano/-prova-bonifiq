using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;
using ProvaPub.Repository;

namespace ProvaPub.Services
{
    public interface ICustomerService
    {
        CustomerList ListCustomers(int page);
        Task<bool> CanPurchase(int customerId, decimal purchaseValue);
    }

    public class CustomerService : BaseService<Customer>, ICustomerService
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public CustomerService(TestDbContext ctx, IDateTimeProvider dateTimeProvider) : base(ctx)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public CustomerList ListCustomers(int page)
        {
            var pagedResult = GetPagedList(_ctx.Customers.Include(c => c.Orders), page);
            
            // Converter timezone das orders para UTC-3 (horário brasileiro)
            var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            foreach (var customer in pagedResult.Items)
            {
                if (customer.Orders != null)
                {
                    foreach (var order in customer.Orders)
                    {
                        order.OrderDate = TimeZoneInfo.ConvertTimeFromUtc(order.OrderDate, brazilTimeZone);
                    }
                }
            }
            
            return new CustomerList() 
            { 
                HasNext = pagedResult.HasNext, 
                TotalCount = pagedResult.TotalCount, 
                Customers = pagedResult.Items 
            };
        }

        public async Task<bool> CanPurchase(int customerId, decimal purchaseValue)
        {
            if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));

            if (purchaseValue <= 0) throw new ArgumentOutOfRangeException(nameof(purchaseValue));

            //Business Rule: Non registered Customers cannot purchase
            var customer = await _ctx.Customers.FindAsync(customerId);
            if (customer == null) throw new InvalidOperationException($"Customer Id {customerId} does not exists");

            //Business Rule: A customer can purchase only a single time per month
            var baseDate = _dateTimeProvider.UtcNow.AddMonths(-1);
            var ordersInThisMonth = await _ctx.Orders.CountAsync(s => s.CustomerId == customerId && s.OrderDate >= baseDate);
            if (ordersInThisMonth > 0)
                return false;

            //Business Rule: A customer that never bought before can make a first purchase of maximum 100,00
            var haveBoughtBefore = await _ctx.Customers.CountAsync(s => s.Id == customerId && s.Orders.Any());
            if (haveBoughtBefore == 0 && purchaseValue > 100)
                return false;

            //Business Rule: A customer can purchases only during business hours and working days
            if (!IsBusinessHours(_dateTimeProvider.UtcNow))
                return false;

            return true;
        }

        private bool IsBusinessHours(DateTime utcTime)
        {
            return utcTime.Hour >= 8 && utcTime.Hour <= 18 && 
                   utcTime.DayOfWeek != DayOfWeek.Saturday && 
                   utcTime.DayOfWeek != DayOfWeek.Sunday;
        }

    }
}
