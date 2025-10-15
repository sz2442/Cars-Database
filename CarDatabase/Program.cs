using CarDatabase.Data;
using CarDatabase.Models;
using CarDatabase.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введите JWT токен в формате: Bearer {ваш токен}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// Регистрация Controllers
builder.Services.AddControllers();

// Регистрация DbContext с PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация JwtService
builder.Services.AddScoped<JwtService>();

// ========== НАСТРОЙКА JWT AUTHENTICATION ==========
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new ArgumentNullException("Jwt:SecretKey not found");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ========== НАСТРОЙКА CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// ========== ВАЖНО: ПРАВИЛЬНЫЙ ПОРЯДОК MIDDLEWARE ==========
app.UseCors("AllowAll");           // 1. CORS
app.UseAuthentication();           // 2. Authentication (проверка токена)
app.UseAuthorization();            // 3. Authorization (проверка прав)

// Подключение маршрутизации для Controllers
app.MapControllers();

// ========== ДОБАВЛЕНИЕ ТЕСТОВЫХ ДАННЫХ ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Создаем тестового пользователя, если его нет
    if (!context.Users.Any())
    {
        Console.WriteLine("🔄 Создаем тестовых пользователей...");
        
        var users = new List<User>
        {
            new User
            {
                Username = "user",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = "USER"
            },
            new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "ADMIN"
            }
        };
        
        context.Users.AddRange(users);
        context.SaveChanges();
        
        Console.WriteLine("✅ Тестовые пользователи созданы:");
        Console.WriteLine("   Username: user   | Password: user123  | Role: USER");
        Console.WriteLine("   Username: admin  | Password: admin123 | Role: ADMIN");
    }
    
    // Создаем владельцев и машины, если их нет
    if (!context.Owners.Any())
    {
        Console.WriteLine("🔄 Добавляем тестовые данные...");
        
        var owner1 = new Owner { FirstName = "John", LastName = "Johnson" };
        var owner2 = new Owner { FirstName = "Mary", LastName = "Robinson" };
        
        context.Owners.AddRange(owner1, owner2);
        context.SaveChanges();
        
        var cars = new List<Car>
        {
            new Car { Brand = "Ford", Model = "Mustang", Color = "Red", Year = 2023, Price = 59000, OwnerId = owner1.Id },
            new Car { Brand = "Nissan", Model = "Leaf", Color = "White", Year = 2020, Price = 29000, OwnerId = owner2.Id },
            new Car { Brand = "Toyota", Model = "Prius", Color = "Silver", Year = 2022, Price = 39000, OwnerId = owner2.Id }
        };
        
        context.Cars.AddRange(cars);
        context.SaveChanges();
        
        Console.WriteLine("✅ Тестовые данные добавлены!");
    }
}

// ========== MINIMAL API ENDPOINTS (фильтры) ==========

// Эти endpoints теперь ТОЖЕ защищены! Нужен токен!
app.MapGet("/cars", async (ApplicationDbContext db) =>
    {
        return await db.Cars
            .Include(c => c.Owner)
            .Select(c => new
            {
                c.Id,
                c.Brand,
                c.Model,
                c.Color,
                c.Year,
                c.Price,
                Owner = new
                {
                    c.Owner.Id,
                    c.Owner.FirstName,
                    c.Owner.LastName
                }
            })
            .ToListAsync();
    })
    .RequireAuthorization() // ← ТРЕБУЕТ АУТЕНТИФИКАЦИЮ!
    .WithName("GetCars")
    .WithOpenApi();

app.MapGet("/owners", async (ApplicationDbContext db) =>
    {
        return await db.Owners
            .Include(o => o.Cars)
            .Select(o => new
            {
                o.Id,
                o.FirstName,
                o.LastName,
                Cars = o.Cars.Select(c => new
                {
                    c.Id,
                    c.Brand,
                    c.Model,
                    c.Color,
                    c.Year,
                    c.Price
                })
            })
            .ToListAsync();
    })
    .RequireAuthorization() // ← ТРЕБУЕТ АУТЕНТИФИКАЦИЮ!
    .WithName("GetOwners")
    .WithOpenApi();

// Фильтры тоже защищены
app.MapGet("/cars/brand/{brand}", async (string brand, ApplicationDbContext db) =>
    {
        var cars = await db.Cars
            .Include(c => c.Owner)
            .Where(c => c.Brand == brand)
            .Select(c => new
            {
                c.Id,
                c.Brand,
                c.Model,
                c.Color,
                c.Year,
                c.Price,
                Owner = new { c.Owner.Id, c.Owner.FirstName, c.Owner.LastName }
            })
            .ToListAsync();
        
        return cars.Any() ? Results.Ok(cars) : Results.NotFound($"Машины бренда '{brand}' не найдены");
    })
    .RequireAuthorization()
    .WithName("GetCarsByBrand")
    .WithOpenApi();

app.Run();