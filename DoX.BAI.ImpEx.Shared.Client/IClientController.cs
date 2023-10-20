using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DoX.BAI.ImpEx.Shared
{

    public struct Constants
    {
        public static readonly String TextEditor = "notepad.exe";
        public static readonly String EventLogName = "Dorner BAI-Client";
        public static readonly int EventLogSizeKb = 500 * 64;

        // Note: Set dirName to lower because the service installation is case sensitive!
        private static readonly int _DirNameHash = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToLower().GetHashCode();
        public static readonly String CallbackPortName = "IClientControllerCallback" + Math.Abs(_DirNameHash).ToString();
        public static readonly String RemotingPortName = "IClientController" + Math.Abs(_DirNameHash).ToString();
    }

    [Flags()]
    [Serializable()]
    public enum ClientStatus
    {
        None = 0,
        Running = 0x0001,
    }

    public delegate void ClientStatusChangedDelegate(ClientStatus status);

    public interface IClientController
    {
        void Start();
        void Stop();
        void Export();
        void Import();
        void SendLogToServer();
        void UpdateConfig();
        Boolean Test();
        Boolean Subscribe();
        Boolean UnSubscribe();

        ClientStatus GetStatus();
        event ClientStatusChangedDelegate ClientStatusChanged;

        ClientConfig ClientSideConfig { get; set; }
        String ClientName { get; }

        void WriteLogEntry(MethodBase method, String message, EventLogEntryType type);
    }

}
