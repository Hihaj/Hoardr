using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hoardr.Common.Messages;
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

        public DeltaQueue(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;
        }

        public async Task Enqueue(long dropboxUserId)
        {
            var queueClient = _storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("deltas");
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            await queue.AddMessageAsync(new CloudQueueMessage(
                JsonConvert.SerializeObject(new PendingDelta
                {
                    DropboxUserId = dropboxUserId
                }))).ConfigureAwait(false);
        }
    }
}
