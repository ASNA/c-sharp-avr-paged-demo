using System.Web.Mvc;
using System.Web.Routing;

namespace mvc_with_avr
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Customer",
                url: "customers/{page}",
                defaults: new { controller = "Customer", action = "Index",
                     page = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index",
                      id = UrlParameter.Optional }
            );
        }
    }
}
