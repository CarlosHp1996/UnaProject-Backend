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
                var productsToDelete = new List<UnaProject.Domain.Entities.Product>();
                var errors = new List<string>();

                foreach (var id in request.Ids)
                {
                    var product = await _productRepository.GetById(id);

                    if (product is null)
                    {
                        errors.Add($"Product with ID {id} not found.");
                        continue;
                    }

                    if (product.OrderItems != null && product.OrderItems.Count > 0)
                    {
                        errors.Add($"The product '{product.Name}' cannot be deleted because there is already an order created for it!");
                        continue;
                    }

                    productsToDelete.Add(product);
                }

                if (productsToDelete.Any())
                {
                    await _productRepository.DeleteRangeAsync(productsToDelete);
                }

                if (errors.Any())
                {
                    result.Message = string.Join(" | ", errors);
                    result.HasSuccess = productsToDelete.Any(); // Parcial success if at least one was deleted
                    if (!productsToDelete.Any()) result.WithError(result.Message);
                }
                else
                {
                    result.Message = request.Ids.Count > 1 
                        ? $"{productsToDelete.Count} products deleted successfully!" 
                        : "Product deleted successfully!";
                    result.HasSuccess = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during bulk deletion: {ex.Message}", ex);
            }
        }
    }
}
