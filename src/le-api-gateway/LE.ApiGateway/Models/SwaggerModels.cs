using System.Collections.Generic;

namespace LE.ApiGateway.Models
{
    public class SwaggerServiceConfig
    {
        public List<string> Versions { get; set; }
        public string Name { get; set; }
    }
    public class SwaggerConfig
    {
        public bool Enable { get; set; }
        public List<SwaggerServiceConfig> Services { get; set; }
    }
    public class SwaggerFileConfig
    {
        public List<object> Routes { get; set; } = new List<object>();
    }
}
