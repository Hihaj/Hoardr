using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;

namespace Hoardr.Api.Dropbox
{
    public interface IRequestVerifier
    {
        bool VerifyRequest(Request request);
    }

    public class RequestVerifier : IRequestVerifier
    {
        private readonly DropboxSettings _settings;

        public RequestVerifier(DropboxSettings settings)
        {
            _settings = settings;
        }

        public bool VerifyRequest(Request request)
        {
            if (request == null)
            {
                return false;
            }
            var requestSignature = request.Headers["X-Dropbox-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(requestSignature))
            {
                return false;
            }
            var bodyHash = request.Body.AsString().CalculateHash(_settings.ApiSecret);
            return string.Equals(bodyHash, requestSignature, StringComparison.Ordinal);
        }
    }
}
