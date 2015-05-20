using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Refit;

namespace Hoardr.FileJob
{
    public interface IDropboxContentApi
    {
        [Get("/files/auto/{path}")]
        [Headers("Authorization: Bearer")]
        Task<HttpResponseMessage> Files(string path);
    }
}
