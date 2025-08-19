namespace ProvaPub.Services.PaymentStrategies
{
	public interface IPaymentStrategy
	{
		Task<bool> ProcessPayment(decimal amount);
		string PaymentMethod { get; }
	}
}