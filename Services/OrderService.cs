using ProvaPub.Models;
using ProvaPub.Repository;
using ProvaPub.Services.PaymentStrategies;

namespace ProvaPub.Services
{
	public interface IOrderService
	{
		Task<Order> PayOrder(string paymentMethod, decimal paymentValue, int customerId);
	}

	public class OrderService : IOrderService
	{
        private readonly TestDbContext _ctx;
        private readonly IPaymentStrategyFactory _paymentStrategyFactory;

        public OrderService(TestDbContext ctx, IPaymentStrategyFactory paymentStrategyFactory)
        {
            _ctx = ctx;
            _paymentStrategyFactory = paymentStrategyFactory;
        }

        public async Task<Order> PayOrder(string paymentMethod, decimal paymentValue, int customerId)
		{
			var strategy = _paymentStrategyFactory.CreateStrategy(paymentMethod);
			var paymentSuccess = await strategy.ProcessPayment(paymentValue);
			
			if (!paymentSuccess)
				throw new InvalidOperationException("Payment processing failed");

			var order = new Order()
            {
                Value = paymentValue,
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow
            };

			return await InsertOrder(order);
		}

		private async Task<Order> InsertOrder(Order order)
        {
			var result = (await _ctx.Orders.AddAsync(order)).Entity;
			await _ctx.SaveChangesAsync();
			
			// Criar uma nova instância para retorno com data convertida para UTC-3
			// Não alterar o objeto que está no contexto do EF
			var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
			var orderForReturn = new Order
			{
				Id = result.Id,
				Value = result.Value,
				CustomerId = result.CustomerId,
				OrderDate = TimeZoneInfo.ConvertTimeFromUtc(result.OrderDate, brazilTimeZone),
				Customer = result.Customer
			};
			
			return orderForReturn;
        }
	}
}
