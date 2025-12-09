using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResponse>>
    {
        private readonly IUserRepository _userRepository;

        public CreateUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<CreateUserResponse>();

            try
            {
                var response = await _userRepository.CreateUser(request.Request, cancellationToken);

                result.Value = response;
                result.Count = 1;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error creating user: {ex.Message}");
                return result;
            }
        }
    }
}
