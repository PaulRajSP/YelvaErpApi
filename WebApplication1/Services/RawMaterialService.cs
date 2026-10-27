using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class RawMaterialService
    {
        private readonly IRawMaterialRepository _repo;
        public RawMaterialService(IRawMaterialRepository repo) => _repo = repo;

        public PageResult<RawMaterialListItem> GetAll(GetRawMaterialListReq req) => _repo.GetAll(req);

        public GetRawMaterialByIdRes GetById(string edid)
        {
            GetRawMaterialByIdRes? mat = _repo.GetById(edid);
            if (mat == null) throw new HoneyErpException(ErrorCode.NotFound, "Raw material not found.");
            return mat;
        }

        public AddRawMaterialRes AddRawMaterial(AddRawMaterialReq req)
        {
            AddRawMaterialRes res = _repo.Create(req);
            return res;
        }

        public EditRawMaterialRes Edit(string edid, EditRawMaterialReq req)
        {
            if (!_repo.Update(edid, req))
                throw new HoneyErpException(ErrorCode.NotFound, "RawMaterial not found.");

            return new EditRawMaterialRes();
        }

        public void UpdateStock(string edid, decimal qtyChange)
        {
            if (!_repo.UpdateStock(edid, qtyChange))
                throw new HoneyErpException(ErrorCode.NotFound, "Raw material not found.");
        }

        public GetRecipeRes GetRecipe(string productEdid) => _repo.GetRecipe(productEdid);

        public ToggleRecipeItemStatusRes ToggleRecipeItemStatus(string productEdid, string materialEdid, ToggleRecipeItemStatusReq req)
        {
            _repo.ToggleRecipeItemStatus(productEdid, materialEdid, req.Active);
            return new ToggleRecipeItemStatusRes();
        }

        public void SaveRecipe(string productEdid, SaveRecipeReq req) => _repo.SaveRecipe(productEdid, req);

        public PageResult<CategoryListItem> GetCategories(GetCategoryListReq req) => _repo.GetCategories(req);

        public AddCategoryRes AddCategory(AddCategoryReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                throw new HoneyErpException(ErrorCode.InvalidData, "Category name is required.");

            return _repo.CreateCategory(req);
        }

        public PageResult<MaterialPurchaseHistoryItem> GetPurchaseHistory(string edid, GetMaterialPurchaseHistoryReq req) => _repo.GetPurchaseHistory(edid, req);

        public MaterialUsageHistoryRes GetUsageHistory(GetMaterialUsageHistoryReq req) => _repo.GetUsageHistory(req);

        public PageResult<GetMaterialBatchListItem> GetMaterialBatchList(GetMaterialBatchListReq req) => _repo.GetMaterialBatchList(req);
        public GetPackingMaterialsRes GetPackingMaterials(string? searchKeyword = null) => _repo.GetPackingMaterials(searchKeyword);
    }
}
