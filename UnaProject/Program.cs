using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Services;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Application.Extensions;
using UnaProject.Application.Services.Background;
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Security;
using UnaProject.Infra.Data;
using UnaProject.Infra.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ===== FUNCTION TO CONVERT DATABASE_URL =====
string ConvertDatabaseUrl(string databaseUrl)
{
    if (string.IsNullOrEmpty(databaseUrl))
        return string.Empty;

    try
    {
        var uri = new Uri(databaseUrl);
        var host = uri.Host;
        var port = uri.Port;
        var database = uri.AbsolutePath.Trim('/');
        var username = uri.UserInfo.Split(':')[0];
        var password = uri.UserInfo.Split(':')[1];

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting DATABASE_URL: {ex.Message}");
        return databaseUrl; // Returns original if failed
    }
}

// ===== CONNECTION STRING CONFIGURATION =====
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    connectionString = ConvertDatabaseUrl(databaseUrl);
    Console.WriteLine("Using DATABASE_URL from Railway");
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                      throw new InvalidOperationException("Connection string not found!");
    Console.WriteLine("Using local connection string");
}

// Não logar a connection string (contém credenciais). Log apenas o host, sem usuário/senha.
try
{
    var csBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
    Console.WriteLine($"Database host: {csBuilder.Host}:{csBuilder.Port}/{csBuilder.Database}");
}
catch
{
    // se não conseguir parsear, não imprime nada sensível
}
// ===== END CONNECTION STRING CONFIGURATION =====

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ===== DISTRIBUTED CACHE (Redis com fallback para memória) =====
// Converte uma REDIS_URL no formato redis://[:password@]host:port (Railway/Heroku) para a
// connection string do StackExchange.Redis (host:port,password=...,ssl=...).
static string ConvertRedisUrl(string redisUrl)
{
    if (string.IsNullOrWhiteSpace(redisUrl))
        return string.Empty;

    try
    {
        var uri = new Uri(redisUrl);
        var port = uri.Port > 0 ? uri.Port : 6379;
        var config = $"{uri.Host}:{port}";

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':');
            var password = parts.Length > 1 ? parts[1] : parts[0];
            if (!string.IsNullOrEmpty(password))
                config += $",password={password}";
        }

        if (redisUrl.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            config += ",ssl=true";

        return config;
    }
    catch
    {
        return redisUrl; // deixa o StackExchange.Redis tentar interpretar direto
    }
}

var redisUrlEnv = Environment.GetEnvironmentVariable("REDIS_URL");
var redisConnectionString = !string.IsNullOrWhiteSpace(redisUrlEnv)
    ? ConvertRedisUrl(redisUrlEnv)
    : (builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");

// Testa a conexão no startup SEM travar o boot: AbortOnConnectFail=false + ConnectTimeout curto.
// Se o Redis não conectar, cai no cache em memória (AddDistributedMemoryCache) e o app sobe normalmente.
bool redisAvailable = false;
try
{
    var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
    redisOptions.AbortOnConnectFail = false;
    redisOptions.ConnectTimeout = 3000;
    redisOptions.ConnectRetry = 1;
    using var testConnection = ConnectionMultiplexer.Connect(redisOptions);
    redisAvailable = testConnection.IsConnected;
}
catch (Exception ex)
{
    Console.WriteLine($"AVISO: falha ao conectar no Redis ({ex.Message}).");
}

if (redisAvailable)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Una";
    });
    Console.WriteLine("Cache distribuído: Redis conectado.");
}
else
{
    builder.Services.AddDistributedMemoryCache();
    Console.WriteLine("AVISO: Redis indisponível — usando cache em memória (AddDistributedMemoryCache) como fallback.");
}
// ===== END DISTRIBUTED CACHE =====

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddScoped<AccessManager>();
// Blacklist de token JWT via IDistributedCache (Redis quando disponível, memória no fallback).
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
// AutoMapper 15: o overload só-assemblies foi removido; usar AddMaps para escanear os assemblies.
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

// ===== DBCONTEXT CONFIGURATION =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    }));
// ===== END DBCONTEXT CONFIGURATION =====

// Register other services
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISocialAuthService, SocialAuthService>();
builder.Services.AddScoped<ISocialTokenValidator, SocialTokenValidator>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddHttpClient();

// AbacatePay Configuration — DESATIVADO na migração para Stripe (mantido no código por
// reversibilidade; ver UnaProject.Application/Extensions/AbacatePayServiceExtensions.cs).
// builder.Services.AddAbacatePay(builder.Configuration);
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IWebhookRetryService, WebhookRetryService>();
builder.Services.AddScoped<IPaymentNotificationService, PaymentNotificationService>();

// Notification Options Configuration
builder.Services.Configure<NotificationOptions>(options =>
{
    options.AdminEmail = builder.Configuration["Notifications:AdminEmail"] ?? "admin@unaestudiocriativo.com.br";
    options.EnableCustomerNotifications = builder.Configuration.GetValue<bool>("Notifications:EnableCustomerNotifications", true);
    options.EnableAdminNotifications = builder.Configuration.GetValue<bool>("Notifications:EnableAdminNotifications", true);
    options.CompanyName = builder.Configuration["Notifications:CompanyName"] ?? "Una Estúdio Criativo";
    options.SupportEmail = builder.Configuration["Notifications:SupportEmail"] ?? "suporte@unaestudiocriativo.com.br";
});

