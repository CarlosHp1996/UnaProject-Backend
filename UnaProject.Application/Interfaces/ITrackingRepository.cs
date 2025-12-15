using UnaProject.Application.Models.Requests.Trackings;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Domain.Entities;

namespace UnaProject.Application.Interfaces
{
    public interface ITrackingRepository : IBaseRepository<Tracking>
    {
        Task<TrackingResponse> CreateTrackingEventAsync(CreateTrackingRequest eventData);

        Task<TrackingResponse> UpdateTrackingAsync(Tracking tracking, UpdateTrackingRequest request, CancellationToken cancellationToken);

        // A method that encapsulates all the logic for filters and pagination.
        Task<(List<TrackingResponse> Trackings, int TotalCount)> GetTrackingsFilteredAsync(
            int? page,
            int? pageSize,
            string sortingProp,
            bool ascending,
            string status = null,
            Guid? orderId = null,
            string trackingNumber = null,
            string description = null,
            string location = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);
    }
}
