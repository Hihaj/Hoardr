using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure;

namespace Hoardr.Api
{
    public class AppSettings
    {
        public string AzureStorageConnectionString { get; set; }
        public string AzureInstrumentationKey { get; set; }
        public string DropboxApiSecret { get; set; }

        public AppSettings()
        {
            AzureStorageConnectionString = CloudConfigurationManager.GetSetting("azure:StorageConnectionString");
            AzureInstrumentationKey = CloudConfigurationManager.GetSetting("azure:InstrumentationKey");
            DropboxApiSecret = CloudConfigurationManager.GetSetting("dropbox:ApiSecret");
        }
    }
}