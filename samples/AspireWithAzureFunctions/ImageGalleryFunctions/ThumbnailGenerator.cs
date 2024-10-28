using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ImageGalleryFunctions;

public class ThumbnailGenerator(ILogger<ThumbnailGenerator> logger,
        QueueServiceClient queueServiceClient,
        BlobServiceClient blobServiceClient)
{
    public class UploadResult
    {
    }

    [Function("ThumbnailGenerator")]
    public async Task Run([BlobTrigger("images/{name}", Connection = "blobs")] Stream stream, string name)
    {
        try
        {
            int targetHeight = 128;

            using var originalBitmap = SKBitmap.Decode(stream);
            float scale = (float)targetHeight / originalBitmap.Height;
            int targetWidth = (int)(originalBitmap.Width * scale);

            using var resizedBitmap = originalBitmap.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.High);
            using var resizedStream = new MemoryStream();
            using var image = SKImage.FromBitmap(resizedBitmap);

            image.Encode(SKEncodedImageFormat.Jpeg, 100).SaveTo(resizedStream);
            resizedStream.Position = 0;

            var containerClient = blobServiceClient.GetBlobContainerClient("thumbnails");
            var blobClient = containerClient.GetBlobClient(name);
            await blobClient.UploadAsync(resizedStream, overwrite: true);

            var resultsQueueClient = queueServiceClient.GetQueueClient("thumbnailresults");
            await resultsQueueClient.SendMessageAsync(JsonSerializer.Serialize(new UploadResult()));
        }
        catch (Exception ex)
        {
            logger.LogError($"Error generating thumbnail for image: {name}. Exception: {ex.Message}");
        }
    }
}
