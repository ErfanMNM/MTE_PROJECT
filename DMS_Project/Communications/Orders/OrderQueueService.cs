using System.Data;
using System.Threading.Channels;
using DMS_Project.Audit;
using DMS_Project.DataPool;
using DMS_Project.Production;
using PoolInfo = DMS_Project.DataPool.PoolInfo;

namespace DMS_Project.Communications.Orders
{
    public class OrderQueueService : BackgroundService
    {
        private readonly Channel<OrderQueueItem> _channel;
        private readonly DataPool.DataPool _dataPool;
        private readonly Production.Production _production;
        private readonly IAuditService _audit;
        private readonly ILogger<OrderQueueService> _logger;

        public OrderQueueService(
            DataPool.DataPool dataPool,
            Production.Production production,
            IAuditService audit,
            ILogger<OrderQueueService> logger)
        {
            _dataPool = dataPool;
            _production = production;
            _audit = audit;
            _logger = logger;

            _channel = Channel.CreateUnbounded<OrderQueueItem>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

        public async Task<OrderResponse> EnqueueAsync(OrderRequest request, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<OrderResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Capture context tại HTTP entry point để giữ actor khi xử lý nền
            var captured = AuditExecutionContext.Capture();
            captured.Source = AuditSources.Http; // entry từ HTTP
            var item = new OrderQueueItem(request, tcs, captured);

            await _channel.Writer.WriteAsync(item, cancellationToken);

            return await tcs.Task.WaitAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderQueueService started");

            try
            {
                await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
                {
                    OrderResponse response;
                    var queueCtx = item.AuditContext;
                    queueCtx.Source = AuditSources.QueueWorker;
                    queueCtx.ActorSource = AuditActorSources.Queue;

                    using (AuditExecutionContext.Scope(queueCtx))
                    {
                        try
                        {
                            response = ProcessOrder(item.Request);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing order {OrderNo}", item.Request.OrderNo);
                            _audit.RecordFailureAsync(
                                action: "Order.Failed",
                                entityType: AuditEntityTypes.Order,
                                entityId: item.Request.OrderNo,
                                error: ex.Message,
                                metadata: new { stackTrace = ex.StackTrace?.Split('\n').FirstOrDefault() }).GetAwaiter().GetResult();

                            response = new OrderResponse
                            {
                                Success = false,
                                Message = $"Lỗi xử lý: {ex.Message}",
                                OrderNo = item.Request.OrderNo,
                                At = DateTime.Now
                            };
                        }

                        _audit.RecordSuccessAsync(
                            action: response.Success ? "Order.Processed" : "Order.Rejected",
                            entityType: AuditEntityTypes.Order,
                            entityId: item.Request.OrderNo,
                            before: null,
                            after: new
                            {
                                response.Success,
                                response.Message,
                                response.InsertedCount,
                                response.DuplicateCount,
                                response.TotalCodes
                            },
                            changedFieldsJson: null,
                            metadata: new { statusCode = response.Success ? "OK" : "FAIL" }).GetAwaiter().GetResult();
                    }

                    item.Completion.TrySetResult(response);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OrderQueueService stopping");
            }
        }

        private OrderResponse ProcessOrder(OrderRequest request)
        {
            // 1. Validate
            if (string.IsNullOrWhiteSpace(request.OrderNo))
                return Fail(request.OrderNo, "OrderNo là bắt buộc");
            if (string.IsNullOrWhiteSpace(request.GTIN))
                return Fail(request.OrderNo, "GTIN là bắt buộc");
            if (request.OrderQty <= 24)
                return Fail(request.OrderNo, "OrderQty phải lớn hơn 24");
            if (request.UniqueCodes == null || request.UniqueCodes.Count == 0)
                return Fail(request.OrderNo, "UniqueCodes là bắt buộc và phải có ít nhất 1 code");

            // 2. Tạo Pool nếu chưa có (theo GTIN = PoolName)
            var poolPathResult = _dataPool.GetPoolPath(request.GTIN);
            if (!poolPathResult.Success)
                return Fail(request.OrderNo, poolPathResult.Message);

            var poolInfoResult = _dataPool.GetPoolInfo(request.GTIN);
            if (!poolInfoResult.Success)
            {
                // Pool chưa có -> tạo mới
                var newPool = new PoolInfo
                {
                    PoolName = request.GTIN,
                    PoolDescription = $"Pool for Order {request.OrderNo}",
                    PoolCreateID = request.OrderNo,
                    PoolNote = request.BlockNo ?? string.Empty,
                    PoolCreatedBy = "API",
                    PoolCreateDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                var createResult = _dataPool.CreatePool(newPool);
                if (!createResult.Success)
                    return Fail(request.OrderNo, $"Tạo pool thất bại: {createResult.Message}");
            }

            // 3. Thêm codes vào Pool (dùng BlockNo làm CreateID)
            var codesTable = new DataTable();
            codesTable.Columns.Add("Code", typeof(string));
            foreach (var code in request.UniqueCodes)
            {
                codesTable.Rows.Add(code);
            }

            var addResult = _dataPool.AddCodes(
                poolName: request.GTIN,
                mode: 2,
                filePath: null,
                singleCode: null,
                dataTable: codesTable,
                createID: request.BlockNo ?? request.OrderNo,
                createdBy: "API"
            );

            if (!addResult.Success && addResult.AddedCount == 0 && addResult.DuplicateCount == 0)
            {
                return Fail(request.OrderNo, $"Thêm codes thất bại: {addResult.Message}");
            }

            // 4. Tạo PO nếu chưa có (theo OrderNo)
            var poInfoResult = _production.GetPOInfo(request.OrderNo);
            if (!poInfoResult.Success || poInfoResult.Data == null)
            {
                var poInfo = new POInfo
                {
                    orderNo = request.OrderNo,
                    site = request.Site ?? "-",
                    factory = request.Factory ?? "-",
                    productionLine = request.ProductionLine ?? "-",
                    productionDate = request.ProductionDate ?? "-",
                    shift = request.Shift ?? "-",
                    orderQty = request.OrderQty.ToString(),
                    lotNumber = request.LotNumber ?? "-",
                    productCode = request.ProductCode ?? "-",
                    productName = request.ProductName ?? "-",
                    gtin = request.GTIN,
                    customerOrderNo = request.CustomerOrderNo ?? "-",
                    uom = request.Uom ?? "-",
                    createDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    createUser = "API",
                    IsEnable = true
                };

                var createPoResult = _production.CreatePO(poInfo);
                if (!createPoResult.Success)
                    return Fail(request.OrderNo, $"Tạo PO thất bại: {createPoResult.Message}");
            }

            return new OrderResponse
            {
                Success = true,
                Message = "OK",
                OrderNo = request.OrderNo,
                InsertedCount = addResult.AddedCount,
                DuplicateCount = addResult.DuplicateCount,
                TotalCodes = addResult.TotalCount,
                ReceiveQty = request.UniqueCodes.Count,
                At = DateTime.Now
            };
        }

        private static OrderResponse Fail(string orderNo, string message) => new()
        {
            Success = false,
            Message = message,
            OrderNo = orderNo,
            At = DateTime.Now
        };

        private sealed record OrderQueueItem(
            OrderRequest Request,
            TaskCompletionSource<OrderResponse> Completion,
            AuditExecutionContext AuditContext);
    }
}