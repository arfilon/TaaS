using BlazorApp1.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaaSApplication();
builder.Services.AddSingleton<Arfilon.TaaS.ITenantSelector, TenantSelector>();

builder.Services.AddForEachTenant((s, t) =>
{
    // Add services to the container.
    s.AddRazorPages().AddApplicationPart(typeof(Program).Assembly);
    s.AddServerSideBlazor();
    s.AddSingleton<WeatherForecastService>();
});
builder.Services.AddTenantServiceProxy<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
builder.Services.AddTenantServiceProxy<Microsoft.Extensions.Configuration.IConfiguration>();





var app = builder.Build();





app.UseForEachTenant((t, a) =>
{



    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        a.UseExceptionHandler("/Error");
    }


    a.UseStaticFiles();

    a.UseRouting();
    a.UseEndpoints(e =>
    {
        e.MapBlazorHub();
        e.MapFallbackToPage("/_Host");
    });
    //a.MapBlazorHub();
    //a.MapFallbackToPage("/_Host");


});

app.Run();