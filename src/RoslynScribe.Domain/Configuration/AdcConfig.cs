using System.Collections.Generic;

namespace RoslynScribe.Domain.Configuration
{
    public class AdcConfig
    {
        public Dictionary<string, AdcType> Types { get; set; } = new Dictionary<string, AdcType>();
    }

    public class AdcType
    {
        // public string TypeFullName { get; set; }

        public AdcMethod[] Methods { get; set; }
    }

    public class AdcMethod
    {
        public bool IncludeMethodDeclaration { get; set; } = true;

        public string MethodName { get; set; }

        public string MethodIdentifier { get; set; }

        public int Level { get; set; }
    }
}
