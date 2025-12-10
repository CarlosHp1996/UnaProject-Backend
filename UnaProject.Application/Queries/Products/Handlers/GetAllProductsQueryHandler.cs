using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Products.Handlers
{
    public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, Result<GetAllProductsResponse>>
    {
        private readonly IProductRepository _repository;

        public GetAllProductsQueryHandler(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<GetAllProductsResponse>> Handle(GetAllProductsQuery query, CancellationToken cancellationToken)
        {
            var result = new Result<GetAllProductsResponse>();

            try
            {
                var filter = query.Filter ?? new GetProductsRequestFilter();
                var productsResult = await _repository.Get(filter);
                var products = productsResult.Result(out int totalCount).ToList();
                int pageSize = filter.PageSize ?? 10;
                int pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

                var filtersData = await _repository.GetFiltersData(cancellationToken);

                var response = new GetAllProductsResponse
                {
                    Products = products.Select(static p => new ProductListItemDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        Attributes = p.Attributes?
                            .Select(a => new ProductAttributeDto
                            {
                                Category = a.Category
                            }).ToList() ?? new List<ProductAttributeDto>()
                    }).ToList(),

                    Pagination = new PaginationDto
                    {
                        CurrentPage = query.Filter.Page ?? 1,
                        PageSize = pageSize,
                        TotalItems = totalCount,
                        TotalPages = pageCount
                    },

                    Filters = filtersData
                };

                result.Value = response;
                result.Count = totalCount;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error when searching for products: {ex.Message}");
                return result;
            }
        }

    }
}
