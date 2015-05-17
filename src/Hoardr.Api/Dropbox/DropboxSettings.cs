using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Hoardr.Api.Dropbox
{
    public class DropboxSettings
    {
        public string ApiSecret { get; set; }
        public string AccessToken { get; set; }

        public DropboxSettings()
        {
            ApiSecret = ConfigurationManager.AppSettings["dropbox:ApiSecret"];
            AccessToken = ConfigurationManager.AppSettings["dropbox:AccessToken"];
        }
    }
}