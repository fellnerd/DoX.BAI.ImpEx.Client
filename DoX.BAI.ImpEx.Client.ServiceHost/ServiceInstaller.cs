using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Xml;
using System.Xml.Linq;

namespace DoX.BAI.ImpEx.Client
{
    [RunInstaller(true)]
    public partial class ServiceInstaller : Installer
    {
        private const string PARAM_NAME = "name";
        private const string PARAM_DISPLAYNAME = "displayname";
        private const string PARAM_ACCOUNT = "account";
        private const string PARAM_USER = "user";
        private const string PARAM_PASSWORD = "password";
        private const string PARAM_STARTTYPE = "starttype";
        private const string PARAM_TARGETDIR = "targetdir";

        private const string EVENT_LOG_NAME = "Dorner BAI-Client";
        private const int EVENT_LOG_SIZE_KB = 500 * 64;

        public ServiceInstaller()
        {
            InitializeComponent();
        }

        public string GetContextParameter(string argParamName)
        {
            string val = "";
            StringDictionary dic = new StringDictionary();

            foreach (var key in this.Context.Parameters.Keys)
            {
                if (key.ToString().ToLower() == argParamName.ToLower())
                    val = this.Context.Parameters[key.ToString()].ToString();
            }
            return val;
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            var success = CheckArgs(this.Context.Parameters);
            if (!success)
                return;

            base.OnBeforeInstall(savedState);

            bool isUserAccount = false;

            // --- Decode the command line switches
            string name = GetContextParameter(PARAM_NAME);
            serviceInstaller1.ServiceName = "BAI-Client [" + name + "]";

            string dname = GetContextParameter(PARAM_DISPLAYNAME);
            if (!string.IsNullOrEmpty(dname))
                serviceInstaller1.DisplayName = dname;
            else
                serviceInstaller1.DisplayName = serviceInstaller1.ServiceName;

            // --- What type of credentials to use to run the service.
            //     The default is networkservice
            string acct = GetContextParameter(PARAM_ACCOUNT);

            if (0 == acct.Length)
                acct = "networkservice";

            acct = acct.ToLower();

            // --- Decode the type of account to use
            switch (acct)
            {
                case "user":
                    serviceProcessInstaller1.Account = ServiceAccount.User;
                    isUserAccount = true;
                    break;
                case "localservice":
                    serviceProcessInstaller1.Account = ServiceAccount.LocalService;
                    break;
                case "localsystem":
                    serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
                    break;
                case "networkservice":
                    serviceProcessInstaller1.Account = ServiceAccount.NetworkService;
                    break;
                default:
                    serviceProcessInstaller1.Account = ServiceAccount.User;
                    isUserAccount = true;
                    break;
            }

            // --- Should I use a user account?
            if (isUserAccount)
            {
                // --- If we need to use a user account, set the user name and password.
                string username = GetContextParameter(PARAM_USER);
                string password = GetContextParameter(PARAM_PASSWORD);

                serviceProcessInstaller1.Username = username;
                serviceProcessInstaller1.Password = password;
            }

            string parstarttype = GetContextParameter(PARAM_STARTTYPE);
            if (!string.IsNullOrEmpty(parstarttype))
            {
                var mode = ServiceStartMode.Automatic;
                try
                {
                    mode = (ServiceStartMode)Enum.Parse(typeof(ServiceStartMode), parstarttype, true);
                    serviceInstaller1.StartType = mode;
                }
                catch (Exception)
                {
                    Log("Parameter " + PARAM_STARTTYPE + " does not have a valid value");
                }
            }

            if (!EventLog.SourceExists(name, "."))
            {
                var escd = new EventSourceCreationData(name, EVENT_LOG_NAME);
                escd.MachineName = Environment.MachineName;
                EventLog.CreateEventSource(escd);
                EventLog tmp = new EventLog(escd.LogName, escd.MachineName, escd.Source);
                tmp.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 7);
                tmp.MaximumKilobytes = EVENT_LOG_SIZE_KB;
                Log("Event-Source '" + name + "' with size " + tmp.MaximumKilobytes + "KB created");
            }
        }

