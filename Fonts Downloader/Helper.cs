using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Fonts_Downloader
{
    public static class Helper
    {
        private static readonly Dictionary<string, string> FontWeights = new()
        {
            ["100"] = "Thin",
            ["200"] = "ExtraLight",
            ["300"] = "Light",
            ["400"] = "Regular",
            ["500"] = "Medium",
            ["600"] = "SemiBold",
            ["700"] = "Bold",
            ["800"] = "ExtraBold",
            ["900"] = "Black",
        };

        public static string GetFontFileStyles(string weight)
        {
            if (string.IsNullOrEmpty(weight))
                return null;

            string normalizedWeight = weight.Replace(" italic", "").Replace("italic", "");
            FontWeights.TryGetValue(normalizedWeight, out string fontFileStyle);
            return fontFileStyle;
        }

        public static string MapVariant(string variant)
        {
            return variant switch
            {
                "regular" => "400",
                "400 italic" => "400italic",
                "italic" => "400italic",
                _ => variant,
            };
        }

        public static string FontFileName(string fontName, bool woff2, string weight)
        {
            if (string.IsNullOrEmpty(fontName) || string.IsNullOrEmpty(weight))
                return string.Empty;

            // Check if weight contains italic
            bool isItalic = weight.Contains("italic");

            // Get numeric weight (remove italic and clean up)
            string numericWeight = MapVariant(weight).Replace(" italic", "").Replace("italic", "");

            // Get weight style name (like Regular, Bold, etc.)
            string weightName = "Regular";
            if (FontWeights.TryGetValue(numericWeight, out string value))
            {
                weightName = value;
            }

            // Format: FontName-WeightName.ttf or FontName-WeightNameItalic.ttf
            string format = woff2 ? "woff2" : "ttf";

            if (isItalic)
            {
                return $"{fontName.Replace(" ", "")}-{weightName}Italic.{format}";
            }
            else
            {
                return $"{fontName.Replace(" ", "")}-{weightName}.{format}";
            }
        }

        public static async Task<bool> IsNetworkAvailableAsync()
        {
            try
            {
                // First check through network interfaces
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface networkInterface in networkInterfaces)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                        (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                         networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                        networkInterface.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        return true;
                    }
                }

                // Then do a ping test
                using var ping = new Ping();
                var result = await ping.SendPingAsync("www.google.com", 3000); // Use a 3 second timeout
                return (result.Status == IPStatus.Success);
            }
            catch (Exception ex)
            {
                Logger.HandleError("Network check failed", ex);
                return false;
            }
        }

        public static string GetAPIKey()
        {
            try
            {
                // Get the project root directory 
                string projectRoot = Directory.GetCurrentDirectory();
                string configFile = Path.Combine(projectRoot, "appsettings.json");

                // If not found in current directory, try one more approach
                if (!File.Exists(configFile))
                {
                    string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string executableDir = Path.GetDirectoryName(executablePath);
                    configFile = Path.Combine(executableDir, "appsettings.json");
                }

                if (!File.Exists(configFile))
                {
                    // Log that the file wasn't found
                    Logger.HandleError("appsettings.json not found", new FileNotFoundException("Could not find appsettings.json"));
                    return null;
                }

                var json = File.ReadAllText(configFile);
                using JsonDocument document = JsonDocument.Parse(json);

                if (document.RootElement.TryGetProperty("APIKey", out JsonElement apiKeyElement))
                {
                    return apiKeyElement.GetString();
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.HandleError("Error loading API key", ex);
                return null;
            }
        }
    }
}