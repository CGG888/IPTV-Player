using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
                _defaultImage = new BitmapImage(new Uri("pack://application:,,,/iptv.png"));
                _defaultImage.Freeze();
            }
            catch
            {
                _defaultImage = new BitmapImage();
            }

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                AllowAutoRedirect = true
            };
            _http = new HttpClient(handler);
            // Reduced timeout to 5s to prevent long waits on bad URLs
            _http.Timeout = TimeSpan.FromSeconds(5);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
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
                    System.Diagnostics.Debug.WriteLine($"[ImageExtensions] Failed to load {url}: {ex.Message}");
                    // Keep default image on failure
                }
            }
        }

        private static async Task<BitmapImage?> LoadImageAsync(string url)
        {
            try
            {
                string processedUrl = url.Trim();
                if (processedUrl.StartsWith("//"))
                    processedUrl = "http:" + processedUrl;
                else if (!processedUrl.Contains("://") && !Path.IsPathRooted(processedUrl))
                {
                    if (!File.Exists(Path.GetFullPath(processedUrl)) && processedUrl.Contains("."))
                        processedUrl = "http://" + processedUrl;
                }

                byte[]? data = null;

                if (processedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // True async call
                    data = await _http.GetByteArrayAsync(processedUrl);
                }
                else if (File.Exists(processedUrl))
                {
                    data = await File.ReadAllBytesAsync(processedUrl);
                }

                if (data != null && data.Length > 0)
                {
                    return await Task.Run(() =>
                    {
                        try
                        {
                            using var ms = new MemoryStream(data);
                            var img = new BitmapImage();
                            img.BeginInit();
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.CreateOptions = BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.IgnoreImageCache;
                            img.StreamSource = ms;
                            img.DecodePixelWidth = 160; // Downscale to save memory
                            img.EndInit();
                            img.Freeze();
                            return img;
                        }
                        catch
                        {
                            return null;
                        }
                    });
                }
            }
            catch
            {
                // Ignore download errors
            }
            return null;
        }
    }
}