// Webhook Retry Background Service Configuration
builder.Services.Configure<WebhookRetryBackgroundOptions>(
    builder.Configuration.GetSection(WebhookRetryBackgroundOptions.SectionName));

var enableWebhookRetryBackground = builder.Configuration.GetValue<bool>("WebhookRetryBackground:Enabled", true);
if (enableWebhookRetryBackground)
{
    builder.Services.AddHostedService<WebhookRetryBackgroundService>();
}

// File Storage Service — caminho vem da configuração (FileStorage:LocalImagesPath);
// fallback para uma pasta "ImagensBackend" ao lado do ContentRoot.
var imagesPath = builder.Configuration["FileStorage:LocalImagesPath"];
if (string.IsNullOrWhiteSpace(imagesPath))
    imagesPath = Path.Combine(builder.Environment.ContentRootPath, "ImagensBackend");
Directory.CreateDirectory(imagesPath);
builder.Services.AddScoped<IFileStorageService>(provider =>
    new FileStorageService(imagesPath));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUrlHelperService, UrlHelperService>();
//builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
//builder.Services.AddStripeServices(builder.Configuration);

// ===== STRIPE CONFIGURATION (gateway ativo de pagamentos) =====
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddScoped<StripeGateway>();
builder.Services.AddScoped<IPaymentGatewayResolver>(sp =>
{
    var stripeGateway = sp.GetRequiredService<StripeGateway>();

    var methodMap = new Dictionary<string, IPaymentGateway>(StringComparer.OrdinalIgnoreCase)
    {
        ["card"] = stripeGateway,
        ["pix"] = stripeGateway
    };

    var providerMap = new Dictionary<string, IPaymentGateway>(StringComparer.OrdinalIgnoreCase)
    {
        ["stripe"] = stripeGateway
    };

    return new PaymentGatewayResolver(methodMap, providerMap);
});
// ===== END STRIPE CONFIGURATION =====

// CORS Configuration
// Produção: apenas os domínios https oficiais. As origens localhost/127.0.0.1 só entram
// em Development. AllowCredentials continua restrito a origens específicas (sem AllowAnyOrigin).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = new List<string>
        {
            "https://unaestudiocriativo.com.br",
            "https://www.unaestudiocriativo.com.br"
        };

        if (builder.Environment.IsDevelopment())
        {
            allowedOrigins.AddRange(new[]
            {
                "http://127.0.0.1:5502",
                "http://localhost:5502",
                "http://localhost:3000",    // React dev
                "http://localhost:5173",    // Vite dev
                "https://localhost:5173"
            });
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ===== IDENTITY CONFIGURATION =====
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
// ===== FIM IDENTITY =====

// ===== JWT CONFIGURATION =====
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrWhiteSpace(jwtKey))
    jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Chave JWT não configurada (defina a variável de ambiente JWT_KEY ou a seção Jwt:Key).");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ??
                builder.Configuration["Jwt:Issuer"] ??
                "Una";

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ??
                  builder.Configuration["Jwt:Audience"] ??
                  "Una";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            try
            {
                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                var token = context.SecurityToken as JwtSecurityToken;
                if (token != null && await blacklist.IsBlacklistedAsync(token.RawData, context.HttpContext.RequestAborted))
                {
                    context.Fail("This token has been invalidated..");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
            }
        }
    };
});

builder.Services.AddAuthorization();
// ===== FIM JWT =====

// ===== SOCIAL AUTH CONFIGURATION =====
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.CallbackPath = "/auth/social/google/callback";
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = true;
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
        options.CallbackPath = "/auth/social/facebook/callback";
        options.Scope.Add("email");
        options.Scope.Add("public_profile");
        options.SaveTokens = true;
    });
// ===== END SOCIAL AUTH =====

// ===== SWAGGER CONFIGURATION =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Una API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    c.EnableAnnotations();
});
// ===== END SWAGGER =====

var app = builder.Build();

// Conexão do Redis já foi testada no registro do cache (com fallback para memória).

