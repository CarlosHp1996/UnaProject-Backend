using MediatR;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Trackings
{
    public class GetTrackingsQuery : IRequest<Result<List<TrackingResponse>>>
    {
        public GetTrackingsRequestFilter Filter { get; set; }

        public GetTrackingsQuery(GetTrackingsRequestFilter filter)
        {
            Filter = filter;
        }
    }
}
