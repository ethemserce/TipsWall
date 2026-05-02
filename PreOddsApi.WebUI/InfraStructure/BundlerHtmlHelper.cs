using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.InfraStructure
{
    public static class Bundler
    {
        public static IWebHostEnvironment HostingEnvironment;

        private const string VirtualFolder = "wwwroot/";

        /// <summary>
        /// Unpacks the bundle (css/js) for debugging purposes. Makes the build faster by not bundling while debugging.
        /// </summary>
        /// <param name="baseFolder">The base folder for this application/</param>
        /// <param name="bundlePath">The bundled file to unpack.</param>
        /// <returns>Unpacked bundles</returns>
        public static HtmlString Unpack(string baseFolder, string bundlePath)
        {
            if (!HostingEnvironment.IsDevelopment())
            {
                return new HtmlString($@"<script src=""{bundlePath}""></script>");
            }
            var configFile = Path.Combine(baseFolder, "bundleconfig.json");
            var bundle = GetBundle(configFile, bundlePath);
            if (bundle == null)
                return null;

            // Clean up the bundle to remove the virtual folder that aspnetcore provides.
            var inputFiles = bundle.InputFiles.Select(file => file.Substring(VirtualFolder.Length));

            var outputString = bundlePath.EndsWith(".js") ?
                inputFiles.Select(inputFile => $"<script src='/{inputFile}' type='text/javascript'></script>") :
                inputFiles.Select(inputFile => $"<link rel='stylesheet' href='/{inputFile}' />");

            return new HtmlString(string.Join("\n", outputString));
        }

        private static Bundle GetBundle(string configFile, string bundlePath)
        {
            var file = new FileInfo(configFile);
            if (!file.Exists)
                return null;

            var bundles = JsonConvert.DeserializeObject<IEnumerable<Bundle>>(File.ReadAllText(configFile));
            return (from b in bundles
                    where b.OutputFileName.EndsWith(bundlePath, StringComparison.InvariantCultureIgnoreCase)
                    select b).FirstOrDefault();
        }

        class Bundle
        {
            [JsonProperty("outputFileName")]
            public string OutputFileName { get; set; }

            [JsonProperty("inputFiles")]
            public List<string> InputFiles { get; set; } = new List<string>();
        }
    }
}
