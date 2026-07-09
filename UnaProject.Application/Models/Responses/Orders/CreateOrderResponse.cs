using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Responses.Orders
{
    public class CreateOrderResponse
    {
        public Guid OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public EnumOrderStatus Status { get; set; } = EnumOrderStatus.Pending;
        public EnumPaymentStatus PaymentStatus { get; set; } = EnumPaymentStatus.Pending;
        public DateTime OrderDate { get; set; }
        public int OrderNumber { get; set; }
        public bool? IsActive { get; set; }
    }
}
