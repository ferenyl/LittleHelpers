using System.Text;
using LittleHelpers.ApiService.Data;
using LittleHelpers.ApiService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var isTesting = builder.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    builder.AddNpgsqlDataSource("littlehelpers");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("littlehelpers")));
}
// In Testing environment the DbContext is registered by the test factory

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set 'Jwt:Key' via environment variable or user secrets. " +
        "Minimum 32 characters required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IChildRepository, EfChildRepository>();
builder.Services.AddScoped<IChoreRepository, EfChoreRepository>();
builder.Services.AddScoped<IChoreLogRepository, EfChoreLogRepository>();
builder.Services.AddScoped<IChoreAvailabilityService, ChoreAvailabilityService>();
builder.Services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddDecoratedQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>, GetUsersQueryHandler>();
builder.Services.AddDecoratedQueryHandler<GetUserByIdQuery, UserDto, GetUserByIdQueryHandler>();
builder.Services.AddDecoratedCommandHandler<CreateUserCommand, UserDto, CreateUserCommandHandler>();
builder.Services.AddDecoratedCommandHandler<UpdateUserCommand, UserDto, UpdateUserCommandHandler>();
builder.Services.AddDecoratedCommandHandler<DeleteUserCommand, Unit, DeleteUserCommandHandler>();
builder.Services.AddDecoratedQueryHandler<LoginQuery, LoginResponse, LoginQueryHandler>();
builder.Services.AddDecoratedQueryHandler<GetMenuQuery, IReadOnlyList<MenuItemDto>, GetMenuQueryHandler>();
builder.Services.AddDecoratedQueryHandler<GetChoreLogQuery, IReadOnlyList<ChoreLogDto>, GetChoreLogQueryHandler>();
builder.Services.AddDecoratedQueryHandler<GetChildrenQuery, IReadOnlyList<ChildSummaryDto>, GetChildrenQueryHandler>();
builder.Services.AddDecoratedQueryHandler<GetChildDetailQuery, ChildSummaryDto, GetChildDetailQueryHandler>();
builder.Services.AddDecoratedQueryHandler<GetChoresQuery, IReadOnlyList<ChoreDto>, GetChoresQueryHandler>();
builder.Services.AddDecoratedQueryHandler<GetChoreByIdQuery, ChoreDto, GetChoreByIdQueryHandler>();
builder.Services.AddDecoratedCommandHandler<CreateChoreCommand, ChoreDto, CreateChoreCommandHandler>();
builder.Services.AddDecoratedCommandHandler<UpdateChoreCommand, ChoreDto, UpdateChoreCommandHandler>();
builder.Services.AddDecoratedCommandHandler<DeleteChoreCommand, Unit, DeleteChoreCommandHandler>();
builder.Services.AddDecoratedCommandHandler<CompleteChoreCommand, ChoreLogDto, CompleteChoreCommandHandler>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();

    // Seed initial admin user if no users exist
    if (!isTesting && !db.Users.Any())
    {
        var seedPassword = builder.Configuration["SeedAdminPassword"]
            ?? throw new InvalidOperationException(
                "SeedAdminPassword is not configured. Set it via environment variable or user secrets.");
        db.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
            UserLevel = UserLevel.Parent
        });
        db.SaveChanges();
    }
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "LittleHelpers API is running.").AllowAnonymous();
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

namespace LittleHelpers.ApiService
{
    public class ApiMarker { }
}
