using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Hoardr.Common;
using Hoardr.Common.Messages;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Refit;

namespace Hoardr.FileJob
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure and start the Azure WebJobs host.
            // Important: since processing of Dropbox deltas is required
            // to be sequential, we can not allow for parallel jobs
            // (per Dropbox user).
            var appSettings = new AppSettings();
            var config = new JobHostConfiguration();
            config.StorageConnectionString = appSettings.AzureStorageConnectionString;
            config.DashboardConnectionString = appSettings.AzureStorageConnectionString;
            config.Queues.BatchSize = 1;
            config.Queues.MaxDequeueCount = 5;
            config.Queues.MaxPollingInterval = TimeSpan.FromSeconds(30);
            var jobHost = new JobHost(config);
            jobHost.RunAndBlock();
        }

        public async static Task DownloadFile(
            [QueueTrigger("files")] PendingFile pendingFile,
            [Blob("{DropboxUserId}/{FilePath}")] ICloudBlob destinationBlob,
            TextWriter logger)
        {
            var appSettings = new AppSettings();
            var dropboxContentApi = RestService.For<IDropboxContentApi>(
                new HttpClient(new AuthenticatedHttpClientHandler(appSettings.DropboxAccessToken))
                {
                    BaseAddress = new Uri(appSettings.DropboxContentApiBaseAddress)
                });

            var requester = new TransformBlock<string, HttpResponseMessage>(async path =>
            {
                var fileResponse = await dropboxContentApi.Files(path).ConfigureAwait(false);
                fileResponse.EnsureSuccessStatusCode();
                return fileResponse;
            });

            var downloader = new ActionBlock<HttpResponseMessage>(async response =>
            {
                var metadata = JsonConvert.DeserializeObject<DropboxFileMetadata>(
                    response.Headers.GetValues("x-dropbox-metadata").FirstOrDefault());
                using (var sourceStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    await destinationBlob.UploadFromStreamAsync(sourceStream).ConfigureAwait(false);
                }
                if (!string.IsNullOrEmpty(metadata.MimeType))
                {
                    destinationBlob.Properties.ContentType = metadata.MimeType;
                    await destinationBlob.SetPropertiesAsync().ConfigureAwait(false);
                }
            });

            requester.LinkTo(downloader, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            await requester.SendAsync(pendingFile.FilePath).ConfigureAwait(false);
            requester.Complete();
            await downloader.Completion.ConfigureAwait(false);
            await logger.WriteLineAsync(string.Format("Sucessfully stored {0}.", pendingFile.FilePath)).ConfigureAwait(false);
        }
    }
}
