using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using UnaProject.Application.Commands.Orders;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Orders;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Application.Queries.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IMediator _mediator;
        public OrderController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [SwaggerOperation(
             Summary = "Create orders",
             Description = "All fields are required.")]
        [SwaggerResponse(200, "Success", typeof(Result<CreateOrderResponse>))]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        {
            var command = new CreateOrderCommand(request);
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        [SwaggerOperation(
         Summary = "Update Orders",
         Description = "Order 'Id' is required.")]
        [SwaggerResponse(200, "Success", typeof(Result<UpdateOrderResponse>))]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderRequest request)
        {
            var command = new UpdateOrderCommand(id, request);
            var result = await _mediator.Send(command);

            if (result.HasSuccess)
                return Ok(result);

            return BadRequest(result.Errors);
        }

        [SwaggerOperation(
            Summary = "Update Order Item",
            Description = "Update an order item quantity.")]
        [SwaggerResponse(200, "Success", typeof(Result<UpdateOrderItemResponse>))]
        [HttpPut("update/{orderId}/item/{itemId}")]
        public async Task<IActionResult> UpdateOrderItem(Guid orderId, Guid itemId, [FromBody] UpdateOrderItemRequest request)
        {
            // Check if the request belongs to the current user (except for administrators).
            if (!User.IsInRole("Admin"))
            {
                var orderQuery = new GetOrderByIdQuery(orderId);
                var orderResult = await _mediator.Send(orderQuery);

                if (orderResult.HasSuccess)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userId != null && orderResult.Value.Order.UserId != Guid.Parse(userId))
                        return Forbid();                    
                }
                else
                    return NotFound();                
            }

            var command = new UpdateOrderItemCommand(orderId, itemId, request);
            var result = await _mediator.Send(command);

            if (result.HasSuccess)
                return Ok(result);

            return BadRequest(result.Errors);
        }

        [SwaggerOperation(
             Summary = "List all Orders",
             Description = "List all Orders in a paginated manner.")]
        [SwaggerResponse(200, "Sucesso", typeof(Result<GetAllOrdersResponse>))]
        [HttpGet("get")]
        public async Task<IActionResult> GetAll([FromQuery] GetOrdersRequestFilter filter)
        {
            var query = new GetAllOrdersQuery(filter);
            var result = await _mediator.Send(query);

            if (result.HasSuccess)
                return Ok(result);

            return BadRequest(result.Errors);
        }

        [SwaggerOperation(
            Summary = "List all Orders according to id",
            Description = "The Order 'Id' is mandatory.")]
        [SwaggerResponse(200, "Success", typeof(Result<GetOrderByIdResponse>))]
        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var command = new GetOrderByIdQuery(id);
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        [SwaggerOperation(
            Summary = "Delete Order",
            Description = "Order 'Id' is required.")]
        [SwaggerResponse(200, "Success", typeof(Result))]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteOrderCommand(id);
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        [SwaggerOperation(
            Summary = "Delete Order Item",
            Description = "Delete an order item by ID.")]
        [SwaggerResponse(200, "Success", typeof(Result))]
        [HttpDelete("delete/{orderId}/item/{itemId}")]
        public async Task<IActionResult> DeleteOrderItem(Guid orderId, Guid itemId)
        {
            var command = new DeleteOrderItemCommand(orderId, itemId);
            var result = await _mediator.Send(command);

            if (result.HasSuccess)
                return Ok(result);

            return BadRequest(result.Errors);
        }
    }
}
