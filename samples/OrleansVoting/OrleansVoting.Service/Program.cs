using OrleansVoting.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddKeyedRedis("voting-redis");
builder.AddServiceDefaults();
builder.UseOrleans(orleansBuilder =>
{
    if (builder.Environment.IsDevelopment())
    {
        orleansBuilder.ConfigureEndpoints(Random.Shared.Next(10_000, 50_000), Random.Shared.Next(10_000, 50_000));
    }
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<PollService>();
builder.Services.AddScoped<DemoService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
