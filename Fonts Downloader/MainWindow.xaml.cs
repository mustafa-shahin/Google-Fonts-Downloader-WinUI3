using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;
using WinRT.Interop;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fonts_Downloader
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private List<Item> FontItems;
        private string FolderName;
        private Item SelectedFontItem;
        private readonly WebFontsService _webFontsService;
        private readonly FontSelector _fontSelector;
        private readonly HtmlBuilder _htmlBuilder;
        private readonly string _htmlPath;
        private bool _isInitialized = false;
        private bool _isWoff2Selected = false;

        public MainWindow()
        {
            this.InitializeComponent();


            // Initialize services
            _htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FontsWebView.html");
            _htmlBuilder = new HtmlBuilder();
            _fontSelector = new FontSelector(_htmlBuilder);
            _webFontsService = new WebFontsService(ContentDialog);

            SetupControls();
            InitializeWebView();
            CheckInternetConnection();
            LoadApiKey();
        }

        private void SetupControls()
        {
            // Disable controls initially
            FontFamilyComboBox.IsEnabled = false;
            DownloadFontButton.IsEnabled = false;
            TtfRadioButton.Visibility = Visibility.Collapsed;
            Woff2RadioButton.Visibility = Visibility.Collapsed;
            MinifyCheckBox.Visibility = Visibility.Collapsed;
            SubsetsLabel.Visibility = Visibility.Collapsed;
            SubsetsListView.Visibility = Visibility.Collapsed;
            FontVariantsLabel.Visibility = Visibility.Collapsed;
            FontVariantsListView.Visibility = Visibility.Collapsed;

            // Setup default download folder
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            FolderName = Path.Combine(desktopPath, "GoogleFonts");
            SelectedFolderTextBox.Text = FolderName;
        }

        private void InitializeWebView()
        {
            try
            {
                webView.DefaultBackgroundColor = new Windows.UI.Color { A = 255, R = 33, G = 33, B = 36 };

                if (File.Exists(_htmlPath))
                {
                    try
                    {
                        string fileContent = File.ReadAllText(_htmlPath);
                        if (!string.IsNullOrEmpty(fileContent))
                        {
                            File.WriteAllText(_htmlPath, string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.HandleError("An error occurred while initializing the WebView", ex);
                    }
                }

                _htmlBuilder.DefaultHtml(Helper.IsNetworkAvailableAsync().Result);

                // Setup URI source
                webView.Source = new Uri(_htmlPath);
                webView.NavigationStarting += WebView_NavigationStarting;
                webView.NavigationCompleted += WebView_NavigationCompleted;
            }
            catch (Exception ex)
            {
                Logger.HandleError("Failed to initialize WebView", ex);
            }
        }

        private async void CheckInternetConnection()
        {
            if (await Helper.IsNetworkAvailableAsync())
            {
                TtfRadioButton.Visibility = Visibility.Collapsed;
                Woff2RadioButton.Visibility = Visibility.Collapsed;
                FontFamilyComboBox.IsEnabled = false;
                MinifyCheckBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                webView.Reload();
                NoInternetImage.Visibility = Visibility.Visible;
            }
        }

        private void LoadApiKey()
        {
            string apiKey = Helper.GetAPIKey();
            if (!string.IsNullOrEmpty(apiKey))
            {
                ApiKeyTextBox.Text = apiKey;
            }
        }

        private async void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            // Initialize the folder picker with the window handle
            InitializeWithWindow.Initialize(folderPicker, WindowNative.GetWindowHandle(this));

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                FolderName = folder.Path;
                SelectedFolderTextBox.Text = FolderName;
            }
        }

        private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string selectedFontFamily = FontFamilyComboBox.SelectedItem as string;
                SelectedFontItem = _fontSelector.FontSelection(selectedFontFamily, FontItems, UpdateUIComponents);

                // Enable download button if variants are selected
                UpdateDownloadButtonState();
            }
            catch (Exception ex)
            {
                Logger.HandleError("Error selecting font", ex);
            }
        }

        private void UpdateUIComponents(string selectedFontLabel, IEnumerable<string> subsets, IEnumerable<string> variants)
        {
            try
            {
                // Clear existing items
                SubsetsListView.Items.Clear();
                FontVariantsListView.Items.Clear();

                // Show UI components
                SubsetsLabel.Visibility = Visibility.Visible;
                SubsetsListView.Visibility = Visibility.Visible;
                FontVariantsLabel.Visibility = Visibility.Visible;
                FontVariantsListView.Visibility = Visibility.Visible;
                MinifyCheckBox.Visibility = Visibility.Visible;

                // Populate lists
                foreach (var subset in subsets)
                {
                    SubsetsListView.Items.Add(subset);
                }

                foreach (var variant in variants)
                {
                    FontVariantsListView.Items.Add(variant);
                }

                // Ensure WebView is navigated to the correct HTML file
                try
                {
                    if (File.Exists(_htmlPath))
                    {
                        webView.Source = new Uri(_htmlPath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.HandleError("Error navigating WebView", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.HandleError("Error updating UI components", ex);
            }
        }

        private async void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ApiKeyTextBox.Text)) return;

            bool isConnected = await Helper.IsNetworkAvailableAsync();
            if (!isConnected)
            {
                webView.Reload();
                NoInternetImage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                // Show loading indicator
                LoadingProgressRing.IsActive = true;
                LoadingProgressRing.Visibility = Visibility.Visible;

                FontItems = await _webFontsService.GetWebFontsAsync(ApiKeyTextBox.Text, _isWoff2Selected);

                if (FontItems != null && FontItems.Count != 0)
                {
                    // Clear and populate font family dropdown
                    FontFamilyComboBox.Items.Clear();
                    foreach (var item in FontItems)
                    {
                        FontFamilyComboBox.Items.Add(item.Family);
                    }

                    // Show format selection options
                    TtfRadioButton.Visibility = Visibility.Visible;
                    Woff2RadioButton.Visibility = Visibility.Visible;
                    FontFamilyComboBox.IsEnabled = true;
                    MinifyCheckBox.Visibility = Visibility.Visible;
                    TtfRadioButton.IsChecked = true;

                    if (_webFontsService.FontResponse?.Error == null)
                    {
                        webView.Reload();
                        NoInternetImage.Visibility = Visibility.Collapsed;
                    }
                }
                else if (_webFontsService.FontResponse?.Error != null)
                {
                    ContentDialog.Title = "API Error";
                    ContentDialogText.Text = $"API Error: {_webFontsService.FontResponse.Error.Message}";
                    await ContentDialog.ShowAsync();

                    _htmlBuilder.DefaultHtml(false, _webFontsService);
                    webView.Reload();
                    NoInternetImage.Visibility = Visibility.Visible;
                }
                else
                {
                    await Task.Run(() => _htmlBuilder.DefaultHtml(false, _webFontsService));
                    webView.Reload();
                    NoInternetImage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Logger.HandleError("An error occurred while processing your request.", ex);

                ContentDialog.Title = "Error";
                ContentDialogText.Text = "An error occurred while processing your request. Please try again later.";
                await ContentDialog.ShowAsync();
            }
            finally
            {
                // Hide loading indicator
                LoadingProgressRing.IsActive = false;
                LoadingProgressRing.Visibility = Visibility.Collapsed;
            }
        }

        private async void DownloadFont_Click(object sender, RoutedEventArgs e)
        {
            if (!await Helper.IsNetworkAvailableAsync())
            {
                webView.Reload();
                NoInternetImage.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrWhiteSpace(FolderName))
            {
                ContentDialog.Title = "Information";
                ContentDialogText.Text = "Please select a folder to save the fonts.";
                await ContentDialog.ShowAsync();
                return;
            }

            if (FontVariantsListView.SelectedItems.Count == 0)
            {
                ContentDialog.Title = "Information";
                ContentDialogText.Text = "Please choose at least one font variant to download.";
                await ContentDialog.ShowAsync();
                return;
            }

            if (SelectedFontItem is null || SelectedFontItem.Variants is null || SelectedFontItem.Variants.Count == 0)
            {
                Logger.HandleError("Selected font item or its variants are invalid.", new Exception("Invalid font selection."));
                return;
            }

            try
            {
                // Show loading indicator and disable button
                LoadingProgressRing.IsActive = true;
                LoadingProgressRing.Visibility = Visibility.Visible;
                DownloadFontButton.IsEnabled = false;
                DownloadFontButton.Content = "Downloading...";

                // Create a copy of the selected font to modify
                var selectedFont = SelectedFontItem;

                // Set the selected variants
                selectedFont.Variants = FontVariantsListView.SelectedItems.Cast<string>().Where(m => !string.IsNullOrEmpty(m)).ToList();

                // Get selected subsets
                var subsets = SubsetsListView.SelectedItems.Cast<string>().ToArray();

                // Create the CSS file first
                var cssGenerator = new CssGenerator();
                await Task.Run(() => cssGenerator.CreateCSS(selectedFont, FolderName, _isWoff2Selected, MinifyCheckBox.IsChecked ?? false, subsets));

                // Download the font files
                using (var downloader = new FontFilesDownloader())
                {
                    await downloader.DownloadAsync(selectedFont, FolderName, _isWoff2Selected);
                }

                // Show success message and offer to open the folder
                await OpenDownloadFolder(FolderName, selectedFont.Family.Replace(" ", ""));
            }
            catch (Exception ex)
            {
                Logger.HandleError("Download failed", ex);

                ContentDialog.Title = "Download Error";
                ContentDialogText.Text = "There was an error downloading the fonts. Please check the log for details.";
                await ContentDialog.ShowAsync();
            }
            finally
            {
                // Reset UI state
                LoadingProgressRing.IsActive = false;
                LoadingProgressRing.Visibility = Visibility.Collapsed;
                DownloadFontButton.IsEnabled = true;
                DownloadFontButton.Content = "Download Font";
            }
        }

        private async Task OpenDownloadFolder(string folderName, string fontFolderName)
        {
            try
            {
                ContentDialog.Title = "Download Completed";
                ContentDialogText.Text = "The download has been completed successfully. Would you like to open the folder containing the downloaded files?";
                ContentDialog.PrimaryButtonText = "Yes";
                ContentDialog.SecondaryButtonText = "No";

                var result = await ContentDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var folderToOpen = Path.Combine(folderName, fontFolderName);
                    if (Directory.Exists(folderToOpen))
                    {
                        // Launch the folder in File Explorer
                        await Launcher.LaunchFolderPathAsync(folderToOpen);
                    }
                    else
                    {
                        ContentDialog.Title = "Error";
                        ContentDialogText.Text = $"Folder not found: {folderToOpen}";
                        ContentDialog.PrimaryButtonText = "OK";
                        ContentDialog.SecondaryButtonText = "";
                        await ContentDialog.ShowAsync();
                    }
                }

                // Reset dialog buttons
                ContentDialog.PrimaryButtonText = "OK";
                ContentDialog.SecondaryButtonText = "";
            }
            catch (Exception ex)
            {
                Logger.HandleError("Error opening folder", ex);

                ContentDialog.Title = "Error";
                ContentDialogText.Text = $"Error opening folder: {ex.Message}";
                ContentDialog.PrimaryButtonText = "OK";
                ContentDialog.SecondaryButtonText = "";
                await ContentDialog.ShowAsync();
            }
        }

        private async void GitHubLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri("https://github.com/mustafa-shahin/Fonts-Downloader"));
            }
            catch (Exception ex)
            {
                Logger.HandleError("Error opening GitHub repository", ex);

                ContentDialog.Title = "Error";
                ContentDialogText.Text = $"Error opening GitHub repository: {ex.Message}";
                await ContentDialog.ShowAsync();
            }
        }


        private void MinifyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ContentDialog.Title = "Information";
            ContentDialogText.Text = "All comments will be deleted in the minified version";
            ContentDialog.ShowAsync();
            SubsetsListView.IsEnabled = false;
        }

        private void MinifyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SubsetsListView.IsEnabled = true;
        }

        private void SubsetsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MinifyCheckBox.IsEnabled = SubsetsListView.SelectedItems.Count == 0;
        }

        private async void NoInternetImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (await Helper.IsNetworkAvailableAsync())
            {
                if (SelectedFontItem is not null)
                    _htmlBuilder.CreateHtml(SelectedFontItem);
                else
                    _htmlBuilder.DefaultHtml();

                NoInternetImage.Visibility = Visibility.Collapsed;
            }
            else
            {
                _htmlBuilder.DefaultHtml(false, _webFontsService);
            }

            webView.Reload();
        }

        private void WebView_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            try
            {
                if (!e.Uri.Contains("FontsWebView.html") && !e.Uri.StartsWith("file:"))
                {
                    e.Cancel = true;
                    Launcher.LaunchUriAsync(new Uri(e.Uri));
                }
            }
            catch (Exception ex)
            {
                Logger.HandleError($"Navigation error: {ex.Message}", ex);

                ContentDialog.Title = "Error";
                ContentDialogText.Text = $"An error occurred while opening the link: {ex.Message}";
                ContentDialog.ShowAsync();
            }
        }

        private void WebView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            LoadingProgressRing.IsActive = false;
            LoadingProgressRing.Visibility = Visibility.Collapsed;
        }

        private async void FontFormat_Checked(object sender, RoutedEventArgs e)
        {
            _isWoff2Selected = Woff2RadioButton.IsChecked ?? false;

            if (_isInitialized && !string.IsNullOrEmpty(ApiKeyTextBox.Text))
            {
                LoadingProgressRing.IsActive = true;
                LoadingProgressRing.Visibility = Visibility.Visible;

                FontItems = await _webFontsService.GetWebFontsAsync(ApiKeyTextBox.Text, _isWoff2Selected);

                LoadingProgressRing.IsActive = false;
                LoadingProgressRing.Visibility = Visibility.Collapsed;
            }

            _isInitialized = true;
        }

        private void UpdateDownloadButtonState()
        {
            DownloadFontButton.IsEnabled = FontVariantsListView.SelectedItems.Count > 0;
        }
    }
}