// ===== APPLY MIGRATIONS AND INITIALIZE DATABASE =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        Console.WriteLine("Checking connection to the database...");
        var context = services.GetRequiredService<AppDbContext>();

        // Test connection with a longer timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // Try connecting multiple times
        bool connected = false;
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                Console.WriteLine($"Attempt {attempt} to connect...");
                connected = await context.Database.CanConnectAsync(cts.Token);
                if (connected) break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                if (attempt < 5)
                {
                    await Task.Delay(5000, cts.Token); // Wait 5 seconds before the next attempt
                }
            }
        }

        if (!connected)
        {
            throw new InvalidOperationException("We were unable to connect to the database after 5 attempts.");
        }

        Console.WriteLine("Database connection established!");

        // Auto-migrate no startup: perigoso em produção com múltiplas instâncias (concorrência).
        // Controlado por Database:AutoMigrate (default = só em Development). Em produção, rodar
        // `dotnet ef database update` no deploy em vez de migrar no boot.
        var autoMigrate = builder.Configuration.GetValue<bool>("Database:AutoMigrate", app.Environment.IsDevelopment());
        if (autoMigrate)
        {
            Console.WriteLine("Applying migrations...");
            await context.Database.MigrateAsync(cts.Token);
            Console.WriteLine("Migrations applied successfully!");
        }
        else
        {
            Console.WriteLine("Auto-migrate desativado (Database:AutoMigrate=false). Aplique as migrations manualmente no deploy.");
        }

        // Seed data
        // await SeedData.Initialize(context);
        // Console.WriteLine("Initial data seeded!");

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Critical error initializing the database");
        Console.WriteLine($"CRITICAL ERROR: {ex.Message}");

        if (ex.InnerException != null)
            Console.WriteLine($"INTERNAL ERROR: {ex.InnerException.Message}");
    }
}
// ===== END MIGRATIONS =====

// ===== USER SEED (apenas em Development e se habilitado em SeedData:Enabled) =====
var seedEnabled = builder.Configuration.GetValue<bool>("SeedData:Enabled", app.Environment.IsDevelopment());
if (app.Environment.IsDevelopment() && seedEnabled)
{
    using var scope = app.Services.CreateScope();
    try
    {
        Console.WriteLine("Inicializando usuários do sistema (seed de desenvolvimento)...");

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Criar roles
        string[] roleNames = { "User", "Admin" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
                Console.WriteLine($"Role '{roleName}' criada!");
            }
        }

        // Admins vêm da configuração (SeedData:Admins) — nunca hardcoded no código.
        var seedAdmins = builder.Configuration.GetSection("SeedData:Admins").Get<List<SeedAdmin>>() ?? new List<SeedAdmin>();
        foreach (var admin in seedAdmins)
        {
            if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
                continue;

            var existing = await userManager.FindByEmailAsync(admin.Email);
            if (existing != null)
            {
                Console.WriteLine($"Admin {admin.Email} já existe.");
                continue;
            }

            var newAdmin = new ApplicationUser
            {
                UserName = string.IsNullOrWhiteSpace(admin.UserName) ? admin.Email : admin.UserName,
                Email = admin.Email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newAdmin, admin.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, "Admin");
                Console.WriteLine($"Admin {admin.Email} criado!");
            }
            else
            {
                Console.WriteLine($"Erro ao criar admin {admin.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        Console.WriteLine("Seed de usuários concluído!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro no seed de usuários: {ex.Message}");
    }
}
// ===== END SEED =====

// ===== STATIC FILE CONFIGURATION =====

//PRODUCTION
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider("/app/ImagensBackend"),
//     RequestPath = "/imagens"
// });

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = ""
});

// ===== END STATIC FILES =====

// ===== MIDDLEWARE PIPELINE =====
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Una API v1");
    c.RoutePrefix = "swagger";
});

//app.UseStripeConfiguration();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
// Nota: os arquivos estáticos já são servidos pelo app.UseStaticFiles(new StaticFileOptions{...})
// configurado acima (RequestPath=""). A chamada parameterless duplicada foi removida (M-5).
app.UseRouting();
app.UseAuthentication();
app.UseSession();
app.UseAuthorization();
app.MapControllers();
// ===== END PIPELINE =====

Console.WriteLine("Application started successfully!");
Console.WriteLine($"Swagger available at: /swagger");

app.Run();

// Modelo usado para ler os administradores de seed da configuração (SeedData:Admins).
public class SeedAdmin
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

// public static class SeedData
// {
//     public static async Task Initialize(AppDbContext context)
//     {
//         try
//         {
//             if (!context.Products.Any())
//             {
//                 Console.WriteLine("🔄 Inserindo produtos de exemplo...");

//                 context.Products.AddRange(
//                     new Product
//                     {
//                         Id = Guid.NewGuid(),
//                         Name = "Whey Protein Concentrado",
//                         Description = "Whey protein de alta qualidade para ganho de massa muscular.",
//                         Price = 120.00m,
//                         StockQuantity = 100,
//                         ImageUrls = new List<string> { "/imagens/whey-protein.jpg" },
//                         IsActive = true,
//                         CreatedAt = DateTime.UtcNow,
//                         UpdatedAt = DateTime.UtcNow
//                     },
//                     new Product
//                     {
//                         Id = Guid.NewGuid(),
//                         Name = "Creatina Monohidratada",
//                         Description = "Suplemento para aumento de força e desempenho.",
//                         Price = 80.00m,
//                         StockQuantity = 150,
//                         ImageUrls = new List<string> { "/imagens/creatina.jpg" },
//                         IsActive = true,
//                         CreatedAt = DateTime.UtcNow,
//                         UpdatedAt = DateTime.UtcNow
//                     }
//                 );
//                 await context.SaveChangesAsync();
//                 Console.WriteLine("✅ Produtos inseridos!");

//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"❌ Erro ao inserir produtos: {ex.Message}");
//         }
//     }
// }