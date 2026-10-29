using AttachmentEnum;
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
        private readonly CommonService _commonService;

        public CourierService(ICourierRepository repo, ICurrentUserService currentUser, CommonService commonService)
        {
            _repo = repo;
            _currentUser = currentUser;
            _commonService = commonService;
        }

        public PageResult<CourierListItem> GetAll(GetCourierListReq req) => _repo.GetAll(req);

        public CourierDetailsRes? GetById(string Edid, string CourierName)
        {
            return _repo.GetById(EncryptionHelper.Decrypt<int>(Edid), CourierName);
        }

        public AddCourierRes Add(AddCourierReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Courier name is required.");

            GetCourierListReq listReq = new GetCourierListReq();
            listReq.SearchText = req.Name.Trim();

            PageResult<CourierListItem> listRes = _repo.GetAll(listReq);

            if (listRes.Items != null && listRes.Items.Count > 0)
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Courier name already exists.");

            AddCourierRes res = new AddCourierRes();
            res.Edid = _repo.Create(req);

            if (req.AttachmentData != null && req.AttachmentData.Count > 0)
            {
                _commonService.SetModuleAttachments(MapAttachments(req.AttachmentData, res.Edid));
            }

            return res;
        }

        public UpdateCourierRes Update(UpdateCourierReq req, string courierName)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Courier name is required.");

            int courierId = EncryptionHelper.Decrypt<int>(req.Edid);

            UpdateCourierRes res = _repo.Update(req, courierName);

            if (req.AttachmentData != null)
            {
                var mapped = MapAttachments(req.AttachmentData, req.Edid);
                if (mapped.Count > 0)
                {
                    _commonService.SetModuleAttachments(mapped);
                }
                else
                {
                    _commonService.ClearModuleAttachments("Courier", req.Edid);
                }
            }

            if (!res.Success)
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Courier ID and name do not match.");

            return res;
        }

        private static List<AttachmentModuleReq> MapAttachments(List<CourierAttachmentReq> attachments, string recordEdid)
        {
            return attachments
                .Where(item => !string.IsNullOrWhiteSpace(item.AttachmentEdid))
                .Select(item => new AttachmentModuleReq
                {
                    AttachmentEdid = item.AttachmentEdid,
                    RecordEdid = recordEdid,
                    ModuleName = AttachmentModule.Courier,
                    AttachmentType = item.AttachmentType.ToString(),
                    Remarks = item.Remarks ?? string.Empty
                })
                .ToList();
        }
    }
}
