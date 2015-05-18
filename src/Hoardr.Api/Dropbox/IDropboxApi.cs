using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Refit;

namespace Hoardr.Api.Dropbox
{
    public interface IDropboxApi
    {
        [Post("/delta")]
        [Headers("Authorization: Bearer")]
        Task<DeltaResponse> Delta([Body(BodySerializationMethod.UrlEncoded)] DeltaRequest request);

        [Post("/delta/latest_cursor")]
        [Headers("Authorization: Bearer")]
        Task<DeltaLatestCursorResponse> DeltaLatestCursor();
    }

    public class DeltaRequest
    {
        [AliasAs("cursor")]
        public string Cursor { get; set; }
    }

    public class DeltaResponse
    {
        [JsonProperty("reset")]
        public bool Reset { get; set; }

        [JsonProperty("cursor")]
        public string Cursor { get; set; }

        [JsonProperty("has_more")]
        public bool HasMore { get; set; }

        [JsonProperty("entries")]
        public DeltaEntry[] Entries { get; set; }

        [JsonConverter(typeof(DeltaEntryJsonConverter))]
        public class DeltaEntry
        {
            public string Path { get; set; }
            public DeltaEntryMetadata Metadata { get; set; }
        }

        public class DeltaEntryMetadata
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

    public class DeltaEntryJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            var raw = serializer.Deserialize<JArray>(reader);
            return new DeltaResponse.DeltaEntry
            {
                Path = raw[0].ToString(),
                Metadata = raw[1].ToObject<DeltaResponse.DeltaEntryMetadata>()
            };
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(DeltaResponse.DeltaEntry).IsAssignableFrom(objectType);
        }
    }

    public class DeltaLatestCursorResponse
    {
        [JsonProperty("cursor")]
        public string Cursor { get; set; }
    }
}
