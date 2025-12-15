using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UnaProject.Application.Commands.Trackings;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Trackings;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Application.Queries.Trackings;
using UnaProject.Domain.Helpers;

namespace UnaProject.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TrackingController : ControllerBase
    {
        private readonly IMediator _mediator;
        public TrackingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [SwaggerOperation(
             Summary = "Create tracking event",
             Description = "Adds a new event to an order's tracking history")]
        [SwaggerResponse(200, "Success", typeof(Result<TrackingResponse>))]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTrackingRequest request)
        {
            var command = new CreateTrackingCommand(request);
            var response = await _mediator.Send(command);

            if (!response.HasSuccess)
                return BadRequest(response);

            return Ok(response);
        }

        [SwaggerOperation(
         Summary = "Update tracking event",
         Description = "The 'Id' of the tracking event is required.")]
        [SwaggerResponse(200, "Success", typeof(Result<TrackingResponse>))]
        [HttpPut("{id}")]
        //[Authorize(Roles = "Admin,Operator")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTrackingRequest request)
        {
            var command = new UpdateTrackingCommand(id, request);
            var response = await _mediator.Send(command);

            if (!response.HasSuccess)
                return BadRequest(response);

            return Ok(response);
        }

        [SwaggerOperation(
             Summary = "List all tracking events",
             Description = "Lists all tracking events in a paginated manner.")]
        [SwaggerResponse(200, "Success", typeof(Result<TrackingResponse>))]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetTrackingsRequestFilter filter)
        {
            var query = new GetTrackingsQuery(filter);
            var response = await _mediator.Send(query);

            return Ok(response);
        }

        [SwaggerOperation(
            Summary = "Get tracking event by ID",
            Description = "The 'Id' of the tracking event is required.")]
        [SwaggerResponse(200, "Success", typeof(Result<TrackingResponse>))]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetTrackingByIdQuery(id);
            var response = await _mediator.Send(query);

            if (!response.HasSuccess)
                return NotFound(response);

            return Ok(response);
        }

        [SwaggerOperation(
            Summary = "Delete tracking event",
            Description = "The 'Id' of the tracking event is required.")]
        [SwaggerResponse(200, "Success", typeof(Result<TrackingResponse>))]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteTrackingCommand(id);
            var result = await _mediator.Send(command);

            if (result.HasSuccess)
                return Ok(result);

            return BadRequest(result.Errors);
        }
    }
}
