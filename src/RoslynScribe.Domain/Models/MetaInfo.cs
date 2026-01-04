using System;
using System.Security.Cryptography;
using System.Text;

namespace RoslynScribe.Domain.Models
{
    public struct MetaInfo
    {
        public string ProjectName { get; set; }

        public string DocumentName { get; set; }

        public string DocumentPath { get; set; }

        public string NameSpace { get; set; }

        public string TypeName { get; set; }

        public string MemberName { get; set; }

        public string Identifier { get; set; }

        public int Line {  get; set; }

        public Guid GetDeterministicId()
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(GetIdentityKey());
                var hash = md5.ComputeHash(bytes);
                return new Guid(hash);
            }
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

        //public override int GetHashCode()
        //{
        //    return ComputeStableHash(GetIdentityKey());
        //}

        private static string Normalize(string value) => value ?? string.Empty;

        // Deterministic, platform-independent hash (FNV-1a 32-bit)
        //private static int ComputeStableHash(string value)
        //{
        //    unchecked
        //    {
        //        const int offset = unchecked((int)2166136261);
        //        const int prime = 16777619;
        //        var hash = offset;
        //        foreach (var c in value)
        //        {
        //            hash ^= c;
        //            hash *= prime;
        //        }
        //        return hash;
        //    }
        //}
    }
}
