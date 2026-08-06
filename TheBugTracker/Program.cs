using ApexCharts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MudBlazor;
using MudBlazor.Services;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Components;
using TheBugTracker.Components.Account;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Services;
using TheBugTracker.Services.Interfaces;
using TheBugTracker.Services.Repositories;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "The Bug Tracker API",
        Version = "v1",
        Description = """
                    <img src="/images/CF_Logo_WO.png" height="120" />

                    This API is used by The Bug Tracker application when executing
                    in WebAssembly to interact with the backend.

                    If you think this project is interesting or would like to see
                    the rest of my work, you can check out [my portoflio](https://coderfoundry.com)!

                    This API uses cookie-based authentication. To test the requests
                    below, you must first log in through the application to set a
                    cookie in your browser and receive a valid response from the
                    "Test Request" buttons below
                    """,
        Contact = new()
        {
            Name = "Coder Foundry",
            Email = "info@coderfoundry.com",
            Url = new("https://coderfoundry.com"),
        }
    });

    //show cookies as the authentication scheme
    options.AddSecurityDefinition("cookie", new OpenApiSecurityScheme
    {
        Name = ".AspNetCore.Identity.Application",
        In = ParameterLocation.Cookie,
        Type = SecuritySchemeType.Http,
        Scheme = "cookie"
    });

    //show which endpoints require login
    options.OperationFilter<SecurityRequirementsOperationFilter>();

    //generate documentation for endpoints from XML comments
    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));

    //exclude documentation for the built-in account enpoints
    options.DocInclusionPredicate((_, description) =>
        description.RelativePath is null || !description.RelativePath.StartsWith("Account"));
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddApexCharts();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
});

builder.Services.AddOutputCache();


builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies(cookieBuilder =>
    {
        cookieBuilder.ApplicationCookie!.Configure(config =>
        {
            config.Events.OnRedirectToLogin += (context) =>
            {
                if (context.Request.Path.StartsWithSegments("/api") || context.Request.HasJsonContentType())
                {
                    context.Response.StatusCode = 401;
                }

                return Task.CompletedTask;
            };

            config.Events.OnRedirectToAccessDenied += (context) =>
            {
                if (context.Request.Path.StartsWithSegments("/api") || context.Request.HasJsonContentType())
                {
                    context.Response.StatusCode = 403;
                }

                return Task.CompletedTask;
            };
        });


    });

var connectionString = DataUtility.GetConnectionString(builder.Configuration) ?? throw new InvalidOperationException("Connection string 'DbConnection' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString, 
        options => options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    ));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, SendGridEmailSender>();
builder.Services.AddSingleton<IEmailSender, SendGridEmailSender>();

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectDTOService, ProjectDTOService>();

builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICompanyDTOService, CompanyDTOService>();

builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketDTOService, TicketDTOService>();

builder.Services.AddScoped<IInviteRepository, InviteRepository>();
builder.Services.AddScoped<IInviteDTOService, InviteDTOService>();

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationDTOService, NotificationDTOService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DataUtility.ManageDataAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger(options => options.RouteTemplate = "/openapi/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options.WithFavicon("/favicon.ico")
           .WithTitle("API Specification || The Bug Tracker")
           .WithTheme(ScalarTheme.Mars);
}); //URL: /scalar/v1

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles();

app.Use(async (ctx, next) =>
{
    await next(ctx);

    if (ctx.Request.Path.StartsWithSegments("/images/png") && ctx.Response.StatusCode == 404)
    {
        ctx.Response.Redirect("/images/png/default.png");
    }
});

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TheBugTracker.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapControllers();

app.Run();
