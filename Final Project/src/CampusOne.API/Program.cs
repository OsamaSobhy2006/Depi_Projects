using CampusOne.API;
using CampusOne.API.Hubs;
using CampusOne.Infrastructure.Identity;
using CampusOne.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using CampusOne.Application.Interfaces.AI;
using CampusOne.Infrastructure.AI;
using CampusOne.Services.AI;
using CampusOne.Application.Interfaces;
using CampusOne.Infrastructure.Services;
using CampusOne.Services.Chat;

var builder = WebApplication.CreateBuilder(args);

// 1. Register services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR();

builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddHttpClient<LlmService>();

builder.Services.AddScoped<IAiIntentClassifier>(
    sp => sp.GetRequiredService<LlmService>());

builder.Services.AddSingleton<EmbeddingStore>();
builder.Services.AddSingleton<SimilarityService>();

builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IAiOrchestrator, AiOrchestrator>();
builder.Services.AddScoped<IAiConversationService, AiConversationService>();

builder.Services.AddScoped<IProfilePictureStorage, LocalProfilePictureStorage>();
builder.Services.AddScoped<IChatService, ChatService>();

<<<<<<< Updated upstream
// 2. Configure CORS
=======
>>>>>>> Stashed changes
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

        // SignalR sends the JWT through the access_token query parameter.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

<<<<<<< Updated upstream
// 7. Configure Swagger in Development
if (app.Environment.IsDevelopment())
=======
// 5. Configure Seeding / Password Reset in Development
using (var scope = app.Services.CreateScope())
>>>>>>> Stashed changes
{
    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var email = "noran1@gmail.com";
    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Noran",
            LastName = "Fawzy",
            Department = "Computer Science",
            AcademicYear = "3rd Year",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Noran@1234");
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
        
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        await userManager.ResetPasswordAsync(user, resetToken, "Noran@1234");

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        Console.WriteLine("User exists: Password reset to 'Noran@1234' & Lockout cleared.");
    }
}

<<<<<<< Updated upstream
// 8. Configure HTTP pipeline
=======
// 6. Configure HTTP pipeline

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CampusOne API v1");
    c.RoutePrefix = "swagger";
});

>>>>>>> Stashed changes
app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 9. Map SignalR Hub
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
