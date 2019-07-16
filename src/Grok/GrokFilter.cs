namespace Grok
{
    public class GrokFilter : CommonFilter
    {
        public string Property { get; set; }
        public string[] Patterns { get; set; }
        public bool BreakOnMatch { get; set; }
    }

    public class CommonFilter
    {
        public string[] AddField { get; set; }
    }
}