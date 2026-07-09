using System.Text.Json.Serialization;

namespace UnaProject.Domain.Enums
{
    /// <summary>
    /// Status de pagamento do pedido (bucket financeiro do Order, distinto de Payment.Status).
    /// Serializa em minúsculas no JSON (pending, approved, paid, failed, cancelled, refunded)
    /// para casar com o contrato do frontend. Persistido como string via HasConversion&lt;string&gt;().
    /// </summary>
    [JsonConverter(typeof(LowerCaseEnumConverter<EnumPaymentStatus>))]
    public enum EnumPaymentStatus
    {
        Pending,
        Approved,
        Paid,
        Failed,
        Cancelled,
        Refunded
    }
}
