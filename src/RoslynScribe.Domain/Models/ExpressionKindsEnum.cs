using System;

namespace RoslynScribe.Domain.Models
{
    [Flags]
    internal enum ExpressionKindsEnum
    {
        Invocation = 1,
        Declaration = 1 << 1,
    }
}
