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
                : [GenerateFontFace(fontFamily, variant, includeWoff2)]);
        }

        private string GenerateFontFace(string fontFamily, string fontWeight, bool woff2, string subset = null)
        {
            string fontStyle = fontWeight.Contains("italic") ? "italic" : "normal";
            string subsetComment = subset != null ? $"/*{subset}*/\n" : string.Empty;

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
            string sanitizedFontFamily = fontFamily.Replace(" ", "");

            string extension = woff2 ? "woff2" : "ttf";


            string weightNumber = Helper.MapVariant(weight).Replace("italic", "");

            if (!FontWeightNames.TryGetValue(weightNumber, out string weightName))
            {
                weightName = "Regular"; 
            }

          
            return fontStyle == "italic"
                ? $"{sanitizedFontFamily}-{weightName}Italic.{extension}"
                : $"{sanitizedFontFamily}-{weightName}.{extension}";
        }

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

    public class CssStyle
    {
        public Dictionary<string, Dictionary<string, string>> Properties { get; set; } = [];

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