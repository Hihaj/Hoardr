using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Hoardr.FileJob
{
    public class DropboxFileMetadata
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
