using DoX.BAI.ImpEx.Client.BAIService;
using DoX.BAI.ImpEx.Shared;
using Microsoft.Win32;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Serialization;



namespace DoX.BAI.ImpEx.Client
{
    public sealed class BAIClient : IClientController
    {
        private const int DEFAULT_POLLINTERVALL_CONFIG = 60000;

        private const string METADATA_KEY_IMPERSONATE_USERNAME = "ImpersonateUsername";
        private const string METADATA_KEY_IMPERSONATE_DOMAIN = "ImpersonateDomain";
        private const string METADATA_KEY_IMPERSONATE_PASSWORD = "ImpersonatePassword";
        private const string METADATA_KEY_OVERWRITE_EXPORT_FILE = "OverwriteExportFile";
        private const int BATCH_SIZE = 7;

        // obtains user token
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(string pszUsername, string pszDomain, string pszPassword,
            int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        // closes open handes returned by LogonUser
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public extern static bool CloseHandle(IntPtr handle);

        private readonly HttpClient _httpClient;

        const string SampleDirectoryPath = @"C:\Users\User\source\ppmc\XMLProjekt\DOI_DornerInterface_2022.02.16\InterfaceExamples";

        private static readonly Regex _FileSearchPattern = new Regex(@"^([\w.-]+[*]*\.\w{2,})|([*]{1}\.\w{2,})");
        private const String UPDATE_PROC_NAME = "DoX.BAI.ImpEx.Client.Updater.exe";

        private static MongoClient _mongoClient = new MongoClient("mongodb+srv://admin:I36oVbYYcYvl5ROs@ppmcbai.7ynygj2.mongodb.net/?retryWrites=true&w=majority");
        private static IMongoDatabase _database = _mongoClient.GetDatabase("BAI_Test_Sample");

        private static BAIClient _Instance;
        private static readonly Type _CallbackType = TypeBinder.GetRemotingTypeByInterface(typeof(IClientControllerCallback));
        private static ClientConfig _ClientSideConfig;

        private ClientStatus _Status;
        private Timer _ConfigTimer;
        private Timer _PollTimer;
        private Int64 _PollCounter;
        private Config _ServerSideConfig;

        private long _ImportRequested;
        private long _ExportRequested;
        private readonly Object _ImportLock = new Object();
        private readonly Object _ExportLock = new Object();

        private readonly Object _ConfigTimerLock = new Object();
        private readonly Object _PollTimerLock = new Object();
        private readonly ReaderWriterLockSlim _LogFileLock = new ReaderWriterLockSlim();

        public event ClientStatusChangedDelegate ClientStatusChanged;

        private BAIClient()
        {
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                String msg = "Unbehandelte Ausnahme aufgetreten";
                if (e.IsTerminating)
                    msg += " - Anwendung wird beendet";

                msg += ":\r\n";
                WriteLogEntry(MethodBase.GetCurrentMethod(), msg + e.ExceptionObject, EventLogEntryType.Error);
            };
            this._httpClient = new HttpClient();
        }

        static BAIClient()
        {
            LoadConfig();
        }

        private bool ImpersonationRequired(Config argCfg, out string argUsername, out string argPassword, out string argDomain)
        {
            argUsername = string.Empty;
            argPassword = string.Empty;
            argDomain = string.Empty;

            if (argCfg == null || argCfg.Metadata == null || !argCfg.Metadata.Any())
                return false;

            argUsername = argCfg.Metadata.Where(e => e.Key.Equals(METADATA_KEY_IMPERSONATE_USERNAME, StringComparison.InvariantCultureIgnoreCase)).Select(e => e.Value).FirstOrDefault();
            argPassword = argCfg.Metadata.Where(e => e.Key.Equals(METADATA_KEY_IMPERSONATE_PASSWORD, StringComparison.InvariantCultureIgnoreCase)).Select(e => e.Value).FirstOrDefault();
            argDomain = argCfg.Metadata.Where(e => e.Key.Equals(METADATA_KEY_IMPERSONATE_DOMAIN, StringComparison.InvariantCultureIgnoreCase)).Select(e => e.Value).FirstOrDefault();

            return !string.IsNullOrEmpty(argUsername);
        }

        private bool OverwriteExportFile(Config argCfg)
        {
            if (argCfg == null || argCfg.Metadata == null || !argCfg.Metadata.Any())
                return false;

            bool boolValue;
            var metaValue = argCfg.Metadata.Where(e => e.Key.Equals(METADATA_KEY_OVERWRITE_EXPORT_FILE, StringComparison.InvariantCultureIgnoreCase)).Select(e => e.Value).FirstOrDefault();
            if (!string.IsNullOrEmpty(metaValue) && bool.TryParse(metaValue, out boolValue))
                return boolValue;

            return false;
        }

        /// Impersonate: to be able to access network drives
        private void RunImpersonatedTask(string argUsername, string argPassword, string argDomain, Action argTask)
        {
            WindowsImpersonationContext impersonationContext = null;
            IntPtr userHandle = IntPtr.Zero;
            const int LOGON32_PROVIDER_DEFAULT = 0;
            //const int LOGON32_LOGON_INTERACTIVE = 2;
            const int LOGON_TYPE_NEW_CREDENTIALS = 9;
            //const int LOGON32_PROVIDER_WINNT50 = 3;

            try
            {
                //WriteLogEntry(MethodBase.GetCurrentMethod(), "windows identify before impersonation: " + WindowsIdentity.GetCurrent().Name, EventLogEntryType.Information);

                // if domain name was blank, assume local machine
                if (string.IsNullOrEmpty(argDomain))
                    argDomain = System.Environment.MachineName;

                // Call LogonUser to get a token for the user
                bool loggedOn = LogonUser(argUsername,
                                            argDomain,
                                            argPassword,
                                            LOGON_TYPE_NEW_CREDENTIALS,
                                            LOGON32_PROVIDER_DEFAULT,
                                            ref userHandle);

                if (!loggedOn)
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Exception impersonating user, error code: " + Marshal.GetLastWin32Error(), EventLogEntryType.Error);
                    return;
                }

                // Begin impersonating the user
                impersonationContext = WindowsIdentity.Impersonate(userHandle);

                //WriteLogEntry(MethodBase.GetCurrentMethod(), "windows identify after impersonation: " + WindowsIdentity.GetCurrent().Name, EventLogEntryType.Information);

                //run the program with elevated privileges (like file copying from a domain server)
                argTask();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception impersonating user: " + ex.Message);
            }
            finally
            {
                // Clean up
                if (impersonationContext != null)
                    impersonationContext.Undo();

                if (userHandle != IntPtr.Zero)
                    CloseHandle(userHandle);
            }
        }


        /// <summary>
        /// Gibt einen Verweis auf den BAI-Client zurück.
        /// </summary>
        public static BAIClient Instance
        {
            get
            {
                if (_Instance == null)
                {
                    // --- Singleton-Instanz anlegen
                    _Instance = new BAIClient();

                    // --- Dem temporären Zertifkat vertrauen
#if DEBUG
                    PermissiveCertifikatePolicy.Enact();

#endif

#if !NO_IPC
                    // --- BAIClientRemoting als Remoting-Objekt via IPC registrieren
                    //     Über dieses Remoting-Objekt können Client-Anwendungen (wie z.B. DoX.BAI.ImpEx.Client.GUI.exe)
                    //     den BAI-Client steuern.
                    SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                    NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));

                    Hashtable prop = new Hashtable();
                    prop.Add("impersonationLevel", "None");
                    prop.Add("authorizedGroup", account.Value);
                    prop.Add("portName", Constants.RemotingPortName);
                    prop.Add("name", Constants.RemotingPortName);

                    IpcServerChannel serverChannel = new IpcServerChannel(prop, new BinaryServerFormatterSinkProvider());
                    ChannelServices.RegisterChannel(serverChannel, false);

                    RemotingConfiguration.RegisterWellKnownServiceType(typeof(BAIClientRemoting), "IClientController", WellKnownObjectMode.SingleCall);

                    // --- Rückkanal aufbauen
                    IpcClientChannel clientChannel = new IpcClientChannel(Constants.CallbackPortName, new BinaryClientFormatterSinkProvider());
                    ChannelServices.RegisterChannel(clientChannel, false);
#endif
                }

