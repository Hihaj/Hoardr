using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;
using Nancy.Security;

namespace Hoardr.Api
{
    public class WebhookModule : NancyModule
    {
        public WebhookModule(
            IRequestVerifier requestVerifier,
            IDeltaQueue deltaQueue) 
            : base("/dropbox/webhook")
        {
            //this.RequiresHttps();

            Get["/"] = _ =>
            {
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
                var body = this.Bind<WebhookRequest>();
                if (body != null && body.Delta != null && body.Delta.Users != null)
                {
                    foreach (var dropboxUserId in body.Delta.Users)
                    {
                        await deltaQueue.Enqueue(dropboxUserId);
                    }
                }
                return HttpStatusCode.OK;
            };
        }

        public class WebhookRequest
        {
            public Body Delta { get; set; }

            public WebhookRequest()
            {
                Delta = new Body();
            }

            public class Body
            {
                public long[] Users { get; set; }

                public Body()
                {
                    Users = new long[0];
                }
            }
        }
    }
}