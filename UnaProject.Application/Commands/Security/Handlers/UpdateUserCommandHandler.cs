using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UpdateUserResponse>>
    {
        private readonly IUserRepository _userRepository;

        public UpdateUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UpdateUserResponse>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<UpdateUserResponse>();

            try
            {
                request.Request.Id = request.Id;
                var response = await _userRepository.UpdateUser(request.Request, cancellationToken);

                result.Value = response;
                result.Count = 1;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error updating user: {ex.Message}");
                return result;
            }
        }
    }
}
