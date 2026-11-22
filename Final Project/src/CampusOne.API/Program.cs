using CampusOne.Api.Hubs;
using CampusOne.API;
using CampusOne.API.Hubs;
using CampusOne.Application.Interfaces;
using CampusOne.Application.Interfaces.AI;
using CampusOne.Application.Interfaces.Emergency;
using CampusOne.Application.Interfaces.UniversityServices;
using CampusOne.Application.Services;
using CampusOne.Infrastructure.AI;
using CampusOne.Infrastructure.Identity;
using CampusOne.Infrastructure.Persistence;
using CampusOne.Infrastructure.Persistence.Seeders;
using CampusOne.Infrastructure.Repositories.Emergency;
using CampusOne.Infrastructure.Repositories.UniversityServices;
using CampusOne.Infrastructure.Services;
using CampusOne.Services.AI;
using CampusOne.Services.Chat;
using CampusOne.Services.Emergency;
using CampusOne.Services.UniversityServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Register services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// University Services
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<ServiceRequestService>();

// SignalR
builder.Services.AddSignalR();

// AI Services
builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddHttpClient<LlmService>();

builder.Services.AddScoped<IAiIntentClassifier>(
    sp => sp.GetRequiredService<LlmService>());

builder.Services.AddSingleton<EmbeddingStore>();
builder.Services.AddSingleton<SimilarityService>();

builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IAiOrchestrator, AiOrchestrator>();
builder.Services.AddScoped<IAiConversationService, AiConversationService>();

// Profile and Chat Services
builder.Services.AddScoped<IProfilePictureStorage, LocalProfilePictureStorage>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IStudyGroupService, StudyGroupService>();

builder.Services.AddScoped<IEmergencyRepository, EmergencyRepository>();
builder.Services.AddScoped<IEmergencyTriageService, EmergencyTriageService>();
builder.Services.AddScoped<IEmergencyService, EmergencyService>();

// 2. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 3. Configure Swagger
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

// 4. Configure SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// 5. Configure ASP.NET Core Identity
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

// 6. Configure JWT
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

        // SignalR JWT Authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/chat") ||
                     path.StartsWithSegments("/hubs/study-room") ||
                     path.StartsWithSegments("/hubs/emergency")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// 7. Build Application
var app = builder.Build();

// 8. Configure Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CampusOne API v1");
        c.RoutePrefix = "swagger";
    });
}

// 9. Configure Middleware
app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// 10. Map Controllers
app.MapControllers();

// 11. Map SignalR Hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<StudyRoomHub>("/hubs/study-room");
app.MapHub<EmergencyHub>("/hubs/emergency");

app.Run();