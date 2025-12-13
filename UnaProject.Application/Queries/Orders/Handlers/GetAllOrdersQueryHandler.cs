using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Orders.Handlers
{
    public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, Result<GetAllOrdersResponse>>
    {
        private readonly IOrderRepository _orderRepository;

        public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<GetAllOrdersResponse>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
        {
            var result = new Result<GetAllOrdersResponse>();

            try
            {
                var ordersResult = await _orderRepository.Get(request.Filter);
                var orders = ordersResult.Result.ToList();
                int totalCount = ordersResult.TotalCount;
                int pageSize = request.Filter.PageSize ?? 10;
                int pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

                var response = new GetAllOrdersResponse
                {
                    Orders = orders.Select(order => new OrderDto
                    {
                        Id = order.Id,
                        UserId = order.UserId,
                        UserName = order.User?.UserName,
                        TotalAmount = order.TotalAmount,
                        Status = order.Status,
                        PaymentStatus = order.PaymentStatus,
                        OrderNumber = order.OrderNumber,
                        OrderDate = order.OrderDate,
                        UpdatedAt = order.UpdatedAt,
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
                        }).ToList() ?? new List<PaymentDto>()
                    }).ToList(),

                    Pagination = new PaginationDto
                    {
                        CurrentPage = request.Filter.Page ?? 1,
                        PageSize = pageSize,
                        TotalItems = totalCount,
                        TotalPages = pageCount
                    },

                    Addresses = orders.SelectMany(order => order.User.Addresses)
                        .Select(address => new AddressDto
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
                };

                result.Count = totalCount;
                result.Value = response;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error retrieving orders: {ex.Message}");
                return result;
            }
        }
    }
}
