namespace HoneyErpAPI.Common
{
    public class BaseApiResponse
    {
    }

    public class BaseApiRequest
    {
    }

    public class ApiResponseModel<T> where T : BaseApiResponse, new()
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public ApiResponseModel()
        {
            Success = false;
            Message = string.Empty;
            Data = new T();
        }
    }
}
