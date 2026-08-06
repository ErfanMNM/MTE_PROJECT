using Microsoft.AspNetCore.Mvc;
using System.Data;

using PoolInfo = DMS_Project.DataPool.PoolInfo;
using PoolInfoBasic = DMS_Project.DataPool.PoolInfoBasic;
using PoolInfoWithCount = DMS_Project.DataPool.PoolInfoWithCount;
using PoolListResult = DMS_Project.DataPool.PoolListResult;
using PoolCodePageResult = DMS_Project.DataPool.PoolCodePageResult;
using CodeCount = DMS_Project.DataPool.CodeCount;

namespace DMS_Project.Controllers
{
    [ApiController]
    [Route("api/datapool")]
    [Produces("application/json")]
    public class DataPoolController : ControllerBase
    {
        private readonly DMS_Project.DataPool.DataPool _dataPool;

        public DataPoolController(DMS_Project.DataPool.DataPool dataPool)
        {
            _dataPool = dataPool;
        }

        /// <summary>
        /// Test endpoint - Simple health check
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { status = "ok", message = "API is running" });
        }

        #region === Pool Endpoints ===

        /// <summary>
        /// Lấy danh sách tất cả pools (có phân trang)
        /// </summary>
        [HttpGet("pools")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetPools([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 100)
        {
            var result = _dataPool.GetPoolsPaginated(pageIndex, pageSize);
            if (!result.Success)
                return BadRequest(new ApiResponse<object> { Success = false, Message = result.Message });

            var dto = new PagedPoolListDto(result.Data!);
            return Ok(new ApiResponse<PagedPoolListDto> { Success = true, Message = result.Message, Data = dto });
        }

        /// <summary>
        /// Lấy thông tin pool theo tên (kèm số đếm codes)
        /// </summary>
        [HttpGet("pools/{poolName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetPoolInfo(string poolName)
        {
            var result = _dataPool.GetPoolInfo(poolName);
            if (!result.Success)
                return NotFound(new ApiResponse<object> { Success = false, Message = result.Message });

            var dto = new PoolInfoDto(result.Data!);
            return Ok(new ApiResponse<PoolInfoDto> { Success = true, Message = result.Message, Data = dto });
        }

        /// <summary>
        /// Tạo pool mới
        /// </summary>
        [HttpPost("pools")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult CreatePool([FromBody] CreatePoolRequest request)
        {
            var poolInfo = new PoolInfo
            {
                PoolName = request.PoolName,
                PoolDescription = request.PoolDescription ?? string.Empty,
                PoolCreateID = request.CreateID ?? Guid.NewGuid().ToString(),
                PoolNote = request.Note ?? string.Empty,
                PoolCreatedBy = request.CreatedBy ?? "API",
                PoolCreateDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var result = _dataPool.CreatePool(poolInfo);
            if (!result.Success)
                return BadRequest(new ApiResponse<object> { Success = false, Message = result.Message });

            return Created(result.Data, new ApiResponse<object> { Success = true, Message = result.Message, Data = new { PoolPath = result.Data } });
        }

        /// <summary>
        /// Lấy đường dẫn file của pool
        /// </summary>
        [HttpGet("pools/{poolName}/path")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetPoolPath(string poolName)
        {
            var result = _dataPool.GetPoolPath(poolName);
            if (!result.Success)
                return BadRequest(new ApiResponse<object> { Success = false, Message = result.Message });

            return Ok(new ApiResponse<object> { Success = true, Message = result.Message, Data = new { Path = result.Data } });
        }

        #endregion

        #region === Code Endpoints ===

        /// <summary>
        /// Lấy danh sách codes trong pool (có phân trang và lọc)
        /// </summary>
        [HttpGet("pools/{poolName}/codes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetCodes(
            string poolName,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] int? status = null,
            [FromQuery] string? batchID = null,
            [FromQuery] string? createID = null,
            [FromQuery] string? createdBy = null,
            [FromQuery] DateTime? fromCreateDate = null,
            [FromQuery] DateTime? toCreateDate = null,
            [FromQuery] DateTime? fromUsedDate = null,
            [FromQuery] DateTime? toUsedDate = null)
        {
            var result = _dataPool.GetPoolCodesPaginated(
                poolName, pageIndex, pageSize, status, batchID, createID, createdBy,
                fromCreateDate, toCreateDate, fromUsedDate, toUsedDate);

            if (!result.Success)
                return NotFound(new ApiResponse<object> { Success = false, Message = result.Message });

            var dto = new PagedCodesDto(result.Data!);
            return Ok(new ApiResponse<PagedCodesDto> { Success = true, Message = result.Message, Data = dto });
        }

        /// <summary>
        /// Lấy số đếm codes trong pool
        /// </summary>
        [HttpGet("pools/{poolName}/codes/counts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetCodeCounts(string poolName)
        {
            var result = _dataPool.GetCodeCounts(poolName);
            if (!result.Success)
                return NotFound(new ApiResponse<object> { Success = false, Message = result.Message });

            return Ok(new ApiResponse<CodeCountDto> { Success = true, Message = result.Message, Data = new CodeCountDto(result.Data!) });
        }

        /// <summary>
        /// Lấy thông tin code cụ thể theo PoolCode
        /// </summary>
        [HttpGet("pools/{poolName}/codes/{code}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetCodeByPoolCode(string poolName, string code)
        {
            var result = _dataPool.GetPoolCode(poolName, code, null);
            if (!result.Success)
                return NotFound(new ApiResponse<object> { Success = false, Message = result.Message });

            var dtos = DataTableToCodeList(result.Data!);
            return Ok(new ApiResponse<List<CodeDto>> { Success = true, Message = result.Message, Data = dtos });
        }

        /// <summary>
        /// Lấy codes theo trạng thái
        /// </summary>
        [HttpGet("pools/{poolName}/codes/status/{status}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetCodesByStatus(string poolName, int status)
        {
            var result = _dataPool.GetCodesByStatus(poolName, status);
            if (!result.Success)
                return NotFound(new ApiResponse<object> { Success = false, Message = result.Message });

            var dtos = DataTableToCodeList(result.Data!);
            return Ok(new ApiResponse<List<CodeDto>> { Success = true, Message = result.Message, Data = dtos });
        }

        /// <summary>
        /// Thêm codes vào pool
        /// </summary>
        /// <remarks>
        /// Mode: 0 = single code, 1 = list of codes
        /// </remarks>
        [HttpPost("pools/{poolName}/codes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult AddCodes(string poolName, [FromBody] AddCodesRequest request)
        {
            DataTable? dataTable = null;

            if (request.Mode == 1 && request.Codes != null && request.Codes.Count > 0)
            {
                dataTable = new DataTable();
                dataTable.Columns.Add("Code", typeof(string));
                foreach (var code in request.Codes)
                {
                    dataTable.Rows.Add(code);
                }
            }

            var result = _dataPool.AddCodes(
                poolName,
                request.Mode,
                null,
                request.Mode == 0 ? request.SingleCode : null,
                dataTable,
                request.CreateID ?? Guid.NewGuid().ToString(),
                request.CreatedBy ?? "API"
            );

            if (!result.Success && result.TotalCount == 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = result.Message });

            return Ok(new ApiResponse<AddCodesResultDto>
            {
                Success = result.Success,
                Message = result.Message,
                Data = new AddCodesResultDto
                {
                    TotalCount = result.TotalCount,
                    AddedCount = result.AddedCount,
                    DuplicateCount = result.DuplicateCount,
                    ErrorCount = result.ErrorCount,
                    Errors = result.Errors
                }
            });
        }

        /// <summary>
        /// Cập nhật trạng thái code
        /// </summary>
        /// <remarks>
        /// Status: 0 = Chưa dùng, 1 = Đã dùng, -1 = Lỗi
        /// </remarks>
        [HttpPatch("pools/{poolName}/codes/{code}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UpdateCodeStatus(string poolName, string code, [FromBody] UpdateStatusRequest request)
        {
            var result = _dataPool.UpdateCodeStatus(poolName, code, null, request.Status);
            if (!result.Success)
            {
                if (result.Message.Contains("không tồn tại"))
                    return NotFound(new ApiResponse<object> { Success = false, Message = result.Message });
                return BadRequest(new ApiResponse<object> { Success = false, Message = result.Message });
            }

            return Ok(new ApiResponse<object> { Success = true, Message = result.Message });
        }

        #endregion

        #region === Helper Methods ===

        private List<CodeDto> DataTableToCodeList(DataTable dt)
        {
            var list = new List<CodeDto>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new CodeDto
                {
                    ID = Convert.ToDouble(row["ID"]),
                    PoolCode = row["PoolCode"]?.ToString() ?? string.Empty,
                    Status = Convert.ToInt32(row["Status"]),
                    StatusName = GetStatusName(Convert.ToInt32(row["Status"])),
                    PoolCodeUsedBatchID = row["PoolCodeUsedBatchID"]?.ToString() ?? string.Empty,
                    PoolCodeUsedDatetime = row["PoolCodeUsedDatetime"]?.ToString() ?? string.Empty,
                    PoolCodeNote = row["PoolCodeNote"]?.ToString() ?? string.Empty,
                    PoolCodeCreateID = row["PoolCodeCreateID"]?.ToString() ?? string.Empty,
                    PoolCodeCreatedBy = row["PoolCodeCreatedBy"]?.ToString() ?? string.Empty,
                    PoolCodeCreateDatetime = row["PoolCodeCreateDatetime"]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        private static string GetStatusName(int status) => status switch
        {
            0 => "Chưa dùng",
            1 => "Đã dùng",
            -1 => "Lỗi",
            _ => "Không xác định"
        };

        #endregion
    }

    #region === API Response & DTOs ===

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class PagedPoolListDto
    {
        public List<PoolInfoDto> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPrevPage { get; set; }

        public PagedPoolListDto(PoolListResult result)
        {
            Items = result.Items.Select(p => new PoolInfoDto(p)).ToList();
            TotalCount = result.TotalCount;
            PageIndex = result.PageIndex;
            PageSize = result.PageSize;
            TotalPages = result.TotalPages;
            HasNextPage = result.HasNextPage;
            HasPrevPage = result.HasPrevPage;
        }
    }

    public class PoolInfoDto
    {
        public double ID { get; set; }
        public string PoolName { get; set; } = string.Empty;
        public string PoolDescription { get; set; } = string.Empty;
        public string PoolCreateID { get; set; } = string.Empty;
        public string PoolNote { get; set; } = string.Empty;
        public string PoolCreatedBy { get; set; } = string.Empty;
        public string PoolCreateDatetime { get; set; } = string.Empty;
        public CodeCountDto? Count { get; set; }

        public PoolInfoDto(PoolInfoBasic basic)
        {
            ID = basic.ID;
            PoolName = basic.PoolName;
            PoolDescription = basic.PoolDescription;
            PoolCreateID = basic.PoolCreateID;
            PoolNote = basic.PoolNote;
            PoolCreatedBy = basic.PoolCreatedBy;
            PoolCreateDatetime = basic.PoolCreateDatetime;
        }

        public PoolInfoDto(PoolInfoWithCount info)
        {
            ID = info.ID;
            PoolName = info.PoolName;
            PoolDescription = info.PoolDescription;
            PoolCreateID = info.PoolCreateID;
            PoolNote = info.PoolNote;
            PoolCreatedBy = info.PoolCreatedBy;
            PoolCreateDatetime = info.PoolCreateDatetime;
            if (info.Count != null)
            {
                Count = new CodeCountDto(info.Count.TotalCount, info.Count.UsedCount, info.Count.UnusedCount, info.Count.ErrorCount);
            }
        }
    }

    public class PagedCodesDto
    {
        public List<CodeDto> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPrevPage { get; set; }

        public PagedCodesDto(PoolCodePageResult result)
        {
            Items = new List<CodeDto>();
            foreach (DataRow row in result.Data.Rows)
            {
                Items.Add(new CodeDto
                {
                    ID = Convert.ToDouble(row["ID"]),
                    PoolCode = row["PoolCode"]?.ToString() ?? string.Empty,
                    Status = Convert.ToInt32(row["Status"]),
                    StatusName = GetStatusName(Convert.ToInt32(row["Status"])),
                    PoolCodeUsedBatchID = row["PoolCodeUsedBatchID"]?.ToString() ?? string.Empty,
                    PoolCodeUsedDatetime = row["PoolCodeUsedDatetime"]?.ToString() ?? string.Empty,
                    PoolCodeNote = row["PoolCodeNote"]?.ToString() ?? string.Empty,
                    PoolCodeCreateID = row["PoolCodeCreateID"]?.ToString() ?? string.Empty,
                    PoolCodeCreatedBy = row["PoolCodeCreatedBy"]?.ToString() ?? string.Empty,
                    PoolCodeCreateDatetime = row["PoolCodeCreateDatetime"]?.ToString() ?? string.Empty
                });
            }
            TotalCount = result.TotalCount;
            PageIndex = result.PageIndex;
            PageSize = result.PageSize;
            TotalPages = result.TotalPages;
            HasNextPage = result.HasNextPage;
            HasPrevPage = result.HasPrevPage;
        }

        private static string GetStatusName(int status) => status switch
        {
            0 => "Chưa dùng",
            1 => "Đã dùng",
            -1 => "Lỗi",
            _ => "Không xác định"
        };
    }

    public class CodeDto
    {
        public double ID { get; set; }
        public string PoolCode { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string PoolCodeUsedBatchID { get; set; } = string.Empty;
        public string PoolCodeUsedDatetime { get; set; } = string.Empty;
        public string PoolCodeNote { get; set; } = string.Empty;
        public string PoolCodeCreateID { get; set; } = string.Empty;
        public string PoolCodeCreatedBy { get; set; } = string.Empty;
        public string PoolCodeCreateDatetime { get; set; } = string.Empty;
    }

    public class CodeCountDto
    {
        public int TotalCount { get; set; }
        public int UsedCount { get; set; }
        public int UnusedCount { get; set; }
        public int ErrorCount { get; set; }

        public CodeCountDto(CodeCount count)
        {
            TotalCount = count.TotalCount;
            UsedCount = count.UsedCount;
        }

        public CodeCountDto(int total, int used, int unused = 0, int error = 0)
        {
            TotalCount = total;
            UsedCount = used;
            UnusedCount = unused;
            ErrorCount = error;
        }
    }

    public class AddCodesResultDto
    {
        public int TotalCount { get; set; }
        public int AddedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    #endregion

    #region === Request Models ===

    public class CreatePoolRequest
    {
        public string PoolName { get; set; } = string.Empty;
        public string? PoolDescription { get; set; }
        public string? CreateID { get; set; }
        public string? Note { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class AddCodesRequest
    {
        public int Mode { get; set; } = 1;
        public string? SingleCode { get; set; }
        public List<string>? Codes { get; set; }
        public string? CreateID { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class UpdateStatusRequest
    {
        public int Status { get; set; }
    }

    #endregion
}
