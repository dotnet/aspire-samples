using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System.Text.Json;

namespace ImageGallery.FrontEnd;

public class StorageWorker(QueueServiceClient queueServiceClient, 
    BlobServiceClient blobServiceClient,
    QueueMessageHandler handler) : BackgroundService
{
    QueueClient thumbnailResultsQueueClient = queueServiceClient.GetQueueClient("thumbnailresults");
    BlobContainerClient imageContainerClient = blobServiceClient.GetBlobContainerClient("images");
    BlobContainerClient thumbsContainerClient = blobServiceClient.GetBlobContainerClient("thumbnails");

    public override Task StopAsync(CancellationToken cancellationToken) => base.StopAsync(cancellationToken);

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await thumbnailResultsQueueClient.CreateIfNotExistsAsync();
        await imageContainerClient.CreateIfNotExistsAsync(publicAccessType: Azure.Storage.Blobs.Models.PublicAccessType.Blob);
        await thumbsContainerClient.CreateIfNotExistsAsync(publicAccessType: Azure.Storage.Blobs.Models.PublicAccessType.Blob);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await thumbnailResultsQueueClient.ReceiveMessageAsync(TimeSpan.FromSeconds(1), stoppingToken);
            if (message is not null && message.Value is not null)
            {
                var result = JsonSerializer.Deserialize<UploadResult>(message.Value.Body.ToString());
                if(result is not null)
                {
                    handler.OnMessageReceived(result);
                    await thumbnailResultsQueueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);
                }
            }

            await Task.Delay(1000);
        }
    }
}

public class QueueMessageHandler
{
    public event EventHandler<UploadResult>? MessageReceived;

    public void OnMessageReceived(UploadResult result)
    {
        if (MessageReceived is not null)
            MessageReceived(this, result);
    }
}

public class UploadResult
{
}
