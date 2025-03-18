using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;


namespace ApiLib.Conventions
{
    public class ApiVersioningRouteConvention : IApplicationModelConvention
    {
        private readonly IConfiguration _configuration;

        public ApiVersioningRouteConvention(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Apply(ApplicationModel application)
        {
            bool enableApiVersioning = _configuration.GetValue<bool>("EnableApiVersioning");
            string basePrefix = _configuration.GetValue<string>("BaseApiPrefix") ?? "api";

            AttributeRouteModel routePrefix = new AttributeRouteModel(new RouteAttribute(
                enableApiVersioning ? $"{basePrefix}/v{{v:ApiVersion}}/" : $"{basePrefix}/"));

            foreach (var selector in application.Controllers.SelectMany(c => c.Selectors))
            {
                if (selector.AttributeRouteModel != null)
                    selector.AttributeRouteModel =
                        AttributeRouteModel.CombineAttributeRouteModel(routePrefix, selector.AttributeRouteModel);
                else
                    selector.AttributeRouteModel = routePrefix;
            }
        }
    }
}
