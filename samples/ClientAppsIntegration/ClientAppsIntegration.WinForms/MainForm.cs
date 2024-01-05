namespace ClientAppsIntegration.WinForms;

public partial class MainForm : Form
{
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
            return;
        }
        catch (Exception ex)
        {
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
