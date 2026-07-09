namespace UnaProject.Application.Models.Requests.Trackings
{
    public class CreateTrackingRequest
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
