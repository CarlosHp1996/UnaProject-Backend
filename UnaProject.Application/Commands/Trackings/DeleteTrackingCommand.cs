using MediatR;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Trackings
{
    public class DeleteTrackingCommand : IRequest<Result>
    {
        public Guid Id { get; set; }
        public DeleteTrackingCommand(Guid id)
        {
            Id = id;
        }
    }
}
