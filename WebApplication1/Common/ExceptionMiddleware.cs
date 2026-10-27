using System.Text.Json;

namespace HoneyErpAPI.Common
{
    // -----------------------------------------------------------------
    // Catches HoneyErpException (business errors) and unhandled exceptions.
    // Controllers do NOT need try-catch - this handles everything.
    // -----------------------------------------------------------------
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (HoneyErpException ex)
            {
                _logger.LogWarning("Business error [{Code}]: {Message}", ex.ErrorCode, ex.Message);

                int statusCode = ex.ErrorCode switch
                {
                    ErrorCode.RequestIsNull  => StatusCodes.Status400BadRequest,
                    ErrorCode.InvalidData    => StatusCodes.Status400BadRequest,
                    ErrorCode.NotFound       => StatusCodes.Status404NotFound,
                    ErrorCode.Unauthorized   => StatusCodes.Status401Unauthorized,
                    ErrorCode.Forbidden      => StatusCodes.Status403Forbidden,
                    ErrorCode.Conflict       => StatusCodes.Status409Conflict,
                    _                        => StatusCodes.Status500InternalServerError
                };

                await TryWriteResponseAsync(context, statusCode, false, ex.Message);
            }
            catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex)
            {
                // Kestrel throws this when the request body exceeds the size limit
                // or the multipart body is malformed. Caught here so we can return
                // a clean 400 instead of a 500, before the connection is fully aborted.
                _logger.LogWarning("Bad HTTP request (possible file-size limit exceeded): {Message}", ex.Message);
                await TryWriteResponseAsync(context, StatusCodes.Status400BadRequest, false, "Request rejected: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await TryWriteResponseAsync(context, StatusCodes.Status500InternalServerError, false,
                    "An internal server error occurred.");
            }
        }

        /// <summary>
        /// Writes a JSON error response only if the response has NOT already started.
        /// If Kestrel already aborted the connection (e.g. body-size limit exceeded),
        /// trying to write headers throws another exception that would crash the server.
        /// </summary>
        private static async Task TryWriteResponseAsync(HttpContext context, int statusCode, bool success, string message)
        {
            if (context.Response.HasStarted)
            {
                // Headers already sent - cannot write another response.
                return;
            }

            try
            {
                context.Response.Clear();
                context.Response.StatusCode  = statusCode;
                context.Response.ContentType = "application/json";

                var response = new { Success = success, Message = message, Data = (object?)null };
                var options  = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
            }
            catch
            {
                // Swallow secondary write errors - connection is already broken.
            }
        }
    }
}
