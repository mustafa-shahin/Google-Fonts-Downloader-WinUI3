using NUglify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fonts_Downloader
{
    public class CssGenerator
    {
        public void CreateCSS(Item selectedFont, string folderName, bool includeWoff2, bool minify = false, IEnumerable<string> subsets = null)
        {
            try
            {
                if (selectedFont == null || string.IsNullOrEmpty(folderName))
                    throw new ArgumentException("Font or folder name cannot be null");


                var fontFamily = selectedFont.Family;
                var fontVariants = new List<string>(selectedFont.Variants ?? Enumerable.Empty<string>());
                var fontSubsets = subsets != null ? new List<string>(subsets) : null;

                var cssEntries = GenerateCssEntries(selectedFont, fontVariants, includeWoff2, fontSubsets);

                if (cssEntries.Any())
                {
                    string fontFolder = Path.Combine(folderName, fontFamily.Replace(" ", ""));
                    Directory.CreateDirectory(fontFolder);

                    string cssFileName = $"{fontFamily.Replace(" ", "")}{(minify ? ".min" : "")}.css";
                    string cssFilePath = Path.Combine(fontFolder, cssFileName);
                    string cssContent = string.Join(Environment.NewLine, cssEntries);

                    if (minify)
                    {
                        var result = Uglify.Css(cssContent);
                        if (result.HasErrors)
                        {
                            throw new Exception("CSS Minification failed: " + result.Errors.First().Message);
                        }
                        cssContent = result.Code;
                    }

                    File.WriteAllText(cssFilePath, cssContent);
                }
            }
            catch (Exception ex)
            {
                Logger.HandleError("Failed to create CSS file", ex);
                throw new InvalidOperationException("Failed to create CSS file", ex);
            }
        }

        private IEnumerable<string> GenerateCssEntries(Item selectedFont, IEnumerable<string> variants, bool includeWoff2, IEnumerable<string> subsets = null)
        {
            var fontFamily = selectedFont.Family;

            return variants.SelectMany(variant =>
                subsets != null && subsets.Any()
                ? subsets.Select(subset => GenerateFontFace(fontFamily, variant, includeWoff2, subset))
                : new[] { GenerateFontFace(fontFamily, variant, includeWoff2) });
        }

        private string GenerateFontFace(string fontFamily, string fontWeight, bool woff2, string subset = null)
        {
            string fontStyle = fontWeight.Contains("italic") ? "italic" : "normal";
            string subsetComment = subset != null ? $"/*{subset}*/\n" : string.Empty;

            // Get proper file name format
            string fontFileName = GenerateCorrectFontFileName(fontFamily, woff2, fontWeight, fontStyle);

            string formatAttribute = woff2 ? " format('woff2')" : string.Empty;
            string fontVariant = Helper.MapVariant(fontWeight).TrimEnd("italic".ToCharArray());

            var styles = new CssStyle();
            styles.Properties[$"{subsetComment}@font-face"] = new Dictionary<string, string>
            {
                {"font-family", $"'{fontFamily}'"},
                {"font-style", fontStyle},
                {"font-weight", fontVariant},
                {"font-display", "swap"},
                {"font-stretch", "normal"},
                {"src", $"url('{fontFileName}'){formatAttribute}"}
            };

            return styles.Render();
        }

        private string GenerateCorrectFontFileName(string fontFamily, bool woff2, string weight, string fontStyle)
        {
            // Remove spaces from font family name
            string sanitizedFontFamily = fontFamily.Replace(" ", "");

            // Get the file extension
            string extension = woff2 ? "woff2" : "ttf";

            // Map the weight number to a name
            string weightNumber = Helper.MapVariant(weight).Replace("italic", "");

            // Get the appropriate weight name (Thin, Light, Regular, Bold, etc.)
            if (!FontWeightNames.TryGetValue(weightNumber, out string weightName))
            {
                weightName = "Regular"; // Default to Regular if not found
            }

            // Create filename in format: FontName-WeightName.ttf or FontName-Italic-WeightName.ttf
            return fontStyle == "italic"
                ? $"{sanitizedFontFamily}-{weightName}Italic.{extension}"
                : $"{sanitizedFontFamily}-{weightName}.{extension}";
        }

        // Dictionary mapping weight numbers to their names
        private static readonly Dictionary<string, string> FontWeightNames = new()
        {
            ["100"] = "Thin",
            ["200"] = "ExtraLight",
            ["300"] = "Light",
            ["400"] = "Regular",
            ["500"] = "Medium",
            ["600"] = "SemiBold",
            ["700"] = "Bold",
            ["800"] = "ExtraBold",
            ["900"] = "Black"
        };
    }

    // Helper class for generating CSS
    public class CssStyle
    {
        public Dictionary<string, Dictionary<string, string>> Properties { get; set; } = new();

        public string Render()
        {
            var result = new StringBuilder();
            foreach (var style in Properties)
            {
                var properties = string.Join(";\n", style.Value.Select(p => $"{p.Key}: {p.Value}"));
                result.AppendLine($"{style.Key} {{\n{properties};\n}}");
            }
            return result.ToString();
        }
    }
}