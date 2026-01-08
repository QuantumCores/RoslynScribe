using System;

namespace RoslynScribe.Domain.Models
{
    public struct MetaInfo
    {
        private const int DefaultDeterministicIdLength = 8;
        private const string Base62Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public string SolutionName { get; set; }

        public string ProjectName { get; set; }

        public string DocumentName { get; set; }

        public string DocumentPath { get; set; }

        public string NameSpace { get; set; }

        public string TypeName { get; set; }

        public string MemberName { get; set; }

        public string Identifier { get; set; }

        public int Line { get; set; }

        public string GetDeterministicId()
        {
            return GetDeterministicId(DefaultDeterministicIdLength);
        }

        public string GetDeterministicId(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");
            }

            var hash = ComputeFnv1a64(GetIdentityKey());
            return ToBase62(hash, length);
        }

        public string GetIdentityKey()
        {
            return string.Join("|",
                Normalize(ProjectName),
                Normalize(DocumentName),
                Normalize(DocumentPath),
                Normalize(NameSpace),
                Normalize(TypeName),
                Normalize(MemberName),
                Normalize(Identifier),
                Line.ToString());
        }

        private static string Normalize(string value) => value ?? string.Empty;

        // Deterministic, platform-independent hash (FNV-1a 64-bit)
        private static ulong ComputeFnv1a64(string value)
        {
            unchecked
            {
                const ulong offset = 14695981039346656037;
                const ulong prime = 1099511628211;
                var hash = offset;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }
                return hash;
            }
        }

        private static string ToBase62(ulong value, int length)
        {
            var buffer = new char[length];
            for (int i = length - 1; i >= 0; i--)
            {
                var index = (int)(value % 62);
                buffer[i] = Base62Alphabet[index];
                value /= 62;
            }

            return new string(buffer);
        }
    }
}

