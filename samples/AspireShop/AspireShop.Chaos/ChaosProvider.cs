using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspireShop.Chaos
{
    public class ChaosProvider
    {
        private readonly ILogger<ChaosProvider> _logger;
        private readonly bool _randomChaos;

        public ChaosProvider(IConfiguration configuration, ILogger<ChaosProvider> logger)
        {
            _logger = logger;
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
                    _logger.LogWarning("Yep, things should break.");
                    throw new Exception($"Random Chaos occurred with random value: {randomValue}");
                }

                _logger.LogWarning("Nah, maybe next time");

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
