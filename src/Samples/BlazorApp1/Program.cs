using BlazorApp1.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaaSApplication();
builder.Services.AddSingleton<Arfilon.TaaS.ITenantSelector, TenantSelector>();

builder.Services.AddForEachTenant((s, t) =>
{
    s.AddRazorPages();
    s.AddServerSideBlazor();
    s.AddSingleton<WeatherForecastService>();
});
builder.Services.AddTenantServiceProxy<IHostApplicationLifetime>();
builder.Services.AddTenantServiceProxy<IConfiguration>();





var app = builder.Build();





app.UseForEachTenant((t, a) =>
{
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
});

app.Run();