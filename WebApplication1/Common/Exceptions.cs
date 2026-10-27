namespace HoneyErpAPI.Common
{
    public enum ErrorCode
    {
        RequestIsNull,
        InvalidData,
        NotFound,
        Unauthorized,
        Forbidden,
        Conflict,
        ValidationFailed,
        InternalServerError,
        Failed,
        InternalError
    }

    public class HoneyErpException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public HoneyErpException(ErrorCode errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public HoneyErpException(ErrorCode errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
