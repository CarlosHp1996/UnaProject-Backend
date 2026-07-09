using UnaProject.Application.Interfaces;

namespace UnaProject.Application.Services
{
    public class PaymentGatewayResolver : IPaymentGatewayResolver
    {
        private readonly IReadOnlyDictionary<string, IPaymentGateway> _byMethod;
        private readonly IReadOnlyDictionary<string, IPaymentGateway> _byProvider;

        public PaymentGatewayResolver(
            IDictionary<string, IPaymentGateway> methodMap,
            IDictionary<string, IPaymentGateway> providerMap)
        {
            _byMethod = new Dictionary<string, IPaymentGateway>(methodMap, StringComparer.OrdinalIgnoreCase);
            _byProvider = new Dictionary<string, IPaymentGateway>(providerMap, StringComparer.OrdinalIgnoreCase);
        }

        public IPaymentGateway Resolve(string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod) || !_byMethod.TryGetValue(paymentMethod, out var gateway))
                throw new InvalidOperationException($"Nenhum gateway de pagamento configurado para o método '{paymentMethod}'.");

            return gateway;
        }

        public IPaymentGateway ResolveByProvider(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider) || !_byProvider.TryGetValue(provider, out var gateway))
                throw new InvalidOperationException($"Nenhum gateway de pagamento configurado para o provedor '{provider}'.");

            return gateway;
        }
    }
}
