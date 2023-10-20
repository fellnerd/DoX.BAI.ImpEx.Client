using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using DoX.BAI.ImpEx.Shared;
using System.IO;
using System.Globalization;

namespace DoX.BAI.ImpEx.Client
{
    public partial class EventLogViewer : UserControl
    {

        private EventLog _EventLog;
        private String _LogName = Constants.EventLogName;
        private String _Source = null;
        private Boolean _Loaded = false;
        private Int32 _EventOrder = 0;

        private Boolean _SourceVisible = true;
        private Boolean _CategoryVisible = true;
        private Boolean _EventIDVisible = true;

        private Int32 _OldRowIndex = 0;

        // --- Anzahl Errors, Warnings und Messages
        private Int32 _NumErrors = 0;
        private Int32 _NumWarnings = 0;
        private Int32 _NumMessages = 0;

        // --- Objekte für die Datenbindung
        private DataSet _DataSet = null;
        private BindingSource _BindingSource = null;

        private delegate void AppendEntryToDataSetDelegate(EventLogEntry entry);

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogViewer"/> class.
        /// </summary>
        public EventLogViewer()
        {
            InitializeComponent();
        }

        #region "Öffentliche Eigenschaften und Methoden"

        /// <summary>
        /// Gets or sets the log.
        /// </summary>
        /// <value>The log.</value>
        public String Log
        {
            get { return _LogName; }
            set
            {
                if (_Loaded)
                {
                    throw new InvalidOperationException("Property 'Log' darf nicht gesetzt werden, wenn schon Daten geladen wurden");
                }
                _LogName = value;
            }
        }

