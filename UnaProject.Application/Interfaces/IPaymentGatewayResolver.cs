namespace UnaProject.Application.Interfaces
{
    /// <summary>
    /// Resolve qual IPaymentGateway usar. Duas formas de resolução:
    /// - Por método de pagamento ("card"/"pix"), usado ao criar/cancelar/consultar um pagamento.
    /// - Por provedor ("stripe"/"abacatepay"), usado ao rotear um webhook recebido em
    ///   POST api/payment/webhook/{provider}.
    /// </summary>
    public interface IPaymentGatewayResolver
    {
        IPaymentGateway Resolve(string paymentMethod);
        IPaymentGateway ResolveByProvider(string provider);
    }
}
