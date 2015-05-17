using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Extensions;
using Nancy.Security;

namespace Hoardr.Api.Dropbox
{
    public class WebhookModule : NancyModule
    {
        public WebhookModule(
            IRequestVerifier requestVerifier) 
            : base("/dropbox/webhook")
        {
            this.RequiresHttps();

            Get["/"] = _ =>
            {
                if (!requestVerifier.VerifyRequest(Request))
                {
                    return HttpStatusCode.Unauthorized;
                }
                var challenge = (string)Request.Query["challenge"];
                if (string.IsNullOrEmpty(challenge))
                {
                    return HttpStatusCode.BadRequest;
                }
                return challenge;
            };

            Post["/", true] = async (_, ct) =>
            {
                if (!requestVerifier.VerifyRequest(Request))
                {
                    return HttpStatusCode.Unauthorized;
                }
                // TODO
                return HttpStatusCode.OK;
            };
        }
    }
}