        public String Source
        {
            get { return _Source; }
            set
            {
                if (_Loaded)
                {
                    throw new InvalidOperationException("Property 'Source' darf nicht gesetzt werden, wenn schon Daten geladen wurden");
                }
                _Source = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [source visible].
        /// </summary>
        /// <value><c>true</c> if [source visible]; otherwise, <c>false</c>.</value>
        public Boolean SourceVisible
        {
            get { return _SourceVisible; }
            set
            {
                _SourceVisible = value;
                if (!this.DesignMode)
                {
                    if (this.Grid.Columns.Contains("Source"))
                        this.Grid.Columns["Source"].Visible = _SourceVisible;

                    this.SourceCombo.Visible = _SourceVisible;
                    this.SourceSeparator.Visible = _SourceVisible;
                    this.SourceLabel.Visible = _SourceVisible;

                    foreach (DataGridViewColumn col in Grid.Columns)
                    {
                        if (col.Name != "Message")
                        {
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                        }
                    }
                }
            }
        }

        public Boolean CategoryVisible
        {
            get { return _CategoryVisible; }
            set
            {
                _CategoryVisible = value;
                if (!this.DesignMode)
                {
                    if (this.Grid.Columns.Contains("Category"))
                        this.Grid.Columns["Category"].Visible = _CategoryVisible;

                    foreach (DataGridViewColumn col in Grid.Columns)
                    {
                        if (col.Name != "Message")
                        {
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                        }
                    }
                }
            }
        }

        public Boolean EventIDVisible
        {
            get { return _EventIDVisible; }
            set
            {
                _EventIDVisible = value;
                if (!this.DesignMode)
                {
                    if (this.Grid.Columns.Contains("EventID"))
                        this.Grid.Columns["EventID"].Visible = _EventIDVisible;

                    foreach (DataGridViewColumn col in Grid.Columns)
                    {
                        if (col.Name != "Message")
                        {
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                        }
                    }
                }
            }
        }

        private void RecCount_TextChanged(object sender, EventArgs e)
        {
            LoadEventProtAsync();
        }

        private int GetRecCount()
        {
            var result = 0;

            if (string.IsNullOrEmpty(RecCount.Text))
                RecCount.Text = "0";

            if (!int.TryParse(RecCount.Text, out result))
            {
                MessageBox.Show("Only numeric input allowed", this.ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                RecCount.Text = result.ToString();
            }

            return result;
        }

        public void LoadEventProtAsync()
        {
            _DataSet.Clear();
            _NumErrors = 0;
            _NumWarnings = 0;
            _NumMessages = 0;

            var cnt = GetRecCount();
            ThreadPool.QueueUserWorkItem(state => LoadEventProt(cnt));
        }

        public void LoadEventProt(int count)
        {

            // --- Kopie der aktuellen Ereignisse für diesen Thread anlegen. Das Array wird
            //     etwas größer als die aktuelle Anzahl des Ereignisprotokolls dimensioniert, da
            //     der aktuelle Thread unterbrochen werden könnte und in der Zwischenzeit neue Ereignisse
            //     protokolliert werden.
            EventLogEntry[] entries = new EventLogEntry[_EventLog.Entries.Count + 100];
            if (_EventLog.Entries.Count > 0)
                _EventLog.Entries.CopyTo(entries, 0);

            // --- Ereignisse zum DataSet hinzufügen (aktuelle zuerst)
            int addedCount = 0;
            for (int i = entries.Length - 1; i >= 0; i--)
            {
                if (entries[i] == null)
                    continue;

                if (!CheckSource(entries[i]))
                    continue;

                if (InvokeRequired)
                    Invoke(new AppendEntryToDataSetDelegate(AppendEntryToDataSet), new Object[] { entries[i] });
                else
                    AppendEntryToDataSet(entries[i]);

                addedCount++;

                if (addedCount >= count)
                    break;
            }

            _Loaded = true;
        }

        public void AddContextMenuItem(System.Windows.Forms.ToolStripMenuItem menuItem)
        {
            contextMenuStrip.Items.Add(menuItem);
        }

        #endregion

        #region "Private Methoden"

        private void AppendEntryToDataSet(EventLogEntry entry)
        {
            if (CheckSource(entry))
            {
                // --- Zeilenumbrüche zwecks besserer Darstellung ersetzen
                String message = entry.Message;
                message = message.Replace("\r\n\r\n", " | ");
                message = message.Replace("\r\n", " | ");

                // --- Zweites Sortierkriterium setzen, damit die Sortierung auch bei Ereignissen
                //     mit der selben Zeit stimmt
                Interlocked.Increment(ref _EventOrder);

                _DataSet.Tables["Events"].Rows.Add(new Object[] { entry.EntryType, entry.TimeGenerated, message, entry.Source, entry.Category, entry.InstanceId, _EventOrder });

                if (_DataSet.Tables["Events"].Rows.Count > GetRecCount())
                {
                    var oldestEntry = _DataSet.Tables["Events"].AsEnumerable().OrderBy(r => r[1]).ThenBy(r => r[6]).FirstOrDefault();
                    if (oldestEntry != null)
                        try
                        {
                            oldestEntry.Delete();
                        }
                        catch (Exception) { }
                }
                else
                {
                    UpdateButtonCounters(entry);
                }

                _DataSet.AcceptChanges();
                RefreshFilterDisplay(); 
            }
        }

        private Boolean CheckSource(EventLogEntry entry)
        {
            if (!String.IsNullOrEmpty(Source))
                return Source == entry.Source;

            return true;
        }

        private void UpdateButtonCounters(EventLogEntry entry)
        {

            // --- Zähler incrementieren
            switch (entry.EntryType)
            {
                case EventLogEntryType.Error:
                    _NumErrors++;
                    break;
                case EventLogEntryType.FailureAudit:
                    break;
                case EventLogEntryType.Information:
                    _NumMessages++;
                    break;
                case EventLogEntryType.SuccessAudit:
                    break;
                case EventLogEntryType.Warning:
                    _NumWarnings++;
                    break;
                default:
                    break;
            }

            // --- Text der Schaltflächen aktualisieren
            btnErrors.Text = _NumErrors.ToString() + " Errors";
            btnWarnings.Text = _NumWarnings.ToString() + " Warnings";
            btnMessages.Text = _NumMessages.ToString() + " Messages";

            // --- Tool-Tip-Text aktualisieren
            btnErrors.ToolTipText = btnErrors.Text;
            btnWarnings.ToolTipText = btnWarnings.Text;
            btnMessages.ToolTipText = btnMessages.Text;
        }

        private String GenerateEventFilter()
        {
            Boolean displayErrors = btnErrors.Checked;
            Boolean displayWarnings = btnWarnings.Checked;
            Boolean displayMessages = btnMessages.Checked;

            String displayFilter = string.Empty;

            if (btnErrors.Checked)
                // --- Fehler sollen angezeigt werden
                displayFilter = "(Type='Error')";

            if (btnWarnings.Checked)
            {
                // --- Warnungen sollen angezeigt werden
                if (string.IsNullOrEmpty(displayFilter))
                {
                    displayFilter = "(Type='Warning')";
                }
                else
                {
                    displayFilter = displayFilter.Trim(")".ToCharArray());
                    displayFilter += " OR Type='Warning')";
                }
            }

            if (btnMessages.Checked)
            {
                // --- Informationen sollen angezeigt werden.
                if (string.IsNullOrEmpty(displayFilter))
                {
                    displayFilter = "(Type='Information')";
                }
                else
                {
                    displayFilter = displayFilter.Trim(")".ToCharArray());
                    displayFilter += " OR Type='Information')";
                }
            }

            if (string.IsNullOrEmpty(displayFilter))
            {
                displayFilter = "Type=''";
            }
            else
            {
                if (!string.IsNullOrEmpty(SourceCombo.Text.Trim()))
                {
                    displayFilter += " AND Source='" + SourceCombo.Text + "'";
                }
                if (!string.IsNullOrEmpty(FindText.Text))
                {
                    displayFilter += " AND (Message LIKE '*" + FindText.Text + "*' OR CONVERT([Date/Time], System.String) LIKE '%" + FindText.Text + "%')";
                }
            }

            return displayFilter;
        }

        private void RefreshFilterDisplay()
        {
            if (Grid.Rows.Count == 0)
            {
                // --- Keine Ereignisse vorhanden, die den eingestellten Filterkriterien entsprechen.
                if (this.FindText.Text.Length > 0)
                {
                    this.FindText.BackColor = Color.Salmon;
                }
                else
                {
                    this.FindText.BackColor = Color.White;
                }
                this.NotFoundLabel.Visible = true;
            }
            else
            {
                this.FindText.BackColor = Color.White;
                this.NotFoundLabel.Visible = false;
            }
        }

        #endregion

        #region "Event-Handler"

        private void EventLogViewer_Load(object sender, System.EventArgs e)
        {
            
            // --- Grid initialisieren
            Grid.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.Transparent;
            Grid.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.Transparent;
            Grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            Grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders);

            if (DesignMode) return;

            // --- Zum Ereignisprotokoll verbinden
            _EventLog = new EventLog(_LogName);

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    _EventLog.EnableRaisingEvents = true;
                    i = 5;
                }
                catch (Win32Exception ex)
                {
                    // --- Fehler passiert, wenn das Ereignisprotokoll noch leer ist!?
                    if (ex.NativeErrorCode != 223) throw;

                    // --- Einfach einen Eintrag schreiben, damit der Fehler nicht mehr auftritt
                    if (String.IsNullOrEmpty(_EventLog.Source))
                        _EventLog.Source = "BAI";

                    _EventLog.WriteEntry("Start logging", EventLogEntryType.Information);
                    i++;
                }
            }
            
            _EventLog.EntryWritten += EntryWritten;

            // --- DataSet erstellen
            _DataSet = new DataSet("EventLog Entries");
            _DataSet.Tables.Add("Events");
            {
                _DataSet.Tables["Events"].Columns.Add("Type");
                _DataSet.Tables["Events"].Columns.Add("Date/Time");
                _DataSet.Tables["Events"].Columns["Date/Time"].DataType = typeof(System.DateTime);
                _DataSet.Tables["Events"].Columns.Add("Message");
                _DataSet.Tables["Events"].Columns.Add("Source");
                _DataSet.Tables["Events"].Columns.Add("Category");
                _DataSet.Tables["Events"].Columns.Add("EventID");
                _DataSet.Tables["Events"].Columns.Add("EventOrder");
                _DataSet.Tables["Events"].Columns["EventOrder"].DataType = typeof(System.Int32);
            }

            // --- BindingSource erstellen
            _BindingSource = new BindingSource(_DataSet, "Events");
            _BindingSource.Sort = "Date/Time DESC, EventOrder DESC";
            Grid.DataSource = _BindingSource;

            this.Grid.Columns["EventOrder"].Visible = false;

            // --- Breite aller Spalten, ausser 'Message' automatisch einstellen
            foreach (DataGridViewColumn col in Grid.Columns)
            {
                if (col.Name != "Message")
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader;
                }
                if (col.Name == "Date/Time")
                {
                    col.ValueType = typeof(System.DateTime);
                    col.DefaultCellStyle = new DataGridViewCellStyle();
                    col.DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                }
            }

