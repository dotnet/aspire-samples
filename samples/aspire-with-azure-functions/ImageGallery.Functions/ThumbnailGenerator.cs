using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using ImageGallery.Shared;
using ImageGallery.Shared.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ImageGallery.Functions;

public class ThumbnailGenerator(ILogger<ThumbnailGenerator> logger,
    QueueServiceClient queueServiceClient,
    BlobServiceClient blobServiceClient)
{
    private readonly BlobContainerClient _containerClient = blobServiceClient.GetBlobContainerClient("thumbnails");
    private readonly QueueClient _resultsQueueClient = queueServiceClient.GetQueueClient("thumbnail-queue");
    private const int TargetHeight = 128;

    [Function(nameof(ThumbnailGenerator))]
    public async Task Resize([BlobTrigger("images/{name}", Connection = "blobs")] Stream stream, string name)
    {
        try
        {
            using var resizedStream = GetResizedImageStream(name, stream, SKEncodedImageFormat.Jpeg);

            await UploadResizedImageAsync(name, resizedStream);

            await SendQueueMessageAsync(name);
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Error generating thumbnail for image: {Name}. Exception: {Message}",
                name, ex.Message);
        }
    }

    private MemoryStream GetResizedImageStream(string name, Stream stream, SKEncodedImageFormat format)
    {
        using var originalBitmap = SKBitmap.Decode(stream);

        var scale = (float)TargetHeight / originalBitmap.Height;
        var targetWidth = (int)(originalBitmap.Width * scale);

        using var resizedBitmap = originalBitmap.Resize(
            new SKImageInfo(targetWidth, TargetHeight), new SKSamplingOptions (SKCubicResampler.Mitchell));

        using var image = SKImage.FromBitmap(resizedBitmap);

        // Do not put in a using, as this is returned to the caller.
        var resizedStream = new MemoryStream();

        image.Encode(format, 100).SaveTo(resizedStream);

        logger.LogInformation(
            "Resized image {Name} from {OriginalWidth}x{OriginalHeight} to {Width}x{Height}.",
            name, originalBitmap.Width, originalBitmap.Height, targetWidth, TargetHeight);

        return resizedStream;
    }

    private async Task UploadResizedImageAsync(string name, MemoryStream resizedStream)
    {
        resizedStream.Position = 0;

        var blobClient = _containerClient.GetBlobClient(name);

        logger.LogDebug("Uploading {Name}", name);

        await blobClient.UploadAsync(resizedStream, overwrite: true);

        logger.LogInformation("Uploaded {Name}", name);
    }

    private async Task SendQueueMessageAsync(string name)
    {
        var jsonMessage = JsonSerializer.Serialize(
            new UploadResult(name, true), SerializationContext.Default.UploadResult);

        logger.LogDebug("Signaling upload of {Name}", name);

        await _resultsQueueClient.SendMessageAsync(jsonMessage);

        logger.LogInformation("Signaled upload of {Name}", name);
    }
}
