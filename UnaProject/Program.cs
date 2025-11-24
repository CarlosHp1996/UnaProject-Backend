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
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Security;
using UnaProject.Infra.Data;
using UnaProject.Infra.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ===== FUNÇÃO PARA CONVERTER DATABASE_URL =====
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
        Console.WriteLine($"❌ Erro ao converter DATABASE_URL: {ex.Message}");
        return databaseUrl; // Retorna original se falhar
    }
}

// ===== FUNÇÃO PARA CONVERTER REDIS_URL =====
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
        Console.WriteLine($"❌ Erro ao converter REDIS_URL: {ex.Message}");
        return "localhost:6379";
    }
}

// ===== CONFIGURAÇÃO DE CONNECTION STRING =====
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    connectionString = ConvertDatabaseUrl(databaseUrl);
    Console.WriteLine("🔗 Usando DATABASE_URL do Railway");
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                      throw new InvalidOperationException("Connection string not found!");
    Console.WriteLine("🔗 Usando connection string local");
}

Console.WriteLine($"🔗 Connection String: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
// ===== FIM CONFIGURAÇÃO CONNECTION STRING =====

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

// ===== CONFIGURAÇÃO DO DBCONTEXT =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    }));
// ===== FIM CONFIGURAÇÃO DBCONTEXT =====

// ===== CONFIGURAÇÃO REDIS =====
//var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
//string redisConnectionString;

//if (!string.IsNullOrEmpty(redisUrl))
//{
//    redisConnectionString = ConvertRedisUrl(redisUrl);
//    Console.WriteLine("🔗 Usando REDIS_URL do Railway");
//}
//else
//{
//    redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
//    Console.WriteLine("🔗 Usando Redis connection string local");
//}

//Console.WriteLine($"🔗 Redis Connection: {redisConnectionString}");

//try
//{
//    builder.Services.AddStackExchangeRedisCache(options =>
//    {
//        options.Configuration = redisConnectionString;
//        options.InstanceName = "Una";
//    });
//    builder.Services.AddScoped<ICartService, CartService>();
//    Console.WriteLine("✅ Redis configurado com sucesso!");
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"❌ Erro ao configurar Redis: {ex.Message}");
//    Console.WriteLine("⚠️ Usando cache em memória como fallback...");
//    builder.Services.AddMemoryCache();
//    builder.Services.AddScoped<ICartService, CartService>();
//}
// ===== FIM CONFIGURAÇÃO REDIS =====

// Registrar outros serviços
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICodeGeneratorService, CodeGeneratorService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient();

//PRODUÇÃO
// File Storage Service
builder.Services.AddScoped<IFileStorageService>(provider =>
    new FileStorageService("/app/ImagensBackend"));
//DESENVOLVIMENTO
//builder.Services.AddScoped<IFileStorageService>(provider =>
//    new FileStorageService(
//        @"C:\Users\Carlos Henrique\Desktop\PROJETOS\una-estudio-criativo\ImagensBackend"
//    ));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUrlHelperService, UrlHelperService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
//builder.Services.AddStripeServices(builder.Configuration);

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://unaestudiocriativo.com.br",
            "https://www.unaestudiocriativo.com.br",
            //URLS PARA TESTAR FRONTEND EM AMBIENTE DEV
            "http://127.0.0.1:5502",
            "http://localhost:5502"
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
                    context.Fail("Este token foi invalidado.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro na validação do token: {ex.Message}");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
// ===== FIM JWT =====

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
// ===== FIM SWAGGER =====

var app = builder.Build();

// ===== TESTE DE CONEXÃO REDIS =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await cache.SetStringAsync("test-connection", "Redis conectado!", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        }, cts.Token);

        var testValue = await cache.GetStringAsync("test-connection", cts.Token);

        if (testValue != null)
        {
            Console.WriteLine("✅ Redis conectado com sucesso!");
        }
        else
        {
            Console.WriteLine("❌ Redis teste falhou");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao testar Redis: {ex.Message}");
        Console.WriteLine("⚠️ Aplicação continuará sem Redis Cache...");
    }
}
// ===== FIM TESTE REDIS =====

