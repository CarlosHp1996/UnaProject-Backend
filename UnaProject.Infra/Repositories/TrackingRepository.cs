using Microsoft.EntityFrameworkCore;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Requests.Trackings;
using UnaProject.Application.Models.Responses.Trackings;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;
using UnaProject.Infra.Data;

namespace UnaProject.Infra.Repositories
{
    public class TrackingRepository : BaseRepository<Tracking>, ITrackingRepository
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public TrackingRepository(AppDbContext dbContext, IEmailService emailService) : base(dbContext)
        {
            _context = dbContext;
            _emailService = emailService;
        }

        public async Task<TrackingResponse> CreateTrackingEventAsync(CreateTrackingRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Where(o => o.Id == request.OrderId)
                .FirstOrDefaultAsync();

                if (order == null)
                    throw new KeyNotFoundException("Order not found.");

                var trackingEvent = new Tracking
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    TrackingNumber = request.TrackingNumber,
                    Status = request.Status,
                    Description = request.Description,
                    Location = request.Location,
                    EventDate = request.EventDate,
                    CreatedAt = DateTime.UtcNow
                };

                var email = _emailService.SendEmailConfirmationTrackingAsync(order.User.Email);

                if (email.Exception != null)
                    throw new KeyNotFoundException("Error sending tracking email.");

                _context.Trackings.Add(trackingEvent);
                await _context.SaveChangesAsync();

                var response = new TrackingResponse
                {
                    Id = trackingEvent.Id,
                    OrderId = order.Id,
                    Status = trackingEvent.Status,
                    Description = trackingEvent.Description,
                    Location = trackingEvent.Location,
                    EventDate = trackingEvent.EventDate,
                    CreatedAt = trackingEvent.CreatedAt,
                    TrackingNumber = trackingEvent.TrackingNumber
                };

                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<TrackingResponse> UpdateTrackingAsync(Tracking tracking, UpdateTrackingRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var existingTracking = await _context.Trackings
                    .FirstOrDefaultAsync(t => t.Id == tracking.Id, cancellationToken);

                if (existingTracking == null)
                    throw new KeyNotFoundException("Tracking event not found.");

                tracking.Status = request.Status;
                tracking.Description = request.Description;
                tracking.Location = request.Location;
                tracking.EventDate = request.EventDate;
                tracking.TrackingNumber = request.TrackingNumber;

                await _context.SaveChangesAsync(cancellationToken);

                var response = new TrackingResponse
                {
                    Id = tracking.Id,
                    OrderId = tracking.OrderId,
                    Status = tracking.Status,
                    Description = tracking.Description,
                    Location = tracking.Location,
                    EventDate = tracking.EventDate,
                    CreatedAt = tracking.CreatedAt,
                    TrackingNumber = tracking.TrackingNumber
                };

                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<(List<TrackingResponse> Trackings, int TotalCount)> GetTrackingsFilteredAsync(
            int? page,
            int? pageSize,
            string sortingProp,
            bool ascending,
            string status = null,
            Guid? orderId = null,
            string trackingNumber = null,
            string description = null,
            string location = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            // Calculate pagination
            var skip = (page - 1) * pageSize;
            var take = pageSize;

            // Create an initial query
            var query = _context.Trackings.AsQueryable();

            // Apply filters if necessary
            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status.Contains(status, StringComparison.OrdinalIgnoreCase));            

            if (orderId.HasValue)
                query = query.Where(t => t.OrderId == orderId.Value);            

            if (startDate.HasValue)
                query = query.Where(t => t.EventDate >= startDate.Value);            

            if (endDate.HasValue)
                query = query.Where(t => t.EventDate <= endDate.Value);

            // Calculate the total before applying pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply ordering
            if (!string.IsNullOrEmpty(sortingProp))
            {
                // Implement dynamic sorting based on the property
                switch (sortingProp.ToLowerInvariant())
                {
                    case "eventdate":
                        query = ascending
                            ? query.OrderBy(t => t.EventDate)
                            : query.OrderByDescending(t => t.EventDate);
                        break;
                    case "status":
                        query = ascending
                            ? query.OrderBy(t => t.Status)
                            : query.OrderByDescending(t => t.Status);
                        break;
                    case "location":
                        query = ascending
                            ? query.OrderBy(t => t.Location)
                            : query.OrderByDescending(t => t.Location);
                        break;
                    default:
                        // Default ordering
                        query = query.OrderByDescending(t => t.EventDate);
                        break;
                }
            }
            else
            {
                // Default ordering
                query = query.OrderByDescending(t => t.EventDate);
            }

            // Apply pagination
            var trackings = await query
                .Skip((int)skip)
                .Take((int)take)
                .ToListAsync(cancellationToken);

            var trackingResponses = trackings.Select(t => new TrackingResponse
            {
                Id = t.Id,
                OrderId = t.OrderId,
                Status = t.Status,
                Description = t.Description,
                Location = t.Location,
                TrackingNumber = t.TrackingNumber,
                EventDate = t.EventDate,
                CreatedAt = t.CreatedAt
            }).ToList();

            return (trackingResponses, totalCount);
        }
    }
}
