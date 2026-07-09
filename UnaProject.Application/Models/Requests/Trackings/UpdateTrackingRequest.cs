namespace UnaProject.Application.Models.Requests.Trackings
{
    public class UpdateTrackingRequest
    {
        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
