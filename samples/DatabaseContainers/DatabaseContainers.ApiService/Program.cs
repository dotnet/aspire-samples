using DatabaseContainers.ApiService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("Todos");
builder.AddMySqlDataSource("Catalog");
builder.AddKeyedSqlServerClient("AddressBook");
builder.AddKeyedSqlServerClient("LocalDB");

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseSwagger();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.MapTodosApi();
app.MapCatalogApi();
app.MapAddressBookApi();
app.MapLocalDbApi();

app.MapDefaultEndpoints();

app.Run();
