using MediatR;
using Microsoft.AspNetCore.Identity;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Helpers;
using UnaProject.Domain.Security;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class CreateLoginCommandHandler : IRequestHandler<CreateLoginCommand, Result<CreateLoginResponse>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AccessManager _accessManager;

        public CreateLoginCommandHandler(UserManager<ApplicationUser> userManager, AccessManager accessManager)
        {
            _userManager = userManager;
            _accessManager = accessManager;
        }

        public async Task<Result<CreateLoginResponse>> Handle(CreateLoginCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<CreateLoginResponse>();

            // Check if the user exists via email
            var user = await _userManager.FindByEmailAsync(request.Request.Email);
            if (user == null)
            {
                result.WithError("Invalid username or password.");
                return result;
            }

            // Check if the password is correct
            if (!await _userManager.CheckPasswordAsync(user, request.Request.Password))
            {
                result.WithError("Invalid username or password.");
                return result;
            }

            // Prepare the answer
            var response = new CreateLoginResponse
            {
                Id = user.Id,
                Name = user.UserName,
                Token = await _accessManager.GenerateToken(user), //Generate token
                Mensage = "Login successfully."
            };

            result.Value = response;
            result.Count = 1;
            return result;
        }
    }
}