            if (Grid.Columns.Contains("Source"))
                Grid.Columns["Source"].Visible = _SourceVisible;

            this.SourceCombo.Visible = _SourceVisible;
            this.SourceSeparator.Visible = _SourceVisible;
            this.SourceLabel.Visible = _SourceVisible;

            if (Grid.Columns.Contains("Category"))
            {
                Grid.Columns["Category"].Visible = _CategoryVisible;
            }

            if (Grid.Columns.Contains("EventID"))
            {
                Grid.Columns["EventID"].Visible = _EventIDVisible;
            }

        }

        private void EntryWritten(object sender, System.Diagnostics.EntryWrittenEventArgs e)
        {
            // --- Ereignis-Protokoll meldet einen neuen Eintrag
            if (this.InvokeRequired)
                // --- Aufruf mittels Invoke
                Invoke(new AppendEntryToDataSetDelegate(AppendEntryToDataSet), new object[] { e.Entry });
            else
                // --- Direkt aufrufen
                AppendEntryToDataSet(e.Entry);
        }

        private void btnErrors_Click(object sender, System.EventArgs e)
        {
            // --- Filter einstellen und Anzeige aktualisieren
            _BindingSource.Filter = GenerateEventFilter();
            RefreshFilterDisplay();
        }

        private void btnWarnings_Click(object sender, System.EventArgs e)
        {
            // --- Filter einstellen und Anzeige aktualisieren
            _BindingSource.Filter = GenerateEventFilter();
            RefreshFilterDisplay();
        }