                return _Instance;
            }
        }

        #region IClientController Members

        public void Start()
        {
            if (_Status.HasFlag(ClientStatus.Running))
                return;

            AddStatus(ClientStatus.Running);

            RenewServerSideConfig(null);

            Subscribe();

            // --- Poll-Timer für Import/Export
            _PollTimer = new Timer(PollTimerCallback, null, 1000, 1000);

            WriteLogEntry(MethodBase.GetCurrentMethod(), "BAI-Client mit folgender Konfiguration gestartet:\r\n" + ClientSideConfig, EventLogEntryType.Information);
        }

        private bool ImportRequested
        {
            get { return Convert.ToBoolean(Interlocked.Read(ref _ImportRequested)); }
            set { Interlocked.Exchange(ref _ImportRequested, Convert.ToInt64(value)); }
        }

        private bool ExportRequested
        {
            get { return Convert.ToBoolean(Interlocked.Read(ref _ExportRequested)); }
            set { Interlocked.Exchange(ref _ExportRequested, Convert.ToInt64(value)); }
        }

        public void Stop()
        {

            if (_Status.HasFlag(ClientStatus.Running))
            {

                lock (_PollTimerLock)
                {
                    if (_PollTimer != null)
                    {
                        _PollTimer.Dispose();
                        _PollTimer = null;
                    }
                }

                lock (_ConfigTimerLock)
                {
                    if (_ConfigTimer != null)
                    {
                        _ConfigTimer.Dispose();
                        _ConfigTimer = null;
                    }
                }

                UnSubscribe();

                StopClientNotifcationService();

                RemoveStatus(ClientStatus.Running);

                WriteLogEntry(MethodBase.GetCurrentMethod(), "BAI-Client beendet", EventLogEntryType.Information);
            }
        }

        public void Export()
        {
            if (ExportRequested)
                return;

            var threadId = Thread.CurrentThread.ManagedThreadId.ToString("00");
            ExportRequested = true;
            Debug.WriteLine(threadId + "   " + DateTime.Now.ToString("HH:mm:ss") + " Export requested");

            lock (_ExportLock)
            {
                while (ExportRequested)
                {
                    Debug.WriteLine(threadId + "   " + DateTime.Now.ToString("HH:mm:ss") + " ExportInternal started");
                    ExportRequested = false;

                    Config tempConfig = null;
                    Interlocked.Exchange(ref tempConfig, _ServerSideConfig);
                    if (tempConfig == null)
                        return;

                    string username, password, domain;
                    if (ImpersonationRequired(tempConfig, out username, out password, out domain))
                        RunImpersonatedTask(username, password, domain, () => ExportInternal(tempConfig));
                    else
                        ExportInternal(tempConfig);

                    Debug.WriteLine(threadId + "   " + DateTime.Now.ToString("HH:mm:ss") + " ExportInternal finished");
                }
            }
        }

        public void Import()
        {
            if (ImportRequested)
                return;

            var threadId = Thread.CurrentThread.ManagedThreadId.ToString("00");
            ImportRequested = true;
            Debug.WriteLine(threadId + "   " + DateTime.Now.ToString("HH:mm:ss") + " Import requested");

            lock (_ImportLock)
            {
                while (ImportRequested)
                {
                    Debug.WriteLine(threadId + "   " + DateTime.Now.ToString("HH:mm:ss") + " ImportInternal started");
                    ImportRequested = false;

                    Config tempConfig = null;
                    Interlocked.Exchange(ref tempConfig, _ServerSideConfig);
                    if (tempConfig == null)
                        return;

                    var overwrite = OverwriteExportFile(tempConfig);

                    string username, password, domain;
                    if (ImpersonationRequired(tempConfig, out username, out password, out domain))
                        RunImpersonatedTask(username, password, domain, () => ImportInternal(overwrite));
                    else
                        ImportInternal(overwrite);

                    Debug.WriteLine(threadId + "   " + DateTime.Now.ToString("HH:mm:ss") + " ImportInternal finished");
                }
            }
        }

        public void UpdateConfig()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId.ToString("00");
            Debug.WriteLine(threadId + "   " + DateTime.Now.ToString("HH:mm:ss") + " Config-Update requested");

            RenewServerSideConfig(null);

            Debug.WriteLine(threadId + "   " + DateTime.Now.ToString("HH:mm:ss") + " Config-Update finished");
        }

        public void SendLogToServer()
        {
            BAIResult result = new BAIResult();
            var methName = MethodBase.GetCurrentMethod();

            try
            {
                using (var service = new BAIService.BAIService(_ClientSideConfig))
                {
                    WriteLogEntry(methName, "Sende Log-Datei zum Server", EventLogEntryType.Information);
                    var logText = CreateLogText(1000);
                    result = service.SendLog(Credentials, logText);
                    if (result.ResultType == BAIResultType.OK)
                        WriteLogEntry(methName, "Log-Datei erfolgreich zum Server gesendet", EventLogEntryType.Information);
                    else
                        WriteLogEntry(methName, "Fehler beim Senden der Log-Datei zum Server:\r\n" + result.Message, EventLogEntryType.Warning);

                }
            }
            catch (Exception ex)
            {
                WriteLogEntry(methName, "Fehler beim Aufruf des BAI-Service:\r\n" + ex, EventLogEntryType.Warning);
            }

            if (result.ResultType != BAIResultType.OK)
            {
                WriteLogEntry(methName, "Fehler auf BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
            }
        }

        public Boolean Test()
        {
            Boolean res = false;

            WriteLogEntry(MethodBase.GetCurrentMethod(), "Testaufruf beim BAI-Service", EventLogEntryType.Information);

            try
            {
                using (var service = new BAIService.BAIService(_ClientSideConfig))
                {
                    service.Timeout = 20000;
                    Config cfg;
                    BAIResult result = service.GetConfig(Credentials, out cfg);
                    if (result.ResultType == BAIResultType.OK)
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Testaufruf beim BAI-Service war erfolgreich", EventLogEntryType.Information);
                        res = true;
                    }
                    else
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Testaufruf des BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex, EventLogEntryType.Warning);
            }
            return res;
        }

        public Boolean Subscribe()
        {
            Boolean subscribed = false;

            try
            {
                using (var service = new BAIService.BAIService(_ClientSideConfig))
                {
                    BAIResult result = service.Subscribe(Credentials);
                    if (result.ResultType == BAIResultType.OK)
                    {
                        subscribed = true;
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Anmeldung beim BAI-Service erfolgt", EventLogEntryType.Information);
                    }
                    else
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler auf BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex, EventLogEntryType.Warning);
            }

            return subscribed;
        }

        public Boolean UnSubscribe()
        {
            Boolean subscribed = false;

            try
            {
                using (var service = new BAIService.BAIService(_ClientSideConfig))
                {
                    BAIResult result = service.UnSubscribe(Credentials);
                    if (result.ResultType == BAIResultType.OK)
                    {
                        subscribed = true;
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Abmeldung beim BAI-Service erfolgt", EventLogEntryType.Information);
                    }
                    else
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler auf BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex, EventLogEntryType.Warning);
            }

            return subscribed;
        }

        public ClientStatus GetStatus()
        {
            return _Status;
        }

        public ClientConfig ClientSideConfig
        {
            get
            {
                return _ClientSideConfig;
            }
            set
            {
                _ClientSideConfig = value;
                SaveConfig();
            }
        }

        public String ClientName
        {
            get
            {
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);

                if (cfg != null)
                {
                    ConfigurationSectionGroup applicationSettings = cfg.GetSectionGroup("applicationSettings");
                    if (applicationSettings != null)
                    {
                        ClientSettingsSection appSetSection = applicationSettings.Sections[0] as ClientSettingsSection;
                        var setting = appSetSection.Settings.Get("Clientname");
                        if (setting != null && setting.Value != null)
                            return setting.Value.ValueXml.InnerText;
                    }
                }

                return "BAI-Client";
            }
        }

        private String LogDir
        {
            get
            {
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);

                if (cfg != null)
                {
                    ConfigurationSectionGroup applicationSettings = cfg.GetSectionGroup("applicationSettings");
                    if (applicationSettings != null)
                    {
                        ClientSettingsSection appSetSection = applicationSettings.Sections[0] as ClientSettingsSection;
                        var setting = appSetSection.Settings.Get("LogDir");
                        if (setting != null && setting.Value != null)
                        {
                            // may contain environment variables
                            var logDir = Regex.Replace(setting.Value.ValueXml.InnerText, "%.+%", m =>
                            {
                                var varName = m.ToString().TrimStart('%').TrimEnd('%');
                                return Environment.GetEnvironmentVariable(varName);
                            });
                            return logDir;
                        }
                    }
                }

                return null;
            }
        }

        private int MaxLogEntries
        {
            get
            {
                const int defaultValue = 1000;
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);

                if (cfg != null)
                {
                    ConfigurationSectionGroup applicationSettings = cfg.GetSectionGroup("applicationSettings");
                    if (applicationSettings != null)
                    {
                        ClientSettingsSection appSetSection = applicationSettings.Sections[0] as ClientSettingsSection;
                        var setting = appSetSection.Settings.Get("MaxLogEntries");
                        if (setting != null && setting.Value != null)
                        {
                            int maxLogEntries;
                            if (!int.TryParse(setting.Value.ValueXml.InnerText, out maxLogEntries))
                                maxLogEntries = defaultValue;
                            return maxLogEntries;
                        }
                    }
                }

                return defaultValue;
            }
        }

        #endregion

        private static void LoadConfig()
        {
            _ClientSideConfig = new ClientConfig();

            _ClientSideConfig.Username = Properties.Settings.Default.Username;
            _ClientSideConfig.Password = Decrypt(Properties.Settings.Default.Password);
            _ClientSideConfig.ServiceUrl = Properties.Settings.Default.ServiceUrl;
            _ClientSideConfig.ProxyAddress = Properties.Settings.Default.ProxyAddress;
            _ClientSideConfig.ProxyUsername = Properties.Settings.Default.ProxyUsername;
            _ClientSideConfig.ProxyPassword = Decrypt(Properties.Settings.Default.ProxyPassword);
            _ClientSideConfig.ProxyDomain = Properties.Settings.Default.ProxyDomain;
        }

        private void SaveConfig()
        {
            if (_ClientSideConfig != null)
            {
                Properties.Settings.Default.Username = _ClientSideConfig.Username;
                Properties.Settings.Default.Password = Encrypt(_ClientSideConfig.Password);
                Properties.Settings.Default.ServiceUrl = _ClientSideConfig.ServiceUrl;
                Properties.Settings.Default.ProxyAddress = _ClientSideConfig.ProxyAddress;
                Properties.Settings.Default.ProxyUsername = _ClientSideConfig.ProxyUsername;
                Properties.Settings.Default.ProxyPassword = Encrypt(_ClientSideConfig.ProxyPassword);
                Properties.Settings.Default.ProxyDomain = _ClientSideConfig.ProxyDomain;

                Properties.Settings.Default.Save();

                WriteLogEntry(MethodBase.GetCurrentMethod(), "Konfiguration gespeichert:\r\n" + ClientSideConfig, EventLogEntryType.Information);
            }
        }

        private void AddStatus(ClientStatus status)
        {
            _Status = (_Status | status);

            FireStatusChanged();
        }

        private void RemoveStatus(ClientStatus status)
        {
            _Status = (_Status & ~status);

            FireStatusChanged();
        }

        private void FireStatusChanged()
        {
            ClientStatusChangedDelegate tmp = ClientStatusChanged;
            if (tmp != null)
                tmp.Invoke(_Status);

#if !NO_IPC
            try
            {
                // --- Rückmeldung per Remoting
                IClientControllerCallback callbackObj = Activator.GetObject(_CallbackType, "ipc://" + Constants.CallbackPortName + "/IClientControllerCallback") as IClientControllerCallback;
                if (callbackObj != null)
                    callbackObj.ClientStatusChanged(_Status);
            }
            catch (RemotingException)
            {
                // --- Macht nichts - wird erneut versucht
            }
#endif
        }

        private void RenewServerSideConfig(Object state)
        {
            if (!_Status.HasFlag(ClientStatus.Running))
                return;

            var configPollIntervall = DEFAULT_POLLINTERVALL_CONFIG;

            lock (_ConfigTimerLock)
            {
                if (_ConfigTimer != null)
                    // --- Timer vorübergehend anhalten
                    _ConfigTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            var result = new BAIResult();

            try
            {
                // --- Konfiguration beim Service abfragen
                using (var service = new BAIService.BAIService(_ClientSideConfig))
                {
                    result = service.GetConfig(Credentials, out _ServerSideConfig);
                }
            }
            catch (Exception ex)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex, EventLogEntryType.Warning);
            }

            if (result.ResultType == BAIResultType.OK && _ServerSideConfig != null)
            {
                configPollIntervall = _ServerSideConfig.ConfigTimerIntervall * 1000;

                // --- Ggfs. Clientseitiger Service für Callbacks (Benachrichtigungen über neue Daten) starten
                if (!String.IsNullOrEmpty(_ServerSideConfig.NotificationAddress))
                    StartClientNotificationService();
                else
                    StopClientNotifcationService();

                WriteLogEntry(MethodBase.GetCurrentMethod(), "Serverseitige Konfiguration aktualisiert", EventLogEntryType.Information);
            }
            else if (result.ResultType != BAIResultType.OK)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler auf BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
            }

            if (_ServerSideConfig != null)
            {
                // --- Update-Vorgang erforderlich?
                if ((_ServerSideConfig.Events & Events.UpdateAvailable) == Events.UpdateAvailable)
                    StartUpdate();

                // --- Log-Datei an Server schicken?
                if ((_ServerSideConfig.Events & Events.SendLogToServer) == Events.SendLogToServer)
                    SendLogToServer();
            }

            // --- Timer (wieder) starten, wenn gültiges Intervall definiert ist
            if (configPollIntervall > 0)
            {
                lock (_ConfigTimerLock)
                {
                    if (_ConfigTimer == null)
                        _ConfigTimer = new Timer(RenewServerSideConfig, null, configPollIntervall, configPollIntervall);
                    else
                        _ConfigTimer.Change(configPollIntervall, configPollIntervall);
                }
            }

        }

        private void StopClientNotifcationService()
        {
            if (IsFx30OrHigherInstalled())
            {
                try
                {
                    if (ClientNotificationService.Stop())
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Notification-Service beendet", EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Schliessen des Callback-Endpoints (" + _ServerSideConfig.NotificationAddress + "):\r\n" + ex, EventLogEntryType.Error);
                }
            }
        }

        private void StartClientNotificationService()
        {
            // --- Nur möglich, wenn .NET-Framework 3.0 oder höher installiert ist,
            //     da für die Callbacks WCF verwendet wird.
            if (IsFx30OrHigherInstalled())
            {
                ClientNotificationService.BAIClient = Instance;
                try
                {
                    if (ClientNotificationService.Start(_ServerSideConfig.NotificationAddress))
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Notification-Service auf Adresse " + _ServerSideConfig.NotificationAddress + " gestartet", EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Öffnen des Callback-Endpoints (" + _ServerSideConfig.NotificationAddress + "):\r\n" + ex, EventLogEntryType.Error);
                }
            }
            else
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "WCF wird von dieser Version des .NET-Framework nicht unterstützt. Es muss mindestens das .NET-Framework 3.0 installiert sein", EventLogEntryType.Error);
            }
        }

        private void PollTimerCallback(Object state)
        {
            if (!_Status.HasFlag(ClientStatus.Running))
                return;

            // --- Timer zwischenzeitlich anhalten
            lock (_PollTimerLock)
            {
                if (_PollTimer != null)
                    _PollTimer.Change(Timeout.Infinite, Timeout.Infinite);
                else
                    // --- Client wurde zwischenzeitlich gestoppt -> keinen Import- bzw. Export mehr durchführen
                    return;
            }

            // --- Mit threadsicherer Kopie arbeiten
            Config tempConfig = null;
            Interlocked.Exchange(ref tempConfig, _ServerSideConfig);
            if (tempConfig != null)
            {

                // --- Zähler inkrementieren
                _PollCounter++;

                // --- Prüfen ob Datenaustausch erforderlich ist
                var exchangeData = false;
                if (tempConfig.DataExchangeTime.HasValue)
                {
                    var dataExchangeTime = tempConfig.DataExchangeTime.Value;
                    if (dataExchangeTime < DateTime.Now)
                    {
                        exchangeData = true;
                        // --- DataExchangeTime gleich hochsetzen, damit beim nächsten PollTimerCallback nicht gleich wieder ein Datenaustausch erfolgt.
                        _ServerSideConfig.DataExchangeTime = DateTime.Now.AddDays(1);
                    }
                }
                else if ((_PollCounter % tempConfig.PollForDataIntervall) == 0)
                {
                    exchangeData = true;
                }

                if (exchangeData)
                {
                    Export();
                    Import();
                }

                if (_PollCounter % 30 == 0)
                {
                    var exports = (from item in tempConfig.ConfigEntries where item.Ident.Direction == Direction.ServerToClient select item.ShallowCopy()).ToList();
                    foreach (var configEntry in exports)
                    {
                        if (string.IsNullOrEmpty(configEntry.Ident.Location))
                        {
                            WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("ConfigEntry {0} : Column Location is empty", ToString()), EventLogEntryType.Error);
                            continue;
                        }
                        else
                        {
                            try { Path.GetDirectoryName(configEntry.Ident.Location); }
                            catch (Exception)
                            {
                                WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("ConfigEntry {0} : Column Location contains an invalid path {1}", ToString(), configEntry.Ident.Location), EventLogEntryType.Error);
                                continue;
                            }
                        }

                        if (!OverwriteExportFile(tempConfig))
                            RenameImportedTmpFiles(new DirectoryInfo(configEntry.Ident.Location), configEntry);
                    }
                }
            }

            // --- Timer wieder starten
            lock (_PollTimerLock)
            {
                if (_Status.HasFlag(ClientStatus.Running))
                {
                    if (_PollTimer == null)
                        _PollTimer = new Timer(PollTimerCallback, null, 1000, 1000);
                    else
                        _PollTimer.Change(1000, 1000);
                }
            }
        }

        private void ExportInternal(Config argConfig)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (argConfig == null)
                return;

            var exports = (from item in argConfig.ConfigEntries where item.Ident.Direction == Direction.ClientToServer select item.ShallowCopy()).ToList();
            if (exports.Count == 0)
                return;

            // --- Export-Einträge aufsteigend nach Priorität sortieren
            exports.Sort((e1, e2) => e1.Priority.GetValueOrDefault(0).CompareTo(e2.Priority.GetValueOrDefault(0)));

            var currentSize = 0;
            var dataList = new List<DataEntry>();
            for (int i = 0; i < exports.Count; i++)
            {
                var export = exports[i];

                // skip disabled items
                if (!export.Enabled)
                    continue;

                // --- Gültiges Datei-Suchmuster?
                if (String.IsNullOrEmpty(export.FilePattern))
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Für die Datenkategorie {0} ist kein FilePattern angegeben", export.Ident.Category), EventLogEntryType.Warning);
                    dataList.Clear();
                    continue;
                }

                // --- Gültiges Export-Verzeichnis?
                if (String.IsNullOrEmpty(export.Ident.Location))
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Für die Datenkategorie {0} ist keine Location angegeben", export.Ident.Category), EventLogEntryType.Warning);
                    dataList.Clear();
                    continue;
                }

                String exportDir = GetAbsoluteDirectory(export.Ident.Location);
                if (!Directory.Exists(exportDir))
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Das Verzeichnis {0} für die Datenkategorie {1} existiert nicht", exportDir, export.Ident.Category), EventLogEntryType.Warning);
                    dataList.Clear();
                    continue;
                }

                DirectoryInfo di = new DirectoryInfo(exportDir);

                // --- Archiv-Unterverzeichnis aufräumen
                ReorgArchiv(di);

                // --- Datei(en) umbenennen
                foreach (FileInfo tmpFile in di.GetFiles(export.FilePattern))
                {
                    // --- Zuerst einen neuen, temporären Dateinamen bestimmen
                    //     (wenn z.B. Dateien mit den Endungen .001 und .003 vorhanden sind,
                    //     dann muss die neue temporäre Datei die Endung .004 bekommen).
                    String newFileName = String.Format("{0}.{1}", tmpFile.FullName, "001");
                    SortedList<int, FileInfo> extensions = new SortedList<int, FileInfo>();

                    // --- Alle Dateien im Quellverzeichnis bestimmen, die bis auf die Extension
                    //     den gleichen Dateinamen haben
                    foreach (FileInfo tmp in di.GetFiles(Path.GetFileNameWithoutExtension(newFileName) + ".???"))
                    {
                        if (HasNumericFileExtension(tmp))
                            extensions.Add(Int32.Parse(tmp.Extension.TrimStart(".".ToCharArray())), tmp);
                    }

                    if (extensions.Count > 0)
                    {
                        Int32 newExt = (Int32)extensions.Keys.Max() + 1;
                        if (newExt > 999)
                            throw new Exception("Zuviele temporäre Dateien im Verzeichnis " + di.FullName);
                        newFileName = newFileName.Replace(Path.GetExtension(newFileName), "." + newExt.ToString("000"));
                    }

                    try
                    {
                        File.Move(tmpFile.FullName, newFileName);
                    }
                    catch (Exception ex)
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Datei {0} kann nicht umbenannt werden:\r\n{1}", tmpFile.FullName, ex), EventLogEntryType.Error);

                        // --- Innere und äußere Schleife sofort verlassen
                        i = -1;
                        break;
                    }
                    WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Datei umbenannt {0} -> {1}", tmpFile.FullName, newFileName), EventLogEntryType.Information);
                }

                if (i == -1)
                {
                    dataList.Clear();
                    break;
                }

                if (export.Ident.DataFormat == DataFormat.RawData)
                {
                    // Send to-be-exported files one by one
                    foreach (FileInfo tmpFile in di.GetFiles(export.FilePattern + ".???"))
                    {
                        if (!HasNumericFileExtension(tmpFile))
                            continue;

                        WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Lese Datei {0}", tmpFile.FullName), EventLogEntryType.Information);

                        StreamReader rd = null;

                        DataEntry dataEntry = new DataEntry();
                        dataEntry.Category = export.Ident.Category;
                        dataEntry.Location = export.Ident.Location;

                        try
                        {

                            var bytes = File.ReadAllBytes(tmpFile.FullName);
                            dataEntry.Data = Convert.ToBase64String(bytes);
                        }
                        catch (Exception ex)
                        {
                            WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Fehler beim Lesen der Datei {0}:\r\n{1}", tmpFile.FullName, ex.ToString()), EventLogEntryType.Warning);
                            continue;
                        }
                        finally
                        {
                            if (rd != null) rd.Close();
                        }

                        // --- Datei merken, damit sie später gelöscht werden kann
                        dataEntry.Tag = tmpFile.FullName;

                        if (!String.IsNullOrEmpty(dataEntry.Data))
                            dataList.Add(dataEntry);
                    }
                }
                else
                {

                    // Merge existing to-be-exported files

                    // --- Neuen Eintrag für diese Datenkategorie erstellen
                    DataEntry dataEntry = new DataEntry();
                    dataEntry.Category = export.Ident.Category;
                    dataEntry.Location = export.Ident.Location;

                    // --- Alle temporären Dateien durchgehen und zusammenfassen
                    if (!AppendExportFiles(export, di, dataEntry, ref currentSize))
                    {
                        dataList.Clear();
                        break;
                    }

                    // --- Daten-Eintrag hinzufügen
                    if (!String.IsNullOrEmpty(dataEntry.Data))
                        dataList.Add(dataEntry);

                    if (currentSize >= _ServerSideConfig.MaxKBClientToServer * 1024)
                        break;
                }
            }

            // --- Daten für Export vorhanden?
            if (dataList.Count == 0)
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Keine Daten für Export vorhanden", EventLogEntryType.Information);
            else
                ExportDataEntries(dataList);

            sw.Stop();
            WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Dauer des Export-Vorgangs: {0} ms", sw.ElapsedMilliseconds), EventLogEntryType.Information);
        }

        private void ExportDataEntries(List<DataEntry> data)
        {
            // --- ALLE Export-Dateien wurden erfolgreich aufbereitet -> Export darf erfolgen.
            try
            {
                // --- Daten an den Server übertragen.
                using (var service = new BAIService.BAIService(_ClientSideConfig))
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Übertrage Daten an Server", EventLogEntryType.Information);

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    var dataArray = data.ToArray();

                    // filter characters that are invalid for XML
                    ApplyCharacterWhitelistFilter(dataArray);

                    BAIResult result = service.SendData(Credentials, dataArray);
                    sw.Stop();
                    WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Dauer der Datenübertragung zum Server: {0} ms", sw.ElapsedMilliseconds), EventLogEntryType.Information);

                    if (result.ResultType != BAIResultType.OK)
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler auf BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
                        return;
                    }

                    for (int i = 0; i < data.Count; i++)
                    {
                        // --- Zugehörige Datei(en) können gelöscht werden
                        String[] files = data[i].Tag.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        String msg = String.Empty;

                        if (files.Length == 0) continue;

                        foreach (var item in files)
                        {
                            if (!File.Exists(item)) continue;

                            try
                            {
                                // --- Datei ins Archiv kopieren
                                CopyToArchiv(item);
                            }
                            catch (Exception copyEx)
                            {
                                WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Fehler beim Kopieren der Datei {0} ins Archiv-Verzeichnis:\r\n{1}", item, copyEx.ToString()), EventLogEntryType.Warning);
                            }

                            try
                            {
                                File.Delete(item);
                                msg += Environment.NewLine + item;
                            }
                            catch (Exception delEx)
                            {
                                WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Fehler beim Löschen der Datei:\r\n{0}", delEx.ToString()), EventLogEntryType.Warning);
                            }
                        }

                        if (msg.Length > 0)
                            WriteLogEntry(MethodBase.GetCurrentMethod(), "Exportierte Datei(en) erfolgreich gelöscht:" + msg, EventLogEntryType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex.ToString(), EventLogEntryType.Warning);
            }
        }

        private void ApplyCharacterWhitelistFilter(DataEntry[] argDataEntries)
        {
            if (argDataEntries == null || !argDataEntries.Any())
                return;

            foreach (var entry in argDataEntries)
            {
                entry.Data = XmlValidString(entry.Data);
            }
        }

        private string XmlValidString(string argIn)
        {
            if (argIn == null) return null;

            char[] values = argIn.ToCharArray();
            var sb = new StringBuilder();

            foreach (var letter in values)
            {
                if (!XmlConvert.IsXmlChar(letter))
                {
                    int value = Convert.ToInt32(letter);
                    string hexOutput = String.Format("{0:X}", value);

                    string hexWithDetectionChar = "%0x" + hexOutput + "%";

                    sb.Append(hexWithDetectionChar);
                }
                else
                    sb.Append(letter);
            }
            return sb.ToString();
        }

        private void ImportInternal(bool argOverwriteFile = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<DataEntry> data = new List<DataEntry>();
            Boolean? furtherDataAvailable = null;

            try
            {
                // --- Daten vom Server abholen
                using (var service = new BAIService.BAIService(_ClientSideConfig))
                {
                    //service.Url
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Hole Daten vom Server", EventLogEntryType.Information);
                    Console.WriteLine("Hole Daten vom Server");
                    DataEntry[] t;

                    var sw2 = new Stopwatch();
                    sw2.Start();

                    Boolean specified;
                    BAIResult result = service.ReceiveData(Credentials, out t, out furtherDataAvailable, out specified);

                    sw2.Stop();
                    WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Dauer der Datenübertragung vom Server: {0} ms", sw2.ElapsedMilliseconds.ToString()), EventLogEntryType.Information);

                    if (result.ResultType == BAIResultType.OK)
                    {
                        if (t != null)
                        {
                            data = new List<DataEntry>(t);
                        }
                    }
                    else
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler auf BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex, EventLogEntryType.Warning);
            }

            if (data.Count == 0)
            {
                Console.WriteLine("Server hat keine Daten für Import");
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Server hat keine Daten für Import", EventLogEntryType.Information);
            }

            // --- Alle importierten Datenkategorien durchgehen
            var imported = ImportDataEntries(data, argOverwriteFile);

            // --- Wurden alle importierten Datensätze erfolgreich übernommen?
            if (data.Count > 0 && imported.Count() == data.Count)
            {
                try
                {
                    // --- Erfolgreiche Übernahme beim BAI-Service quittieren
                    using (var service = new BAIService.BAIService(_ClientSideConfig))
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), imported.Count() + " Datenkategorien erfolgreich importiert: " + String.Join("; ", imported.ToArray()), EventLogEntryType.Information);

                        BAIResult result = service.DataReceived(Credentials);
                        if (result.ResultType != BAIResultType.OK)
                            WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler auf BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
                    }
                }
                catch (Exception ex)
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex.ToString(), EventLogEntryType.Warning);
                }
            }

            sw.Stop();
            WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Dauer des Import-Vorgangs: {0} ms", sw.ElapsedMilliseconds.ToString()), EventLogEntryType.Information);

            // --- Wenn der BAI-Server weitere Daten hat, dann sofort wieder einen Datenaustausch anstossen
            if (furtherDataAvailable.GetValueOrDefault(false))
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "BAI-Server hat weitere Daten", EventLogEntryType.Information);
                if (_Status.HasFlag(ClientStatus.Running))
                    ImportInternal(argOverwriteFile);
            }
        }

        public static string ConvertXmlToJson(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented, true);
            return json;
        }

    



        JObject RemoveAtPrefixes(JObject obj)
        {
            var properties = obj.Properties().ToList();
            foreach (var property in properties)
            {
                if (property.Name.StartsWith("@"))
                {
                    var newName = property.Name.Substring(1);
                    var newProperty = new JProperty(newName, property.Value);
                    obj.Remove(property.Name);
                    obj.Add(newProperty);
                }

                if (property.Value.Type == JTokenType.Object)
                {
                    RemoveAtPrefixes((JObject)property.Value);
                }
                else if (property.Value.Type == JTokenType.Array)
                {
                    foreach (var item in property.Value)
                    {
                        if (item.Type == JTokenType.Object)
                        {
                            RemoveAtPrefixes((JObject)item);
                        }
                    }
                }
            }
            return obj;
        }


        public async Task<HttpResponseMessage> PostJsonAsync(string endpoint, string jsonPayload, string category = "")
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Endpoint cannot be null or whitespace.", EventLogEntryType.Error);
                throw new ArgumentException("Endpoint cannot be null or whitespace.", nameof(endpoint));
            }

            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "JSON payload cannot be null or whitespace.", EventLogEntryType.Error);
                throw new ArgumentException("JSON payload cannot be null or whitespace.", nameof(jsonPayload));
            }

            try
            {
                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    // Log the error
                    string errorMessage = $"Error: {response.StatusCode} - {response.ReasonPhrase}";
                    Console.Error.WriteLine(errorMessage);
                    WriteLogEntry(MethodBase.GetCurrentMethod(), errorMessage, EventLogEntryType.Error);

                    // Throw an exception
                    throw new HttpRequestException(errorMessage);
                }

                Console.WriteLine($"Daten wurden erfolgreich übertragen: {category}");
                WriteLogEntry(MethodBase.GetCurrentMethod(), $"Daten wurden erfolgreich übertragen: {category}", EventLogEntryType.Information);

                return response;
            }
            catch (HttpRequestException e)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), $"Request error: {e.Message}", EventLogEntryType.Error);

                Console.Error.WriteLine($"Request error: {e.Message}");
                throw; // Re-throwing the exception preserves the original stack trace.
            }
            catch (Exception e)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), $"Unexpected error: {e.Message}", EventLogEntryType.Error);

                Console.Error.WriteLine($"Unexpected error: {e.Message}");
                throw; // Re-throwing the exception preserves the original stack trace.
            }
        }

        private IEnumerable<string> ImportDataEntries(IEnumerable<DataEntry> data, bool argOverwriteFile = false)
        {
            var imported = new List<string>();


            foreach (var import in data)
            {
                try
                {
                    Console.WriteLine("Datenkategorie: " + import.Category);
                    if(import.Category == "BAI_Err")
                    {
                        imported.Add(import.Category);
                        //return imported;
                    }
                    var cfgEntry = _ServerSideConfig.ConfigEntries.FirstOrDefault(c => string.Equals(c.Ident.Category, import.Category, StringComparison.InvariantCultureIgnoreCase) && string.Equals(c.Ident.Location, import.Location, StringComparison.InvariantCultureIgnoreCase));

                    if (cfgEntry == null || !cfgEntry.Enabled)
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Die Datenkategorie ist nicht aktiv oder nicht vorhanden.", EventLogEntryType.Error);
                        Console.WriteLine("Die Datenkategorie ist nicht aktiv oder nicht vorhanden.");
                        continue;
                    }

                    var format = cfgEntry.Ident.DataFormat;
                    if (format == DataFormat.Xml)
                    {
                        var jsonData = ConvertXmlToJson(import.Data);
                        var jsonObject = JObject.Parse(jsonData);
                        jsonObject = RemoveAtPrefixes(jsonObject);
                        jsonData = jsonObject.ToString();
                        PostJsonAsync(_ClientSideConfig.IntegrationClientUrl, jsonData).Wait();

                        Thread.Sleep(2000);
                        
                    }
                    else if (format == DataFormat.RawData)
                    {
                        // Base64-string in lesbaren Text konvertieren
                        var base64String = import.Data;
                        var bytes = Convert.FromBase64String(base64String);
                        var decodedData = Encoding.UTF8.GetString(bytes);
                        Console.WriteLine(decodedData);
                    }
                    else
                    {
                        // Ausgabe für 'normale' Textdaten
                        if (import.Data.Contains("%0x"))
                        {
                            var formattedData = ReplaceStringDelimiterWithAsciiDelimiter(import.Data);
                            Console.WriteLine(formattedData);
                        }
                        else
                        {
                            Console.WriteLine(import.Data); 
                        }
                    }

                    imported.Add(import.Category);
                }
                catch (Exception ex)
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Import der Datenkategorie:\r\n" + ex.ToString(), EventLogEntryType.Error);
                    //Console.WriteLine(String.Format("Fehler beim Import der Datenkategorie {0}:\r\n{1}", import.Category, ex));
                    Thread.Sleep(2000);
                }
            }

            // Depug only
            //imported.Clear();

            return imported;
        }



        private string ReplaceStringDelimiterWithAsciiDelimiter(string argData)
        {
            var pattern = @"0x[0-9-Fa-f]{2}";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(argData);
            string delimiter = string.Empty;

            if (match.Success)
                delimiter = match.Value;

            var delimiterWithDetectionChar = "%" + delimiter + "%";

            byte[] bytes = null;
            int NumberChars = delimiter.Length;
            bytes = new byte[NumberChars / 2];

            for (int i = 2; i < NumberChars; i += 2)
            {
                try
                {
                    bytes[i / 2] = Convert.ToByte(delimiter.Substring(i, 2), 16);
                }
                catch (FormatException)
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Fehler beim Konvertieren des Ascii Zeichens - kein gültiges Zeichen gefunden {0}.", delimiter), EventLogEntryType.Warning);
                }

            }


            var splitData = argData.Split(new String[] { delimiterWithDetectionChar }, StringSplitOptions.None);

            StringBuilder output = new StringBuilder();

            char ascii = (char)0x20;
            try
            {
                ascii = (char)Convert.ToInt32(string.Format("0x{0:x2}", bytes[1]), 16);
            }
            catch (FormatException)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Fehler beim Konvertieren des Ascii Zeichens - {0} kein gültiges Zeichen.", delimiter), EventLogEntryType.Warning);
            }

            output.Append(splitData[0]);
            for (int i = 1; i < splitData.Length; i++)
            {
                output.Append(ascii);
                output.Append(splitData[i]);
            }

            argData = output.ToString();

            return argData;
        }

        private bool TryGetImportFileName(DateTime dt, ConfigEntry cfgEntry, string category, out string fileName)
        {
            fileName = null;

            if (String.IsNullOrEmpty(cfgEntry.FilePattern))
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Für die Datenkategorie {0} ist kein FilePattern angegeben", category), EventLogEntryType.Warning);
                return false;
            }

            // --- Das FilePattern darf einen Datums-Format-String eingeschlossen zwischen spitzen Klammern enthalten
            fileName = Regex.Replace(cfgEntry.FilePattern, @"<.+?>", (m) =>
            {
                var formatString = m.ToString().TrimStart('<').TrimEnd('>');
                return dt.ToString(formatString);
            });

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Der Dateiname <{0}> der Datenkategorie {1} ist kein gültiger Dateiname", fileName, category), EventLogEntryType.Warning);
                return false;
            }

            return true;
        }

        public void WriteLogEntry(MethodBase method, String message, EventLogEntryType type)
        {
            WriteEventLogEntry(String.Format("{0}()\r\n\r\n{1}", method.Name, message), type);

            WriteFileLogEntry(message, type);
        }

        private void WriteEventLogEntry(string message, EventLogEntryType type)
        {
            try
            {
                EventSourceCreationData escd;
                // --- Ereignis-Protokoll und Quelle für den Prozess anlegen
                if (!EventLog.SourceExists(ClientName, "."))
                {
                    escd = new EventSourceCreationData(ClientName, Constants.EventLogName);
                    escd.MachineName = Environment.MachineName;
                    EventLog.CreateEventSource(escd);
                    EventLog tmp = new EventLog(escd.LogName, escd.MachineName, escd.Source);
                    tmp.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 7);
                    // --- Protokollgröße setzen
                    tmp.MaximumKilobytes = Constants.EventLogSizeKb;
                }

                // --- Ereignis-Protokoll-Nachrichten sind auf 30000 Zeichen und ein paar Zerquetschte beschränkt.
                if (message.Length > 3000)
                    message = message.Substring(0, 3000);

                EventLog.WriteEntry(ClientName, message, type);
            }
            catch (Exception) { }
        }

        private void WriteFileLogEntry(string message, EventLogEntryType type)
        {
            string logDir = LogDir;
            if (!string.IsNullOrWhiteSpace(logDir))
            {
                if (type == EventLogEntryType.Warning || type == EventLogEntryType.Error)
                {
                    _LogFileLock.EnterWriteLock();

                    try
                    {
                        if (!Directory.Exists(logDir))
                            Directory.CreateDirectory(logDir);

                        XmlSerializer xs = new XmlSerializer(typeof(LogRoot));
                        String fileName = Path.Combine(logDir, ClientName + ".log.xml");
                        LogRoot logRoot = new LogRoot();
                        if (File.Exists(fileName))
                        {
                            using (Stream inStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                                logRoot = xs.Deserialize(inStream) as LogRoot;
                        }

                        logRoot.Append(message, type, Credentials == null ? "Unknown" : Credentials.Username);
                        logRoot.Shrink(MaxLogEntries);

                        using (Stream outStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                        {
                            using (var xmlwr = XmlWriter.Create(outStream, new XmlWriterSettings() { Indent = true, CloseOutput = true }))
                            {
                                var ns = new XmlSerializerNamespaces();
                                ns.Add("", "");
                                xs.Serialize(xmlwr, logRoot, ns);
                            }
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        _LogFileLock.ExitWriteLock();
                    }
                }
            }
        }

        public BAIUser Credentials
        {
            get
            {
                BAIUser user = null;

                if (_ClientSideConfig != null)
                {
                    user = new BAIUser();
                    user.Username = _ClientSideConfig.Username;
                    user.Password = _ClientSideConfig.Password;
                    user.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    user.Identifier = "FileClient";
                    user.MacAddress = DoX.FX.Util.NetworkHelper.GetMacAddress();
                }

                return user;
            }
        }

        private static String GetAbsoluteDirectory(String relativeOrAbsoluteDirectory)
        {
            if (!Path.IsPathRooted(relativeOrAbsoluteDirectory))
            {
                // --- Pfad ist relativ zur exe angegeben. Absoluter Pfad zusammensetzen
                String baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.GetFullPath(Path.Combine(baseDir, relativeOrAbsoluteDirectory));
            }

            return relativeOrAbsoluteDirectory;
        }

        /// <summary>
        /// Encrypts the specified clear text.
        /// </summary>
        /// <param name="clearText">The clear text.</param>
        /// <returns></returns>
        private static String Encrypt(String clearText)
        {
            if (String.IsNullOrEmpty(clearText)) return String.Empty;

            byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(clearText);
            PasswordDeriveBytes pdb = new PasswordDeriveBytes("b5@*8X\\47xhU2Cl?<8)8x&zcRvQ68^L>wQ>:P6", new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

            MemoryStream ms = new MemoryStream();
            Rijndael alg = Rijndael.Create();
#pragma warning disable 612,618
            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);
#pragma warning restore 612,618
            CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(clearBytes, 0, clearBytes.Length);
            cs.Close();
            byte[] encryptedData = ms.ToArray();
            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Decrypts the specified cipher text.
        /// </summary>
        /// <param name="cipherText">The cipher text.</param>
        /// <returns></returns>
        private static String Decrypt(String cipherText)
        {
            if (String.IsNullOrEmpty(cipherText)) return String.Empty;

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            PasswordDeriveBytes pdb = new PasswordDeriveBytes("b5@*8X\\47xhU2Cl?<8)8x&zcRvQ68^L>wQ>:P6", new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

            MemoryStream ms = new MemoryStream();
            Rijndael alg = Rijndael.Create();
#pragma warning disable 612,618
            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);
#pragma warning restore 612,618
            CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherBytes, 0, cipherBytes.Length);
            cs.Close();
            byte[] decryptedData = ms.ToArray();

            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }

        private static void CopyToArchiv(String fullFileName)
        {
            DateTime now = DateTime.Now;
            String archivDirName = Path.Combine(Path.GetDirectoryName(fullFileName), "Archive Files");
            String archivSubDirName = Path.Combine(archivDirName, now.ToString("yyyyMMdd"));

            if (!Directory.Exists(archivDirName)) Directory.CreateDirectory(archivDirName);
            if (!Directory.Exists(archivSubDirName)) Directory.CreateDirectory(archivSubDirName);

            String archivFileName = String.Format("Archive_{0}_{1}_{2}",
                                    now.ToString("yyyyMMdd"),
                                    now.ToString("HHmmss"),
                                    Path.GetFileName(fullFileName));

            File.Copy(fullFileName, Path.Combine(archivSubDirName, archivFileName), true);

            // --- Damit die Datei-Zeitstempel mit dem Dateinamen zusammenpassen...
            File.SetCreationTime(Path.Combine(archivSubDirName, archivFileName), now);
            File.SetLastWriteTime(Path.Combine(archivSubDirName, archivFileName), now);
            File.SetLastAccessTime(Path.Combine(archivSubDirName, archivFileName), now);
        }

        private static void ReorgArchiv(DirectoryInfo di)
        {
            try
            {
                String archivDirName = Path.Combine(di.FullName, "Archive Files");
                if (!Directory.Exists(archivDirName))
                    return;

                DateTime dt = DateTime.Now.AddDays(-30);
                String delDt = dt.ToString("yyyyMMdd");

                foreach (String archivDir in Directory.GetDirectories(archivDirName))
                {
                    string test = Path.GetFileName(archivDir);
                    int testerix = Path.GetFileName(archivDir).CompareTo(delDt);

                    if (Path.GetFileName(archivDir).CompareTo(delDt) < 0)
                        Directory.Delete(archivDir, true);
                }
            }
            catch (Exception) { }
        }


        private void RenameImportedTmpFiles(DirectoryInfo di, ConfigEntry cfgEntry)
        {
            var filePattern = Regex.Replace(cfgEntry.FilePattern, @"<.+?>", (m) => "*") + ".tmp";
            FileInfo[] files;
            try
            {
                files = di.GetFiles(filePattern);
            }
            catch (Exception)
            {
                return;
            }

            foreach (FileInfo fi in files)
            {
                var destFileName = fi.FullName.Remove(fi.FullName.Length - 4, 4);
                if (!File.Exists(destFileName))
                {
                    // --- Zuerst ins Archiv kopieren
                    CopyToArchiv(fi.FullName);

                    // --- Tmp-Datei in Ziel-Datei umbenennen
                    File.Move(fi.FullName, destFileName);

                    // --- Flag-File schreiben
                    if (!String.IsNullOrEmpty(cfgEntry.FlagFileName))
                        using (FileStream stream = File.Create(Path.Combine(di.FullName, cfgEntry.FlagFileName))) { }
                }
            }
        }

        private Boolean AppendExportFiles(ConfigEntry export, DirectoryInfo di, DataEntry dataEntry, ref int currentSize)
        {
            if (export.Ident.DataFormat == DataFormat.Xml)
            {
                // --- XML-Dateien
                Int32 cnt = 0;
                XmlWriterSettings settings = new XmlWriterSettings();
                Encoding enc = GetEncoding(export);
                settings.CloseOutput = true;
                settings.Encoding = Encoding.Unicode;
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                settings.ConformanceLevel = ConformanceLevel.Auto;

                StringBuilder sb = new StringBuilder();

                using (XmlWriter wr = XmlWriter.Create(sb, settings))
                {
                    foreach (FileInfo tmpFile in di.GetFiles(export.FilePattern + ".???"))
                    {
                        String content = String.Empty;
                        try
                        {
                            if (!HasNumericFileExtension(tmpFile))
                                continue;

                            WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Lese Datei {0}", tmpFile.FullName), EventLogEntryType.Information);

                            cnt++;

                            content = File.ReadAllText(tmpFile.FullName, enc);

                            using (XmlReader rd = XmlReader.Create(new StringReader(content)))
                            {
                                if (!export.Ident.IgnoreRootElements)
                                {
                                    // --- Bis zum 1. Element lesen...
                                    do
                                    {
                                        rd.Read();
                                    } while (rd.NodeType != XmlNodeType.Element && rd.Name != export.Ident.Category);

                                    // --- Namespace auslesen
                                    var ns = rd.NamespaceURI;
                                    var prefix = rd.Prefix;

                                    if (cnt == 1)
                                        // --- Root-Element schreiben
                                        wr.WriteStartElement(prefix, export.Ident.Category, ns);

                                    // --- Alles nachfolgende übernehmen

                                    // skip first
                                    if (!rd.EOF)
                                        rd.Read(); // take inner elements into acount only

                                    while (!rd.EOF)
                                    {
                                        if (rd.NodeType == XmlNodeType.Element && rd.Name == export.Ident.Category)
                                            wr.WriteNode(rd, true); // write + go to next node
                                        else
                                            rd.Read(); // go to next node
                                    }
                                }
                                else
                                {
                                    // --- XML lesen
                                    while (!rd.EOF)
                                    {
                                        if (rd.NodeType == XmlNodeType.Element)
                                            wr.WriteNode(rd, true); // write + go to next node
                                        else
                                            rd.Read(); // go to next node
                                    }
                                }

                                //NOTE FiMi 20140214: 
                                // http://stackoverflow.com/questions/4259597/xmlreader-failing-when-there-are-no-return-characters
                                // The uncommented code below only works for XML where nodes are seperated by Whitespace (or any other nodes)
                                //
                                // OK:
                                // <Sample Name="A" Description= "a"/>
                                // <Sample Name="B" Description= "b"/>
                                //
                                // OK:
                                // <Sample Name="A" Description= "a"/><Dummy /><Sample Name="B" Description= "b"/>
                                //
                                // NOK:
                                // <Sample Name="A" Description= "a"/><Sample Name="B" Description= "b"/>
                                //
                                // "ReadToFollowing" moves the read-cursor to the next node with given name
                                // ".WriteNode" moves the read-cursor to the next node
                                // "NewLines"/"Returns" are handles as Nodes of type "Whitespace"
                                // In the above (NOK)-case after reading Node "Sample A" the cursor is already on Node "Sample B" (instead of Node "WhiteSpace")
                                // calling ReadToFollowing lets the cursor jump over...

                                //while ( rd.ReadToFollowing(export.Ident.Category, ns))
                                //wr.WriteNode(rd, true);
                            }
                            // --- Datei merken, damit sie später gelöscht werden kann
                            dataEntry.Tag += ";" + tmpFile.FullName;

                            currentSize += GetByteCount(content, GetEncoding(export));
                            if (currentSize >= _ServerSideConfig.MaxKBClientToServer * 1024)
                                break;
                        }
                        catch (Exception ex)
                        {
                            WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Fehler beim Lesen der XML-Datei {0}:\r\n{1}\r\n\r\n\r\nDateiinhalt (Codierung={2}):\r\n{3}", tmpFile.FullName, ex.ToString(), enc.WebName, content.Length > 5000 ? content.Substring(0, 5000) : content), EventLogEntryType.Error);
                            return false;
                        }
                    }
                    if ((cnt > 0) && (!export.Ident.IgnoreRootElements))  //nur wenn BAI-Rootelemente enthalten sind.
                        wr.WriteEndElement();
                }
                if (sb.Length > 0)
                {
                    dataEntry.Data = sb.ToString();
                    //WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("XML: {0}", dataEntry.Data), EventLogEntryType.Information);
                }
            }
            else
            {
                // --- CSV- bzw. Fixe Länge - Dateien
                foreach (FileInfo tmpFile in di.GetFiles(export.FilePattern + ".???"))
                {
                    if (!HasNumericFileExtension(tmpFile))
                        continue;

                    WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Lese Datei {0}", tmpFile.FullName), EventLogEntryType.Information);

                    // --- Ggfs. Zeilenumbruch hinzufügen
                    if (!String.IsNullOrEmpty(dataEntry.Data) && !dataEntry.Data.EndsWith(Environment.NewLine))
                        dataEntry.Data += Environment.NewLine;

                    StreamReader rd = null;

                    try
                    {

                        // --- Encoding konfiguriert?
                        if (String.IsNullOrEmpty(export.Encoding))
                            // --- Verwende Unicode-Encoding
                            rd = new StreamReader(tmpFile.OpenRead(), true);
                        else
                            // --- Konfiguriertes Encoding verwenden
                            rd = new StreamReader(tmpFile.OpenRead(), Encoding.GetEncoding(export.Encoding), true);

                        // --- Inhalt der Datei hinzufügen
                        dataEntry.Data += rd.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), String.Format("Fehler beim Lesen der Datei {0}:\r\n{1}", tmpFile.FullName, ex.ToString()), EventLogEntryType.Warning);
                        return false;
                    }
                    finally
                    {
                        if (rd != null) rd.Close();
                    }

                    // --- Datei merken, damit sie später gelöscht werden kann
                    dataEntry.Tag += ";" + tmpFile.FullName;

                    currentSize += GetByteCount(dataEntry.Data, GetEncoding(export));
                    if (currentSize >= _ServerSideConfig.MaxKBClientToServer * 1024)
                        break;
                }
            }

            return true;
        }

        private Encoding GetEncoding(ConfigEntry export)
        {
            return String.IsNullOrEmpty(export.Encoding) ? Encoding.Unicode : Encoding.GetEncoding(export.Encoding);
        }

        private Int32 GetByteCount(String content, Encoding enc)
        {
            if (String.IsNullOrEmpty(content))
                return 0;

            return enc.GetByteCount(content);
        }

        private void AppendImportFiles(DirectoryInfo di, ConfigEntry cfgEntry, Encoding enc, String destTmpFileName)
        {
            var destFileName = destTmpFileName.Replace(".tmp", "");
            if (File.Exists(destFileName))
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Move " + destFileName + " to " + destTmpFileName.Replace(".tmp", ".000"), EventLogEntryType.Information);
                File.Move(destFileName, destTmpFileName.Replace(".tmp", ".000"));
            }

            if (cfgEntry.Ident.DataFormat == DataFormat.Xml)
            {
                Int32 cnt = 0;
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.CloseOutput = true;
                settings.Encoding = enc;
                settings.Indent = true;
                settings.OmitXmlDeclaration = false;
                settings.ConformanceLevel = ConformanceLevel.Auto;



                using (XmlWriter wr = XmlWriter.Create(destTmpFileName, settings))
                {
                    var filePattern = Regex.Replace(cfgEntry.FilePattern, @"<.+?>", (m) => "*") + ".???";

                    foreach (FileInfo fi in di.GetFiles(filePattern))
                    {
                        // --- Nur Files mit einer numerischen Extension zusammenfassen
                        if (!HasNumericFileExtension(fi)) continue;

                        // --- Nur Dateien mit Inhalt übernehmen
                        if (fi.Length > 0)
                        {
                            cnt++;
                            using (XmlReader rd = XmlReader.Create(new StringReader(File.ReadAllText(fi.FullName, enc))))
                            {
                                if (!cfgEntry.Ident.IgnoreRootElements)
                                {
                                    // --- Bis zum 1. Element lesen...
                                    do
                                    {
                                        rd.Read();
                                    } while (rd.NodeType != XmlNodeType.Element && rd.Name != cfgEntry.Ident.Category);

                                    // --- Namespace auslesen
                                    String ns = rd.NamespaceURI;
                                    var prefix = rd.Prefix;

                                    if (cnt == 1)
                                        // --- Root-Element übernehmen
                                        wr.WriteStartElement(prefix, cfgEntry.Ident.Category, ns);

                                    // --- Alles nachfolgende übernehmen
                                    while (rd.ReadToFollowing(cfgEntry.Ident.Category, ns))
                                        wr.WriteNode(rd, true);
                                }
                                else
                                {
                                    // --- XML ohne Root-Elemente
                                    while (rd.Read())
                                        wr.WriteNode(rd, true);
                                }
                            }
                        }
                        fi.Delete();
                    }
                    if (cnt > 0 && !cfgEntry.Ident.IgnoreRootElements)
                        wr.WriteEndElement();
                }
            }
            else
            {
                var filePattern = Regex.Replace(cfgEntry.FilePattern, @"<.+?>", (m) => "*") + ".???";
                foreach (FileInfo fi in di.GetFiles(filePattern))
                {
                    // --- Nur Files mit einer numerischen Extension zusammenfassen
                    if (!HasNumericFileExtension(fi)) continue;

                    // --- Nur Dateien mit Inhalt übernehmen
                    if (fi.Length > 0)
                    {
                        String content = File.ReadAllText(fi.FullName, enc);
                        if (!content.EndsWith(Environment.NewLine))
                            content += Environment.NewLine;
                        File.AppendAllText(destTmpFileName, content, enc);
                    }

                    // --- Angehängte Datei löschen
                    fi.Delete();
                }
            }
        }

        private void StartUpdate()
        {
            BAIResult result = new BAIResult();
            UpdateFile[] files = null;

            try
            {
                using (var service = new BAIService.BAIService(_ClientSideConfig))
                    result = service.ReceiveUpdate(Credentials, out files);
            }
            catch (Exception ex)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex.ToString(), EventLogEntryType.Warning);
            }

            if (result.ResultType == BAIResultType.OK && files != null && files.Length > 0)
            {
                Boolean doUpdate = true;

                String updateDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Dorner Electronic\\BAI Client\\" + ClientName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_Update");
                Directory.CreateDirectory(updateDir);

                // --- Neue Assemblies übernehmen
                SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
                foreach (UpdateFile file in files)
                {
                    String hash = Convert.ToBase64String(sha1.ComputeHash(file.FileContent));
                    if (hash == Convert.ToBase64String(file.SHA1Hash))
                    {
                        File.WriteAllBytes(Path.Combine(updateDir, file.FileName), file.FileContent);
                    }
                    else
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Hash-Fehler bei Datei: " + file.FileName, EventLogEntryType.Error);
                        doUpdate = false;
                        break;
                    }
                }
                if (doUpdate)
                {
                    // --- Anhalten
                    Stop();

                    // --- Den Updater-Prozess aus einem temporären Verzeichnis aus starten, damit er sich beim
                    //     Update nicht selber blockiert.
                    var tmpDir = new DirectoryInfo(updateDir + "_Updater");
                    tmpDir.Create();
                    var tmpExe = Path.Combine(tmpDir.FullName, UPDATE_PROC_NAME);
                    var mainExe = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), UPDATE_PROC_NAME);
                    if (File.Exists(tmpExe))
                        File.Delete(tmpExe);
                    File.Copy(mainExe, tmpExe);
                    var args = ClientName + " \"" + Process.GetCurrentProcess().MainModule.FileName + "\" \"" + updateDir + "\"";

                    try
                    {
                        using (var service = new BAIService.BAIService(_ClientSideConfig))
                            result = service.UpdateReceived(Credentials);
                    }
                    catch (Exception ex)
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Aufruf des BAI-Service:\r\n" + ex.ToString(), EventLogEntryType.Warning);
                    }

                    // --- Update ausführen
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Update wird angestossen:\r\n" + tmpExe + " " + args, EventLogEntryType.Information);
                    try
                    {
                        Process.Start(tmpExe, args);
                    }
                    catch (Exception ex)
                    {
                        WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler beim Ausführen des Update-Prozesses:\r\n" + ex.ToString(), EventLogEntryType.Error);
                    }
                }
                else
                {
                    WriteLogEntry(MethodBase.GetCurrentMethod(), "Update wird nicht ausgeführt", EventLogEntryType.Error);
                }
            }
            else if (result.ResultType != BAIResultType.OK)
            {
                WriteLogEntry(MethodBase.GetCurrentMethod(), "Fehler auf BAI-Service:\r\n" + result.Message, EventLogEntryType.Warning);
            }
        }

        private string CreateLogText(int count)
        {
            StringBuilder log = new StringBuilder();
            EventLog eventLog = new EventLog(Constants.EventLogName);
            EventLogEntry[] entries = new EventLogEntry[eventLog.Entries.Count + 100];
            if (eventLog.Entries.Count > 0)
                eventLog.Entries.CopyTo(entries, 0);

            Int32 addedCount = 0;
            for (int i = entries.Length - 1; i >= 0; i--)
            {
                if (entries[i] == null)
                    continue;

                if (ClientName != entries[i].Source)
                    continue;

                log.AppendFormat("{0} {1} : {2}", entries[i].TimeGenerated, entries[i].EntryType, entries[i].Message);
                log.AppendLine();
                log.Append('=', 100);
                log.AppendLine();

                addedCount++;

                if (addedCount >= count)
                    break;
            }

            return log.ToString();
        }

        /// <summary>
        /// Prüft, ob eine Datei eine Extension der Form .000, .001 usw. hat.
        /// </summary>
        /// <param name="fi">Datei, deren Extension geprüft werden soll.</param>
        /// <returns>True, wenn numerische Extension im Bereich von .000 bis .999.</returns>
        private static Boolean HasNumericFileExtension(FileInfo fi)
        {
            Boolean erg = false;

            if (fi != null)
            {
                String ext = fi.Extension.TrimStart(".".ToCharArray());
                Int32 numericExt = -1;

                if (Int32.TryParse(ext, out numericExt))
                {
                    if (ext.Length == 3)
                        erg = (numericExt >= 0 && numericExt <= 999);
                }
            }
            return erg;
        }

        private static Boolean IsFx30OrHigherInstalled()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.0\Setup");
            if (key != null)
            {
                Int32? installSuccess = key.GetValue("InstallSuccess") as Int32?;
                if (installSuccess.HasValue && installSuccess.Value == 1) return true;
            }

            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");

            try
            {
                return
                    key.GetSubKeyNames()
                    // .NET versions start with v (v4.0, v2.0.50727 etc)
                    .Where(skn => skn.StartsWith("v"))
                    // remove 'v', the get first number before .
                    .Select(skn => skn.Substring(1).Split('.')[0])
                    .Select(ver => int.Parse(ver))
                    .Any(v => v >= 3);
            }
            catch
            {
                return false;
            }
        }

#if DEBUG
        private class PermissiveCertifikatePolicy
        {
            static PermissiveCertifikatePolicy _CurrentPolicy;

            private PermissiveCertifikatePolicy()
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, error) => true;
            }

            public static void Enact()
            {
                _CurrentPolicy = new PermissiveCertifikatePolicy();
            }
        }
#endif
    }

}
