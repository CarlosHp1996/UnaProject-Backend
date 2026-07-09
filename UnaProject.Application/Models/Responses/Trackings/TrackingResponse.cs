namespace UnaProject.Application.Models.Responses.Trackings
{
    public class TrackingResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
