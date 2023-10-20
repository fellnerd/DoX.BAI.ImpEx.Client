using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using DoX.BAI.ImpEx.Shared;

namespace DoX.BAI.ImpEx.Client
{
    public partial class MainForm : Form
    {
        private IClientController _BAIClient;
        private Boolean _TestOk = false;
        private readonly ToolStripMenuItem _SendLogToServer;

        public MainForm()
        {
            InitializeComponent();
            Application.ThreadException += (o, eventArgs) => MessageBox.Show("Unhandled Exception:\r\n\r\n" + eventArgs.Exception.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (o, eventArgs) => MessageBox.Show("Unhandled Exception:\r\n\r\n" + eventArgs.ExceptionObject.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

            _SendLogToServer = new ToolStripMenuItem();
            _SendLogToServer.Text = "&Transmit to BAI-Service";
            _SendLogToServer.ShortcutKeys = Keys.Control | Keys.L;
            _SendLogToServer.Click += new EventHandler(SendLogToServer_Click);
            LogViewer.AddContextMenuItem(_SendLogToServer);


            toolStripStatusLabelVersion.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void SendLogToServer_Click(object sender, EventArgs e)
        {
            if (LogViewer.Grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("No rows selected", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Cursor = Cursors.WaitCursor;
            try
            {
                foreach (DataGridViewRow row in LogViewer.Grid.SelectedRows)
                {
                    Debug.WriteLine(row.Cells["Date/Time"].Value.ToString());
                    Debug.WriteLine(row.Cells["Message"].Value.ToString());
                    Debug.WriteLine(row.Cells["Source"].Value.ToString());
                    Debug.WriteLine(row.Cells["Type"].Value.ToString());
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            // --- Laufen schon mehrere GUI-Prozesse mit dem gleichen Namen?
            if (processes.Length > 1)
            {
                foreach (var p in processes)
                {
                    // --- Ist es der eigene Prozess?
                    if (p.Id != current.Id)
                    {
                        // --- Nicht der eigene Prozess
                        if (p.MainModule.FileName == current.MainModule.FileName)
                        {
                            // --- Es wurde versucht die GUI-Anwendung aus dem selben Verzeichnis
                            //     ein 2. Mal zu starten.
                            //     Das ist nicht zulässig, da ansonsten Remoting-Konflikte auftreten!
                            MessageBox.Show("Application already running!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Application.Exit();
                        }
                    }
                }                
            }

            // ------------------------------------------------------------------------------------
            // --- Channel für Remoting-Verbindung zum BAI-Client registrieren
            Type t = TypeBinder.GetRemotingTypeByInterface(typeof(IClientController));
            IpcClientChannel clientChannel = new IpcClientChannel(Constants.RemotingPortName, new BinaryClientFormatterSinkProvider());
            ChannelServices.RegisterChannel(clientChannel, false);

            // ------------------------------------------------------------------------------------
            // --- Callback-Objekt für Remoting bereitstellen
            SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));

            Dictionary<string, string> prop = new Dictionary<string, string>();
            prop.Add("impersonationLevel", "None");
            prop.Add("authorizedGroup", account.Value);
            prop.Add("portName", Constants.CallbackPortName);

            IpcServerChannel serverChannel = new IpcServerChannel(prop, new BinaryServerFormatterSinkProvider());
            ChannelServices.RegisterChannel(serverChannel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(BAIClientCallback), "IClientControllerCallback", WellKnownObjectMode.Singleton);

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        _BAIClient = Activator.GetObject(t, "ipc://" + Constants.RemotingPortName + "/IClientController") as IClientController;
                        toolStripStatusLabel.Text = "Connected with BAI-Client [" + _BAIClient.ClientName + "]";
                        break;
                    }
                    catch (RemotingException)
                    {
                        // --- Damit beim debuggen der Remoting-Server gestartet wird, bevor sich der Client darauf verbindet.
                        System.Threading.Thread.Sleep(500);
                    }                    
                }

                LogViewer.Source = _BAIClient.ClientName;
                this.Text = "BAI-Client [" + _BAIClient.ClientName + "]";
                SetStatus(_BAIClient.GetStatus());

                // --- Per default nur Warnungen und Fehler anzeigen
                LogViewer.btnMessages.PerformClick();

                // --- Ereigniss asynchron laden
                LogViewer.LoadEventProtAsync();
            }
            catch (TargetInvocationException ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            // --- Tool-Strip des Controls zum ToolStripPanel des Formulars hinzufügen
            this.toolStripContainer.TopToolStripPanel.Controls.Clear();
            LogViewer.toolStrip.GripStyle = ToolStripGripStyle.Visible;
            LogViewer.toolStrip.Padding = this.toolStrip.Padding;
            this.toolStripContainer.TopToolStripPanel.Join(LogViewer.toolStrip);
            this.toolStripContainer.TopToolStripPanel.Join(this.toolStrip);

        }

        public void SetStatus(ClientStatus status)
        {
            toolStripButtonStop.Enabled = ((status & ClientStatus.Running) == ClientStatus.Running);
            toolStripButtonStart.Enabled = !toolStripButtonStop.Enabled;
            toolStripButtonTest.Enabled = !toolStripButtonStart.Enabled;
            toolStripButtonImport.Enabled = !toolStripButtonStart.Enabled;
            toolStripButtonExport.Enabled = !toolStripButtonStart.Enabled;
        }

        private void toolStripButtonLogin_Click(object sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm();

            loginForm.Config = _BAIClient.ClientSideConfig;
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                _BAIClient.ClientSideConfig = loginForm.Config;
            }
        }

        private void toolStripButtonStop_Click(object sender, EventArgs e)
        {
            // --- In eigenem Thread ausführen, da die Aktion einen Statuswechsel (Aufruf von RemotingStatusEvent)
            //     auslöst und dies sonst zu einem Deadlock führen würde.
            ThreadPool.QueueUserWorkItem(delegate(Object state)
            {
                _BAIClient.Stop();
            });
        }

        private void toolStripButtonStart_Click(object sender, EventArgs e)
        {
            // --- In eigenem Thread ausführen, da die Aktion einen Statuswechsel (Aufruf von RemotingStatusEvent)
            //     auslöst und dies sonst zu einem Deadlock führen würde.
            ThreadPool.QueueUserWorkItem(state => _BAIClient.Start());
        }

        private void toolStripButtonTest_Click(object sender, EventArgs e)
        {
            // --- In eigenem Thread ausführen, da die Aktion einen Statuswechsel (Aufruf von RemotingStatusEvent)
            //     auslösen kann und dies sonst zu einem Deadlock führen würde.
            _TestOk = false;
            Thread t = new Thread(delegate()
            {
                _TestOk = _BAIClient.Test();
            });

            // --- Aufrufen und auf Antwort warten
            t.Start();
            t.Join();

            // --- Dem Benutzer mitteilen wie der Test ausgefallen ist
            if (!_TestOk)
                MessageBox.Show("Test failed! See log for further details.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show("Test succesfully!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void toolStripButtonImport_Click(object sender, EventArgs e)
        {
            _BAIClient.Import();
        }

        private void toolStripButtonExport_Click(object sender, EventArgs e)
        {
            _BAIClient.Export();
        }

        private void toolStripButtonSendLogToServer_Click(object sender, EventArgs e)
        {
            _BAIClient.SendLogToServer();
        }

    }
}
