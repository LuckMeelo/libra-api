using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApiLib.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ApiVersionRouteAttribute : RouteAttribute
    {
        public ApiVersionRouteAttribute(IConfiguration configuration)
            : base(GetRoute(configuration))
        {
        }

        private static string GetRoute(IConfiguration configuration)
        {
            bool isApiVersioningEnabled = configuration.GetValue<bool>("EnableApiVersioning");

            if (isApiVersioningEnabled)
            {
                return $"api/v{{v:apiVersion}}/[controller]";
            }
            else
            {
                return "api/[controller]";
            }
        }
    }
}
