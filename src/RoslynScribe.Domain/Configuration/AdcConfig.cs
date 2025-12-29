using System;

namespace RoslynScribe.Domain.Configuration
{
    public class AdcConfig
    {
        public AdcType[] Types { get; set; }
    }

    public class AdcType
    {
        public string TypeFullName { get; set; }

        public AdcMethod[] Methods { get; set; }
    }

    public class AdcMethod
    {
        public string MethodName { get; set; }

        public string MethodIdentifier { get; set; }

        public int Level { get; set; }
    }
}
