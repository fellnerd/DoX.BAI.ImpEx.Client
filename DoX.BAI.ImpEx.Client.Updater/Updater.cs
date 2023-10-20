using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Threading;

namespace DoX.BAI.ImpEx.Client
{
    internal sealed class Updater : IUpdater
    {
        private String _ClientName;
        private DirectoryInfo _ProgramDir;
        private DirectoryInfo _UpdateDir;

        #region IUpdater Members

        public Boolean Update(String clientName, String programDir, String updateDir, String callingProc)
        {
            if (String.IsNullOrEmpty(clientName))
                throw new ArgumentNullException("clientName");

            if (String.IsNullOrEmpty(programDir))
                throw new ArgumentNullException("programDir");

            if (String.IsNullOrEmpty(updateDir))
                throw new ArgumentNullException("updateDir");

            _ClientName = clientName;

            _ProgramDir = new DirectoryInfo(programDir);
            if (!_ProgramDir.Exists)
            {
                EventLog.WriteEntry(clientName, "Update()\r\n\r\nUpdate kann nicht ausgeführt werden, da das Programm-Verzeichnis nicht existiert: " + _ProgramDir.FullName, EventLogEntryType.Warning);
                return false;
            }

            _UpdateDir = new DirectoryInfo(updateDir);
            if (!_UpdateDir.Exists)
            {
                EventLog.WriteEntry(clientName, "Update()\r\n\r\nUpdate kann nicht ausgeführt werden, da das Programm-Verzeichnis nicht existiert: " + _UpdateDir.FullName, EventLogEntryType.Warning);
                return false;
            }

            // --- Alle Prozess im Programm-Verzeichnis, welches upgedated werden soll, beenden
            foreach (var item in _ProgramDir.GetFiles("*.exe"))
            {
                if (!item.FullName.Equals(Process.GetCurrentProcess().MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!StopService(item.FullName))
                        StopWin32Process(item.FullName);                    
                }
            }

            Thread.Sleep(2000);

            CreateBackup();

            CopyUpdate();

            DeleteOldBackupDirs(3);
            DeleteOldUpdateDirs(3);

            if (!StartService(callingProc))
                StartWin32Process(callingProc);

            return true;
        }

        #endregion

        private ServiceController GetService(String pathName)
        {
            ManagementObjectCollection coll;
            String serviceName = null;
            String escapedPath = pathName.Replace("\\", "\\\\");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Service WHERE PathName = '" + escapedPath + "'"))
            {
                coll = searcher.Get();
                if (coll.Count == 0)
                {
                    // --- Manchmal ist der Pfad noch zusätzlich mit " versehen...
                    searcher.Query = new ObjectQuery("SELECT Name FROM Win32_Service WHERE PathName = '\"" + escapedPath + "\"'");
                    coll = searcher.Get();
                }

                foreach (var item in coll)
                {
                    serviceName = item.GetPropertyValue("Name").ToString();
                    break;
                }
            }

            if (!String.IsNullOrEmpty(serviceName))
                return new ServiceController(serviceName);
            else
                return null;
        }

        private Boolean StopService(String pathName)
        {
            var sc = GetService(pathName);
            if (sc != null)
            {
                // --- Dienst anhalten
                sc.Stop();
                while (sc.Status != ServiceControllerStatus.Stopped)
                    sc.Refresh();

                EventLog.WriteEntry(_ClientName, "Update()\r\n\r\nDienst " + sc.ServiceName + " beendet", EventLogEntryType.Information);
                return true;
            }

            return false;
        }

        private Boolean StartService(String pathName)
        {
            var sc = GetService(pathName);
            if (sc != null)
            {
                sc.Start();
                EventLog.WriteEntry(_ClientName, "Update()\r\n\r\nDienst " + sc.ServiceName + " gestartet", EventLogEntryType.Information);
                return true;
            }

            return false;
        }

