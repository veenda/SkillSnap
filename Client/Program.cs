using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SkillSnap.Client;
using SkillSnap.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register ProjectService.cs and SkillService.cs in the client project:
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>(); 
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped(sp => new HttpClient {
    BaseAddress = new Uri("http://localhost:5062/")
});

var host = builder.Build();

var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAuthStateAsync();

// start the application
await host.RunAsync();