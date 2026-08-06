using Microsoft.AspNetCore.Mvc;
using DMS_Project.Auth;
using DMS_Project.Infrastructure;
using DMS_Project.Production;
using Microsoft.AspNetCore.Authorization;

namespace DMS_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiGroup("main")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Operator}")]
    public class ProductionController : ControllerBase
    {
        private readonly Production.Production _production;

        public ProductionController(Production.Production production)
        {
            _production = production;
        }

        #region ============== PO ENDPOINTS ==============

        /// <summary>
        /// Lấy danh sách tất cả PO
        /// </summary>
        [HttpGet("polist")]
        public IActionResult GetPOList()
        {
            var result = _production.GetPOList();
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một PO
        /// </summary>
        [HttpGet("{orderNo}")]
        public IActionResult GetPOInfo(string orderNo)
        {
            var result = _production.GetPOInfo(orderNo);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Tạo PO mới
        /// </summary>
        [HttpPost]
        public IActionResult CreatePO([FromBody] POInfo poInfo)
        {
            var result = _production.CreatePO(poInfo);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        #endregion

        #region ============== CODE ENDPOINTS ==============

        /// <summary>
        /// Tải mã từ DataPool (GTIN = PoolName)
        /// </summary>
        [HttpPost("{orderNo}/loadcodes")]
        public IActionResult LoadCodesFromGTIN(string orderNo, [FromBody] LoadCodesRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.gtin))
                return BadRequest(new POResult(false, "GTIN là bắt buộc"));

            var result = _production.LoadCodesFromGTIN(orderNo, request.gtin, request.qty);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy 1 mã tiếp theo (chưa active)
        /// </summary>
        [HttpGet("{orderNo}/nextcode")]
        public IActionResult GetNextCode(string orderNo)
        {
            var result = _production.GetNextCode(orderNo);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Kích hoạt mã (Pass)
        /// </summary>
        [HttpPost("{orderNo}/activate")]
        public IActionResult ActivateCode(string orderNo, [FromBody] ActivateCodeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.code))
                return BadRequest(new POResult(false, "Code là bắt buộc"));

            string user = request.user ?? "system";
            var result = _production.ActivateCode(orderNo, request.code, user);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật trạng thái mã
        /// </summary>
        [HttpPut("{orderNo}/code/{code}/status")]
        public IActionResult UpdateCodeStatus(string orderNo, string code, [FromBody] UpdateCodeStatusRequest request)
        {
            if (request == null)
                return BadRequest(new POResult(false, "Status là bắt buộc"));

            var result = _production.UpdateCodeStatus(orderNo, code, request.status);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        #endregion

        #region ============== CARTON ENDPOINTS ==============

        /// <summary>
        /// Tạo thùng mới
        /// </summary>
        [HttpPost("{orderNo}/carton")]
        public IActionResult CreateCarton(string orderNo, [FromBody] CreateCartonRequest request)
        {
            string user = request?.user ?? "system";
            var result = _production.CreateCarton(orderNo, user);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Thêm sản phẩm vào thùng
        /// </summary>
        [HttpPost("{orderNo}/carton/add")]
        public IActionResult AddToCarton(string orderNo, [FromBody] AddToCartonRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.code) || string.IsNullOrWhiteSpace(request.cartonCode))
                return BadRequest(new POResult(false, "Code và CartonCode là bắt buộc"));

            var result = _production.AddToCarton(orderNo, request.code, request.cartonCode);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        #endregion

        #region ============== COUNTER & RECORDS ==============

        /// <summary>
        /// Lấy counter hiện tại
        /// </summary>
        [HttpGet("{orderNo}/counter")]
        public IActionResult GetCounter(string orderNo)
        {
            var result = _production.GetCounter(orderNo);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy records có phân trang
        /// </summary>
        [HttpGet("{orderNo}/records")]
        public IActionResult GetRecords(string orderNo, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 100)
        {
            var result = _production.GetRecords(orderNo, pageIndex, pageSize);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        #endregion

        #region ============== AWS STATUS ==============

        /// <summary>
        /// Cập nhật trạng thái gửi AWS
        /// </summary>
        [HttpPut("{orderNo}/aws/sendstatus")]
        public IActionResult UpdateSendStatus(string orderNo, [FromBody] UpdateSendStatusRequest request)
        {
            if (request == null || request.codes == null || request.codes.Count == 0)
                return BadRequest(new POResult(false, "Codes là bắt buộc"));

            var result = _production.UpdateSendStatus(orderNo, request.codes, request.sendStatus);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        #endregion
    }

    #region ============== REQUEST MODELS ==============

    public class LoadCodesRequest
    {
        public string gtin { get; set; } = "";
        public int qty { get; set; } = 100;
    }

    public class ActivateCodeRequest
    {
        public string code { get; set; } = "";
        public string? user { get; set; }
    }

    public class UpdateCodeStatusRequest
    {
        public int status { get; set; }
    }

    public class CreateCartonRequest
    {
        public string? user { get; set; }
    }

    public class AddToCartonRequest
    {
        public string code { get; set; } = "";
        public string cartonCode { get; set; } = "";
    }

    public class UpdateSendStatusRequest
    {
        public List<string> codes { get; set; } = new();
        public int sendStatus { get; set; }
    }

    #endregion
}
