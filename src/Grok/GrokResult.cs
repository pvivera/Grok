using Newtonsoft.Json.Linq;

namespace Grok
{
    public class GrokResult
    {
        public bool DataExtracted { get; set; }
        public JObject Data { get; set; }
    }
}