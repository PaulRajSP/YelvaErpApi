using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static HoneyErpAPI.Models.AddRawMaterialReq;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/raw-materials")]
    public class RawMaterialController : ControllerBase
    {
        private readonly RawMaterialService _service;
        private readonly ILogger<RawMaterialController> _logger;

        public RawMaterialController(RawMaterialService service, ILogger<RawMaterialController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAll([FromBody] GetRawMaterialListReq req)
        {
            ApiResponseModel<PageResult<RawMaterialListItem>> res = new ApiResponseModel<PageResult<RawMaterialListItem>>();
            res.Success = true;
            res.Data    = _service.GetAll(req);
            return Ok(res);
        }

        [HttpGet("{edid}")]
        public IActionResult GetById(string edid)
        {
            ApiResponseModel<GetRawMaterialByIdRes> res = new ApiResponseModel<GetRawMaterialByIdRes>();
            res.Success = true;
            res.Data    = _service.GetById(edid);
            return Ok(res);
        }

        [HttpPost]
        public IActionResult AddRawMaterial([FromBody] AddRawMaterialReq req)
        {
            ApiResponseModel<AddRawMaterialRes> res = new ApiResponseModel<AddRawMaterialRes>();
            res.Success = true;
            res.Data = _service.AddRawMaterial(req);
            return Ok(res);
        }

        [HttpPut("{edid}")]
        public IActionResult Edit(string edid, [FromBody] EditRawMaterialReq req)
        {
            ApiResponseModel<EditRawMaterialRes> res = new ApiResponseModel<EditRawMaterialRes>();
            res.Success = true;
            res.Data    = _service.Edit(edid, req);
            return Ok(res);
        }

        [HttpGet("{edid}/recipe")]
        public IActionResult GetRecipe(string edid)
        {
            ApiResponseModel<GetRecipeRes> res = new ApiResponseModel<GetRecipeRes>();
            res.Success = true;
            res.Data    = _service.GetRecipe(edid);
            res.Message = "Recipe fetched successfully";
            return Ok(res);
        }

        [HttpPost("{productEdid}/recipe/{materialEdid}/status")]
        public IActionResult ToggleRecipeItemStatus(string productEdid, string materialEdid, [FromBody] ToggleRecipeItemStatusReq req)
        {
            ApiResponseModel<ToggleRecipeItemStatusRes> res = new ApiResponseModel<ToggleRecipeItemStatusRes>();
            res.Success = true;
            res.Data    = _service.ToggleRecipeItemStatus(productEdid, materialEdid, req);
            res.Message = req.Active ? "Recipe item activated successfully" : "Recipe item deactivated successfully";
            return Ok(res);
        }

        [HttpPost("{edid}/recipe/save")]
        public IActionResult SaveRecipe(string edid, [FromBody] SaveRecipeReq req)
        {
            ApiResponseModel<SaveRecipeRes> res = new ApiResponseModel<SaveRecipeRes>();
            _service.SaveRecipe(edid, req);
            res.Success = true;
            res.Message = "Recipe saved successfully";
            return Ok(res);
        }

        [HttpPost("category/list")]
        public IActionResult GetCategories([FromBody] GetCategoryListReq req)
        {
            ApiResponseModel<PageResult<CategoryListItem>> res = new ApiResponseModel<PageResult<CategoryListItem>>();
            res.Success = true;
            res.Data    = _service.GetCategories(req);
            return Ok(res);
        }

        [HttpPost("category/add")]
        public IActionResult AddCategory([FromBody] AddCategoryReq req)
        {
            ApiResponseModel<AddCategoryRes> res = new ApiResponseModel<AddCategoryRes>();
            res.Success = true;
            res.Data    = _service.AddCategory(req);
            res.Message = "Category added successfully";
            return Ok(res);
        }
        [HttpPost("{edid}/purchase-history")]
        public IActionResult GetPurchaseHistory(string edid, [FromBody] GetMaterialPurchaseHistoryReq req)
        {
            ApiResponseModel<PageResult<MaterialPurchaseHistoryItem>> res = new ApiResponseModel<PageResult<MaterialPurchaseHistoryItem>>();
            res.Success = true;
            res.Data    = _service.GetPurchaseHistory(edid, req);
            res.Message = "Purchase history fetched successfully";
            return Ok(res);
        }

        [HttpGet("packing-materials")]
        public IActionResult GetPackingMaterials([FromQuery] string? searchKeyword = null)
        {
            ApiResponseModel<GetPackingMaterialsRes> res = new ApiResponseModel<GetPackingMaterialsRes>();
            res.Success = true;
            res.Data = _service.GetPackingMaterials(searchKeyword);
            res.Message = "Packing materials fetched successfully";
            return Ok(res);
        }

    }
}