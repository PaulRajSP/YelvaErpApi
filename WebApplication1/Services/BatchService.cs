using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;

namespace HoneyErpAPI.Services
{
    public class BatchService
    {
        private readonly IBatchRepository _repo;

        public BatchService(IBatchRepository repo)
        {
            _repo = repo;
        }

        public GetBatchListRes GetBatches(string materialEdid, string? batchNumber, decimal? unitCost, string? supplierEdid) => _repo.GetBatches(materialEdid, batchNumber, unitCost, supplierEdid);

        public GetBatchByIdRes GetBatchById(string edid)
        {
            GetBatchByIdRes? b = _repo.GetBatchById(edid);
            if (b == null) throw new HoneyErpException(ErrorCode.NotFound, "Batch not found.");
            return b;
        }

        public CreateBatchRes CreateBatch(CreateBatchReq req)
        {
            req.BatchNumber = Utilities.GenerateRandomNumber(6);
            req.QtyPurchased = 0;
            req.QtyRemaining = 0;

            if (string.IsNullOrWhiteSpace(req.BatchNumber))
                throw new HoneyErpException(ErrorCode.InvalidData, "Batch number is required.");

            return _repo.CreateBatch(req);
        
        }

        public void UpdateBatchQty(string edid, decimal qtyRemaining)
        {
            if (!_repo.UpdateBatchQty(edid, qtyRemaining))
                throw new HoneyErpException(ErrorCode.NotFound, "Batch not found.");
        }
    }
}
