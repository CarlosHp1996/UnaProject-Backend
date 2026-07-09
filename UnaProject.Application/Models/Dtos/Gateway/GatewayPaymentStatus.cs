namespace UnaProject.Application.Models.Dtos.Gateway
{
    public class GatewayPaymentStatus
    {
        public string ExternalId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }
}
