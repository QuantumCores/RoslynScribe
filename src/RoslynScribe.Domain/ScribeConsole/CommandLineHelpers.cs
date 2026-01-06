using System;
using System.Linq;

namespace RoslynScribe.Domain.ScribeConsole
{
    internal static class CommandLineHelpers
    {
        internal static bool IsHelpCommand(string command)
        {
            return string.Equals(command, "-h", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "--help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "help", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ContainsHelpFlag(string[] args)
        {
            return args.Any(arg =>
                string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase));
        }

        internal static string NormalizeOption(string arg)
        {
            if (!arg.StartsWith("-", StringComparison.Ordinal))
            {
                return null;
            }

            var key = arg.StartsWith("--", StringComparison.Ordinal) ? arg.Substring(2) : arg.Substring(1);
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            switch (key.ToLowerInvariant())
            {
                case "solution":
                    return "s";
                case "project":
                    return "p";
                case "project-exclude":
                    return "pe";
                case "project-exclude-contains":
                    return "pec";
                case "dir":
                    return "d";
                case "file":
                    return "f";
                case "output":
                    return "o";
                case "config":
                    return "c";
                default:
                    return key.ToLowerInvariant();
            }
        }

        internal static bool TryReadOptionValue(string[] args, ref int index, out string value, out string error)
        {
            error = null;
            value = null;

            if (index + 1 >= args.Length)
            {
                error = $"Missing value for option '{args[index]}'.";
                return false;
            }

            var next = args[index + 1];
            if (next.StartsWith("-", StringComparison.Ordinal))
            {
                error = $"Missing value for option '{args[index]}'.";
                return false;
            }

            index++;
            value = next;
            return true;
        }
    }
}
