using LE.ApiGateway.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LE.ApiGateway
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((context, config) =>
                 {
                     config
                         .AddCommonConfigs(context)
                         .SetBasePath(context.HostingEnvironment.ContentRootPath)
                         .AddOcelotWithSwaggerSupport("Routes", true)
                         .AddJsonFile("ocelot.global.json")
                         .AddJsonFile($"ocelot.global.{context.HostingEnvironment.EnvironmentName}.json", true)
                         .AddEnvironmentVariables();
                 })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public static IConfigurationBuilder AddCommonConfigs(this IConfigurationBuilder configurationBuilder, HostBuilderContext context)
        {
            var env = context.HostingEnvironment.EnvironmentName;
            var appSettingName = "appsettings.json";
            var envAppSettingName = $"appsettings.{env}.json";
            var appSettingCommonName = $"appsettings.common.json";

            return configurationBuilder.AddJsonFile(appSettingName)
                    .AddJsonFile(envAppSettingName, optional: true)
                    .AddJsonFile(appSettingCommonName, optional: true);
        }
        
        public static IConfigurationBuilder AddOcelotWithSwaggerSupport(this IConfigurationBuilder config, string folder, bool optional = false)
        {
            List<FileInfo> ocelotFiles = GetListOfFiles(folder);
            SwaggerFileConfig fileConfigurationMerged = MergeFilesOfOcelotConfiguration(ocelotFiles);
            string jsonFileConfiguration = JsonConvert.SerializeObject(fileConfigurationMerged);
            try
            {
                MemoryStream ms = new(Encoding.UTF8.GetBytes(jsonFileConfiguration));
                config.AddJsonStream(ms);
            }
            catch
            {
            }
            return config;
        }

        private static List<FileInfo> GetListOfFiles(string folder)
        {
            return new DirectoryInfo(folder)
                    .EnumerateFiles().ToList();
        }
        public static SwaggerFileConfig MergeFilesOfOcelotConfiguration(List<FileInfo> files)
        {
            SwaggerFileConfig fileConfigurationMerged = new SwaggerFileConfig();

            foreach (FileInfo itemFile in files)
            {
                string linesOfFile = File.ReadAllText(itemFile.FullName);
                SwaggerFileConfig config = JsonConvert.DeserializeObject<SwaggerFileConfig>(linesOfFile);
                fileConfigurationMerged.Routes.AddRange(config.Routes);
            }

            return fileConfigurationMerged;
        }
    }
}
