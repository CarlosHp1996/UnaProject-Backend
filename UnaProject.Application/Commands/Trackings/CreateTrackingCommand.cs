using MediatR;
using UnaProject.Application.Models.Requests.Trackings;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Trackings
{
    public class CreateTrackingCommand : IRequest<Result<TrackingResponse>>
    {
        public CreateTrackingRequest Request;
        public CreateTrackingCommand(CreateTrackingRequest request)
        {
            Request = request;

        }
    }
}
