namespace ProvaPub.Services.PaymentStrategies
{
	public class PayPalPaymentStrategy : IPaymentStrategy
	{
		public string PaymentMethod => "paypal";

		public async Task<bool> ProcessPayment(decimal amount)
		{
			await Task.Delay(150);
			return true;
		}
	}
}