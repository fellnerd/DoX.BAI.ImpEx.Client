using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using DoX.BAI.ImpEx.Shared;

namespace DoX.BAI.ImpEx.Client
{
    public partial class BAIClientSvc : ServiceBase
    {
        private IClientController _Client;
        private Thread _Thread;

        public BAIClientSvc()
        {
            InitializeComponent();

            // --- Assembly laden, die IClientController implementiert
            Type clientType = TypeBinder.GetTypeByInterface(typeof(IClientController));
            if (clientType != null)
            {
                PropertyInfo pi = clientType.GetProperty("Instance", clientType);
                _Client = (IClientController)pi.GetValue(null, null);
            }
            else
                throw new Exception("Im Verzeichnis " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + " wurde keine Assembly gefunden, die " + typeof(IClientController).Name + " implementiert");
        }

        protected override void OnStart(string[] args)
        {
            // --- Bearbeitung in eigenem Thread starten
            _Thread = new Thread(new ThreadStart(_Client.Start));
            _Thread.Start();
        }

        protected override void OnStop()
        {
            // --- Der Dienst kann nicht über den ServiceController gestoppt werden.
            //     Das ist nur über die GUI-Anwendung möglich...
        }

    }
}
