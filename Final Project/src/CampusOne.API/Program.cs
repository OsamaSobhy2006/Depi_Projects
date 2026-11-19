using CampusOne.API;
using CampusOne.Application.Interfaces;
using CampusOne.Application.Interfaces.AI;
using CampusOne.Application.Interfaces.UniversityServices;
using CampusOne.Infrastructure.AI;
using CampusOne.Infrastructure.Identity;
using CampusOne.Infrastructure.Persistence;
using CampusOne.Infrastructure.Persistence.Seeders;
using CampusOne.Infrastructure.Repositories.UniversityServices;
using CampusOne.Infrastructure.Services;
using CampusOne.Services.AI;
using CampusOne.Services.UniversityServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Register services
builder.Services.AddControllers();

// University Services Repositories
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<ServiceRequestService>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddHttpClient<LlmService>();
builder.Services.AddScoped<IAiIntentClassifier>(sp => sp.GetRequiredService<LlmService>());
builder.Services.AddSingleton<EmbeddingStore>();
builder.Services.AddSingleton<SimilarityService>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IAiOrchestrator, AiOrchestrator>();
builder.Services.AddScoped<IAiConversationService, AiConversationService>();
builder.Services.AddScoped<IProfilePictureStorage, LocalProfilePictureStorage>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

// 2. Configure SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Configure ASP.NET Core Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 4. Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is missing");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is missing");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is missing");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await UniversityServicesSeeder.SeedAsync(context);
}




// 5. Configure Swagger in Development
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var email = "Eman@gmail.com";

    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Osama",
            LastName = "Sobhy",
            Department = "Computer Science",
            AcademicYear = "3rd Year",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(
            user,
            "Osama@123"
        );

        if (result.Succeeded)
        {
            Console.WriteLine("User created successfully.");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error.Description);
            }
        }
    }
    else
    {
        Console.WriteLine("User already exists.");
    }
}

// 6. Configure HTTP pipeline
app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();