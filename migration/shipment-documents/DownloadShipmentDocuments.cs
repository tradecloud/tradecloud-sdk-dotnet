using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using DotNetEnv;

namespace Com.Tradecloud1.SDK.Client
{
    class DownloadShipmentDocuments
    {
        // Configuration loaded from environment variables
        static string legacyUsername;
        static string legacyPassword;
        static readonly HttpClient legacyHttpClient = new HttpClient();

        // Progress tracking
        static int totalDocuments = 0;
        static int processedDocuments = 0;
        static int successfulDownloads = 0;
        static int failedDownloads = 0;
        static DateTime downloadStartTime;

        // Output directory
        static string outputDirectory = "downloaded_documents";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud shipment documents downloader");

            // Load environment variables from .env file
            Env.Load();

            // Load configuration from environment variables
            legacyUsername = Environment.GetEnvironmentVariable("LEGACY_USERNAME");
            legacyPassword = Environment.GetEnvironmentVariable("LEGACY_PASSWORD");

            if (string.IsNullOrEmpty(legacyUsername) || string.IsNullOrEmpty(legacyPassword))
            {
                Console.WriteLine("Error: LEGACY_USERNAME and LEGACY_PASSWORD must be set in .env file");
                return;
            }

            // Setup authentication for legacy API
            var legacyCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(legacyUsername + ":" + legacyPassword));
            legacyHttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", legacyCredentials);

