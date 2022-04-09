// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Azure.Storage.Blobs;
using ThumbnailFunctionApp;

namespace johnwalraven.Thumbnail
{
    public static class ThumbnailFunction
    {
        private static readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        /// <summary>
        /// Creates a thumbnail and places it into another contatiner in blob storage when an image is added.
        /// </summary>
        /// <param name="eventGridEvent">The event sent from Azure Event Grid</param>
        /// <param name="log">The logger</param>
        /// <returns>void</returns>
        [FunctionName("ThumbnailFunction")]
        public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            try
            {
                var createdEvent = ((JObject)eventGridEvent.Data).ToObject<StorageBlobCreatedEventData>();
                var extension = Path.GetExtension(createdEvent.Url);
                var encoder = ThumbnailHelper.GetEncoder(extension);

                if (encoder != null)
                {
                    var thumbnailContainerName = Environment.GetEnvironmentVariable("THUMBNAIL_CONTAINER_NAME");
                    var imagesContainerName = Environment.GetEnvironmentVariable("IMAGES_CONTAINER_NAME");

                    var blobServiceClient = new BlobServiceClient(BLOB_STORAGE_CONNECTION_STRING);
                    var blobContainerClient = blobServiceClient.GetBlobContainerClient(thumbnailContainerName);

                    var blobName = ThumbnailHelper.GetBlobNameFromUrl(createdEvent.Url);

                    var blob = new BlobClient(BLOB_STORAGE_CONNECTION_STRING, imagesContainerName, blobName);
                    var stream = await blob.DownloadStreamingAsync();

                    using var output = new MemoryStream();
                    using Image image = await Image.LoadAsync(stream.Value.Content);

                    var thumbnailWidth = Convert.ToInt32(Environment.GetEnvironmentVariable("THUMBNAIL_WIDTH"));
                    var divisor = image.Width / thumbnailWidth;
                    var height = Convert.ToInt32(Math.Round((decimal)(image.Height / divisor)));

                    image.Mutate(x => x.Resize(thumbnailWidth, height));
                    image.Save(output, encoder);
                    output.Position = 0;

                    await blobContainerClient.UploadBlobAsync(blobName, output);
                }
                else
                {
                    log.LogInformation($"No encoder support for: {createdEvent.Url}");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                throw;
            }

        }
    }
}