        private void btnMessages_Click(object sender, System.EventArgs e)
        {
            // --- Filter einstellen und Anzeige aktualisieren
            _BindingSource.Filter = GenerateEventFilter();
            RefreshFilterDisplay();
        }

        private void cmbSourceCombo_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            // --- Filter einstellen und Anzeige aktualisieren
            _BindingSource.Filter = GenerateEventFilter();
            RefreshFilterDisplay();
        }

        private void FindText_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            // --- Bestimmte Zeichen sind nicht erlaubt, da diese zu einer Exception
            //     beim Filtern führen können.
            //if (Char.IsPunctuation(e.KeyChar))
            //{
            //    if (!Char.IsWhiteSpace(e.KeyChar) && e.KeyChar != ':' && e.KeyChar != '.')
            //    {
            //        e.Handled = true;
            //        System.Media.SystemSounds.Exclamation.Play();
            //    }
            //}
        }

        private void FindText_TextChanged(object sender, System.EventArgs e)
        {
            _BindingSource.Filter = GenerateEventFilter();

            if (this.Grid.Rows.Count == 0)
            {
                // --- keine Events gefunden
                this.FindText.BackColor = Color.Salmon;
                this.NotFoundLabel.Visible = true;
            }
            else
            {
                this.FindText.BackColor = Color.White;
                this.NotFoundLabel.Visible = false;
            }
            FindText.Focus();
        }

        private void Grid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            this.Grid.Invalidate();
        }

        private void Grid_CurrentCellChanged(object sender, EventArgs e)
        {
            // --- Zeile neu zeichnen, wenn der Benutzer eine neue Zeile anklickt.
            if (_OldRowIndex != -1)
            {
                this.Grid.InvalidateRow(_OldRowIndex);
            }
            _OldRowIndex = this.Grid.CurrentCellAddress.Y;

        }

        private void Grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            // --- Fokus nicht automatisch zeichnen
            e.PaintParts = (e.PaintParts & ~DataGridViewPaintParts.Focus);

            if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
            {

                Rectangle rowBounds = new Rectangle(0, e.RowBounds.Top, this.Grid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible) - this.Grid.HorizontalScrollingOffset + 1, e.RowBounds.Height);

                // --- auswahl zeichnen
                System.Drawing.Drawing2D.LinearGradientBrush backbrush = new System.Drawing.Drawing2D.LinearGradientBrush(rowBounds, this.Grid.DefaultCellStyle.SelectionBackColor, e.InheritedRowStyle.ForeColor, System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                try
                {
                    e.Graphics.FillRectangle(backbrush, rowBounds);
                }
                finally
                {
                    backbrush.Dispose();
                }
            }

        }

        private void Grid_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {

            Rectangle rowBounds = new Rectangle(0, e.RowBounds.Top, this.Grid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible) - this.Grid.HorizontalScrollingOffset + 1, e.RowBounds.Height);
            SolidBrush forebrush = null;
            
            try
            {
                if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
                    forebrush = new SolidBrush(e.InheritedRowStyle.SelectionForeColor);
                else
                    forebrush = new SolidBrush(e.InheritedRowStyle.ForeColor);
            }
            finally
            {
                forebrush.Dispose();
            }

            if (this.Grid.CurrentCellAddress.Y == e.RowIndex)
                e.DrawFocus(rowBounds, true);

        }

        private void Grid_CellFormatting(object sender, System.Windows.Forms.DataGridViewCellFormattingEventArgs e)
        {

            if (Grid.Columns[e.ColumnIndex].Name.Equals("EventImage") && Grid.Columns.Contains("Type"))
            {

                // --- Ist der Wert wirklich ein String?
                String stringValue = Grid["Type", e.RowIndex].Value as string;
                if (stringValue == null)
                    return;

                // --- Tool-Tip setzen
                DataGridViewCell cell = Grid[e.ColumnIndex, e.RowIndex];
                cell.ToolTipText = stringValue;

                // --- String-Wert mit Image ersetzen
                switch (stringValue)
                {
                    case "Error":
                        e.Value = Properties.Resources.Error;
                        break;
                    case "Warning":
                        e.Value = Properties.Resources.Warning;
                        break;
                    case "Information":
                        e.Value = Properties.Resources.Message;
                        break;
                }
            }

        }

        private void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                e.ToolTipText = String.Format("e.RowIndex = {0}, e.ColumnIndex = {1}, e.ToolTipText = {2}", e.RowIndex, e.ColumnIndex, e.ToolTipText);
                return;
            }

            DataGridViewCell cell = Grid[e.ColumnIndex, e.RowIndex];
            if (cell.OwningColumn.Name == "Message")
            {
                String msg = cell.Value as String;
                if (msg != null)
                {
                    if (msg.Length > 500) msg = msg.Substring(0, 500);
                    e.ToolTipText = msg.Replace(" | ", "\r\n");
                }
            }
        }

        private void MenuItemCopyToClipb_Click(object sender, EventArgs e)
        {
            if (Grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("No rows selected", this.ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.Cursor = Cursors.WaitCursor;
            try
            {
                String text = GetSelectedEventsAsString();
                Int32 cnt = 0;
                while (true)
                {
                    try
                    {
                        Clipboard.Clear();
                        Clipboard.SetText(text, TextDataFormat.UnicodeText);
                        break;
                    }
                    catch (System.Runtime.InteropServices.ExternalException ex)
                    {
                        cnt++;
                        if (cnt == 5)
                        {
                            MessageBox.Show("Error while copying entry to clipboard (another process locks the clipboard):\r\n" + ex.Message, ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }
                        Thread.Sleep(500);
                    }
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void MenuItemExport_Click(object sender, EventArgs e)
        {
            if (Grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("No rows selected", ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            saveFileDialog.FileName = String.Format("{0}-{1}-{2}-{3}.log",
                                                    Environment.MachineName,
                                                    _EventLog.LogDisplayName,
                                                    DateTime.Now.ToString("yyyyMMdd"),
                                                    DateTime.Now.ToString("HHmmss"));

            DialogResult res = saveFileDialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                try
                {
                    String text = GetSelectedEventsAsString();
                    using (System.IO.StreamWriter wr = new System.IO.StreamWriter(saveFileDialog.FileName, false, Encoding.Unicode))
                    {
                        wr.Write(text);
                    }
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void MenuItemOpen_Click(object sender, EventArgs e)
        {
            if (Grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("No rows selected", ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenInTextEditor();
        }

        private void OpenInTextEditor()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                String tempFile = Path.Combine(Path.GetTempPath(), String.Format("{0}-{1}-{2}-{3}.log",
                                                                   Environment.MachineName,
                                                                   _EventLog.LogDisplayName,
                                                                   DateTime.Now.ToString("yyyyMMdd"),
                                                                   DateTime.Now.ToString("HHmmss")));

                File.WriteAllText(tempFile, GetSelectedEventsAsString());
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = Constants.TextEditor;
                startInfo.Arguments = tempFile;
                startInfo.UseShellExecute = true;
                try
                {
                    Process proc = Process.Start(startInfo);
                    if (!proc.WaitForInputIdle(10000))
                        throw new Exception("Timeout");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not start process " + startInfo.FileName + " with argument " + startInfo.Arguments, ParentForm.Text + Environment.NewLine + Environment.NewLine + ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception) { }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void Grid_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (Grid.SelectedRows.Count == 0)
                return;

            OpenInTextEditor();
        }


        private String GetSelectedEventsAsString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Char.Parse("="), 200);
            sb.AppendLine();
            sb.AppendLine("======= Ereignis-Protokoll <" + _EventLog.LogDisplayName + "> von Computer <" + Environment.MachineName + ">");
            sb.Append(Char.Parse("="), 200);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();

            foreach (DataGridViewRow row in Grid.SelectedRows)
            {
                sb.Append(Char.Parse("="), 200);
                sb.AppendLine();
                sb.AppendLine(row.Cells["Date/Time"].Value.ToString() + "   " + row.Cells["Source"].Value.ToString().PadRight(35) + row.Cells["Type"].Value.ToString().PadRight(20));
                sb.AppendLine();
                sb.AppendLine(row.Cells["Message"].Value.ToString().Replace(" | ", "\r\n"));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion 

    }
}
