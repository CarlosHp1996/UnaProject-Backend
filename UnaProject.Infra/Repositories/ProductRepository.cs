using Microsoft.EntityFrameworkCore;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Products;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;
using UnaProject.Domain.Enums;
using UnaProject.Infra.Data;

namespace UnaProject.Infra.Repositories
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IUrlHelperService _urlHelper;

        public ProductRepository(AppDbContext dbContext, IFileStorageService fileStorage, IUrlHelperService urlHelper) : base(dbContext)
        {
            _context = dbContext;
            _fileStorage = fileStorage;
            _urlHelper = urlHelper;
        }

        public async Task<AsyncOutResult<IEnumerable<Product>, int>> Get(GetProductsRequestFilter filter)
        {
            int page = filter.Page ?? 1;
            int pageSize = filter.PageSize ?? 10;
            int offset = (page - 1) * pageSize;
            string sortBy = filter.SortBy ?? "Name";
            
            // Normalize sortBy to match the property casing in the Product class
            var property = typeof(Product).GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, sortBy, StringComparison.OrdinalIgnoreCase));
            
            if (property != null)
                sortBy = property.Name;

            bool ascending = filter.SortDirection?.ToLower() != "desc";

            var query = _context.Products
                .Include(x => x.Attributes)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{filter.Name}%"));
            else if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{filter.Search}%"));

            if (filter.CategoryIds != null && filter.CategoryIds.Any())
            {
                var categoryValues = filter.CategoryIds.Select(f => f).ToList();
                query = query.Where(p => p.Attributes.Any(a => a.Category != null && categoryValues.Contains(a.Category.Value)));
            }
            else if (!string.IsNullOrEmpty(filter.Category) && filter.Category != "all")
            {
                if (Enum.TryParse<EnumCategory>(filter.Category, true, out var categoryEnum))
                {
                    query = query.Where(p => p.Attributes.Any(a => a.Category == categoryEnum));
                }
            }

            if (filter.QuantityRanges != null && filter.QuantityRanges.Any())
            {
                var maxRange = filter.QuantityRanges.Max();
                query = query.Where(p => p.StockQuantity <= maxRange);
            }

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.StockQuantity.HasValue && filter.StockQuantity > 0)
                query = query.Where(p => p.StockQuantity >= filter.StockQuantity.Value);

            if (filter.StockQuantity.HasValue && filter.StockQuantity < 1)
                query = query.Where(p => p.StockQuantity < 1);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            // Dynamic sorting
            if (DataHelpers.CheckExistingProperty<Product>(sortBy))
                query = query.OrderByDynamic(sortBy, ascending);
            else
                query = ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name);

            var totalCount = await query.CountAsync();
            var products = await query.Skip(offset).Take(pageSize).ToListAsync();

            // Convert paths to full URLs
            foreach (var product in products)
            {
                if (product.ImageUrls != null)
                {
                    for (int i = 0; i < product.ImageUrls.Count; i++)
                    {
                        product.ImageUrls[i] = _urlHelper.GenerateImageUrl(product.ImageUrls[i]);
                    }
                }
            }

            return new AsyncOutResult<IEnumerable<Product>, int>(products, totalCount);
        }

        public async Task<FiltersDto> GetFiltersData(CancellationToken cancellationToken)
        {            
            var quantityRanges = new List<FilterQuantityRangeDto>
            {
                new FilterQuantityRangeDto { MinQuantity = 0, MaxQuantity = 10, ProductCount = 0 },
                new FilterQuantityRangeDto { MinQuantity = 11, MaxQuantity = 50, ProductCount = 0 },
                new FilterQuantityRangeDto { MinQuantity = 51, MaxQuantity = 100, ProductCount = 0 },
                new FilterQuantityRangeDto { MinQuantity = 101, MaxQuantity = int.MaxValue, ProductCount = 0 }
            };

            foreach (var range in quantityRanges)
            {
                range.ProductCount = await _context.Products
                    .CountAsync(p => p.StockQuantity >= range.MinQuantity && p.StockQuantity <= range.MaxQuantity, cancellationToken);
            }

            var minPrice = await _context.Products.MinAsync(p => p.Price, cancellationToken);
            var maxPrice = await _context.Products.MaxAsync(p => p.Price, cancellationToken);

            return new FiltersDto
            {
                QuantityRanges = quantityRanges,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };
        }

        public async Task<UpdateProductResponse> UpdateProduct(Product product, UpdateProductRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var existingProduct = await _context.Products
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken) ?? throw new Exception($"Product with ID {product.Id} not found.");

                if (request.NewImages != null && request.NewImages.Any())
                {
                    foreach (var file in request.NewImages)
                    {
                        if (file.Length > 0)
                        {
                            var fileUrl = await _fileStorage.UploadFileAsync(
                                file,
                                "images",
                                $"{Guid.NewGuid()}_{file.FileName}");
                            existingProduct.ImageUrls.Add(fileUrl);
                        }
                    }
                }
                if (request.RemovedImageUrls != null && request.RemovedImageUrls.Any())
                {
                    foreach (var urlToRemove in request.RemovedImageUrls)
                    {
                        existingProduct.ImageUrls.RemoveAll(img => urlToRemove.Contains(img) || img.Contains(urlToRemove));
                    }
                }                

                if (request.Name != null)
                    existingProduct.Name = request.Name;
                if (request.Description != null)
                    existingProduct.Description = request.Description;
                if (request.Price is not null)
                    existingProduct.Price = (decimal)request.Price;
                if (request.StockQuantity is not null)
                    existingProduct.StockQuantity = (int)request.StockQuantity;

                if (request.IsActive is not null)
                    existingProduct.IsActive = (bool)request.IsActive;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                // Remove old attributes
                _context.ProductAttributes.RemoveRange(product.Attributes);

                // Add new attributes
                if (request.Attributes != null && request.Attributes.Any())
                {
                    foreach (var attr in request.Attributes)
                    {
                        var productAttribute = new ProductAttribute
                        {
                            ProductId = product.Id,
                            Category = attr.Category
                        };

                        product.Attributes.Add(productAttribute);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                var response = new UpdateProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageUrls = product.ImageUrls,
                    IsActive = product.IsActive,
                    UpdatedAt = product.UpdatedAt,
                    Attributes = product.Attributes
                        .Select(a => new ProductAttributeDto
                        {
                            Category = a.Category
                        }).ToList()
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating product: {ex.Message}", ex);
            }
        }

        public async Task<Product?> GetProductById(Guid id)
        {
            var product = await _context.Products
            .Where(x => x.Id == id)
            .Include(p => p.Attributes)
            .Include(p => p.OrderItems)
            .FirstOrDefaultAsync();

            if (product != null && product.ImageUrls != null)
            {
                for (int i = 0; i < product.ImageUrls.Count; i++)
                {
                    product.ImageUrls[i] = _urlHelper.GenerateImageUrl(product.ImageUrls[i]);
                }
            }

            return product;
        }

        public async Task<CreateProductResponse> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var imageUrls = new List<string>();
                if (request.Images != null && request.Images.Any())
                {
                    foreach (var file in request.Images)
                    {
                        if (file.Length > 0)
                        {
                            var fileUrl = await _fileStorage.UploadFileAsync(
                                file,
                                "images",
                                $"{Guid.NewGuid()}_{file.FileName}");
                            imageUrls.Add(fileUrl);
                        }
                    }
                }

                var now = DateTime.UtcNow;
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    StockQuantity = request.StockQuantity,
                    ImageUrls = imageUrls,
                    IsActive = request.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Products.Add(product);

                var productAttributes = new List<ProductAttribute>();
                if (request.Attributes != null)
                {
                    foreach (var attr in request.Attributes)
                    {
                        var productAttribute = new ProductAttribute
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Category = attr.Category
                        };

                        _context.ProductAttributes.Add(productAttribute);
                        productAttributes.Add(productAttribute);
                    }
                }

                // Salvar todas as alterações de uma vez
                await _context.SaveChangesAsync(cancellationToken);

                // Preparar a resposta
                var response = new CreateProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageUrls = product.ImageUrls,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    Attributes = productAttributes.Select(a => new ProductAttributeDto
                    {
                        Id = a.Id,                        
                        Category = a.Category
                    }).ToList(),
                    Message = "Product created successfully."
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating product: {ex.Message}", ex);
            }
        }
    }
}
