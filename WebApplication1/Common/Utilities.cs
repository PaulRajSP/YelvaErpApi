using System;

namespace HoneyErpAPI.Common
{
    public static class Utilities
    {
        public static object DbValue<T>(T? value) where T : struct
        {
            return value.HasValue && value.Value is bool
                ? value.Value
                : value.HasValue && Convert.ToDecimal(value.Value) > 0 ? value.Value : DBNull.Value;
        }

        public static object DbValue(DateTime? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        public static string GeneratePublicId(string prefix, int length)
        {
            return $"{prefix}{Random.Shared.Next((int)Math.Pow(10, length - 1), (int)Math.Pow(10, length) - 1)}";
        }

        public static object DbValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : (object)value;
        }

        public static string GenerateRandomNumber(int length)
        {
            if (length <= 0)
                throw new HoneyErpException(ErrorCode.InternalServerError,"Length must be greater than 0.");

            Random random = new Random();

            int min = (int)Math.Pow(10, length - 1);
            int max = (int)Math.Pow(10, length) - 1;

            return random.Next(min, max + 1).ToString();
        }


        public static T? ParseEnum<T>(string? value) where T : struct, Enum
        {
            if (Enum.TryParse<T>(value, true, out T result) && Enum.IsDefined(typeof(T), result))
            {
                return result;
            }

            return null;
        }


        public static string? CleanMobileNumber(string? mobile)
        {
            return string.IsNullOrWhiteSpace(mobile) ? null : mobile.Trim();
        }
    }
}
