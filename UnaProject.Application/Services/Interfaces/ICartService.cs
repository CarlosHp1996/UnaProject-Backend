using UnaProject.Application.Models.Requests.Orders;
using UnaProject.Domain.Entities;

namespace UnaProject.Application.Services.Interfaces
{
    public interface ICartService
    {
        Task<Cart> GetCartAsync(string userId);
        Task<Cart> AddItemAsync(string userId, CartItem item);
        Task<Cart> UpdateItemQuantityAsync(string userId, Guid productId, int quantity);
        Task<Cart> RemoveItemAsync(string userId, Guid productId);
        Task<bool> ClearCartAsync(string userId);
        Task<int> GetCartItemCountAsync(string userId);

        Task<Guid> ConvertCartToOrderAsync(string userId, AddressRequest shippingAddress);
        Task<Cart> MigrateSessionCartToUserAsync(string sessionCartId, string userId);
    }
}
