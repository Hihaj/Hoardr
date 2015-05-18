using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;

namespace Hoardr.Api.Dropbox
{
    public interface IFileWorker
    {
        Task EnqueueDownload(long dropboxUserId, string path);
    }

    public class FileWorker : IFileWorker
    {
        private readonly BufferBlock<string> _queue;
        private readonly TransformBlock<string, HttpResponseMessage> _requester;
        private readonly ActionBlock<HttpResponseMessage> _downloader; 

        public FileWorker(IDropboxContentApi dropboxContentApi)
        {
            _queue = new BufferBlock<string>();

            _requester = new TransformBlock<string, HttpResponseMessage>(async path =>
            {
                var fileResponse = await dropboxContentApi.Files(path).ConfigureAwait(false);
                fileResponse.EnsureSuccessStatusCode();
                return fileResponse;
            });

            _downloader = new ActionBlock<HttpResponseMessage>(async response =>
            {
                var metadata = JsonConvert.DeserializeObject<FileMetadata>(
                    response.Headers.GetValues("x-dropbox-metadata").FirstOrDefault());
                var rootFolderPath = Path.Combine(Path.GetTempPath(), "Hoardr");
                var filePath = Path.Combine(rootFolderPath, metadata.Path.TrimStart('/').Replace("/", @"\"));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var stream = File.Create(filePath))
                {
                    await response.Content.CopyToAsync(stream).ConfigureAwait(false);
                }
            });

            _queue.LinkTo(_requester);
            _requester.LinkTo(_downloader);
        }

        public async Task EnqueueDownload(long dropboxUserId, string path)
        {
            await _queue.SendAsync(path).ConfigureAwait(false);
        }

        public class FileMetadata
        {
            [JsonProperty("bytes")]
            public long Bytes { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("is_dir")]
            public bool IsFolder { get; set; }

            [JsonProperty("thumb_exists")]
            public bool ThumbnailExists { get; set; }

            [JsonProperty("rev")]
            public string Revision { get; set; }

            [JsonProperty("modified")]
            public DateTime? Modified { get; set; }
        }
    }
}
