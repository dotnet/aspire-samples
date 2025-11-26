namespace ClientAppsIntegration.WinForms;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        btnLoadWeather = new Button();
        pbLoading = new ProgressBar();
        dgWeather = new DataGridView();
        chkForceError = new CheckBox();
        ((System.ComponentModel.ISupportInitialize)dgWeather).BeginInit();
        SuspendLayout();
        // 
        // btnLoadWeather
        // 
        btnLoadWeather.Location = new Point(12, 12);
        btnLoadWeather.Name = "btnLoadWeather";
        btnLoadWeather.Size = new Size(145, 29);
        btnLoadWeather.TabIndex = 0;
        btnLoadWeather.Text = "Load Weather";
        btnLoadWeather.UseVisualStyleBackColor = true;
        btnLoadWeather.Click += btnLoadWeather_Click;
        // 
        // pbLoading
        // 
        pbLoading.Cursor = Cursors.IBeam;
        pbLoading.Location = new Point(163, 12);
        pbLoading.Name = "pbLoading";
        pbLoading.Size = new Size(125, 29);
        pbLoading.Style = ProgressBarStyle.Continuous;
        pbLoading.TabIndex = 1;
        pbLoading.Visible = false;
        // 
        // dgWeather
        // 
        dgWeather.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgWeather.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgWeather.Location = new Point(12, 77);
        dgWeather.Name = "dgWeather";
        dgWeather.ReadOnly = true;
        dgWeather.RowHeadersWidth = 51;
        dgWeather.Size = new Size(809, 453);
        dgWeather.TabIndex = 2;
        // 
        // chkForceError
        // 
        chkForceError.AutoSize = true;
        chkForceError.Location = new Point(12, 47);
        chkForceError.Name = "chkForceError";
        chkForceError.Size = new Size(103, 24);
        chkForceError.TabIndex = 3;
        chkForceError.Text = "Force Error";
        chkForceError.UseVisualStyleBackColor = true;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(833, 542);
        Controls.Add(chkForceError);
        Controls.Add(dgWeather);
        Controls.Add(pbLoading);
        Controls.Add(btnLoadWeather);
        Name = "MainForm";
        Text = "Weather Forecasts";
        FormClosing += MainForm_FormClosing;
        ((System.ComponentModel.ISupportInitialize)dgWeather).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button btnLoadWeather;
    private ProgressBar pbLoading;
    private DataGridView dgWeather;
    private CheckBox chkForceError;
}
