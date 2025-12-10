using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Products.Handlers
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<CreateProductResponse>>
    {
        private readonly IProductRepository _productRepository;

        public CreateProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<CreateProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<CreateProductResponse>();

            try
            {
                var response = await _productRepository.CreateProduct(request.Request, cancellationToken);

                result.Value = response;
                result.Count = 1;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error creating product: {ex.Message}");
                return result;
            }
        }
    }
}
