using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace AspireShop.Chaos
{
    public class ChaosProvider
    {
        private readonly bool _randomChaos;

        public ChaosProvider(IConfiguration configuration)
        {
            _randomChaos = !string.IsNullOrWhiteSpace(configuration["RANDOM_CHAOS"]);

            //introduce some chaos

        }

        public Task PonderChaosAsync()
        {
            if (!_randomChaos)
            {
                //chaos not necessary
                return Task.CompletedTask;
            }

            try
            {
                int randomValue = new Random(Guid.NewGuid().GetHashCode()).Next(0, 100);
                if (randomValue == 0)
                {
                    throw new Exception($"Random Chaos occurred with random value: {randomValue}");
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Activity.Current?.AddException(ex);
                throw;
            }

        }
    }
}
