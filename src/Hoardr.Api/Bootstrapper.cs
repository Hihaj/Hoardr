using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using Hoardr.Api.Dropbox;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Refit;

namespace Hoardr.Api
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<JsonSerializer, CustomJsonSerializer>();

            var dropboxSettings = container.Resolve<DropboxSettings>();
            container.Register<IDropboxApi>((c, npo) =>
                RestService.For<IDropboxApi>(new HttpClient(new AuthenticatedHttpClientHandler(dropboxSettings.AccessToken))
                {
                    BaseAddress = new Uri(dropboxSettings.ApiBaseAddress)
                }));
            container.Register<IDropboxContentApi>((c, npo) =>
                RestService.For<IDropboxContentApi>(new HttpClient(new AuthenticatedHttpClientHandler(dropboxSettings.AccessToken))
                {
                    BaseAddress = new Uri(dropboxSettings.ContentApiBaseAddress)
                }));
        }
    }
}