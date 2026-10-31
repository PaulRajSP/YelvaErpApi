using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class MaterialBatchUsageService
    {
        private readonly IMaterialBatchUsageRepository _repo;

        public MaterialBatchUsageService(IMaterialBatchUsageRepository repo) => _repo = repo;

        public MaterialUsageHistoryRes GetUsageHistory(GetMaterialUsageHistoryReq req) =>
            _repo.GetUsageHistory(req);

        public PageResult<GetMaterialBatchListItem> GetMaterialBatchList(GetMaterialBatchListReq req) =>
            _repo.GetMaterialBatchList(req);

        public bool AddMaterialBatchUsage(MaterialBatchUsage req)
        {
            if (string.IsNullOrWhiteSpace(req.MaterialEdid) || string.IsNullOrWhiteSpace(req.MaterialBatchEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Material and batch IDs are required.");
            if (req.QuantityUsed <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Material batch quantity must be greater than zero.");
            if (!Enum.IsDefined(req.EntryType))
                throw new HoneyErpException(ErrorCode.InvalidData, "Material batch entry type must be In or Out.");

            return _repo.Add(req);
        }

        public void AdjustBatch(string batchEdid, MaterialBatchAdjustmentReq req)
        {
            if (string.IsNullOrWhiteSpace(batchEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Batch ID is required.");
            if (req.Quantity <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Adjustment quantity must be greater than zero.");
            if (string.IsNullOrWhiteSpace(req.Remark))
                throw new HoneyErpException(ErrorCode.InvalidData, "A remark is required for every stock adjustment.");
            if (req.Remark.Trim().Length > 500)
                throw new HoneyErpException(ErrorCode.InvalidData, "Adjustment remark cannot exceed 500 characters.");
            if (!Enum.IsDefined(req.EntryType))
                throw new HoneyErpException(ErrorCode.InvalidData, "Adjustment type must be In or Out.");

            int batchId = EncryptionHelper.Decrypt<int>(batchEdid);
            if (!_repo.AdjustBatchQuantity(batchId, req))
                throw new HoneyErpException(ErrorCode.InvalidData, "Batch is closed or the adjustment exceeds its remaining quantity.");
        }

        public void CloseBatch(string batchEdid)
        {
            if (string.IsNullOrWhiteSpace(batchEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Batch ID is required.");

            int result = _repo.CloseBatch(EncryptionHelper.Decrypt<int>(batchEdid));
            if (result == 0)
                throw new HoneyErpException(ErrorCode.NotFound, "Batch not found.");
            if (result == -1)
                throw new HoneyErpException(ErrorCode.Conflict, "Batch is already closed.");
            if (result == -2)
                throw new HoneyErpException(ErrorCode.Conflict, "Batch can only be closed when its remaining quantity is zero.");
        }
    }
}
