using Microsoft.EntityFrameworkCore;
using ProvaPub.Models;
using ProvaPub.Repository;

namespace ProvaPub.Services
{
	public interface IRandomService
	{
		Task<int> GetRandom();
	}

	public class RandomService : IRandomService
	{
        private readonly TestDbContext _ctx;
        
		public RandomService(TestDbContext ctx)
        {
            _ctx = ctx;
        }
        public async Task<int> GetRandom()
		{
            int number;
            bool isUnique = false;
            int attempts = 0;
            const int maxAttempts = 1000;

            do
            {
                number = new Random().Next(100);
                
                var existingNumber = await _ctx.Numbers.FirstOrDefaultAsync(n => n.Number == number);
                if (existingNumber == null)
                {
                    isUnique = true;
                }
                
                attempts++;
                if (attempts >= maxAttempts)
                {
                    throw new InvalidOperationException("Não foi possível gerar um número único após várias tentativas");
                }
            }
            while (!isUnique);

            _ctx.Numbers.Add(new RandomNumber() { Number = number });
            await _ctx.SaveChangesAsync();
			return number;
		}

	}
}
