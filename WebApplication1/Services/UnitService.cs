using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;

namespace HoneyErpAPI.Services
{
    public class UnitService
    {
        private readonly IUnitRepository _repo;
        public UnitService(IUnitRepository repo) => _repo = repo;

        public GetUnitListRes GetAll() => _repo.GetAll();

        public AddUnitRes Add(AddUnitReq req)
        {
            string id = _repo.Create(req);
            return new AddUnitRes { Edid = EncryptionHelper.Encrypt(id) };
        }
    }
}
