namespace ProvaPub.Services.PaymentStrategies
{
	public interface IPaymentStrategyFactory
	{
		IPaymentStrategy CreateStrategy(string paymentMethod);
	}

	public class PaymentStrategyFactory : IPaymentStrategyFactory
	{
		private readonly IEnumerable<IPaymentStrategy> _strategies;

		public PaymentStrategyFactory(IEnumerable<IPaymentStrategy> strategies)
		{
			_strategies = strategies;
		}

		public IPaymentStrategy CreateStrategy(string paymentMethod)
		{
			var strategy = _strategies.FirstOrDefault(s => s.PaymentMethod.Equals(paymentMethod, StringComparison.OrdinalIgnoreCase));
			
			if (strategy == null)
				throw new NotSupportedException($"Payment method '{paymentMethod}' is not supported");
				
			return strategy;
		}
	}
}