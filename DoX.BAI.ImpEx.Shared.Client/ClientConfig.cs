using System;
using System.Collections.Generic;
using System.Text;

namespace DoX.BAI.ImpEx.Shared
{
    [Serializable()]
    public class ClientConfig
    {
        public String Username { get; set; }
        public String Password { get; set; }
        public String ServiceUrl { get; set; }
        public String IntegrationClientUrl { get; set; }
        public String IntegrationClientToken { get; set; }
        public String ProxyAddress { get; set; }
        public String ProxyUsername { get; set; }
        public String ProxyPassword { get; set; }
        public String ProxyDomain { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Username={0}\r\nServiceUrl={1}\r\nProxyAddress={2}\r\nProxyUsername={3}\r\nProxyDomain={4}\r\nIntegrationClientUrl={5}\r\nIntegrationClientToken={6}",
                            Username,
                            ServiceUrl,
                            IntegrationClientUrl,
                            IntegrationClientToken,
                            ProxyAddress,
                            ProxyUsername,
                            ProxyDomain);

            return sb.ToString();
        }
    }
}
