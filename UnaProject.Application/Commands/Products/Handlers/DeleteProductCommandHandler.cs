using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Products.Handlers
{
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
    {
        private readonly IProductRepository _productRepository;

        public DeleteProductCommandHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = new Result();
                var product = await _productRepository.GetById(request.Id);

                if (product is null)
                {
                    result.WithNotFound("Product not found!");
                    return result;
                }

                if (product.OrderItems.Count > 0)
                {
                    result.WithNotFound("The product cannot be deleted because there is already an order created for it!");
                    return result;
                }

                await _productRepository.DeleteAsync(product);

                result.Message = "Product deleted successfully!";
                result.HasSuccess = true;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
