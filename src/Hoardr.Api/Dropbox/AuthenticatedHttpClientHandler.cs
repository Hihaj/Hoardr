using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Hoardr.Api.Dropbox
{
    public class AuthenticatedHttpClientHandler : HttpClientHandler
    {
        private readonly string _bearerToken;

        public AuthenticatedHttpClientHandler(string bearerToken)
        {
            _bearerToken = bearerToken;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var auth = request.Headers.Authorization;
            if (auth != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, _bearerToken);
            }
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}