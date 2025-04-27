using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace Fonts_Downloader
{
    public class Files
    {
        [JsonPropertyName("100")]
        public string _100 { get; set; }

        [JsonPropertyName("200")]
        public string _200 { get; set; }

        [JsonPropertyName("300")]
        public string _300 { get; set; }

        [JsonPropertyName("500")]
        public string _500 { get; set; }

        [JsonPropertyName("600")]
        public string _600 { get; set; }

        [JsonPropertyName("700")]
        public string _700 { get; set; }

        [JsonPropertyName("800")]
        public string _800 { get; set; }

        [JsonPropertyName("900")]
        public string _900 { get; set; }

        [JsonPropertyName("100italic")]
        public string _100italic { get; set; }

        [JsonPropertyName("200italic")]
        public string _200italic { get; set; }

        [JsonPropertyName("300italic")]
        public string _300italic { get; set; }

        [JsonPropertyName("regular")]
        public string _400 { get; set; }

        [JsonPropertyName("italic")]
        public string _400italic { get; set; }

        [JsonPropertyName("500italic")]
        public string _500italic { get; set; }

        [JsonPropertyName("600italic")]
        public string _600italic { get; set; }

        [JsonPropertyName("700italic")]
        public string _700italic { get; set; }

        [JsonPropertyName("800italic")]
        public string _800italic { get; set; }

        [JsonPropertyName("900italic")]
        public string _900italic { get; set; }
    }

    public class Item
    {
        public string Family { get; set; }
        public List<string> Variants { get; set; }
        public List<string> Subsets { get; set; }
        public Files Files { get; set; }
        public string category { get; set; }
    }

    public class Root
    {
        public List<Item> Items { get; set; }
        public Error Error { get; set; }
    }

    public class Error
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }
}