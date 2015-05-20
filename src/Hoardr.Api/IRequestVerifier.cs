using System;
using System.Linq;
using Nancy;
using Nancy.Extensions;

namespace Hoardr.Api
{
    public interface IRequestVerifier
    {
        bool VerifyRequest(Request request);
    }

    public class RequestVerifier : IRequestVerifier
    {
        private readonly AppSettings _settings;

        public RequestVerifier(AppSettings settings)
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
            var bodyHash = request.Body.AsString().CalculateHash(_settings.DropboxApiSecret);
            return string.Equals(bodyHash, requestSignature, StringComparison.Ordinal);
        }
    }
}
