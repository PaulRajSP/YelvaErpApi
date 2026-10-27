using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class SupplierService
    {
        private readonly ISupplierRepository _repo;
        public SupplierService(ISupplierRepository repo) => _repo = repo;

        public PageResult<SupplierListItem> GetAll(GetSupplierListReq req) => _repo.GetAll(req);

        public GetSupplierByIdRes GetById(string edid)
        {
            GetSupplierByIdRes? s = _repo.GetById(edid);
            if (s == null) throw new HoneyErpException(ErrorCode.NotFound, "Supplier not found.");
            return s;
        }

        public AddSupplierRes AddSupplier(AddSupplierReq req)
        {
            req.Balance = 0;
            AddSupplierRes res = _repo.AddSupplier(req);
            
            return res;
        }

        public EditSupplierRes Edit(string edid, EditSupplierReq req)
        {
            if (!_repo.Update(edid, req))
                throw new HoneyErpException(ErrorCode.NotFound, "Supplier not found.");

            return new EditSupplierRes();
        }
    }
}