// ===== APLICAR MIGRATIONS E INICIALIZAR BANCO =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        Console.WriteLine("🔄 Verificando conexão com o banco de dados...");
        var context = services.GetRequiredService<AppDbContext>();

        // Testar conexão com timeout maior
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // Tentar conectar várias vezes
        bool connected = false;
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                Console.WriteLine($"🔄 Tentativa {attempt} de conexão...");
                connected = await context.Database.CanConnectAsync(cts.Token);
                if (connected) break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Tentativa {attempt} falhou: {ex.Message}");
                if (attempt < 5)
                {
                    await Task.Delay(5000, cts.Token); // Aguarda 5 segundos antes da próxima tentativa
                }
            }
        }

        if (!connected)
        {
            throw new InvalidOperationException("Não foi possível conectar ao banco de dados após 5 tentativas");
        }

        Console.WriteLine("✅ Conexão com banco de dados estabelecida!");

        // Aplicar migrations
        Console.WriteLine("🔄 Aplicando migrations...");
        await context.Database.MigrateAsync(cts.Token);
        Console.WriteLine("✅ Migrations aplicadas com sucesso!");

        // Seed de dados
        // await SeedData.Initialize(context);
        // Console.WriteLine("✅ Dados iniciais configurados!");

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Erro crítico ao inicializar o banco de dados");
        Console.WriteLine($"❌ ERRO CRÍTICO: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.WriteLine($"❌ ERRO INTERNO: {ex.InnerException.Message}");
        }

        // Em produção, vamos tentar continuar sem o banco para debug
        Console.WriteLine("⚠️ Continuando sem banco de dados inicializado...");
        // throw; // Descomente em produção se quiser parar aqui
    }
}
// ===== FIM MIGRATIONS =====

// ===== SEED DE USUÁRIOS =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        Console.WriteLine("🔄 Inicializando usuários do sistema...");

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Criar roles
        string[] roleNames = { "User", "Admin" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
                Console.WriteLine($"✅ Role '{roleName}' criada!");
            }
        }

        // Criar usuário Admin Carlos
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
                Console.WriteLine("✅ Admin Carlos criado!");
            }
        }

        // Criar usuário Admin Rivael
        var adminUserRivael = await userManager.FindByEmailAsync("rivaelrocha@icloud.com");
        if (adminUserRivael == null)
        {
            adminUserRivael = new ApplicationUser
            {
                UserName = "RivaelAdmin",
                Email = "rivaelrocha@icloud.com",
                EmailConfirmed = true
            };

            var resultRivael = await userManager.CreateAsync(adminUserRivael, "@Rivael123");
            if (resultRivael.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUserRivael, "Admin");
                Console.WriteLine("✅ Usuário Admin Rivael criado com sucesso!");
            }
            else
            {
                Console.WriteLine($"❌ Erro ao criar usuário Admin Rivael: {string.Join(", ", resultRivael.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine("ℹ️  Usuário Admin Rivael já existe.");
        }

        // Criar usuários Admin adicionais para teste
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
                    Console.WriteLine($"✅ Usuário {userName} criado com sucesso!");
                }
                else
                {
                    Console.WriteLine($"❌ Erro ao criar {userName}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine($"ℹ️ Usuário {userName} já existe.");
            }
        }

        Console.WriteLine("✅ Usuários inicializados!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao criar usuários: {ex.Message}");
    }
}
// ===== FIM SEED =====

// ===== CONFIGURAÇÃO DE ARQUIVOS ESTÁTICOS =====

//PRODUÇÃO
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider("/app/ImagensBackend"),
//     RequestPath = "/imagens"
// });

//DESENVOLVIMENTO
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
       @"C:\Users\Carlos Henrique\Desktop\PROJETOS\una-estudio-criativo\ImagensBackend"),
    RequestPath = ""
});

// ===== FIM ARQUIVOS ESTÁTICOS =====

// ===== PIPELINE DE MIDDLEWARE =====
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
// ===== FIM PIPELINE =====

Console.WriteLine("Aplicação iniciada com sucesso!");
Console.WriteLine($"Swagger disponível em: /swagger");

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