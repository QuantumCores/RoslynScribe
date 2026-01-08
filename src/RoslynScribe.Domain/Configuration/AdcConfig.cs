using System.Collections.Generic;

namespace RoslynScribe.Domain.Configuration
{
    public class AdcConfig
    {
        public bool DiscardDocumentRootNode { get; set; } = false;

        public Dictionary<string, AdcType> Types { get; set; } = new Dictionary<string, AdcType>();

        internal void FlattenMethodOverrides()
        {
            foreach (var type in Types.Values)
            {
                if (type.GetMethods == null)
                {
                    continue;
                }

                foreach (var method in type.GetMethods)
                {
                    if (method.SetGuidesOverrides == null && type.SetGuidesOverrides != null)
                    {
                        method.SetGuidesOverrides = type.SetGuidesOverrides;
                    }
                    else if (method.SetGuidesOverrides != null && type.SetGuidesOverrides != null)
                    {
                        // flatten overrides, method-level takes precedence
                        var combinedOverrides = new Dictionary<string, string>(type.SetGuidesOverrides);
                        foreach (var fromMethod in method.SetGuidesOverrides)
                        {
                            if (!type.SetGuidesOverrides.ContainsKey(fromMethod.Key))
                            {
                                combinedOverrides.Add(fromMethod.Key, fromMethod.Value);
                            }
                            else
                            {
                                combinedOverrides[fromMethod.Key] = fromMethod.Value;
                            }
                        }
                    }
                }
            }
        }
    }

    public class AdcType
    {
        // public string TypeFullName { get; set; }

        public AdcMethod[] GetMethods { get; set; }

        public HashSet<string> GetAttributes { get; set; }

        public Dictionary<string, string> SetGuidesOverrides { get; set; }

        public int SetDefaultLevel { get; set; } = 1;
    }

    public class AdcMethod
    {
        public bool IncludeMethodSignatures { get; set; } = false;

        public string MethodName { get; set; }

        public string MethodIdentifier { get; set; }

        public HashSet<string> GetAttributes { get; set; }

        public int SetDefaultLevel { get; set; } = 1;

        public Dictionary<string, string> SetGuidesOverrides { get; set; }
    }
}
