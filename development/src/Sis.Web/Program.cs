using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sis.Web;
using Sis.Web.Services;
using TailwindUi.Blazor.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Relative request paths ("api/...") only resolve under a base address ending in "/".
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;
if (!apiBaseUrl.EndsWith('/'))
{
    apiBaseUrl += "/";
}

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<SystemInfoClient>();
builder.Services.AddScoped<StudentsClient>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddTailwindUi();

await builder.Build().RunAsync();
