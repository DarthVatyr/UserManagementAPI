using UserManagementAPI.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add built-in services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token."
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
            new string[] {}
        }
    });
});

// Add built-in authentication/authorization (JWT Bearer example)
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = false,
            RequireSignedTokens = false,
            SignatureValidator = (token, parameters) =>
            {
                // Accept any well-formed JWT without validating the signature (for demo/testing only)
                var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token);
                return jwt;
            }
        };
        options.UseSecurityTokenValidators = true; // <-- Required for .NET 8+ to use SignatureValidator
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// 1. Error handling middleware (built-in)
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error." });
    });
});

// 2. Authentication and Authorization middleware (built-in)
app.UseAuthentication();
app.UseAuthorization();

// 3. Logging middleware (built-in, with simple inline logger for requests/responses)
var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.Use(async (context, next) =>
{
    logger.LogInformation("HTTP {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
    logger.LogInformation("Response {StatusCode}", context.Response.StatusCode);
});

app.UseSwagger();
app.UseSwaggerUI();

var users = new Dictionary<int, User>
{
    { 1, new User { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "alice@techhive.com" } },
    { 2, new User { Id = 2, FirstName = "Bob", LastName = "Johnson", Email = "bob@techhive.com" } }
};

// Helper method for user validation using data annotations (CreateUserRequest)
bool IsValidUser(CreateUserRequest? user, out List<ValidationResult> results)
{
    results = new List<ValidationResult>();
    if (user is null) return false;
    var context = new ValidationContext(user);
    return Validator.TryValidateObject(user, context, results, true);
}

// GET all users (protected)
// Returns a list of all users.
app.MapGet("/users", () =>
{
    return Results.Ok(users.Values.ToList());
}).RequireAuthorization();

// GET user by ID (protected)
app.MapGet("/users/{id}", (int id) =>
{
    users.TryGetValue(id, out var user);
    return user is not null ? Results.Ok(user) : Results.NotFound(new { error = "User not found." });
}).RequireAuthorization();

// POST create user (protected)
app.MapPost("/users", (CreateUserRequest request) =>
{
    if (!IsValidUser(request, out var validationResults))
    {
        return Results.BadRequest(new { errors = validationResults.Select(r => r.ErrorMessage) });
    }

    var user = new User
    {
        Id = users.Count > 0 ? users.Keys.Max() + 1 : 1,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email
    };
    users[user.Id] = user;
    return Results.Created($"/users/{user.Id}", user);
}).RequireAuthorization();

// PUT update user (protected)
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    if (updatedUser is null)
    {
        return Results.BadRequest(new { errors = new[] { "User data is required." } });
    }

    if (!users.ContainsKey(id))
    {
        return Results.NotFound(new { error = "User not found." });
    }

    updatedUser.Id = id;
    users[id] = updatedUser;
    return Results.Ok(updatedUser);
}).RequireAuthorization();

// DELETE user (protected)
app.MapDelete("/users/{id}", (int id) =>
{
    if (!users.Remove(id)) return Results.NotFound(new { error = "User not found." });
    return Results.NoContent();
}).RequireAuthorization();

// Starts the application
app.Run();