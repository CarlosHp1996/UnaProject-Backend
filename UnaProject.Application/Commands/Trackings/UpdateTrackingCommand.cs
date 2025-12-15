using MediatR;
using UnaProject.Application.Models.Requests.Trackings;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Trackings
{
    public class UpdateTrackingCommand : IRequest<Result<TrackingResponse>>
    {
        public Guid Id { get; set; }
        public UpdateTrackingRequest Request;
        public UpdateTrackingCommand(Guid id, UpdateTrackingRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
