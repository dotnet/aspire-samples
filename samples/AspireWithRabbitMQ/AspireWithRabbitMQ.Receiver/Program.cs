using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging");

builder.Services.AddHostedService<ProcessRabbitMQMessage>();

var app = builder.Build();
app.Run();

