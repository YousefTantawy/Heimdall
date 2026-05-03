using HeimdallBackend.Data;
using HeimdallBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONNECT TO DATABASE ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 2. SETUP JWT TOKENS ---
builder.Services.AddScoped<TokenService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddControllers();

// --- 3. SETUP NEW API DOCUMENTATION ---
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 1. Define the Security Scheme
        var scheme = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        // 2. Register it using AddComponent (Critical in v2.1.0 to avoid serialization bugs)
        document.AddComponent("Bearer", scheme);

        // 3. Initialize the Security list if it's null
        document.Security ??= new List<Microsoft.OpenApi.OpenApiSecurityRequirement>();

        // 4. Apply the Requirement using the new typed Reference class
        document.Security.Add(new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// --- 4. START THE APP PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Builds the API map

    // Attaches the new Scalar UI to look at the map
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Heimdall API")
               .WithTheme(ScalarTheme.Mars); // Dark mode theme
    });
}

// Order matters here! Verify who they are, check permissions, map the endpoints.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();