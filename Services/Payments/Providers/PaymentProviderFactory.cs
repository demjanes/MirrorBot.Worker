using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs.Payments;
using MirrorBot.Worker.Data.Models.Payments;
using MirrorBot.Worker.Services.Payments.Providers.YooKassa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Payments.Providers
{
    public class PaymentProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly PaymentConfiguration _config;

        public PaymentProviderFactory(
            IServiceProvider serviceProvider,
            IOptions<PaymentConfiguration> config)
        {
            _serviceProvider = serviceProvider;
            _config = config.Value;
        }

        public IPaymentProvider GetProvider(PaymentProvider? providerType = null)
        {
            var provider = providerType ?? _config.DefaultProvider;

            return provider switch
            {
                PaymentProvider.YooKassa => _serviceProvider.GetRequiredService<YooKassaPaymentProvider>(),
                // PaymentProvider.Stripe => _serviceProvider.GetRequiredService<StripePaymentProvider>(),
                // PaymentProvider.Crypto => _serviceProvider.GetRequiredService<CryptoPaymentProvider>(),
                _ => throw new NotSupportedException($"Payment provider {provider} is not supported")
            };
        }
    }
}
