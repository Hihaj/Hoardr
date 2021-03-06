﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
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

            var appSettings = new AppSettings();
            container.Register(appSettings);
            container.Register(CloudStorageAccount.Parse(appSettings.AzureStorageConnectionString));
            container.Register<JsonSerializer, CustomJsonSerializer>();
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            var telemetryClient = new TelemetryClient
            {
                Context =
                {
                    InstrumentationKey = container.Resolve<AppSettings>().AzureInstrumentationKey
                }
            };
            container.Register(telemetryClient);
        }
    }
}