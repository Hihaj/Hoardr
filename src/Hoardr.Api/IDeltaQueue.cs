using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hoardr.Common.Messages;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Hoardr.Api
{
    public interface IDeltaQueue
    {
        Task Enqueue(long dropboxUserId);
    }

    public class DeltaQueue : IDeltaQueue
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly TelemetryClient _telemetryClient;

        public DeltaQueue(CloudStorageAccount storageAccount, TelemetryClient telemetryClient)
        {
            _storageAccount = storageAccount;
            _telemetryClient = telemetryClient;
        }

        public async Task Enqueue(long dropboxUserId)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var queueClient = _storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("deltas");
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            await queue.AddMessageAsync(new CloudQueueMessage(
                JsonConvert.SerializeObject(new PendingDelta
                {
                    DropboxUserId = dropboxUserId
                }))).ConfigureAwait(false);
            stopwatch.Stop();
            _telemetryClient.TrackEvent(
                "EnqueuedDelta",
                metrics: new Dictionary<string, double>
                {
                    { "elapsedMilliseconds", stopwatch.ElapsedMilliseconds }
                });
        }
    }
}
