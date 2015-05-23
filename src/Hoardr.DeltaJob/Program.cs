using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Hoardr.Common;
using Hoardr.Common.Messages;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using Refit;

namespace Hoardr.DeltaJob
{
    public class Program
    {
        private static TelemetryClient _telemetryClient;

        public static void Main(string[] args)
        {
            // Configure and start the Azure WebJobs host.
            // Important: since processing of Dropbox deltas is required
            // to be sequential, we can not allow for parallel jobs
            // (per Dropbox user).
            var appSettings = new AppSettings();
            _telemetryClient = new TelemetryClient
            {
                Context =
                {
                    InstrumentationKey = appSettings.AzureInstrumentationKey
                }
            };
            var config = new JobHostConfiguration();
            config.StorageConnectionString = appSettings.AzureStorageConnectionString;
            config.DashboardConnectionString = appSettings.AzureStorageConnectionString;
            config.Queues.BatchSize = 1;
            config.Queues.MaxDequeueCount = 5;
            config.Queues.MaxPollingInterval = TimeSpan.FromSeconds(30);
            _telemetryClient.TrackEvent("DeltaJobStarted");
            var jobHost = new JobHost(config);
            jobHost.RunAndBlock();
        }

        public async static Task DownloadDelta(
            [QueueTrigger("deltas")] PendingDelta pendingDelta,
            [Table("deltaCursors")] CloudTable deltaCursorsTable,
            [Queue("deltas")] IAsyncCollector<PendingDelta> moreDeltas,
            [Queue("files")] IAsyncCollector<PendingFile> filesToDownload,
            TextWriter logger)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var appSettings = new AppSettings();
            var dropboxApi = RestService.For<IDropboxApi>(
                new HttpClient(new AuthenticatedHttpClientHandler(appSettings.DropboxAccessToken))
                {
                    BaseAddress = new Uri(appSettings.DropboxApiBaseAddress)
                });

            var retrieveDeltaCursor = TableOperation.Retrieve<DeltaCursorEntity>(pendingDelta.DropboxUserId.ToString(), pendingDelta.DropboxUserId.ToString());
            var retrieveDeltaCursorResult = await deltaCursorsTable.ExecuteAsync(retrieveDeltaCursor).ConfigureAwait(false);
            DeltaCursorEntity deltaCursorEntity =
                retrieveDeltaCursorResult != null && retrieveDeltaCursorResult.Result != null ?
                    (DeltaCursorEntity)retrieveDeltaCursorResult.Result :
                    new DeltaCursorEntity(pendingDelta.DropboxUserId, null);

            var downloader = new TransformBlock<DeltaRequest, DeltaResponse>(
                async request => await dropboxApi.Delta(request).ConfigureAwait(false));

            var responseProcessor = new ActionBlock<DeltaResponse>(async response =>
            {
                // Queue Dropbox files for download.
                var filePaths = response.Entries
                                        .Where(x => x.Metadata != null && !x.Metadata.IsFolder)
                                        .Select(x => x.Path.TrimStart('/'));

                foreach (var filePath in filePaths)
                {
                    await filesToDownload.AddAsync(new PendingFile
                    {
                        DropboxUserId = pendingDelta.DropboxUserId,
                        FilePath = filePath
                    }).ConfigureAwait(false);
                    await logger.WriteLineAsync(string.Format("Enqueued {0} for download.", filePath)).ConfigureAwait(false);
                    _telemetryClient.TrackEvent(
                        "FileEnqueued",
                        properties: new Dictionary<string, string>
                        {
                            { "dropboxUserId", pendingDelta.DropboxUserId.ToString() },
                            { "filePath", filePath }
                        });
                }

                // Update delta cursor.
                deltaCursorEntity.Cursor = response.Cursor;
                var saveDeltaCursor = TableOperation.InsertOrReplace(deltaCursorEntity);
                await deltaCursorsTable.ExecuteAsync(saveDeltaCursor).ConfigureAwait(false);
                await logger.WriteLineAsync(string.Format("Updated delta cursor for user {0}.", pendingDelta.DropboxUserId)).ConfigureAwait(false);

                // If there are more delta result pages, add a new pending delta for the user.
                if (response.HasMore)
                {
                    await moreDeltas.AddAsync(new PendingDelta
                    {
                        DropboxUserId = pendingDelta.DropboxUserId
                    }).ConfigureAwait(false);
                }
            });

            downloader.LinkTo(responseProcessor, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            await downloader.SendAsync(new DeltaRequest
            {
                Cursor = deltaCursorEntity.Cursor,
                PathPrefix = "/Camera Uploads"
            }).ConfigureAwait(false);
            downloader.Complete();
            await responseProcessor.Completion.ConfigureAwait(false);

            stopwatch.Stop();
            _telemetryClient.TrackEvent(
                "DeltaProcessed",
                properties: new Dictionary<string, string>
                {
                    { "dropboxUserId", pendingDelta.DropboxUserId.ToString() }
                },
                metrics: new Dictionary<string, double>
                {
                    { "elapsedMilliseconds", stopwatch.ElapsedMilliseconds }
                });
        }
    }
}