        public override void Install(IDictionary stateSaver)
        {
            try
            {
                var targetDir = GetContextParameter(PARAM_TARGETDIR);
                if (!String.IsNullOrEmpty(targetDir))
                {
                    DirectoryInfo di = new DirectoryInfo(targetDir);
                    foreach (var item in di.GetFiles("*.config"))
                    {
                        var config = XDocument.Load(item.FullName);

                        var applicationSettings = config.Descendants("applicationSettings").SingleOrDefault();
                        if (applicationSettings != null)
                        {
                            XElement setting = applicationSettings.Descendants("setting").Where(e => e.Attributes("name") != null && e.Attribute("name").Value == "Clientname").FirstOrDefault();
                            if (setting != null)
                            {
                                if (setting.Element("value") != null)
                                    setting.Element("value").Remove();
                                setting.Add(new XElement("value", GetContextParameter(PARAM_NAME)));
                                config.Save(item.FullName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Could not write client name to config file: " + ex);
            }
            finally
            {
                base.Install(stateSaver);
            }
        }

        /// <summary>
        /// Uninstall based on the service name
        /// </summary>
        /// <PARAM name="savedState"></PARAM>
        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            base.OnBeforeUninstall(savedState);

            // --- Set the service name based on the command line
            var name = GetContextParameter(PARAM_NAME);

            using (ServiceController sc = new ServiceController(name))
            {
                try
                {
                    Log("Service-Name: " + sc.ServiceName);
                }
                catch (InvalidOperationException)
                {
                    name = "BAI-Client [" + name + "]";
                }
            }

            serviceInstaller1.ServiceName = name;
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceInstaller1.ServiceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                        sc.Start();
                }
            }
            catch (Exception) { }
        }

        private void serviceInstaller1_BeforeUninstall(object sender, InstallEventArgs e)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceInstaller1.ServiceName))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                        sc.Stop();
                }
            }
            catch (Exception) { }
        }

        public void ShowHelp()
        {
            Log("");
            Log("Valid Parameters: ");
            Log("");
            Log("/" + PARAM_NAME + "\r\n   Specifies the unique Name of the Service (is required)");
            Log("");
            Log("/" + PARAM_DISPLAYNAME + "\r\n   Specifies Display-Name of the Service");
            Log("");
            Log("/" + PARAM_ACCOUNT + "\r\n   Specifies the Account on which the Service runs\r\n    possible values: networkservice, localsystem, localservice, user");
            Log("");
            Log("/" + PARAM_USER + "\r\n   Username in case the Account = User\r\n    not necessary with built-in accounts");
            Log("");
            Log("/" + PARAM_PASSWORD + "\r\n   Password in case the Account = User\r\n    not necessary with built-in accounts");
            Log("");
            Log("/" + PARAM_STARTTYPE + "\r\n   Starttype of the service (optional)\r\n    valid are: automatic, manual, disabled");
            Log("");
            Log("Example: Install");
            Log("DoX.BAI.ImpEx.Client.ServiceHost.exe /i\r\n   " +
                "/" + PARAM_NAME + @"=Dorner_FileClient1" + "\r\n   " +
                "/" + PARAM_DISPLAYNAME + @"=Dorner FileClient1" + "\r\n   " +
                "/" + PARAM_ACCOUNT + @"=User" + "\r\n   " +
                "/" + PARAM_USER + @"=domainX\userX" + "\r\n   " +
                "/" + PARAM_STARTTYPE + @"=automatic" + "\r\n   " +
                "/" + PARAM_PASSWORD + @"=Foo");
            Log("");
            Log("Example: Uninstall");
            Log("DoX.BAI.ImpEx.Client.ServiceHost.exe /u\r\n" +
                "/" + PARAM_NAME + @"=Dorner_FileClient1");
            Log("");
            Log("");
        }

        private void Log(string argText)
        {
            this.Context.LogMessage(argText);
            if (Environment.UserInteractive)
            {
                Console.WriteLine(argText);
            }
        }


        private bool CheckArgs(StringDictionary argContextParams)
        {
            bool showHelp = false;
            var paramName = "";

            if (argContextParams == null)
                showHelp = true;
            if (argContextParams.Count < 2)
                showHelp = true;

            try
            {
                foreach (var arg in argContextParams.Keys)
                {
                    if (arg.ToString().ToLower().StartsWith(PARAM_NAME))
                        paramName = arg.ToString();
                }

                if (string.IsNullOrEmpty(paramName))
                {
                    Log("");
                    Log("Argument " + PARAM_NAME + " is missing");
                    ShowHelp();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log("Reading Arguments failed, Error: " + ex.Message);
                showHelp = true;
            }

            if (showHelp)
            {
                ShowHelp();
                return false;
            }

            return true;
        }

    }
}
