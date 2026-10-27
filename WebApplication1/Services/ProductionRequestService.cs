using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using System.Net.NetworkInformation;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class ProductionRequestService
    {
        private readonly IProductionRequestRepository _repo;
        private readonly IRawMaterialRepository _rawMaterialRepo;
        private readonly IProductRepository _productRepo;
        public ProductionRequestService(IProductionRequestRepository repo, IRawMaterialRepository rawMaterialRepo, IProductRepository productRepo)
        {
            _repo = repo;
            _rawMaterialRepo = rawMaterialRepo;
            _productRepo = productRepo;

        }

        public PageResult<ProductionRequestListItem> GetAll(GetProductionRequestListReq req) => _repo.GetAll(req);

        public GetProductionRequestByIdRes GetById(string edid)
        {
            if (string.IsNullOrWhiteSpace(edid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Production request ID is required.");

            GetProductionRequestByIdRes? res = _repo.GetById(edid);
            if (res == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Production request not found.");

            return res;
        }

        public CreateProductionRequestRes CreateProductionRequest(CreateProductionRequestReq req)
        {
            GetRecipeRawMaterialsRes recipe = GetRecipeRawMaterials(req.ProductEdid);

            Validation validation = new Validation();
            validation.ValidateCreateProductionRequest(req, recipe);

            req.Status = ProductionStatus.Pending;

            return _repo.CreateProductionRequest(req);
        }
        
        public ProductionRequestActionRes UpdateProductionRequestStatus(UpdateProductionRequestStatusReq req)
        {
            // 1. Validation
            if (string.IsNullOrWhiteSpace(req.Edid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Production request ID is required.");

            // 2. Fetch Production Request
            GetProductionRequestByIdRes productionRequest = GetById(req.Edid);

            if (productionRequest == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Production request not found.");

            // 3. Handle Completed Status
            if (req.Status == ProductionStatus.Completed)
            {
                if (req.CompletedQty <= 0)
                    throw new HoneyErpException(ErrorCode.InvalidData, "Completed quantity must be greater than zero.");

                if (req.CompletedQty > productionRequest.RequestedQty)
                    throw new HoneyErpException(ErrorCode.InvalidData, "Completed quantity cannot exceed requested quantity.");

                decimal additionalCompletedQty = req.CompletedQty - productionRequest.CompletedQty;

                if (additionalCompletedQty <= 0)
                    throw new HoneyErpException(ErrorCode.InvalidData, "Completed quantity must be greater than the previously completed quantity.");

                // 4. Fetch Recipe
                GetRecipeRes recipe = _rawMaterialRepo.GetRecipe(productionRequest.ProductEdid);

                if (recipe == null || recipe.Items == null || recipe.Items.Count == 0)
                    throw new HoneyErpException(ErrorCode.InvalidData, "Recipe not found for this product.");

                // 5. Validate and Allocate Raw Materials
                List<(ProductionRequestMaterialItem Material, decimal Quantity)> materialUsages = new();

                foreach (RecipeItemLine recipeItem in recipe.Items)
                {
                    decimal requiredQty = recipeItem.Qty * additionalCompletedQty;

                    List<ProductionRequestMaterialItem> materials = productionRequest.Materials.Where(x => EncryptionHelper.Decrypt<int>(x.MaterialEdid) == EncryptionHelper.Decrypt<int>(recipeItem.MaterialEdid)).ToList();

                    if (materials.Count == 0)
                        throw new HoneyErpException(ErrorCode.NotFound, $"Material '{recipeItem.MaterialName}' not found in production request.");

                    decimal availableQty = materials.Sum(x => x.AvailableStock);

                    if (availableQty < requiredQty)
                        throw new HoneyErpException(ErrorCode.InvalidData,$"Insufficient stock for '{recipeItem.MaterialName}'. Required: {requiredQty}, Available: {availableQty}.");

                    decimal remainingQty = requiredQty;

                    foreach (ProductionRequestMaterialItem material in materials)
                    {
                        if (remainingQty <= 0) break;

                        if (string.IsNullOrWhiteSpace(material.MaterialBatchEdid))
                            throw new HoneyErpException(ErrorCode.InvalidData,$"Material batch is missing for '{material.MaterialName}'.");

                        decimal usedQty = Math.Min(remainingQty, material.AvailableStock);

                        if (usedQty <= 0) continue;

                        materialUsages.Add((material, usedQty));

                        remainingQty -= usedQty;
                    }

                    if (remainingQty > 0)
                        throw new HoneyErpException(ErrorCode.InvalidData,$"Unable to allocate sufficient stock for '{recipeItem.MaterialName}'.");
                }

                // 6. Record Raw Material Consumption
                foreach ((ProductionRequestMaterialItem Material, decimal Quantity) usage in materialUsages)
                {
                    MaterialBatchUsage materialBatchUsage = new MaterialBatchUsage();
                    materialBatchUsage.MaterialEdid = usage.Material.MaterialEdid;
                    materialBatchUsage.MaterialBatchEdid = usage.Material.MaterialBatchEdid;
                    materialBatchUsage.QuantityUsed = usage.Quantity;
                    materialBatchUsage.Remark = $"Production Request #{productionRequest.RequestNumber} consumed {usage.Quantity} of '{usage.Material.MaterialName}'";
                    
                    _rawMaterialRepo.AddMaterialBatchUsage(materialBatchUsage);
                }

                // 7. Create Finished Product Batch
                ProductBatchEntry productBatch = new ProductBatchEntry();
                productBatch.ProductEdid = productionRequest.ProductEdid;
                productBatch.BatchNumber = Utilities.GeneratePublicId("HY", 5);
                productBatch.ProducedQty = additionalCompletedQty;
                productBatch.AvailableQty = additionalCompletedQty;
                productBatch.Remark = $"Production Request #{productionRequest.RequestNumber} produced {additionalCompletedQty} additional units";
                
                _productRepo.AddProductBatchMaster(productBatch);
            }

            // 8. Update Production Request Status and Completed Quantity
            bool ok = _repo.UpdateProductionRequestStatus(req);

            if (!ok)
                throw new HoneyErpException(ErrorCode.NotFound, "Production request not found or could not be updated.");

            return new ProductionRequestActionRes();
        }

        public GetRecipeRawMaterialsRes GetRecipeRawMaterials(string productEdid)
        {
            if (string.IsNullOrWhiteSpace(productEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Product ID is required.");

            return _repo.GetRecipeRawMaterials(productEdid);
        }



        public class Validation
        {

            public void ValidateCreateProductionRequest(CreateProductionRequestReq req, GetRecipeRawMaterialsRes recipe)
            {
                if (string.IsNullOrWhiteSpace(req.ProductEdid))
                    throw new HoneyErpException(ErrorCode.InvalidData, "Product is required.");

                if (req.RequestedQty <= 0)
                    throw new HoneyErpException(ErrorCode.InvalidData, "Requested quantity must be greater than zero.");

                if (recipe == null || recipe.RawMaterials == null || recipe.RawMaterials.Count == 0)
                    throw new HoneyErpException(ErrorCode.InvalidData, "No active recipe found for this product.");

                if (req.Materials == null || req.Materials.Count != recipe.RawMaterials.Count)
                    throw new HoneyErpException(ErrorCode.InvalidData, "All recipe materials must be selected exactly once.");

                HashSet<string> submittedRecipeItemIds = new HashSet<string>();

                foreach (CreateProductionRequestMaterialLine line in req.Materials)
                {
                    if (string.IsNullOrWhiteSpace(line.MaterialEdid))
                        throw new HoneyErpException(ErrorCode.InvalidData, "Material is required.");

                    if (string.IsNullOrWhiteSpace(line.MaterialBatchEdid))
                        throw new HoneyErpException(ErrorCode.InvalidData, "Material batch is required.");

                    if (string.IsNullOrWhiteSpace(line.RecipeItemEdid))
                        throw new HoneyErpException(ErrorCode.InvalidData, "Recipe item is required.");

                    string recipeItemId = EncryptionHelper.Decrypt(line.RecipeItemEdid);
                    string materialId = EncryptionHelper.Decrypt(line.MaterialEdid);
                    string materialBatchId = EncryptionHelper.Decrypt(line.MaterialBatchEdid);

                    if (!int.TryParse(recipeItemId, out int parsedRecipeItemId)
                        || !int.TryParse(materialId, out int parsedMaterialId)
                        || !int.TryParse(materialBatchId, out int parsedMaterialBatchId))
                    {
                        throw new HoneyErpException(ErrorCode.InvalidData, "Invalid material, batch, or recipe item.");
                    }

                    if (!submittedRecipeItemIds.Add(recipeItemId))
                        throw new HoneyErpException(ErrorCode.InvalidData, "Duplicate recipe items are not allowed.");

                    ProductionRequestRecipeRawMaterialItem recipeItem = recipe.RawMaterials.FirstOrDefault(x =>
                        EncryptionHelper.Decrypt(x.RecipeItemEdid) == recipeItemId);

                    if (recipeItem == null)
                        throw new HoneyErpException(ErrorCode.InvalidData, "Selected recipe item does not belong to this product.");

                    if (EncryptionHelper.Decrypt(recipeItem.MaterialEdid) != materialId)
                        throw new HoneyErpException(ErrorCode.InvalidData, "Selected material does not match the recipe item.");

                    ProductionRequestRecipeBatchItem batch = recipeItem.RecipeBatches.FirstOrDefault(x =>
                        EncryptionHelper.Decrypt(x.Edid) == materialBatchId
                        && EncryptionHelper.Decrypt(x.MaterialEdid) == materialId);

                    if (batch == null)
                        throw new HoneyErpException(ErrorCode.InvalidData, "Selected batch is not available for this recipe material.");

                    if (!batch.MaxProductionPossibleQty.HasValue
                        || req.RequestedQty > batch.MaxProductionPossibleQty.Value)
                    {
                        throw new HoneyErpException(
                            ErrorCode.InvalidData,
                            "Requested quantity exceeds the maximum production quantity allowed for batch "
                            + batch.BatchNumber
                            + ". Maximum allowed quantity: "
                            + (batch.MaxProductionPossibleQty ?? 0)
                        );
                    }
                }
            }
        }
    }
}
