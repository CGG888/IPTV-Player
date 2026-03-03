using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using LibmpvIptvClient.Diagnostics;

namespace LibmpvIptvClient.Helpers
{
    public static class ImageExtensions
    {
        private static readonly HttpClient _http;
        private static readonly ConcurrentDictionary<string, BitmapImage> _cache = new ConcurrentDictionary<string, BitmapImage>();
        private static readonly BitmapImage _defaultImage;

        static ImageExtensions()
        {
            try
            {
                _defaultImage = new BitmapImage(new Uri("pack://application:,,,/srcbox.png"));
                _defaultImage.Freeze();
            }
            catch
            {
                _defaultImage = new BitmapImage();
            }

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _http = new HttpClient(handler);
            // Increased timeout to 15s to allow slower servers
            _http.Timeout = TimeSpan.FromSeconds(15);
            // Simulate a common browser User-Agent
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
            _http.DefaultRequestHeaders.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
            _http.DefaultRequestHeaders.Add("Referer", "http://12.12.12.177:9443/"); // 尝试添加 Referer 绕过防盗链
        }

        public static string GetRemoteUrl(DependencyObject obj)
        {
            return (string)obj.GetValue(RemoteUrlProperty);
        }

        public static void SetRemoteUrl(DependencyObject obj, string value)
        {
            obj.SetValue(RemoteUrlProperty, value);
        }

        // Using a DependencyProperty as the backing store for RemoteUrl.
        public static readonly DependencyProperty RemoteUrlProperty =
            DependencyProperty.RegisterAttached("RemoteUrl", typeof(string), typeof(ImageExtensions), new PropertyMetadata(null, OnRemoteUrlChanged));

        private static async void OnRemoteUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.Image img)
            {
                var url = e.NewValue as string;

                if (string.IsNullOrWhiteSpace(url))
                {
                    img.Source = _defaultImage;
                    return;
                }

                // Check cache first
                if (_cache.TryGetValue(url, out var cached))
                {
                    img.Source = cached;
                    return;
                }

                // Set default/loading image while fetching
                img.Source = _defaultImage;

                try
                {
                    // Use captured context to ensure UI update happens on UI thread
                    var bitmap = await LoadImageAsync(url);
                    if (bitmap != null)
                    {
                        _cache[url] = bitmap;
                        // Verify the URL hasn't changed while we were loading
                        if (GetRemoteUrl(img) == url)
                        {
                            img.Source = bitmap;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load {url}: {ex.Message}");
                    // Keep default image on failure
                }
            }
        }

        private static async Task<BitmapImage?> LoadImageAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null; // Pre-check to prevent null argument exceptions

            try
            {
                string processedUrl = url.Trim();
                
                // Better URL normalization
                if (processedUrl.StartsWith("//"))
                    processedUrl = "http:" + processedUrl;
                else if (!processedUrl.Contains("://") && !Path.IsPathRooted(processedUrl))
                {
                    // If it looks like a domain, assume http
                    if (!File.Exists(Path.GetFullPath(processedUrl)) && processedUrl.Contains("."))
                        processedUrl = "http://" + processedUrl;
                }

                byte[]? data = null;

                if (processedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // True async call
                    try 
                    {
                        data = await _http.GetByteArrayAsync(processedUrl);
                    }
                    catch (HttpRequestException he)
                    {
                        Logger.Error($"HTTP Request failed for {processedUrl}: {he.Message} Status: {he.StatusCode}");
                        return null;
                    }
                }
                else if (File.Exists(processedUrl))
                {
                    data = await File.ReadAllBytesAsync(processedUrl);
                }
                else
                {
                    // URL is neither a valid http(s) url nor a local file path
                    Logger.Warn($"Skipping invalid image source: {processedUrl} (Original: {url})");
                    return null;
                }

                if (data != null && data.Length > 0)
                {
                    // Check if it's HTML (common error for "200 OK" but not image)
                    if (data.Length < 1024)
                    {
                        try
                        {
                            var header = System.Text.Encoding.ASCII.GetString(data, 0, Math.Min(data.Length, 100));
                            if (header.Contains("<html") || header.Contains("<!DOCTYPE"))
                            {
                                Logger.Warn($"Received HTML instead of image for {url}");
                                return null;
                            }
                        }
                        catch { }
                    }

                    // Create bitmap on UI thread or Frozen on background thread?
                    // Best practice: Create on background, freeze, return.
                    return await Task.Run(() =>
                    {
                        using var ms = new MemoryStream(data);
                        
                        try
                        {
                            var img = new BitmapImage();
                            img.BeginInit();
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.CreateOptions = BitmapCreateOptions.None; // Use default
                            img.StreamSource = ms;
                            img.EndInit();
                            img.Freeze();
                            return img;
                        }
                        catch (Exception ex)
                        {
                            // Log detailed error
                            Logger.Error($"Bitmap decode error for {url}. Data len: {data.Length}. Error: {ex}");
                            return null;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Download error for {url}: {ex.Message}");
            }
            return null;
        }
    }
}
