using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using ImageGallery.Shared.Serialization;
using System.Text.Json;

namespace ImageGallery.FrontEnd;

public sealed class StorageWorker(
    QueueClient thumbnailResultsQueueClient,
    [FromKeyedServices("images")] BlobContainerClient imagesContainerClient,
    [FromKeyedServices("thumbnails")] BlobContainerClient thumbsContainerClient,
    QueueMessageHandler handler,
    ILogger<StorageWorker> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting storage worker.");

        await Task.WhenAll(
            thumbnailResultsQueueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken),
            imagesContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken),
            thumbsContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken));

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await thumbnailResultsQueueClient.ReceiveMessageAsync(
                    TimeSpan.FromSeconds(1), stoppingToken);

                if (message is null or { Value: null })
                {
                    logger.LogDebug(
                        "Message received but was either null or without value.");

                    continue;
                }

                var result = JsonSerializer.Deserialize(
                    message.Value.Body.ToString(), SerializationContext.Default.UploadResult);

                if (result is null or { IsSuccessful: false })
                {
                    logger.LogWarning(
                        "Message upload result was either null or unsuccessful for {Name}.",
                        result?.Name);

                    continue;
                }

                logger.LogInformation(
                        "Relaying message of a successful upload...");

                await handler.OnMessageReceivedAsync(result);

                await thumbnailResultsQueueClient.DeleteMessageAsync(
                    message.Value.MessageId, message.Value.PopReceipt, stoppingToken);
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMilliseconds(7_500), stoppingToken);
            }
        }
    }
}
