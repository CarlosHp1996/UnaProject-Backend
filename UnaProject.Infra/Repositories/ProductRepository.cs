using Microsoft.EntityFrameworkCore;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Products;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;
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
            bool ascending = filter.SortDirection?.ToLower() != "desc";

            var query = _context.Products
                .Include(x => x.Attributes)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{filter.Name}%"));

            if (filter.CategoryIds != null && filter.CategoryIds.Any())
            {
                var categoryValues = filter.CategoryIds.Select(f => f).ToList();
                query = query.Where(p => p.Attributes.Any(a => a.Category != null && categoryValues.Contains(a.Category.Value)));
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
                product.ImageUrl = _urlHelper.GenerateImageUrl(product.ImageUrl);
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
                    .Include(p => p.Inventory)
                    .FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken) ?? throw new Exception($"Product with ID {product.Id} not found.");

                string? imageUrl = null;
                if (request.ImageUrl != null && request.ImageUrl.Length > 0)
                {
                    imageUrl = await _fileStorage.UploadFileAsync(
                        request.ImageUrl,
                        "videos/images",
                        $"{Guid.NewGuid()}_{request.ImageUrl.FileName}");
                }                

                if (request.Name != null)
                    existingProduct.Name = request.Name;
                if (request.Description != null)
                    existingProduct.Description = request.Description;
                if (request.Price is not null)
                    existingProduct.Price = (decimal)request.Price;
                if (request.StockQuantity is not null)
                    existingProduct.StockQuantity = (int)request.StockQuantity;
                if (imageUrl != null)
                    existingProduct.ImageUrl = imageUrl;
                if (request.IsActive is not null)
                    existingProduct.IsActive = (bool)request.IsActive;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                if (request.InventoryId is not null)
                {
                    existingProduct.Inventory.Quantity = (int)request.StockQuantity;
                    existingProduct.Inventory.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    product.Inventory = new Inventory
                    {
                        ProductId = product.Id,
                        Quantity = (int)request.StockQuantity,
                        LastUpdated = DateTime.UtcNow
                    };
                }

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
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    UpdatedAt = product.UpdatedAt,
                    InventoryId = product.Inventory.Id,
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

        public async Task<Product> GetProductById(Guid id)
        {
            var product = await _context.Products
            .Where(x => x.Id == id)
            .Include(p => p.Attributes)
            .Include(p => p.Inventory)
            .Include(p => p.OrderItems)
            .FirstOrDefaultAsync();

            if (product != null)
                product.ImageUrl = _urlHelper.GenerateImageUrl(product.ImageUrl);            

            return product;
        }

        public async Task<CreateProductResponse> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
        {
            try
            {
                string? imageUrl = null;
                if (request.ImageUrl != null && request.ImageUrl.Length > 0)
                {
                    imageUrl = await _fileStorage.UploadFileAsync(
                        request.ImageUrl,
                        "videos/images",
                        $"{Guid.NewGuid()}_{request.ImageUrl.FileName}");
                }

                var now = DateTime.UtcNow;
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    StockQuantity = request.StockQuantity,
                    ImageUrl = imageUrl,
                    IsActive = request.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Products.Add(product);

                var inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = request.StockQuantity,
                    LastUpdated = now
                };
                _context.Inventories.Add(inventory);

                var productAttributes = new List<ProductAttribute>();
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
                    ImageUrl = product.ImageUrl,
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
