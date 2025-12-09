using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<DeleteUserResponse>>
    {
        private readonly IUserRepository _userRepository;

        public DeleteUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<DeleteUserResponse>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
        {
            var result = new Result<DeleteUserResponse>();

            // Find the user by ID
            var user = await _userRepository.GetById(command.Id);
            if (user == null)
            {
                result.WithError("User not found.");
                return result;
            }

            // Store user ID for response
            var userId = user.Id;

            // Delete the user
            var deleteResult = await _userRepository.DeleteUser(command.Id, cancellationToken);
            if (!deleteResult)
            {
                result.WithError("Error deleting user");
                return result;
            }

            // Prepare response
            var response = new DeleteUserResponse
            {
                Id = userId,
                Message = "User deleted successfully"
            };

            result.Count = 1;
            result.Value = response;
            return result;
        }
    }
}
