using Float;
using Microsoft.AspNetCore.Hosting;

namespace pvgisjsonapi {
    public class Program {
        public static void Main(string[] args) {
            // Serve pages.
            FloatEngine.RegisterRouteFunction("", FloatHttpMethod.GET, API.ServePageHome);
            FloatEngine.RegisterRouteFunction("documentation.html", FloatHttpMethod.GET, API.ServePageDocumentation);
            FloatEngine.RegisterRouteFunction("playground.html", FloatHttpMethod.GET, API.ServePagePlayground);

            // Get PV values for given location.
            FloatEngine.RegisterRouteFunction("api/v1/pv", FloatHttpMethod.GET, API.GetPVValueByGet);
            FloatEngine.RegisterRouteFunction("api/v1/pv", FloatHttpMethod.POST, API.GetPVValueByPost);

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<FloatEngine>()
                .Build();

            host.Run();
        }
    }
}