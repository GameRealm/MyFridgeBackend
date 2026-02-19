using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using myFridge.Services;
using myFridge.Services.Interfaces;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddHttpClient<IAuthService, AuthService>();
builder.Services.AddHttpClient<IProductService, ProductService>();
builder.Services.AddHttpClient<IStoragePlaceService, StoragePlaceService>();
builder.Services.AddHttpClient<IUserService, UserService>();
builder.Services.AddHttpClient<IImageAnalysisService, ImageAnalysisService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyFridge API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Вставте токен (БЕЗ слова Bearer)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSecret = builder.Configuration["SUPABASE_JWT_SECRET"];

if (string.IsNullOrEmpty(jwtSecret))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("❌ CRITICAL: SUPABASE_JWT_SECRET is missing!");
    Console.ResetColor();
    throw new Exception("JWT Secret is missing!");
}
else
{
    Console.WriteLine($"ℹ️ JWT Secret found. Length: {jwtSecret.Length}");
}

byte[] GetSecretBytes(string secret)
{
    try
    {
        var b = Convert.FromBase64String(secret);
        Console.WriteLine("ℹ️ Secret treated as BASE64.");
        return b;
    }
    catch
    {
        var b = Encoding.UTF8.GetBytes(secret);
        Console.WriteLine("ℹ️ Secret treated as UTF8 String.");
        return b;
    }
}

var secretBytes = GetSecretBytes(jwtSecret);

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services
.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{

    options.UseSecurityTokenValidators = true;

    options.MapInboundClaims = false; 

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = false,

        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        SignatureValidator = delegate (string token, TokenValidationParameters parameters)
        {
            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token);
            return jwt;
        }
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ TOKEN ACCEPTED (Signature Check Skipped)");

            var userId = context.Principal?.FindFirst("sub")?.Value;
            Console.WriteLine($"   User ID: {userId}");

            Console.ResetColor();
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"🔥🔥 AUTH FAILED: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "API is running. Use /swagger to test.");

app.MapGet("/debug-auth", (ClaimsPrincipal user) =>
{
    if (user.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var info = new
    {
        Message = "You are authorized!",
        Name = user.Identity.Name,
        Claims = user.Claims.Select(c => new { c.Type, c.Value })
    };
    return Results.Ok(info);
}).RequireAuthorization(); 

app.Run();