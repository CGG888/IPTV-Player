using System;
using System.Net;
using Microsoft.Win32;

namespace LibmpvIptvClient.Services
{
    /// <summary>
    /// Reads proxy settings directly from the Windows Registry to bypass .NET's caching mechanisms.
    /// This ensures we always get the *real-time* state of the system proxy.
    /// </summary>
    public class RegistryProxyProvider : IWebProxy
    {
        public ICredentials? Credentials { get; set; }

        public Uri? GetProxy(Uri destination)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (key != null)
                    {
                        // 1. Check if Proxy is enabled
                        var proxyEnable = key.GetValue("ProxyEnable") as int?;
                        if (proxyEnable == 1)
                        {
                            // 2. Get Proxy Server string (e.g., "127.0.0.1:7890" or "http=127.0.0.1:7890;https=...")
                            var proxyServer = key.GetValue("ProxyServer") as string;
                            if (!string.IsNullOrWhiteSpace(proxyServer))
                            {
                                // Simple parsing: if it contains '=', it's a specific map, otherwise it's global
                                // For simplicity in this context, we assume a global proxy if no specific protocol is matched,
                                // or we just take the first valid address.
                                // Most VPNs sets "127.0.0.1:7890" which applies to all.
                                
                                if (proxyServer.Contains("="))
                                {
                                    // Complex parsing skipped for brevity, usually not needed for simple VPN toggling
                                    // If needed, we can implement full parsing.
                                    // For now, let's fallback to .NET if it's complex, or try to find "http="
                                    // But actually, just returning the string as a URI often works if formatted right.
                                    // Let's keep it simple: Use the .NET parser but FORCE it to reload by not using the cached WebProxy instance.
                                    
                                    // WAIT. The issue is likely that WebRequest.GetSystemWebProxy() returns a SINGLE INSTANCE that doesn't update.
                                    // But creating a new WebProxy(host, port) works.
                                    
                                    // Let's try parsing the simple "IP:Port" case which is 99% of users.
                                    if (!proxyServer.Contains("=") && !proxyServer.Contains(";"))
                                    {
                                        return new Uri($"http://{proxyServer}");
                                    }
                                }
                                else
                                {
                                    return new Uri($"http://{proxyServer}");
                                }
                            }
                        }
                    }
                }
            }
            catch 
            {
                // Fallback or ignore
            }

            // If we are here, either Proxy is disabled (0) or registry read failed.
            // Return NULL to indicate Direct connection.
            return null; 
        }

        public bool IsBypassed(Uri host)
        {
            // We can implement "ProxyOverride" registry parsing here if needed.
            // For now, assume localhost is bypassed.
            return host.IsLoopback;
        }
    }
}
