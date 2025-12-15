using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Requests.Orders;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;

namespace UnaProject.Application.Services
{
    public class CartService : ICartService
    {
        private readonly IDistributedCache _cache;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<CartService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromDays(7); // Shopping cart expires in 7 days.

        public CartService(
            IDistributedCache cache,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            ILogger<CartService> logger)
        {
            _cache = cache;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        private string GetCartKey(string userId) => $"cart:{userId}";

        public async Task<Cart> GetCartAsync(string userId)
        {
            var cartKey = GetCartKey(userId);
            var cartJson = await _cache.GetStringAsync(cartKey);

            if (string.IsNullOrEmpty(cartJson))
            {
                var newCart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"Created new cart for user {userId}");
                return newCart;
            }

            var cart = JsonSerializer.Deserialize<Cart>(cartJson);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
            }

            _logger.LogInformation($"Loaded cart for user {userId} - Items: {cart.Items.Count}, TotalItems: {cart.TotalItems}");
            return cart;
        }

        public async Task<Cart> AddItemAsync(string userId, CartItem newItem)
        {
            var cart = await GetCartAsync(userId);

            var product = await _productRepository.GetProductById(newItem.ProductId);
            if (product == null || !product.IsActive)
                throw new Exception("Product not found or inactive");

            if (product.StockQuantity < newItem.Quantity)
                throw new Exception($"Insufficient stock. Available: {product.StockQuantity}");
            var existingItem = cart.Items.FirstOrDefault(i =>
                i.ProductId == newItem.ProductId &&
                i.Size == newItem.Size);

            if (existingItem != null)
            {
                //Check if the new quantity does not exceed the stock
                var newQuantity = existingItem.Quantity + newItem.Quantity;
                if (product.StockQuantity < newQuantity)
                    throw new Exception($"Insufficient stock. Available: { product.StockQuantity}");

                existingItem.Quantity = newQuantity;
            }
            else
            {
                newItem.ProductName = product.Name;
                newItem.ProductImage = product.ImageUrl;
                newItem.UnitPrice = product.Price;
                newItem.AddedAt = DateTime.UtcNow;

                cart.Items.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await SaveCartAsync(cart);
            return cart;
        }

        public async Task<Cart> UpdateItemQuantityAsync(string userId, Guid productId, int quantity)
        {
            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item == null)
                throw new Exception("Item not found in cart.");

            if (quantity <= 0)
                cart.Items.Remove(item);
            
            else
            {
                var product = await _productRepository.GetProductById(productId);
                if (product != null && product.StockQuantity < quantity)
                    throw new Exception($"Insufficient stock. Available: {product.StockQuantity}");

                item.Quantity = quantity;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await SaveCartAsync(cart);
            return cart;
        }

        public async Task<Cart> RemoveItemAsync(string userId, Guid productId)
        {
            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                cart.Items.Remove(item);
                cart.UpdatedAt = DateTime.UtcNow;
                await SaveCartAsync(cart);
            }

            return cart;
        }

        public async Task<bool> ClearCartAsync(string userId)
        {
            var cartKey = GetCartKey(userId);
            await _cache.RemoveAsync(cartKey);
            return true;
        }

        public async Task<int> GetCartItemCountAsync(string userId)
        {
            var cart = await GetCartAsync(userId);
            return cart.TotalItems;
        }

        public async Task<Cart> MigrateSessionCartToUserAsync(string sessionCartId, string userId)
        {
            try
            {
                _logger.LogInformation($"Migrating session cart {sessionCartId} for user {userId}");

                // Search session cart
                var sessionCart = await GetCartAsync(sessionCartId);

                // If there are no items in the session, return the user's cart
                if (!sessionCart.Items.Any())
                    return await GetCartAsync(userId);

                // Search user cart
                var userCart = await GetCartAsync(userId);

                // Migrate session items to the user
                foreach (var sessionItem in sessionCart.Items)
                {
                    // Check if the item already exists in the user's cart
                    var existingItem = userCart.Items.FirstOrDefault(i =>
                        i.ProductId == sessionItem.ProductId &&
                        i.Size == sessionItem.Size);

                    if (existingItem != null)
                    {
                        // Add quantities
                        existingItem.Quantity += sessionItem.Quantity;
                    }
                    else
                    {
                        // Add new item
                        userCart.Items.Add(sessionItem);
                    }
                }

                userCart.UserId = userId;
                userCart.UpdatedAt = DateTime.UtcNow;

                await SaveCartAsync(userCart);
                await ClearCartAsync(sessionCartId);

                _logger.LogInformation($"Migration complete. {sessionCart.Items.Count} migrated items.");

                return userCart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error migrating cart from {sessionCartId} to {userId}");
                throw;
            }
        }

        public async Task<Guid> ConvertCartToOrderAsync(string userId, AddressRequest request)
        {
            var cart = await GetCartAsync(userId);

            if (!cart.Items.Any())
                throw new Exception("Cart is empty");

            // Check if userId is a valid GUID (authenticated user)              
            if (!Guid.TryParse(userId, out var userGuid))
                throw new Exception("User must be authenticated to complete purchase");
            var address = await _orderRepository.GetById(userGuid, CancellationToken.None);

            // Convert to CreateOrderRequest  
            var createOrderRequest = new CreateOrderRequest
            {
                UserId = userGuid,
                AddressId = request.Id,
                PaymentMethod = request.PaymentMethod,
                Items = cart.Items.Select(i => new OrderItemRequest
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            var orderResponse = await _orderRepository.CreateOrder(createOrderRequest, CancellationToken.None);

            // Clear cart after creating order  
            await ClearCartAsync(userId);

            _logger.LogInformation($"Order {orderResponse.OrderId} created for user {userId}");

            return orderResponse.OrderId;
        }

        private async Task SaveCartAsync(Cart cart)
        {
            var cartKey = GetCartKey(cart.UserId);
            var cartJson = JsonSerializer.Serialize(cart);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };

            await _cache.SetStringAsync(cartKey, cartJson, options);
        }
    }
}
