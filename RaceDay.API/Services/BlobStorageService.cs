using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString =
                configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");

            var containerName =
                configuration["AzureStorage:ContainerName"]
                ?? "raceday-images";

            var blobServiceClient =
                new BlobServiceClient(connectionString);

            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                throw new ArgumentException(
                    "Please select an image to upload.");
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (image.Length > maxFileSize)
            {
                throw new ArgumentException(
                    "Image size must not exceed 5 MB.");
            }

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(
                image.ContentType.ToLowerInvariant()))
            {
                throw new ArgumentException(
                    "Only JPG, PNG and WebP images are allowed.");
            }

            // Create the container if it does not exist.
            await _containerClient.CreateIfNotExistsAsync();

            // Allow browsers to read images without authentication.
            // Use this only for images intended to be publicly visible.
            await _containerClient.SetAccessPolicyAsync(
                PublicAccessType.Blob);

            var extension =
                Path.GetExtension(image.FileName).ToLowerInvariant();

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var blobClient =
                _containerClient.GetBlobClient(fileName);

            await using var stream = image.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = image.ContentType
                    }
                });

            return blobClient.Uri.ToString();
        }
    }
}