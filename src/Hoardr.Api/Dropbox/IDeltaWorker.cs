using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Hoardr.Api.Dropbox
{
    public interface IDeltaWorker
    {
        Task Ping(long dropboxUserId);
    }

    public class DeltaWorker : IDeltaWorker
    {
        private readonly ConcurrentDictionary<long, bool> _dropboxUserInProgress = new ConcurrentDictionary<long, bool>(); 
        private readonly ConcurrentDictionary<long, string> _dropboxUserCursors = new ConcurrentDictionary<long, string>(); 
        private readonly BufferBlock<long> _queue;
        private readonly TransformBlock<long, UserDeltaRequest> _requestBuilder;
        private readonly TransformBlock<UserDeltaRequest, UserDeltaResponse> _downloader;
        private readonly ActionBlock<UserDeltaResponse> _responseProcessor; 

        public DeltaWorker(IDropboxApi dropboxApi, IFileWorker fileWorker)
        {
            _queue = new BufferBlock<long>();

            _requestBuilder = new TransformBlock<long, UserDeltaRequest>(dropboxUserId =>
            {
                return new UserDeltaRequest
                {
                    DropboxUserId = dropboxUserId,
                    Request = new DeltaRequest
                    {
                        Cursor = _dropboxUserCursors.GetOrAdd(dropboxUserId, x => dropboxApi.DeltaLatestCursor().Result.Cursor)
                    }
                };
            });

            _downloader = new TransformBlock<UserDeltaRequest, UserDeltaResponse>(async request =>
            {
                var response = await dropboxApi.Delta(request.Request).ConfigureAwait(false);
                return new UserDeltaResponse
                {
                    DropboxUserId = request.DropboxUserId,
                    Response = response
                };
            });

            _responseProcessor = new ActionBlock<UserDeltaResponse>(async response =>
            {
                // Queue files for download
                var filePaths = response.Response.Entries
                                        .Where(x => x.Metadata != null && !x.Metadata.IsFolder)
                                        .Select(x => x.Path);

                foreach (var filePath in filePaths)
                {
                    await fileWorker.EnqueueDownload(response.DropboxUserId, filePath).ConfigureAwait(false);
                }

                // Update cursor for the current user, release "lease" on user if finished.
                _dropboxUserCursors[response.DropboxUserId] = response.Response.Cursor;
                if (response.Response.HasMore)
                {
                    await _queue.SendAsync(response.DropboxUserId).ConfigureAwait(false);
                }
                else
                {
                    bool temp;
                    _dropboxUserInProgress.TryRemove(response.DropboxUserId, out temp);
                }
            });

            _queue.LinkTo(_requestBuilder);
            _requestBuilder.LinkTo(_downloader);
            _downloader.LinkTo(_responseProcessor);
        }

        public async Task Ping(long dropboxUserId)
        {
            var proceed = _dropboxUserInProgress.TryAdd(dropboxUserId, true);
            if (proceed)
            {
                await _queue.SendAsync(dropboxUserId).ConfigureAwait(false);
            }
        }
    }

    public class UserDeltaRequest
    {
        public long DropboxUserId { get; set; }
        public DeltaRequest Request { get; set; }
    }

    public class UserDeltaResponse
    {
        public long DropboxUserId { get; set; }
        public DeltaResponse Response { get; set; }
    }
}
