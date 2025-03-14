using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace ApiLib.Config
{
    public static class VersioningConfig
    {
        public static void AddApiVersioning(IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1);
                options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Supports /v1, /v2 in URLs
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        }
    }
}
