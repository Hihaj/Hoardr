using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Hoardr.Api
{
    public class AppSettings
    {
        public string AzureStorageConnectionString { get; set; }
        public string DropboxApiSecret { get; set; }

        public AppSettings()
        {
            AzureStorageConnectionString = ConfigurationManager.AppSettings["azure:StorageConnectionString"];
            DropboxApiSecret = ConfigurationManager.AppSettings["dropbox:ApiSecret"];
        }
    }
}