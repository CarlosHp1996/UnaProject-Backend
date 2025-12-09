using MediatR;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Security
{
    public class GetAllUsersQuery : IRequest<Result<GetAllUsersResponse>>
    {
        public GetUsersRequestFilter Filter { get; }

        public GetAllUsersQuery(GetUsersRequestFilter filter)
        {
            Filter = filter;
        }
    }
}
