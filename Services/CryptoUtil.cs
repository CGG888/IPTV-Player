using System;
using System.Security.Cryptography;
using System.Text;

namespace LibmpvIptvClient.Services
{
    public static class CryptoUtil
    {
        public static string ProtectString(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            var bytes = Encoding.UTF8.GetBytes(plain);
            var prot = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(prot);
        }
        public static string UnprotectString(string b64)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(b64)) return "";
                var bytes = Convert.FromBase64String(b64);
                var unprot = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(unprot);
            }
            catch
            {
                return "";
            }
        }
    }
}
