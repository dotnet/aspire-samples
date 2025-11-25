using System.Diagnostics;

namespace ClientAppsIntegration.WinForms;

public partial class MainForm : Form
{
    private readonly ActivitySource _activitySource = new(Program.HostEnvironment?.ApplicationName ?? "");
    private readonly ILogger _logger;
    private readonly WeatherApiClient _weatherApiClient;
    private readonly CancellationTokenSource _closingCts = new();

    public MainForm(ILogger<MainForm> logger, WeatherApiClient weatherApiClient)
    {
        InitializeComponent();

        _logger = logger;
        _weatherApiClient = weatherApiClient;
    }

    private async void btnLoadWeather_Click(object sender, EventArgs e)
    {
        using var activity = _activitySource.StartActivity("Load Weather", ActivityKind.Client);

        btnLoadWeather.Enabled = false;
        pbLoading.Visible = true;

        try
        {
            if (chkForceError.Checked)
            {
                throw new InvalidOperationException("Forced error!");
            }

            var weather = await _weatherApiClient.GetWeatherAsync(_closingCts.Token);
            dgWeather.DataSource = weather;
        }
        catch (TaskCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Operation was canceled");
            return;
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error loading weather");

            dgWeather.DataSource = null;
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        pbLoading.Visible = false;
        btnLoadWeather.Enabled = true;
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _closingCts.Cancel();
    }
}
