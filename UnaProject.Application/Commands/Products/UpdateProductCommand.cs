using MediatR;
using UnaProject.Application.Models.Requests.Products;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Products
{
    public class UpdateProductCommand : IRequest<Result<UpdateProductResponse>>
    {
        public Guid Id;
        public UpdateProductRequest Request { get; }

        public UpdateProductCommand(Guid id, UpdateProductRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
