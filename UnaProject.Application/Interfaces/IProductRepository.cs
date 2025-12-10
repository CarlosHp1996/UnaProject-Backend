using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Products;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Interfaces
{
    public interface IProductRepository : IBaseRepository<Product>
    {
        Task<AsyncOutResult<IEnumerable<Product>, int>> Get(GetProductsRequestFilter filter);
        Task<FiltersDto> GetFiltersData(CancellationToken cancellationToken);
        Task<UpdateProductResponse> UpdateProduct(Product product, UpdateProductRequest request, CancellationToken cancellationToken);
        Task<Product> GetProductById(Guid id);
        Task<CreateProductResponse> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken);
    }
}
