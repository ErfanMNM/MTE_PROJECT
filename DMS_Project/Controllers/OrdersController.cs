using System.Security.Claims;
using DMS_Project.Audit;
using DMS_Project.Auth;
using DMS_Project.Communications.Orders;
using DMS_Project.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS_Project.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    [ApiGroup("orders")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Operator}")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderQueueService _queueService;
        private readonly IAuditService _audit;

        public OrdersController(OrderQueueService queueService, IAuditService audit)
        {
            _queueService = queueService;
            _audit = audit;
        }

        /// <summary>
        /// Tạo/cập nhật Order. Request được đẩy vào Channel queue và xử lý tuần tự bởi background consumer.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrUpdateOrder([FromBody] OrderRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest(new OrderResponse
                {
                    Success = false,
                    Message = "Request body là bắt buộc",
                    At = DateTime.Now
                });

            // Capture actor info tại HTTP entry để queue worker giữ được actor khi xử lý nền
            AuditExecutionContext.Current!.ActorUsername = User.Identity?.Name ?? "anonymous";
            AuditExecutionContext.Current.ActorRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Anonymous";
            var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(sub, out var actorIdParsed)) AuditExecutionContext.Current.ActorId = actorIdParsed;
            AuditExecutionContext.Current.ActorSource = AuditActorSources.Queue;

            try
            {
                _audit.RecordSuccessAsync(
                    action: "Order.Enqueued",
                    entityType: AuditEntityTypes.Order,
                    entityId: request.OrderNo,
                    before: null,
                    after: new { request.OrderNo, request.GTIN, request.OrderQty, codeCount = request.UniqueCodes?.Count ?? 0 },
                    changedFieldsJson: null,
                    metadata: new { receivedAt = DateTime.UtcNow.ToString("o") }).GetAwaiter().GetResult();

                var response = await _queueService.EnqueueAsync(request, cancellationToken);
                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest, new OrderResponse
                {
                    Success = false,
                    Message = "Client đóng kết nối trước khi xử lý xong",
                    OrderNo = request.OrderNo,
                    At = DateTime.Now
                });
            }
        }
    }
}
