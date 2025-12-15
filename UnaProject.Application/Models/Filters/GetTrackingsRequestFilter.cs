namespace UnaProject.Application.Models.Filters
{
    public class GetTrackingsRequestFilter : BaseRequestFilter
    {
        public string? Status { get; set; }
        public Guid? OrderId { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
