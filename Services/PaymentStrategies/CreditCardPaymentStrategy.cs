namespace ProvaPub.Services.PaymentStrategies
{
	public class CreditCardPaymentStrategy : IPaymentStrategy
	{
		public string PaymentMethod => "creditcard";

		public async Task<bool> ProcessPayment(decimal amount)
		{
			await Task.Delay(200);
			return true;
		}
	}
}