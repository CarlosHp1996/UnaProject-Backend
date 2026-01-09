using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Interfaces
{
    public interface IUserRepository : IBaseRepository<ApplicationUser>
    {
        Task<CreateUserResponse> CreateUser(CreateUserRequest request, CancellationToken cancellationToken);
        Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteUser(Guid id, CancellationToken cancellationToken);
        Task<ApplicationUser> GetUserById(Guid id, CancellationToken cancellationToken);
        Task<AsyncOutResult<IEnumerable<ApplicationUser>, int>> GetUsers(GetUsersRequestFilter filter, CancellationToken cancellationToken);

        // Social Login Methods
        Task<ApplicationUser?> GetBySocialIdAsync(string provider, string providerId);
        Task<bool> HasSocialAccountAsync(string provider, string providerId);
    }
}
