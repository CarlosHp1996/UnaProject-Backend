using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Trackings.Handlers
{
    public class UpdateTrackingCommandHandler : IRequestHandler<UpdateTrackingCommand, Result<TrackingResponse>>
    {
        private readonly ITrackingRepository _trackingRepository;

        public UpdateTrackingCommandHandler(ITrackingRepository trackingRepository)
        {
            _trackingRepository = trackingRepository;
        }

        public async Task<Result<TrackingResponse>> Handle(UpdateTrackingCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<TrackingResponse>();

            try
            {
                var tracking = await _trackingRepository.GetById(request.Id);
                if (tracking == null)
                {
                    result.WithError("Tracking not found");
                    return result;
                }

                var response = await _trackingRepository.UpdateTrackingAsync(tracking, request.Request, cancellationToken);

                result.Value = response;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError(ex.Message);
            }

            return result;
        }
    }
}
