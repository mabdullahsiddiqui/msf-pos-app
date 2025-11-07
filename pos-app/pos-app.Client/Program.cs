using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using pos_app.Client.Services;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient to use same origin (relative paths)
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// Register client-side services with their dependencies
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<SuperAdminService>();
builder.Services.AddScoped<AuthenticationStateService>();
builder.Services.AddScoped<SessionService>();

await builder.Build().RunAsync();
