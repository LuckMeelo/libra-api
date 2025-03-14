using ApiLib.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApiLib.Config
{
    public static class AppConfig
    {
        public static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Enable API Versioning (if configured)
            if (builder.Configuration.GetValue<bool>("EnableLogging"))
            {
                LoggingConfig.AddLogging(builder);
            }

            // Enable Logging (if configured)
            if (builder.Configuration.GetValue<bool>("EnableApiVersioning"))
            {
                VersioningConfig.AddApiVersioning(builder.Services);
            }

            builder.Services.AddControllers(options =>
            {
                // Register our custom convention for route versioning
               options.Conventions.Add(new ApiVersioningRouteConvention(builder.Configuration));
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

        }

        public static void ConfigureApplication(WebApplication app, IConfiguration configuration)
        {
            if (configuration.GetValue<bool>("EnableLogging"))
            {
                LoggingConfig.UseLogging(app);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
        }
    }
}
