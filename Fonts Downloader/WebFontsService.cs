using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fonts_Downloader
{
    public class WebFontsService
    {
        public Root FontResponse { get; private set; }
        public bool Succeeded { get; private set; }
        private readonly HttpClient _httpClient;
        private ContentDialog _dialog;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public WebFontsService(ContentDialog dialog)
        {
            _dialog = dialog;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<Item>> GetWebFontsAsync(string apiKey, bool woff2)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                _dialog.Title = "API Key Required";
                _dialog.Content = "Please enter a valid Google Fonts API key";
                _dialog.PrimaryButtonText = "OK";
                await _dialog.ShowAsync();
                return new List<Item>();
            }

            try
            {
                var woffQuery = woff2 ? "capability=WOFF2" : "";
                var link = $"https://www.googleapis.com/webfonts/v1/webfonts?{woffQuery}&sort=alpha&key={apiKey}";
                var result = await GetAsync(link);

                if (!Succeeded)
                {
                    return new List<Item>();
                }

                // Deserialize and check for errors in the response
                FontResponse = JsonSerializer.Deserialize<Root>(result, _jsonOptions);
                if (FontResponse.Error != null)
                {
                    await Logger.HandleErrorAsync($"API Error: {FontResponse.Error.Message}",
                        new Exception($"API Error Code: {FontResponse.Error.Code}"), _dialog);
                    return new List<Item>();
                }

                return FontResponse.Items ?? new List<Item>();
            }
            catch (HttpRequestException httpRequestException)
            {
                await Logger.HandleErrorAsync("Network error occurred.", httpRequestException, _dialog);
                return new List<Item>();
            }
            catch (Exception ex)
            {
                await Logger.HandleErrorAsync("An error occurred while fetching web fonts.", ex, _dialog);
                return new List<Item>();
            }
        }

        private async Task<string> GetAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    Succeeded = true;
                    return await response.Content.ReadAsStringAsync();
                }

                // Check if the error is related to the API key
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await Logger.HandleErrorAsync("API key is invalid or unauthorized.",
                        new Exception(response.ReasonPhrase), _dialog);
                }
                else
                {
                    // Handle other HTTP errors
                    await Logger.HandleErrorAsync($"HTTP Error: {response.StatusCode} - {response.ReasonPhrase}",
                        new Exception(response.ReasonPhrase), _dialog);
                }

                Succeeded = false;
                return string.Empty;
            }
            catch (HttpRequestException ex)
            {
                Succeeded = false;
                await Logger.HandleErrorAsync("HTTP Request Error", ex, _dialog);
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Succeeded = false;
                await Logger.HandleErrorAsync("Request timeout", ex, _dialog);

                _dialog.Title = "Timeout Error";
                _dialog.Content = "The request to Google Fonts API timed out. Please try again later.";
                _dialog.PrimaryButtonText = "OK";
                await _dialog.ShowAsync();

                return string.Empty;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}