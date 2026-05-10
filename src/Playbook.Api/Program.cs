using System.Text;
using FluentValidation;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Playbook.Api.Auth;
using Playbook.Api.GraphQL.Errors;
using Playbook.Application.Activities;
using Playbook.Application.Auth;
using Playbook.Application.Categories;
using Playbook.Application.Common.Abstractions;
using Playbook.Application.Dashboard;
using Playbook.Application.Tags;
using Playbook.Infrastructure;
using Playbook.Infrastructure.Auth;
using Playbook.Infrastructure.Mongo;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithProperty("App", "Playbook.Api")
       .WriteTo.Console());

// ---- Options ----
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection(MongoOptions.Section));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.Configure<Playbook.Infrastructure.Storage.BlobOptions>(
    builder.Configuration.GetSection(Playbook.Infrastructure.Storage.BlobOptions.Section));

// ---- Infrastructure ----
builder.Services.AddInfrastructure();

// ---- Auth ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.Section);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"] ?? "playbook-api",
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"] ?? "playbook-web",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey missing"))),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Allow passing JWT via query string for GraphQL subscriptions if needed later.
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token)) ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ---- CORS ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ---- Rate limiting (auth + uploads only) ----
builder.Services.AddRateLimiter(o =>
{
    o.AddSlidingWindowLimiter("auth", lim =>
    {
        lim.Window = TimeSpan.FromMinutes(1);
        lim.SegmentsPerWindow = 4;
        lim.PermitLimit = 10;
        lim.QueueLimit = 0;
    });
    o.AddSlidingWindowLimiter("uploads", lim =>
    {
        lim.Window = TimeSpan.FromMinutes(1);
        lim.SegmentsPerWindow = 4;
        lim.PermitLimit = 20;
        lim.QueueLimit = 0;
    });
});

// ---- Application handlers (scoped) ----
builder.Services.AddScoped<CreateActivityHandler>();
builder.Services.AddScoped<UpdateActivityHandler>();
builder.Services.AddScoped<DeleteActivityHandler>();
builder.Services.AddScoped<ArchiveActivityHandler>();
builder.Services.AddScoped<ToggleFavoriteHandler>();
builder.Services.AddScoped<RecordViewHandler>();
builder.Services.AddScoped<PinActivityHandler>();
builder.Services.AddScoped<UnpinActivityHandler>();
builder.Services.AddScoped<AttachToActivityHandler>();
builder.Services.AddScoped<DetachFromActivityHandler>();
builder.Services.AddScoped<GetActivityByIdHandler>();
builder.Services.AddScoped<ListActivitiesHandler>();
builder.Services.AddScoped<RecentlyViewedHandler>();
builder.Services.AddScoped<FavoritesHandler>();
builder.Services.AddScoped<CreateCategoryHandler>();
builder.Services.AddScoped<UpdateCategoryHandler>();
builder.Services.AddScoped<DeleteCategoryHandler>();
builder.Services.AddScoped<ListCategoriesHandler>();
builder.Services.AddScoped<ListTagsHandler>();
builder.Services.AddScoped<GetDashboardHandler>();
builder.Services.AddScoped<SignupHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RefreshSessionHandler>();
builder.Services.AddScoped<LogoutHandler>();
builder.Services.AddScoped<GetMeHandler>();

// ---- FluentValidation ----
builder.Services.AddValidatorsFromAssemblyContaining<CreateActivityValidator>();

// ---- HotChocolate ----
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddMutationType()
    .AddTypeExtension<Playbook.Api.GraphQL.Auth.AuthQueries>()
    .AddTypeExtension<Playbook.Api.GraphQL.Auth.AuthMutations>()
    .AddTypeExtension<Playbook.Api.GraphQL.Activities.ActivityQueries>()
    .AddTypeExtension<Playbook.Api.GraphQL.Activities.ActivityMutations>()
    .AddDataLoader<Playbook.Api.GraphQL.DataLoaders.CategoryByIdDataLoader>()
    .AddErrorFilter(PlaybookErrorFilter.OnError)
    .AddAuthorizationCore()
    .AddAuthorizationHandler<Playbook.Api.Auth.HotChocolateAuthorizationHandler>()
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// ---- Health checks ----
builder.Services.AddHealthChecks();

// ---- Controllers (for /api/uploads) ----
builder.Services.AddControllers();

var app = builder.Build();

// ---- Middleware pipeline ----
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL("/graphql");
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// ---- Index bootstrapping ----
using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<MongoBootstrapper>();
    await bootstrapper.EnsureIndexesAsync();
}

await app.RunAsync();

// Exposed for WebApplicationFactory in tests.
public partial class Program { }
