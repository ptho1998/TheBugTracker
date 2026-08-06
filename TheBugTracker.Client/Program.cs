using ApexCharts;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using TheBugTracker.Client.Services;
using TheBugTracker.Client.Services.Interfaces;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddScoped(sp => new HttpClient { 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

builder.Services.AddApexCharts();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
});

builder.Services.AddScoped<IProjectDTOService, WASMProjectDTOService>();
builder.Services.AddScoped<ICompanyDTOService, WASMCompanyDTOService>();
builder.Services.AddScoped<ITicketDTOService, WASMTicketDTOService>();
builder.Services.AddScoped<IInviteDTOService, WASMInviteDTOService>();
builder.Services.AddScoped<INotificationDTOService, WASMNotificationDTOService>();

await builder.Build().RunAsync();
