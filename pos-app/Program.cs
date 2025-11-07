using pos_app.Client.Pages;
using pos_app.Components;
using Microsoft.EntityFrameworkCore;
using pos_app.Data;
using pos_app.Services;
using pos_app.Client.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using pos_app.Models;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add API Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "POS API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add Entity Framework
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add custom services (server-side)
builder.Services.AddScoped<pos_app.Services.AuthService>();
builder.Services.AddScoped<DataAccessService>();

// Add Client Database EF services
builder.Services.AddScoped<ClientDbContextFactory>();
builder.Services.AddScoped<ClientDataService>();

// Add HttpClient for client-side services (needed for host-side rendering)
builder.Services.AddScoped<HttpClient>(sp =>
{
    var httpClient = new HttpClient();
    var request = sp.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.Request;
    if (request != null)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
        httpClient.BaseAddress = new Uri(baseUrl);
    }
    return httpClient;
});
builder.Services.AddHttpContextAccessor();

// Add client-side services for WebAssembly components
// These need HttpClient, ILogger, and IJSRuntime which are available in the host
builder.Services.AddScoped<pos_app.Client.Services.AuthService>();
builder.Services.AddScoped<pos_app.Client.Services.DataService>();
builder.Services.AddScoped<pos_app.Client.Services.SuperAdminService>();
builder.Services.AddScoped<pos_app.Client.Services.AuthenticationStateService>();
builder.Services.AddScoped<pos_app.Client.Services.SessionService>();

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "POS-API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "POS-Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Add CORS for WebAssembly client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.WithOrigins("https://softxonepk.com", "http://softxonepk.com", "http://localhost:5000", "https://localhost:5001", "http://localhost:5273", "https://localhost:5273")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure globalization for PKR currency
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-PK") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-PK");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Use CORS
app.UseCors("AllowBlazorWasm");

// Use localization
app.UseRequestLocalization();

// Ensure data directory exists for persistent storage
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        // Extract directory path from connection string
        var dbPath = connectionString.Replace("Data Source=", "").Trim();
        var dataDir = Path.GetDirectoryName(dbPath);
        
        // Handle relative paths - resolve to application root
        if (string.IsNullOrEmpty(dataDir) || dataDir == ".")
        {
            dataDir = Path.Combine(app.Environment.ContentRootPath, "App_Data");
        }
        else if (!Path.IsPathRooted(dataDir))
        {
            // Relative path - make it relative to ContentRootPath
            dataDir = Path.Combine(app.Environment.ContentRootPath, dataDir);
        }
        
        logger.LogInformation("Database directory: {DataDir}", dataDir);
        
        if (!Directory.Exists(dataDir))
        {
            try
            {
                Directory.CreateDirectory(dataDir);
                logger.LogInformation("Created data directory: {DataDir}", dataDir);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create data directory: {DataDir}", dataDir);
                throw;
            }
        }
        
        // Verify write permissions by attempting to create a test file
        var testFile = Path.Combine(dataDir, ".write-test");
        try
        {
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            logger.LogInformation("Write permissions verified for: {DataDir}", dataDir);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Write permissions test failed for: {DataDir}", dataDir);
            throw;
        }
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "CRITICAL: Failed to set up data directory. Application cannot start.");
    throw;
}

// Ensure database is created and seeded
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        var scopeLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        scopeLogger.LogInformation("Creating database tables...");
        var created = await context.Database.EnsureCreatedAsync();
        scopeLogger.LogInformation("Database created: {Created}", created);
        
        // Seed Super Admin if none exists
        await SeedSuperAdminAsync(context, scopeLogger);
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "CRITICAL: Failed to initialize database. Application cannot start.");
    throw;
}

app.UseAuthentication();
app.UseAuthorization();

// Map Blazor WebAssembly first (before API controllers to avoid route conflicts)
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(pos_app.Client._Imports).Assembly);

// Map API controllers (with /api prefix to avoid conflicts)
app.MapControllers();

app.Run();

// Seed Super Admin method
static async Task SeedSuperAdminAsync(MasterDbContext context, ILogger logger)
{
    try
    {
        // Check if any Super Admin exists
        var existingSuperAdmin = await context.SuperAdmins.FirstOrDefaultAsync();
        
        if (existingSuperAdmin == null)
        {
            logger.LogInformation("No Super Admin found. Creating default Super Admin...");
            
            var superAdmin = new SuperAdmin
            {
                Username = "imabdullahsiddiqui@gmail.com",
                Email = "imabdullahsiddiqui@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test1234"),
                FullName = "Super Administrator",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            context.SuperAdmins.Add(superAdmin);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Default Super Admin created successfully");
        }
        else
        {
            logger.LogInformation("Super Admin already exists. Skipping seeding.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding Super Admin");
    }
}
