using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace DoX.BAI.ImpEx.Client.BAIService
{
    public partial class BAIService
    {
        public BAIService(DoX.BAI.ImpEx.Shared.ClientConfig cfg)
        {
            Url = cfg.ServiceUrl;

            if ((IsLocalFileSystemWebService(this.Url) == true))
            {
                UseDefaultCredentials = true;
                useDefaultCredentialsSetExplicitly = false;
            }
            else
            {
                useDefaultCredentialsSetExplicitly = true;
            }

            IWebProxy proxy = null;
            if (!String.IsNullOrEmpty(cfg.ProxyAddress))
                proxy = new WebProxy(new Uri(cfg.ProxyAddress));

            if (proxy != null)
            {
                if ((!String.IsNullOrEmpty(cfg.ProxyUsername)) && (!String.IsNullOrEmpty(cfg.ProxyPassword)))
                    proxy.Credentials = new NetworkCredential(cfg.ProxyUsername, cfg.ProxyPassword, cfg.ProxyDomain);
                Proxy = proxy;
            }

            if (Proxy != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Proxy-Einstellungen:");
                sb.Append("Addresse=" + ((WebProxy)proxy).Address.ToString());

                NetworkCredential credentials = proxy.Credentials as NetworkCredential;
                if (credentials != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("Username=" + credentials.UserName);
                    sb.AppendLine("Password=" + credentials.Password);
                    sb.Append("Domain=" + credentials.Domain);
                }
                BAIClient.Instance.WriteLogEntry(System.Reflection.MethodBase.GetCurrentMethod(), sb.ToString(), System.Diagnostics.EventLogEntryType.Information);

            }
        }
    }

    public partial class ConfigEntry
    {
        public ConfigEntry ShallowCopy()
        {
            return (ConfigEntry)this.MemberwiseClone();
        }

    }
}
