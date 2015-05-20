using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Hoardr.FileJob
{
    public class AppSettings
    {
        public string AzureStorageConnectionString { get; set; }
        public string DropboxApiSecret { get; set; }
        public string DropboxAccessToken { get; set; }
        public string DropboxContentApiBaseAddress { get; set; }

        public AppSettings()
        {
            AzureStorageConnectionString = ConfigurationManager.AppSettings["azure:StorageConnectionString"];
            DropboxApiSecret = ConfigurationManager.AppSettings["dropbox:ApiSecret"];
            DropboxAccessToken = ConfigurationManager.AppSettings["dropbox:AccessToken"];
            DropboxContentApiBaseAddress = ConfigurationManager.AppSettings["dropbox:ContentApiBaseAddress"];
        }
    }
}