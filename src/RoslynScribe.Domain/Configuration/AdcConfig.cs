using System.Collections.Generic;

namespace RoslynScribe.Domain.Configuration
{
    public class AdcConfig
    {
        public bool DiscardDocumentRootNode { get; set; }

        public Dictionary<string, AdcType> Types { get; set; } = new Dictionary<string, AdcType>();
    }

    public class AdcType
    {
        // public string TypeFullName { get; set; }

        public AdcMethod[] GetMethods { get; set; }

        public string[] GetAttributes { get; set; }
    }

    public class AdcMethod
    {
        public bool IncludeMethodDeclaration { get; set; } = true;

        public string MethodName { get; set; }

        public string MethodIdentifier { get; set; }

        public string[] GetAttributes { get; set; }

        public int SetDefaultLevel { get; set; } = 1;

        public Dictionary<string, string> SetGuidesOverrides { get; set; }
    }
}
