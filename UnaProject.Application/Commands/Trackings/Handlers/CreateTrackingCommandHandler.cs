using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Trackings.Handlers
{
    public class CreateTrackingCommandHandler : IRequestHandler<CreateTrackingCommand, Result<TrackingResponse>>
    {
        private readonly ITrackingRepository _trackingRepository;

        public CreateTrackingCommandHandler(ITrackingRepository trackingRepository)
        {
            _trackingRepository = trackingRepository;
        }

        public async Task<Result<TrackingResponse>> Handle(CreateTrackingCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<TrackingResponse>();

            try
            {
                var trackingInfo = await _trackingRepository.CreateTrackingEventAsync(request.Request);

                if (trackingInfo == null)
                {
                    result.WithError("It was not possible to retrieve the tracking information after creation.");
                    return result;
                }

                result.Value = trackingInfo;
                result.Count = 1;
                result.HasSuccess = true;
            }
            catch (KeyNotFoundException ex)
            {
                result.WithError(ex.Message);
            }
            catch (Exception ex)
            {
                result.WithError($"Error creating tracking event: {ex.Message}");
            }

            return result;
        }
    }
}
