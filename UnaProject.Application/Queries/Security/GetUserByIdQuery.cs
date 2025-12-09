using MediatR;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Security
{
    public class GetUserByIdQuery : IRequest<Result<GetUserByIdResponse>>
    {
        public Guid Id { get; }

        public GetUserByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
