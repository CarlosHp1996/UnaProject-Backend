using MediatR;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Trackings
{
    public class GetTrackingByIdQuery : IRequest<Result<TrackingResponse>>
    {
        public Guid Id { get; set; }

        public GetTrackingByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
