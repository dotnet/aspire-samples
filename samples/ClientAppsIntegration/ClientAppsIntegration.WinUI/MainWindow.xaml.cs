using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI.Popups;


namespace ClientAppsIntegration.WinUI;

public sealed partial class MainWindow : Window
{
    //public MainWindow()
    //{
    //    this.InitializeComponent();
    //}

    private readonly ILogger<MainWindow> _logger;
    private readonly WeatherApiClient _weatherApiClient;
    private readonly CancellationTokenSource _closingCts = new();

    public MainWindow(ILogger<MainWindow> logger, WeatherApiClient weatherApiClient)
    {
        _logger = logger;
        _weatherApiClient = weatherApiClient;

        InitializeComponent();

        pbLoading.Visibility = Visibility.Collapsed;
        dgWeather.Visibility = Visibility.Collapsed;
    }

    private async void btnLoad_Click(object sender, RoutedEventArgs e)
    {
        btnLoad.IsEnabled = false;
        pbLoading.Visibility = Visibility.Visible;

        try
        {
            if (chkForceError.IsChecked == true)
            {
                throw new InvalidOperationException("Forced error!");
            }

            var weather = await _weatherApiClient.GetWeatherAsync(_closingCts.Token);
            dgWeather.ItemsSource = weather;
            dgWeather.Visibility = Visibility.Visible;
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading weather");

            dgWeather.Visibility = Visibility.Collapsed;
            dgWeather.ItemsSource = null;

            var dlg = new MessageDialog(ex.Message, "Error");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(dlg, hwnd);

            await dlg.ShowAsync();  
        }

        pbLoading.Visibility = Visibility.Collapsed;
        btnLoad.IsEnabled = true;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _closingCts.Cancel();
    }
}
