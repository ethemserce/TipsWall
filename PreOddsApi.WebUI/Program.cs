using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace PreOddsApi.WebUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuiilder(args).Build().Run();
        }

        public static IHostBuilder CreateWebHostBuiilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                webBuilder.UseIISIntegration();
                webBuilder.UseStartup<Startup>();
            });

    }
}
