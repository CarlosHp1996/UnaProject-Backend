using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Responses.Orders
{
    public class UpdateOrderResponse
    {
        public Guid OrderId { get; set; }
        public EnumOrderStatus Status { get; set; } = EnumOrderStatus.Pending;
        public EnumPaymentStatus PaymentStatus { get; set; } = EnumPaymentStatus.Pending;
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
