using Microsoft.EntityFrameworkCore;
using LibrApi.Data;
using Serilog;
using ApiLib.Config;

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure services
    AppConfig.ConfigureServices(builder);

    builder.Services.AddDbContext<LibrApiDbContext>(options =>
        options.UseLazyLoadingProxies().UseSqlServer(builder.Configuration.GetConnectionString("librapi_db")));

    var app = builder.Build();

    // Configure application
    AppConfig.ConfigureApplication(app, app.Configuration);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
