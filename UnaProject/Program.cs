using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

// ===== FUNCTION TO CONVERT REDIS_URL =====
string ConvertRedisUrl(string redisUrl)
{
    if (string.IsNullOrEmpty(redisUrl))
        return "localhost:6379";

    try
    {
        if (redisUrl.StartsWith("redis://"))
        {
            var uri = new Uri(redisUrl);
            var host = uri.Host;
            var port = uri.Port;
            var password = !string.IsNullOrEmpty(uri.UserInfo) ? uri.UserInfo.Split(':').Last() : null;

            if (!string.IsNullOrEmpty(password))
            {
                return $"{host}:{port},password={password}";
            }
            else
            {
                return $"{host}:{port}";
            }
        }
        return redisUrl;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting REDIS_URL: {ex.Message}");
        return "localhost:6379";
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

Console.WriteLine($"Connection String: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
// ===== END CONNECTION STRING CONFIGURATION =====

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddScoped<AccessManager>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

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

// ===== REDIS CONFIGURATION =====
//var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
//string redisConnectionString;

//if (!string.IsNullOrEmpty(redisUrl))
//{
//    redisConnectionString = ConvertRedisUrl(redisUrl);
//    Console.WriteLine("Using Railway's REDIS_URL");
//}
//else
//{
//    redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
//    Console.WriteLine("Using Redis local connection string");
//}

//Console.WriteLine($"Redis Connection: {redisConnectionString}");

//try
//{
//    builder.Services.AddStackExchangeRedisCache(options =>
//    {
//        options.Configuration = redisConnectionString;
//        options.InstanceName = "Una";
//    });
//    builder.Services.AddScoped<ICartService, CartService>();
//    Console.WriteLine("Redis configured successfully!");
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"Error configuring Redis: {ex.Message}");
//    Console.WriteLine("Using in-memory cache as fallback...");
//    builder.Services.AddMemoryCache();
//    builder.Services.AddScoped<ICartService, CartService>();
//}
// ===== END REDIS CONFIGURATION =====

// Register other services
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISocialAuthService, SocialAuthService>();
builder.Services.AddHttpClient();

// AbacatePay Configuration
builder.Services.AddAbacatePay(builder.Configuration);
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

//PRODUCTION
// File Storage Service
//builder.Services.AddScoped<IFileStorageService>(provider =>
//    new FileStorageService("/app/ImagensBackend"));
//DEVELOPMENT
builder.Services.AddScoped<IFileStorageService>(provider =>
    new FileStorageService(
        @"C:\Users\Carlos Henrique\Desktop\DESKTOP\PROJETOS\una-estudio-criativo\ImagensBackend"
    ));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUrlHelperService, UrlHelperService>();
//builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
//builder.Services.AddStripeServices(builder.Configuration);

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://unaestudiocriativo.com.br",
            "https://www.unaestudiocriativo.com.br",
            //URLs for testing frontends in a development environment.
            "http://127.0.0.1:5502",
            "http://localhost:5502",
            "http://localhost:3000",    // React dev
            "http://localhost:5173"     // Vite dev
        )
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
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 1;
    options.Password.RequiredUniqueChars = 1;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
// ===== FIM IDENTITY =====

// ===== JWT CONFIGURATION =====
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ??
             builder.Configuration["Jwt:Key"] ??
             throw new InvalidOperationException("JWT_KEY environment variable is required");

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
        OnTokenValidated = context =>
        {
            try
            {
                var accessManager = context.HttpContext.RequestServices.GetRequiredService<AccessManager>();
                var token = context.SecurityToken as JwtSecurityToken;
                if (token != null && AccessManager.IsTokenBlacklisted(token.RawData))
                {
                    context.Fail("This token has been invalidated..");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
            }
            return Task.CompletedTask;
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

// ===== REDIS CONNECTION TEST =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await cache.SetStringAsync("test-connection", "Redis connected!", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        }, cts.Token);

        var testValue = await cache.GetStringAsync("test-connection", cts.Token);

        if (testValue != null)
        {
            Console.WriteLine("Redis connected successfully!");
        }
        else
        {
            Console.WriteLine("❌ Redis connection test failed");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error testing Redis: {ex.Message}");
        Console.WriteLine("Application will continue without Redis Cache...");
    }
}
// ===== END REDIS CONNECTION TEST =====

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

        // Apply migrations
        Console.WriteLine("Applying migrations...");
        await context.Database.MigrateAsync(cts.Token);
        Console.WriteLine("Migrations applied successfully!");

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

// ===== USER SEED =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        Console.WriteLine("Inicializando usuários do sistema...");

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

        // Create user Admin Carlos
        var adminUser = await userManager.FindByEmailAsync("carloshpsantos1996@gmail.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "CarlosAdmin",
                Email = "carloshpsantos1996@gmail.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "@Caique123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Admin Carlos created!");
            }
        }

        // Create user Admin Geisa
        var adminUserGeisa = await userManager.FindByEmailAsync("geisaferoli@gmail.com");
        if (adminUserGeisa == null)
        {
            adminUserGeisa = new ApplicationUser
            {
                UserName = "GeisaAdmin",
                Email = "geisaferoli@gmail.com",
                EmailConfirmed = true
            };

            var resultGeisa = await userManager.CreateAsync(adminUserGeisa, "@Geisa123");
            if (resultGeisa.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUserGeisa, "Admin");
                Console.WriteLine("User Admin Geisa created successfully!");
            }
            else
            {
                Console.WriteLine($"Error creating user Admin Geisa: {string.Join(", ", resultGeisa.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine("User Admin Geisa already exists.");
        }

        // Create additional Admin users for testing
        for (int i = 1; i <= 5; i++)
        {
            string userName = $"admin{i}";
            string email = $"admin{i}@gmail.com";
            string password = $"@Admin{i}";

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(newAdmin, password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                    Console.WriteLine($"User {userName} created successfully!");
                }
                else
                {
                    Console.WriteLine($"Error creating {userName}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine($"ℹUser {userName} already exists.");
            }
        }

        Console.WriteLine("Users initialized!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating users: {ex.Message}");
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

//DEVELOPMENT
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
       @"C:\Users\Carlos Henrique\Desktop\DESKTOP\PROJETOS\una-estudio-criativo\ImagensBackend"),
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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseSession();
app.UseAuthorization();
app.MapControllers();
// ===== END PIPELINE =====

Console.WriteLine("Application started successfully!");
Console.WriteLine($"Swagger available at: /swagger");

app.Run();

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
//                         ImageUrl = "/imagens/whey-protein.jpg",
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
//                         ImageUrl = "/imagens/creatina.jpg",
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