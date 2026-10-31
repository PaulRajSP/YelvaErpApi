using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class ProductService
    {
        private readonly IProductRepository _repo;
        private readonly ICurrentUserService _currentUserService;

        public ProductService(IProductRepository repo, ICurrentUserService currentUserService)
        {
            _repo = repo;
            _currentUserService = currentUserService;
        }

        public PageResult<ProductListItem> GetAllProduct(GetProductListReq req) => _repo.GetAllProduct(req);

        public GetProductByIdRes GetById(string edid)
        {
            GetProductByIdRes? product = _repo.GetById(edid);
            if (product == null) throw new HoneyErpException(ErrorCode.NotFound, "Product not found.");
            return product;
        }

        public AddProductRes AddProduct(AddProductReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                throw new HoneyErpException(ErrorCode.InvalidData, "Product name is required.");

            if (req.IsCombo && (req.ComboItems == null || req.ComboItems.Count == 0))
                throw new HoneyErpException(ErrorCode.InvalidData, "A combo product must have at least one product in it.");

            string prefix = req.IsCombo ? "CO" : "HY";

            req.Sku=Utilities.GeneratePublicId(prefix,3);

            string createdBy = _currentUserService.EmployeeCode ?? "SYSTEM";
            string id = _repo.AddProduct(req, createdBy);
            AddProductRes res=new AddProductRes();
            res.ProductEdid = EncryptionHelper.Encrypt(id);
            return res;
        }

        public ToggleActiveRes ToggleActive(string productEdid, ToggleActiveReq req)
        {
            _repo.ToggleActive(productEdid, req.Active);
            return new ToggleActiveRes();
        }

        public GetComboItemsRes GetComboItems(string comboProductEdid) => _repo.GetComboItems(comboProductEdid);

        public GetCombosUsingProductRes GetCombosUsingProduct(string productEdid, GetCombosUsingProductReq req) => _repo.GetCombosUsingProduct(productEdid, req);

        public void SaveComboItems(string comboProductEdid, SaveComboItemsReq req) => _repo.SaveComboItems(comboProductEdid, req);

        public GetPricingRes GetPricing(string productEdid, CustomerType? customerType) => _repo.GetPricing(productEdid, customerType);

        public void SavePricing(string productEdid, SavePricingReq req) => _repo.SavePricing(productEdid, req);

        public GetPriceHistoryRes GetPriceHistory(GetPriceHistoryReq req) => _repo.GetPriceHistory(req);

        public ValidateMaterialRes ValidateMaterial(string productEdid, ValidateMaterialReq req) => _repo.ValidateMaterial(productEdid, req);

        public ReconcileProductBatchRes ReconcileProductBatch(ReconcileProductBatchReq req)
        {
            if (req == null)
                throw new HoneyErpException(ErrorCode.RequestIsNull, "Request body is required.");

            if (string.IsNullOrWhiteSpace(req.ProductEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Product is required.");

            bool batchNumberProvided      = !string.IsNullOrWhiteSpace(req.BatchNumber);
            bool productBatchEdidProvided = !string.IsNullOrWhiteSpace(req.ProductBatchEdid);

            if (batchNumberProvided && !productBatchEdidProvided)
                throw new HoneyErpException(ErrorCode.InvalidData,
                    "An existing batch must be identified by productBatchEdid.");

            if (!batchNumberProvided && productBatchEdidProvided)
                throw new HoneyErpException(ErrorCode.InvalidData,
                    "A new batch cannot have an existing productBatchEdid.");

            if (req.ProductionQty <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData,
                    "Production quantity must be greater than zero.");

            if (!req.ShelfLifeMonths.HasValue || !ProductBatchOptions.ShelfLifeMonths.Contains(req.ShelfLifeMonths.Value))
                throw new HoneyErpException(ErrorCode.InvalidData, "Choose a shelf life of 12 or 18 months.");
            if (!req.PackedDate.HasValue)
                throw new HoneyErpException(ErrorCode.InvalidData, "Packed date is required.");
            if (req.PackedDate.Value.Date > DateTime.Today)
                throw new HoneyErpException(ErrorCode.InvalidData, "Packed date cannot be in the future.");
            req.PackedDate = req.PackedDate.Value.Date;
            req.ExpiryDate = req.PackedDate.Value.AddMonths(req.ShelfLifeMonths.Value);

            if (req.Materials == null || req.Materials.Count == 0)
                throw new HoneyErpException(ErrorCode.InvalidData,
                    "At least one material is required.");

            return _repo.ReconcileProductBatch(req);
        }

        public PageResult<CustomerProductListItem> GetProductsByCustomer(GetProductsByCustomerReq req)
        {
            if (req == null)
                throw new HoneyErpException(ErrorCode.RequestIsNull, "Request body is required.");

            return _repo.GetProductsByCustomer(req);
        }

        public ProductUsageHistoryRes GetUsageHistory(GetProductUsageHistoryReq req) => _repo.GetUsageHistory(req);

        public void AdjustProductBatch(string productEdid, string productBatchEdid, ProductBatchAdjustmentReq req)
        {
            if (req.Quantity <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Adjustment quantity must be greater than zero.");
            if (!Enum.IsDefined(req.EntryType))
                throw new HoneyErpException(ErrorCode.InvalidData, "Choose a valid adjustment type.");
            if (string.IsNullOrWhiteSpace(req.Remark))
                throw new HoneyErpException(ErrorCode.InvalidData, "A remark is required for every stock adjustment.");

            bool adjusted = _repo.AddProductBatchUsage(new ProductBatchUsage
            {
                ProductEdid = productEdid,
                ProductBatchEdid = productBatchEdid,
                QuantityUsed = req.Quantity,
                EntryType = req.EntryType,
                Remark = req.Remark.Trim()
            });

            if (!adjusted)
                throw new HoneyErpException(ErrorCode.InvalidData, "Adjustment could not be applied. Check the batch status and remaining stock.");
        }

        public void CloseProductBatch(string productEdid, string productBatchEdid)
        {
            int productId = EncryptionHelper.Decrypt<int>(productEdid);
            int productBatchId = EncryptionHelper.Decrypt<int>(productBatchEdid);
            int result = _repo.CloseProductBatch(productId, productBatchId);

            if (result == 0)
                throw new HoneyErpException(ErrorCode.NotFound, "Product batch not found.");
            if (result == -1)
                throw new HoneyErpException(ErrorCode.InvalidData, "Product batch is already closed.");
            if (result == -2)
                throw new HoneyErpException(ErrorCode.InvalidData, "Product batch can only be closed when remaining stock is zero.");
        }

        public PageResult<ProductBatchListItem> GetProductBatchList(GetProductBatchListReq req) => _repo.GetProductBatchList(req);
    }
}
