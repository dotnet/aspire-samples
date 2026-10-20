using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRabbitMQClient("messaging");

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("sendmessage",async Task<IResult>(IConnection connection, string messageToSend)=>{

    var channel = connection.CreateModel();
    channel.QueueDeclare(
        queue:"testMessage",
        durable:false,
        exclusive:false,
        autoDelete:false,
        arguments:null
    );
    var body = Encoding.UTF8.GetBytes(messageToSend);

    channel.BasicPublish(
        exchange:string.Empty,
        routingKey:"testMessage",
        mandatory:false,
        basicProperties:null,
        body:body
    );
    return Results.Ok(new {});

});
app.Run();


