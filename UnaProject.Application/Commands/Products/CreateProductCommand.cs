using MediatR;
using UnaProject.Application.Models.Requests.Products;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Products
{
    public class CreateProductCommand : IRequest<Result<CreateProductResponse>>
    {
        public CreateProductRequest Request { get; }

        public CreateProductCommand(CreateProductRequest request)
        {
            Request = request;
        }
    }
}
