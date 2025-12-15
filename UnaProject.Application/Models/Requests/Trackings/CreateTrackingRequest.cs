namespace UnaProject.Application.Models.Requests.Trackings
{
    public class CreateTrackingRequest
    {
        public string TrackingNumber { get; set; }
        public Guid OrderId { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
