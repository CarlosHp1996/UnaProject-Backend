using System.Text.Json.Serialization;

namespace UnaProject.Domain.Enums
{
    /// <summary>
    /// Status do pedido (bucket de fulfillment). Serializa em minúsculas no JSON
    /// (pending, processing, shipped, delivered, cancelled) para casar com o contrato do frontend.
    /// Persistido como string legível no banco via HasConversion&lt;string&gt;() (PascalCase na coluna).
    /// </summary>
    [JsonConverter(typeof(LowerCaseEnumConverter<EnumOrderStatus>))]
    public enum EnumOrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
}
