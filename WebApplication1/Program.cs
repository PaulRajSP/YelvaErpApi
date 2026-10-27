using HoneyErpAPI.Common;
using HoneyErpAPI.Repository;
using HoneyErpAPI.Services;
using HoneyErpAPI.DocumentHandlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Allow up to 100 MB for file uploads at the Kestrel level.
// The upload endpoint also has [DisableRequestSizeLimit] for per-endpoint override.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

// =========================================================
// Logging
// =========================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// =========================================================
// JWT Configuration
// =========================================================

#region JWT Configuration

var jwtKey      = builder.Configuration["Jwt:Key"];
var jwtIssuer   = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT configuration is missing in appsettings.json");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = signingKey,
        ValidateIssuer           = true,
        ValidIssuer              = jwtIssuer,
        ValidateAudience         = true,
        ValidAudience            = jwtAudience,
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.FromMinutes(5),
        RequireExpirationTime    = true,
        RequireSignedTokens      = true,
        NameClaimType            = ClaimTypes.NameIdentifier,
        RoleClaimType            = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            // 1. Check cookie first
            if (context.Request.Cookies.TryGetValue("AuthToken", out var tokenFromCookie))
            {
                context.Token = tokenFromCookie;
                logger.LogInformation("Token found in cookie, length: {Length}", tokenFromCookie?.Length ?? 0);
                return Task.CompletedTask;
            }

            // 2. Fall back to Authorization header
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var token = authHeader.ToString();
                if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = token.Substring("Bearer ".Length).Trim();
                    logger.LogInformation("Token found in Authorization header");
                    return Task.CompletedTask;
                }
            }

            logger.LogWarning("No token found in cookie or Authorization header");
            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validated for: {User}",
                context.Principal?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown");
            return Task.CompletedTask;
        }
    };
});

#endregion

// =========================================================
// Services & Dependency Injection
// =========================================================

#region Services

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// .NET 10 built-in OpenAPI (replaces Swashbuckle)
builder.Services.AddOpenApi();

// HTTP Context Accessor for accessing claims from JWT
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<CurrentUserService>();

// ADO.NET DbClient (same as EMS pattern)
builder.Services.AddScoped<DbClient>();

// Register Repositories
builder.Services.AddScoped<IUnitRepository,        UnitRepository>();
builder.Services.AddScoped<IProductRepository,     ProductRepository>();
builder.Services.AddScoped<IRawMaterialRepository, RawMaterialRepository>();
builder.Services.AddScoped<IcustomerRepository,    customerRepository>();
builder.Services.AddScoped<IOrderRepository,       OrderRepository>();
builder.Services.AddScoped<ISupplierRepository,    SupplierRepository>();
builder.Services.AddScoped<IPurchaseRepository,    PurchaseRepository>();
builder.Services.AddScoped<IBatchRepository,       BatchRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICommonRepository,      CommonRepository>();
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IProductionRequestRepository,  ProductionRequestRepository>();
builder.Services.AddScoped<IOrderPaymentRepository,       OrderPaymentRepository>();
builder.Services.AddScoped<ICourierRepository,           CourierRepository>();
builder.Services.AddScoped<IExpenseRepository,           ExpenseRepository>();
builder.Services.AddScoped<IEmployeeRepository,          EmployeeRepository>();
builder.Services.AddScoped<IDashboardRepository,         DashboardRepository>();

// Register Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<UnitService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<RawMaterialService>();
builder.Services.AddScoped<customerService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddScoped<BatchService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<CommonService>();
builder.Services.AddScoped<PaymentTransactionService>();
builder.Services.AddScoped<ProductionRequestService>();
builder.Services.AddScoped<OrderPaymentService>();
builder.Services.AddScoped<CourierService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<DashboardService>();

// Register Document Handlers (same as EMS pattern)
builder.Services.AddScoped<HoneyErpAPI.Services.DocumentService>();
builder.Services.AddScoped<HoneyErpAPI.DocumentHandlers.DocumentHandlerFactory>();
builder.Services.AddScoped<HoneyErpAPI.DocumentHandlers.DeliverySlip.DeliverySlipHandler>();
builder.Services.AddScoped<HoneyErpAPI.DocumentHandlers.Invoice.InvoiceHandler>();
builder.Services.AddScoped<HoneyErpAPI.DocumentHandlers.OrderReceipt.OrderReceiptHandler>();
builder.Services.AddScoped<HoneyErpAPI.DocumentHandlers.ConfirmedOrders.ConfirmedOrdersHandler>();

#endregion

// =========================================================
// CORS
// =========================================================

#region CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.SetIsOriginAllowed(origin => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");
        });
});

#endregion

var app = builder.Build();

// =========================================================
// Middleware Pipeline
// =========================================================

#region Middleware Pipeline

// Global exception handler — controllers throw, middleware catches
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    // .NET 10 OpenAPI endpoint
    app.MapOpenApi();

    // Scalar API UI (works with .NET 10) — visit /scalar/v1
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "Honey ERP API v1"); });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

#endregion

app.Run();