            // Create output directory
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await RunDocumentDownload();
        }

        static async Task RunDocumentDownload()
        {
            var logPath = $"{DateTime.Now:yyyyMMdd-HHmmss}-download-shipment-documents.log";
            var csvFile = "20251028_technetix_download_shipment_documents.csv";

            using (var log = new StreamWriter(logPath, append: true))
            {
                downloadStartTime = DateTime.Now;
                await Log(log, "Starting shipment document download (sequential mode)");
                await Log(log, $"CSV file: {csvFile}");
                await Log(log, $"Output directory: {outputDirectory}");

                try
                {
                    // Read URLs from CSV file
                    if (!File.Exists(csvFile))
                    {
                        var errorMsg = $"CSV file not found: {csvFile}";
                        await Log(log, errorMsg);
                        Console.WriteLine(errorMsg);
                        return;
                    }

                    var urls = File.ReadAllLines(csvFile)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(line => line.Trim())
                        .ToList();

                    totalDocuments = urls.Count;

                    await Log(log, $"Found {totalDocuments} URLs to process");
                    Console.WriteLine($"Found {totalDocuments} URLs to process");

                    if (totalDocuments == 0)
                    {
                        await Log(log, "No URLs found in CSV file.");
                        Console.WriteLine("No URLs found in CSV file.");
                        return;
                    }

                    // Process URLs sequentially
                    var startTime = DateTime.Now;

                    for (int i = 0; i < urls.Count; i++)
                    {
                        var url = urls[i];
                        var documentNumber = i + 1;

                        var success = await DownloadDocument(url, log, documentNumber);

                        if (success)
                        {
                            successfulDownloads++;
                        }
                        else
                        {
                            failedDownloads++;
                        }

                        processedDocuments++;

                        // Show progress every 10 documents or at the end
                        if (processedDocuments % 10 == 0 || processedDocuments == totalDocuments)
                        {
                            var elapsed = DateTime.Now - startTime;
                            var avgTimePerDoc = elapsed.TotalSeconds / processedDocuments;
                            var estimatedRemaining = TimeSpan.FromSeconds(avgTimePerDoc * (totalDocuments - processedDocuments));

                            Console.WriteLine($"Progress: {processedDocuments}/{totalDocuments} ({(processedDocuments * 100.0 / totalDocuments):F1}%) " +
                                            $"Success: {successfulDownloads}, Failed: {failedDownloads}, " +
                                            $"Est. remaining: {estimatedRemaining:mm\\:ss}");
                        }
                    }

                    var endTime = DateTime.Now;
                    var totalTime = endTime - startTime;

                    // Final summary
                    var summaryMessage = $"Download completed in {totalTime:hh\\:mm\\:ss}. " +
                                       $"Success: {successfulDownloads}, Failed: {failedDownloads}, Total: {totalDocuments}";

                    await Log(log, summaryMessage);
                    Console.WriteLine(summaryMessage);
                    Console.WriteLine($"Documents saved to: {Path.GetFullPath(outputDirectory)}");
                    Console.WriteLine($"Detailed log: {logPath}");
                }
                catch (Exception ex)
                {
                    await Log(log, $"Download failed with error: {ex.Message}");
                    Console.WriteLine($"Download failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }

        static async Task<bool> DownloadDocument(string url, StreamWriter log, int documentNumber)
        {
            const int maxRetries = 2;
            const int retryDelayMs = 2000; // 2 seconds between retries

            try
            {
                // Parse URL to extract shipment ID and document ID
                // Expected format: https://portal.tradecloud.nl/api/v1/shipment/{shipmentId}/document/{documentId}
                var match = Regex.Match(url, @"shipment/([^/]+)/document/([^/]+)");
                if (!match.Success)
                {
                    await Log(log, $"[{documentNumber}/{totalDocuments}] Invalid URL format: {url}");
                    return false;
                }

                var shipmentId = match.Groups[1].Value;
                var documentId = match.Groups[2].Value;

                await Log(log, $"[{documentNumber}/{totalDocuments}] Downloading shipmentId={shipmentId}, documentId={documentId}");

                HttpResponseMessage response = null;
                int attempt = 0;

                // Retry loop for BadGateway errors
                while (attempt <= maxRetries)
                {
                    attempt++;

                    // Download the document
                    response = await legacyHttpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        // Success - break out of retry loop
                        break;
                    }

                    // Check if it's a BadGateway error and we have retries left
                    if (response.StatusCode == System.Net.HttpStatusCode.BadGateway && attempt <= maxRetries)
                    {
                        await Log(log, $"[{documentNumber}/{totalDocuments}] BadGateway error (attempt {attempt}/{maxRetries + 1}), retrying in {retryDelayMs}ms...");
                        await Task.Delay(retryDelayMs);
                        continue;
                    }

                    // Other error or no more retries
                    await Log(log, $"[{documentNumber}/{totalDocuments}] Failed to download: Status={response.StatusCode}, URL={url}");
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    await Log(log, $"[{documentNumber}/{totalDocuments}] Failed to download after {attempt} attempts: Status={response.StatusCode}, URL={url}");
                    return false;
                }

                // Get content and filename
                var content = await response.Content.ReadAsByteArrayAsync();

                // Get extension from content type
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var extension = GetExtensionFromContentType(contentType);

                // Use document ID as filename with extension
                var filename = $"{documentId}{extension}";

                // Save the file directly to output directory
                var filepath = Path.Combine(outputDirectory, filename);
                await File.WriteAllBytesAsync(filepath, content);

                var successMessage = attempt > 1
                    ? $"[{documentNumber}/{totalDocuments}] Successfully downloaded (after {attempt} attempts): {filename} (shipmentId={shipmentId}, {content.Length} bytes)"
                    : $"[{documentNumber}/{totalDocuments}] Successfully downloaded: {filename} (shipmentId={shipmentId}, {content.Length} bytes)";
                await Log(log, successMessage);
                return true;
            }
            catch (Exception ex)
            {
                await Log(log, $"[{documentNumber}/{totalDocuments}] Error downloading {url}: {ex.Message}");
                return false;
            }
        }

        static string GetExtensionFromContentType(string contentType)
        {
            return contentType.ToLower() switch
            {
                "application/pdf" => ".pdf",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "application/zip" => ".zip",
                "application/xml" => ".xml",
                "text/xml" => ".xml",
                "text/plain" => ".txt",
                "application/json" => ".json",
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                _ => ".bin"
            };
        }

        static async Task Log(StreamWriter log, string message)
        {
            var timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            await log.WriteLineAsync(timestampedMessage);
            await log.FlushAsync();
        }
    }
}