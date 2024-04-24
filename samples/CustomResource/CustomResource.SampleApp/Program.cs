using System.Net.Mail;
using CustomResource.SampleApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SmtpClient>((sp) =>
{
    var smtpUri = new Uri(builder.Configuration.GetConnectionString("maildev")!);
    var smtpClient = new SmtpClient(smtpUri.Host, smtpUri.Port);
    return smtpClient;
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/smtp", async (SmtpClient smtpClient) =>
{
    var message = new MailMessage("test@test.com", "test@test.com");
    await smtpClient.SendMailAsync(message);
});

app.Run();
