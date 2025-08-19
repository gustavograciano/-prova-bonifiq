namespace ProvaPub.Services.PaymentStrategies
{
	public class PixPaymentStrategy : IPaymentStrategy
	{
		public string PaymentMethod => "pix";

		public async Task<bool> ProcessPayment(decimal amount)
		{
			await Task.Delay(100);
			return true;
		}
	}
}