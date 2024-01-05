using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientAppsIntegration.WPF;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly WeatherApiClient _weatherApiClient;
    private readonly CancellationTokenSource _closingCts = new();

    public MainWindow(ILogger<MainWindow> logger, WeatherApiClient weatherApiClient)
    {
        _logger = logger;
        _weatherApiClient = weatherApiClient;

        InitializeComponent();

        pbLoading.Visibility = Visibility.Hidden;
        dgWeather.Visibility = Visibility.Hidden;
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

            dgWeather.Visibility = Visibility.Hidden;
            dgWeather.ItemsSource = null;

            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        pbLoading.Visibility = Visibility.Hidden;
        btnLoad.IsEnabled = true;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _closingCts.Cancel();
    }
}
