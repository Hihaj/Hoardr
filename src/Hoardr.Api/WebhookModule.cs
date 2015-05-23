using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
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
            IDeltaQueue deltaQueue,
            TelemetryClient telemetryClient) 
            : base("/dropbox/webhook")
        {
            this.RequiresHttps();

            Get["/"] = _ =>
            {
                var challenge = (string)Request.Query["challenge"];
                if (string.IsNullOrEmpty(challenge))
                {
                    telemetryClient.TrackEvent("WebhookActivationFailed");
                    return HttpStatusCode.BadRequest;
                }
                return challenge;
            };

            Post["/", true] = async (_, ct) =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    if (!requestVerifier.VerifyRequest(Request))
                    {
                        telemetryClient.TrackEvent("UnauthorizedWebhookRequest");
                        return HttpStatusCode.Unauthorized;
                    }
                    var body = this.Bind<WebhookRequest>();
                    if (body != null && body.Delta != null && body.Delta.Users != null)
                    {
                        foreach (var dropboxUserId in body.Delta.Users)
                        {
                            await deltaQueue.Enqueue(dropboxUserId);
                            telemetryClient.TrackEvent(
                                "DeltaEnqueued",
                                properties: new Dictionary<string, string>
                                {
                                    { "dropboxUserId", dropboxUserId.ToString() }
                                });
                        }
                    }
                    return HttpStatusCode.OK;
                }
                finally
                {
                    stopwatch.Stop();
                    telemetryClient.TrackEvent(
                        "WebhookRequestHandled",
                        metrics: new Dictionary<string, double>
                        {
                            { "elapsedMilliseconds", stopwatch.ElapsedMilliseconds }
                        });
                }
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