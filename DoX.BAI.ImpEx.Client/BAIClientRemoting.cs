using System;
using DoX.BAI.ImpEx.Shared;

namespace DoX.BAI.ImpEx.Client
{
    public class BAIClientRemoting : MarshalByRefObject, IClientController
    {
        #region IClientController Members

        public void Start()
        {
            BAIClient.Instance.Start();
        }

        public void Stop()
        {
            BAIClient.Instance.Stop();
        }

        public void Export()
        {
            BAIClient.Instance.Export();
        }

        public void Import()
        {
            BAIClient.Instance.Import();
        }

        public void UpdateConfig()
        {
            BAIClient.Instance.UpdateConfig();
        }

        public void SendLogToServer()
        {
            BAIClient.Instance.SendLogToServer();
        }

        public bool Test()
        {
            return BAIClient.Instance.Test();
        }

        public Boolean Subscribe()
        {
            return BAIClient.Instance.Subscribe();
        }

        public Boolean UnSubscribe()
        {
            return BAIClient.Instance.UnSubscribe();
        }

        public ClientStatus GetStatus()
        {
            return BAIClient.Instance.GetStatus();
        }

        public event ClientStatusChangedDelegate ClientStatusChanged
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public ClientConfig ClientSideConfig
        {
            get
            {
                return BAIClient.Instance.ClientSideConfig;
            }
            set
            {
                BAIClient.Instance.ClientSideConfig = value;
            }
        }

        public string ClientName
        {
            get { return BAIClient.Instance.ClientName; }
        }

        #endregion

        public void WriteLogEntry(System.Reflection.MethodBase method, string message, System.Diagnostics.EventLogEntryType type)
        {
            BAIClient.Instance.WriteLogEntry(method, message, type);
        }
      
    }
}
