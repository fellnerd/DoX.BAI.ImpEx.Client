using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.ServiceProcess;
using System.Diagnostics;
using System.Management;

namespace DoX.BAI.ImpEx.Client
{
    class Program
    {
        static void Main(String[] args)
        {
            if (args.Length != 3) return;

            String clientName = args[0];
            String callingProc = args[1];
            String updateDir = args[2];

            IUpdater updater = new Updater();
            try
            {
                updater.Update(clientName, Path.GetDirectoryName(callingProc), updateDir, callingProc);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(clientName, "Update()\r\n\r\nFehler:\r\n" + ex.ToString(), EventLogEntryType.Error);
            }
        }
    }
}
