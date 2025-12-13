using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Orders.Handlers
{
    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<GetOrderByIdResponse>>
    {
        private readonly IOrderRepository _orderRepository;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetOrderByIdQueryHandler(IOrderRepository orderRepository, IHttpContextAccessor httpContextAccessor)
        {
            _orderRepository = orderRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<GetOrderByIdResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var result = new Result<GetOrderByIdResponse>();

            try
            {
                var order = await _orderRepository.GetById(request.OrderId, cancellationToken);

                if (order == null)
                {
                    result.WithError("Order not found");
                    return result;
                }

                var user = _httpContextAccessor.HttpContext.User;
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = user.IsInRole("Admin");

                if (!isAdmin && order.UserId.ToString() != userId)
                {
                    result.WithError("You do not have permission to view this order.");
                    return result;
                }

                var response = new GetOrderByIdResponse
                {
                    Order = new OrderDto
                    {
                        Id = order.Id,
                        UserId = order.UserId,
                        UserName = order.User?.UserName,
                        TotalAmount = order.TotalAmount,
                        Status = order.Status,
                        PaymentStatus = order.PaymentStatus,
                        OrderDate = order.OrderDate,
                        UpdatedAt = order.UpdatedAt,
                        OrderNumber = order.OrderNumber,
                        IsActive = order.IsActive,
                        PaymentMethod = order.PaymentMethod,
                        Items = order.OrderItems?.Select(item => new OrderItemDto
                        {
                            Id = item.Id,
                            OrderId = item.OrderId,
                            ProductId = item.ProductId,
                            ProductName = item.Product?.Name,
                            ProductImageUrl = item.Product?.ImageUrl,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        }).ToList() ?? new List<OrderItemDto>(),
                        Payments = order.Payments?.Select(payment => new PaymentDto
                        {
                            Id = payment.Id,
                            OrderId = payment.OrderId,
                            PaymentMethod = payment.PaymentMethod,
                            TransactionId = payment.TransactionId,
                            Amount = payment.Amount,
                            Status = payment.Status,
                            PaymentDate = payment.PaymentDate
                        }).ToList() ?? new List<PaymentDto>(),
                        Addresses = order.User.Addresses?.Select(address => new AddressDto
                        {
                            Id = address.Id,
                            Street = address.Street,
                            CompletName = address.CompletName,
                            City = address.City,
                            State = address.State,
                            ZipCode = address.ZipCode,
                            Neighborhood = address.Neighborhood,
                            Number = address.Number,
                            Complement = address.Complement,
                            MainAddress = address.MainAddress,
                        }).ToList()
                    }
                };

                result.Value = response;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error retrieving order: {ex.Message}");
                return result;
            }
        }
    }
}
