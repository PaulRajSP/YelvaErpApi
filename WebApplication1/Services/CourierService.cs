using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class CourierService
    {
        private readonly ICourierRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public CourierService(ICourierRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public GetCourierListRes GetAll(GetCourierListReq req) => _repo.GetAll(req);

        

        public CourierListItem? GetById(string Edid)
        {
            return _repo.GetById(EncryptionHelper.Decrypt<int>(Edid));
        }

        public AddCourierRes Add(AddCourierReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Courier name is required.");

            string edid = _repo.Create(req);
            return new AddCourierRes { Edid = edid };
        }

        public bool Update(UpdateCourierReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Courier name is required.");

            return _repo.Update(req);
        }
    }
}

