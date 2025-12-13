using Microsoft.EntityFrameworkCore;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Orders;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;
using UnaProject.Infra.Data;

namespace UnaProject.Infra.Repositories
{
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly IUrlHelperService _urlHelper;
        private readonly IEmailService _emailService;

        public OrderRepository(AppDbContext context, IUrlHelperService urlHelperService, IEmailService emailService) : base(context)
        {
            _context = context;
            _urlHelper = urlHelperService;
            _emailService = emailService;

        }

        public async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null)
                throw new Exception("User not found");

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == request.UserId, cancellationToken);

            if (address == null)
                throw new Exception("Address not found or does not belong to the user");

            if (request.Items == null || !request.Items.Any())
                throw new Exception("An order must have at least one item");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                AddressId = request.AddressId,
                Status = "Pending",
                PaymentStatus = "Pending",
                OrderDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TotalAmount = 0,
                PaymentMethod = request.PaymentMethod,
                IsActive = true
            };

            var orderNumber = await _context.Orders
                .Where(o => o.UserId == request.UserId && o.OrderNumber > 0)
                .ToListAsync();

            if (orderNumber.Count > 0)
                order.OrderNumber = orderNumber.Max(o => o.OrderNumber) + 1;
            else
                order.OrderNumber = 1; // User's first request

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            // Process the order items.
            foreach (var item in request.Items)
            {
                var product = await _context.Products
                    .Where(p => p.Id == item.ProductId && p.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                    throw new Exception($"Product with ID {item.ProductId} not found or inactive");

                if (product.StockQuantity < item.Quantity)
                    throw new Exception($"Not enough stock for product {product.Name}. Available: {product.StockQuantity}");

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };

                orderItems.Add(orderItem);
                totalAmount += orderItem.Quantity * orderItem.UnitPrice;

                // Update product inventory
                product.StockQuantity -= item.Quantity;
            }

            order.TotalAmount = totalAmount;

            // Save the order and items to the database using ExecutionStrategy.
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await _context.Orders.AddAsync(order, cancellationToken);
                    await _context.OrderItems.AddRangeAsync(orderItems, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    // Email sent only if everything goes well.
                    await _emailService.SendEmailConfirmationOrderAsync(order.User.Email);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return new CreateOrderResponse
            {
                OrderId = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                OrderDate = order.OrderDate,
                OrderNumber = order.OrderNumber,
                IsActive = true,
            };
        }

        public async Task<(IQueryable<Order> Result, int TotalCount)> Get(GetOrdersRequestFilter filter)
        {
            string sortBy = filter.SortBy ?? "OrderDate";
            bool ascending = filter.SortDirection?.ToLower() != "desc";

            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .AsQueryable();

            // Apply filters
            if (filter.UserId.HasValue)
                query = query.Where(o => o.UserId == filter.UserId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(o => o.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                query = query.Where(o => o.PaymentStatus == filter.PaymentStatus);

            if (filter.StartDate.HasValue)
                query = query.Where(o => o.OrderDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(o => o.OrderDate <= filter.EndDate.Value);

            if (filter.MinAmount.HasValue)
                query = query.Where(o => o.TotalAmount >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                query = query.Where(o => o.TotalAmount <= filter.MaxAmount.Value);

            if (filter.OrderNumber.HasValue)
                query = query.Where(o => o.OrderNumber == filter.OrderNumber);

            if (!string.IsNullOrEmpty(filter.PaymentMethod))
                query = query.Where(o => o.PaymentMethod == filter.PaymentMethod);

            // Dynamic sorting
            if (DataHelpers.CheckExistingProperty<Order>(sortBy))
                query = query.OrderByDynamic(sortBy, ascending);
            else
                query = ascending ? query.OrderBy(p => p.OrderDate) : query.OrderByDescending(p => p.OrderDate);

            // Total record count for pagination.
            int totalCount = await query.CountAsync();

            // Ordering
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy.ToLower())
                {
                    case "orderdate":
                        query = filter.Ascending
                            ? query.OrderBy(o => o.OrderDate)
                            : query.OrderByDescending(o => o.OrderDate);
                        break;
                    case "totalamount":
                        query = filter.Ascending
                            ? query.OrderBy(o => o.TotalAmount)
                            : query.OrderByDescending(o => o.TotalAmount);
                        break;
                    case "status":
                        query = filter.Ascending
                            ? query.OrderBy(o => o.Status)
                            : query.OrderByDescending(o => o.Status);
                        break;
                    case "paymentstatus":
                        query = filter.Ascending
                            ? query.OrderBy(o => o.PaymentStatus)
                            : query.OrderByDescending(o => o.PaymentStatus);
                        break;
                    default:
                        query = query.OrderByDescending(o => o.OrderDate);
                        break;
                }
            }
            //else
            //{
            //    query = query.OrderByDescending(o => o.OrderDate);
            //}

            // Pagination
            if (filter.Page.HasValue && filter.PageSize.HasValue)
            {
                int page = filter.Page.Value;
                int pageSize = filter.PageSize.Value;
                query = query.Skip((page - 1) * pageSize).Take(pageSize);
            }

            // Convert paths to full URLs
            foreach (var order in query)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.ImageUrl = _urlHelper.GenerateImageUrl(item.Product.ImageUrl);
                    }
                }
            }

            return (query, totalCount);
        }

        public async Task<Order?> GetById(Guid id, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            // Convert paths to full URLs
            if (order != null)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.ImageUrl = _urlHelper.GenerateImageUrl(item.Product.ImageUrl);
                    }
                }
            }

            return order;
        }

        public async Task<UpdateOrderResponse> UpdateOrder(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.FindAsync([id], cancellationToken);
            if (order == null)
                throw new Exception("Order not found");

            // Update the received fields.
            if (!string.IsNullOrEmpty(request.Status))
                order.Status = request.Status;

            if (!string.IsNullOrEmpty(request.PaymentStatus))
                order.PaymentStatus = request.PaymentStatus;

            if (request.IsActive.HasValue)
                order.IsActive = request.IsActive.Value;

            if (order.IsActive == false)
                order.Status = "Finish";

            if (order.IsActive == true)
                order.Status = "Processing";

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new UpdateOrderResponse
            {
                OrderId = order.Id,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                UpdatedAt = order.UpdatedAt,
                IsActive = order.IsActive,
                PaymentMethod = order.PaymentMethod
            };
        }

        public async Task<UpdateOrderItemResponse> UpdateOrderItem(Guid orderId, Guid orderItemId, UpdateOrderItemRequest request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.FindAsync([orderId], cancellationToken);
            if (order == null)
                throw new Exception("Order not found");

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.Id == orderItemId && oi.OrderId == orderId, cancellationToken);

            if (orderItem == null)
                throw new Exception("Order item not found");

            // Check the quantity difference to adjust the inventory.
            int quantityDifference = request.Quantity - orderItem.Quantity;

            // If we are increasing the quantity, check available stock.
            if (quantityDifference > 0)
            {
                var product = await _context.Products.FindAsync([orderItem.ProductId], cancellationToken);
                if (product.StockQuantity < quantityDifference)
                    throw new Exception($"Not enough stock for product {product.Name}. Available: {product.StockQuantity}");

                // Update stock
                product.StockQuantity -= quantityDifference;
            }
            else if (quantityDifference < 0)
            {
                // We are reducing the quantity, returning it to stock.
                var product = await _context.Products.FindAsync([orderItem.ProductId], cancellationToken);
                product.StockQuantity += Math.Abs(quantityDifference);
            }

            // Update item quantity
            orderItem.Quantity = request.Quantity;

            // Recalculate the total order value.
            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync(cancellationToken);

            decimal totalAmount = orderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
            order.TotalAmount = totalAmount;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new UpdateOrderItemResponse
            {
                OrderItemId = orderItem.Id,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.Quantity * orderItem.UnitPrice
            };
        }

        public async Task<bool> DeleteOrder(Guid id, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order == null)
                return false;

            // Restore product stock
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync([item.ProductId], cancellationToken);
                if (product != null)
                    product.StockQuantity += item.Quantity;                
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IEnumerable<Order>> GetPendingOrders(TimeSpan pendingTime)
        {
            var cutoff = DateTime.UtcNow - pendingTime;
            return await _context.Orders
                .Where(o => o.Status == "Pending" && o.OrderDate < cutoff)
                .ToListAsync();
        }

        public async Task<bool> DeleteOrderItem(Guid orderId, Guid orderItemId, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.FindAsync([orderId], cancellationToken);
            if (order == null)
                return false;

            var orderItem = await _context.OrderItems
                .FirstOrDefaultAsync(oi => oi.Id == orderItemId && oi.OrderId == orderId, cancellationToken);

            if (orderItem == null)
                return false;

            // Restore product stock
            var product = await _context.Products.FindAsync([orderItem.ProductId], cancellationToken);
            if (product != null)
                product.StockQuantity += orderItem.Quantity;            

            // Remove Order Item
            _context.OrderItems.Remove(orderItem);

            // Recalculate the total order value.
            order.TotalAmount -= orderItem.Quantity * orderItem.UnitPrice;
            order.UpdatedAt = DateTime.UtcNow;

            // Check if there are more items in the order.
            var remainingItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId && oi.Id != orderItemId)
                .CountAsync(cancellationToken);

            // If there are no more items, delete the order as well.
            if (remainingItems == 0)
                _context.Orders.Remove(order);            

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