        private Boolean StopWin32Process(String pathName)
        {
            Boolean stopped = false;
            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(pathName));
            foreach (var process in processes)
            {
                if (process.MainModule.FileName.Equals(pathName, StringComparison.OrdinalIgnoreCase))
                {
                    // --- Das ist der gesuchte Prozess
                    try
                    {
                        process.CloseMainWindow();
                        if (!process.WaitForExit(2000))
                            process.Kill();

                        EventLog.WriteEntry(_ClientName, "Update()\r\n\r\nProzess " + pathName + " beendet", EventLogEntryType.Information);
                        stopped = true;
                    }
                    catch (Exception) { }
                    break;
                }
            }

            return stopped;

        }

        private Boolean StartWin32Process(String pathName)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(pathName);
            try
            {
                p.Start();
                EventLog.WriteEntry(_ClientName, "Update()\r\n\r\nProzess " + pathName + " gestartet", EventLogEntryType.Information);
                return true;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(_ClientName, "Update()\r\n\r\nProzess " + pathName + " kann nicht gestartet werden:\r\n" + ex.ToString(), EventLogEntryType.Information);
                return false;
            }
        }

        private void CreateBackup()
        {
            DirectoryInfo backupDir = new DirectoryInfo(_UpdateDir.FullName + "_Backup");
            if (!backupDir.Exists)
                backupDir.Create();

            String msg = "Update()\r\n\r\nKopieren der Dateien ins Backup-Verzeichnis " + backupDir.FullName + Environment.NewLine;
            foreach (var item in _ProgramDir.GetFiles())
            {
                String destFile = Path.Combine(backupDir.FullName, item.Name);
                if (File.Exists(destFile))
                    File.Delete(destFile);

                item.CopyTo(destFile);
                msg += Environment.NewLine + "   " + item + " -> " + destFile;
            }
            EventLog.WriteEntry(_ClientName, msg, EventLogEntryType.Information);

        }

        private void CopyUpdate()
        {
            String msg = "Update()\r\n\r\nKopieren der Dateien aus Update-Verzeichnis " + _UpdateDir.FullName + Environment.NewLine;
            foreach (var item in _UpdateDir.GetFiles())
            {
                String destFile = Path.Combine(_ProgramDir.FullName, item.Name);
                if (File.Exists(destFile))
                    File.Delete(destFile);

                item.CopyTo(destFile);
                msg += Environment.NewLine + "   " + item + " -> " + destFile;
            }
            EventLog.WriteEntry(_ClientName, msg, EventLogEntryType.Information);
        }

        private void DeleteOldBackupDirs(Int32 remainingDirs)
        {
            try
            {
                DirectoryInfo di = _UpdateDir.Parent;
                DirectoryInfo[] dirs = di.GetDirectories(_ClientName + "_????????_??????_Update_Backup");
                if (dirs.Length > remainingDirs)
                {
                    for (int i = 0; i < dirs.Length - remainingDirs; i++)
                        dirs[i].Delete(true);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(_ClientName, "Update()\r\n\r\nFehler beim Löschen alter Backup-Verzeichnisse:\r\n" + ex.ToString(), EventLogEntryType.Warning);
            }
        }

        private void DeleteOldUpdateDirs(Int32 remainingDirs)
        {
            try
            {
                DirectoryInfo[] dirs = _UpdateDir.Parent.GetDirectories(_ClientName + "_????????_??????_Update");
                if (dirs.Length > remainingDirs)
                {
                    for (int i = 0; i < dirs.Length - remainingDirs; i++)
                        dirs[i].Delete(true);
                }

                dirs = _UpdateDir.Parent.GetDirectories(_ClientName + "_????????_??????_Update_Updater");
                if (dirs.Length > remainingDirs)
                {
                    for (int i = 0; i < dirs.Length - remainingDirs; i++)
                        dirs[i].Delete(true);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(_ClientName, "Update()\r\n\r\nFehler beim Löschen alter Update-Verzeichnisse:\r\n" + ex.ToString(), EventLogEntryType.Warning);
            }

        }

    }
}
