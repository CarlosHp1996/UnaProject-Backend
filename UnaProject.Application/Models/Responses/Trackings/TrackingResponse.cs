namespace UnaProject.Application.Models.Responses.Trackings
{
    public class TrackingResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TrackingNumber { get; set; }
    }
}
