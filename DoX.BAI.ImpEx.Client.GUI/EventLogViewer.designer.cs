namespace DoX.BAI.ImpEx.Client
{
    partial class EventLogViewer
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        internal System.Windows.Forms.ToolStrip toolStrip;
        internal System.Windows.Forms.ToolStripButton btnErrors;
        internal System.Windows.Forms.ToolStripButton btnWarnings;
        internal System.Windows.Forms.ToolStripButton btnMessages;
        internal System.Windows.Forms.ToolStripSeparator RecCountSeparator;
        internal System.Windows.Forms.ToolStripTextBox FindText;
        internal System.Windows.Forms.ToolStripComboBox SourceCombo;
        internal System.Windows.Forms.ToolStripSeparator ButtonSeparator1;
        internal System.Windows.Forms.ToolStripSeparator ButtonSeparator2;
        internal System.Windows.Forms.ToolStripSeparator FindSeparator;
        internal System.Windows.Forms.ToolStripLabel NotFoundLabel;
        internal System.Windows.Forms.ToolStripLabel SourceLabel;
        internal System.Windows.Forms.ToolStripLabel FindLabel;
        internal System.Windows.Forms.DataGridView Grid;
        internal System.Windows.Forms.DataGridViewImageColumn EventImage; 

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.btnErrors = new System.Windows.Forms.ToolStripButton();
            this.ButtonSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnWarnings = new System.Windows.Forms.ToolStripButton();
            this.ButtonSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnMessages = new System.Windows.Forms.ToolStripButton();
            this.RecCountSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.RecCount = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.SourceSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.SourceLabel = new System.Windows.Forms.ToolStripLabel();
            this.SourceCombo = new System.Windows.Forms.ToolStripComboBox();
            this.FindSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.FindLabel = new System.Windows.Forms.ToolStripLabel();
            this.FindText = new System.Windows.Forms.ToolStripTextBox();
            this.NotFoundLabel = new System.Windows.Forms.ToolStripLabel();
            this.Grid = new System.Windows.Forms.DataGridView();
            this.EventImage = new System.Windows.Forms.DataGridViewImageColumn();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.MenuItemCopyToClipb = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemExport = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnErrors,
            this.ButtonSeparator1,
            this.btnWarnings,
            this.ButtonSeparator2,
            this.btnMessages,
            this.RecCountSeparator,
            this.toolStripLabel1,
            this.RecCount,
            this.toolStripLabel2,
            this.SourceSeparator,
            this.SourceLabel,
            this.SourceCombo,
            this.FindSeparator,
            this.FindLabel,
            this.FindText,
            this.NotFoundLabel});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(5, 1, 1, 1);
            this.toolStrip.Size = new System.Drawing.Size(1024, 25);
            this.toolStrip.TabIndex = 4;
            // 
            // btnErrors
            // 
            this.btnErrors.Checked = true;
            this.btnErrors.CheckOnClick = true;
            this.btnErrors.CheckState = System.Windows.Forms.CheckState.Checked;
            this.btnErrors.Image = global::DoX.BAI.ImpEx.Client.Properties.Resources.Error;
            this.btnErrors.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btnErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnErrors.Name = "btnErrors";
            this.btnErrors.Size = new System.Drawing.Size(63, 20);
            this.btnErrors.Text = "0 Errors";
            this.btnErrors.ToolTipText = "0 Errors";
            this.btnErrors.Click += new System.EventHandler(this.btnErrors_Click);
            // 
            // ButtonSeparator1
            // 
            this.ButtonSeparator1.Margin = new System.Windows.Forms.Padding(-1, 0, 1, 0);
            this.ButtonSeparator1.Name = "ButtonSeparator1";
            this.ButtonSeparator1.Size = new System.Drawing.Size(6, 23);
            // 
            // btnWarnings
            // 
            this.btnWarnings.Checked = true;
            this.btnWarnings.CheckOnClick = true;
            this.btnWarnings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.btnWarnings.Image = global::DoX.BAI.ImpEx.Client.Properties.Resources.Warning;
            this.btnWarnings.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btnWarnings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnWarnings.Name = "btnWarnings";
            this.btnWarnings.Size = new System.Drawing.Size(81, 20);
            this.btnWarnings.Text = "0 Warnings";
            this.btnWarnings.ToolTipText = "0 Warnings";
            this.btnWarnings.Click += new System.EventHandler(this.btnWarnings_Click);
            // 
            // ButtonSeparator2
            // 
            this.ButtonSeparator2.Margin = new System.Windows.Forms.Padding(-1, 0, 1, 0);
            this.ButtonSeparator2.Name = "ButtonSeparator2";
            this.ButtonSeparator2.Size = new System.Drawing.Size(6, 23);
            // 
            // btnMessages
            // 
            this.btnMessages.Checked = true;
            this.btnMessages.CheckOnClick = true;
            this.btnMessages.CheckState = System.Windows.Forms.CheckState.Checked;
            this.btnMessages.Image = global::DoX.BAI.ImpEx.Client.Properties.Resources.Message;
            this.btnMessages.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btnMessages.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnMessages.Name = "btnMessages";
            this.btnMessages.Size = new System.Drawing.Size(82, 20);
            this.btnMessages.Text = "0 Messages";
            this.btnMessages.ToolTipText = "0 Messages";
            this.btnMessages.Click += new System.EventHandler(this.btnMessages_Click);
            // 
            // RecCountSeparator
            // 
            this.RecCountSeparator.Margin = new System.Windows.Forms.Padding(-1, 0, 1, 0);
            this.RecCountSeparator.Name = "RecCountSeparator";
            this.RecCountSeparator.Size = new System.Drawing.Size(6, 23);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(33, 20);
            this.toolStripLabel1.Text = "Show";
            // 
            // RecCount
            // 
            this.RecCount.MaxLength = 7;
            this.RecCount.Name = "RecCount";
            this.RecCount.Size = new System.Drawing.Size(50, 23);
            this.RecCount.Text = "100";
            this.RecCount.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.RecCount.ToolTipText = "Enter the number of log entries to show";
            this.RecCount.TextChanged += new System.EventHandler(this.RecCount_TextChanged);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(40, 20);
            this.toolStripLabel2.Text = "Entries";
            // 
            // SourceSeparator
            // 
            this.SourceSeparator.Name = "SourceSeparator";
            this.SourceSeparator.Size = new System.Drawing.Size(6, 23);
            // 
            // SourceLabel
            // 
            this.SourceLabel.Name = "SourceLabel";
            this.SourceLabel.Size = new System.Drawing.Size(44, 20);
            this.SourceLabel.Text = "Source:";
            // 
            // SourceCombo
            // 
            this.SourceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SourceCombo.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
            this.SourceCombo.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SourceCombo.Items.AddRange(new object[] {
            " "});
            this.SourceCombo.Margin = new System.Windows.Forms.Padding(1, 0, 2, 0);
            this.SourceCombo.Name = "SourceCombo";
            this.SourceCombo.Size = new System.Drawing.Size(116, 23);
            this.SourceCombo.Sorted = true;
            this.SourceCombo.SelectedIndexChanged += new System.EventHandler(this.cmbSourceCombo_SelectedIndexChanged);
            // 
            // FindSeparator
            // 
            this.FindSeparator.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.FindSeparator.Name = "FindSeparator";
            this.FindSeparator.Size = new System.Drawing.Size(6, 23);
            // 
            // FindLabel
            // 
            this.FindLabel.Name = "FindLabel";
            this.FindLabel.Size = new System.Drawing.Size(31, 20);
            this.FindLabel.Text = "Find:";
            // 
            // FindText
            // 
            this.FindText.ForeColor = System.Drawing.SystemColors.WindowText;
            this.FindText.Name = "FindText";
            this.FindText.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.FindText.Size = new System.Drawing.Size(92, 23);
            this.FindText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FindText_KeyPress);
            this.FindText.TextChanged += new System.EventHandler(this.FindText_TextChanged);
            // 
            // NotFoundLabel
            // 
            this.NotFoundLabel.Image = global::DoX.BAI.ImpEx.Client.Properties.Resources.NotFound;
            this.NotFoundLabel.Margin = new System.Windows.Forms.Padding(5, 1, 0, 2);
            this.NotFoundLabel.Name = "NotFoundLabel";
            this.NotFoundLabel.Size = new System.Drawing.Size(103, 20);
            this.NotFoundLabel.Text = "No events found";
            this.NotFoundLabel.ToolTipText = "There are no events that match the defined filter.";
            this.NotFoundLabel.Visible = false;
            // 
            // Grid
            // 
            this.Grid.AllowUserToAddRows = false;
            this.Grid.AllowUserToDeleteRows = false;
            this.Grid.AllowUserToResizeColumns = false;
            this.Grid.AllowUserToResizeRows = false;
            this.Grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.Grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.Grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.EventImage});
            this.Grid.ContextMenuStrip = this.contextMenuStrip;
            this.Grid.Location = new System.Drawing.Point(0, 32);
            this.Grid.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Grid.Name = "Grid";
            this.Grid.ReadOnly = true;
            this.Grid.RowHeadersVisible = false;
            this.Grid.Size = new System.Drawing.Size(1024, 394);
            this.Grid.TabIndex = 5;
            this.Grid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.Grid_CellFormatting);
            this.Grid.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.Grid_CellToolTipTextNeeded);
            this.Grid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.Grid_ColumnWidthChanged);
            this.Grid.CurrentCellChanged += new System.EventHandler(this.Grid_CurrentCellChanged);
            this.Grid.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.Grid_RowPostPaint);
            this.Grid.RowPrePaint += new System.Windows.Forms.DataGridViewRowPrePaintEventHandler(this.Grid_RowPrePaint);
            this.Grid.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.Grid_MouseDoubleClick);
            // 
            // EventImage
            // 
            this.EventImage.HeaderText = "";
            this.EventImage.Name = "EventImage";
            this.EventImage.ReadOnly = true;
            this.EventImage.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.EventImage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemCopyToClipb,
            this.MenuItemExport,
            this.MenuItemOpen});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(206, 70);
            // 
            // MenuItemCopyToClipb
            // 
            this.MenuItemCopyToClipb.Name = "MenuItemCopyToClipb";
            this.MenuItemCopyToClipb.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.MenuItemCopyToClipb.Size = new System.Drawing.Size(205, 22);
            this.MenuItemCopyToClipb.Text = "&Copy to clipboard";
            this.MenuItemCopyToClipb.Click += new System.EventHandler(this.MenuItemCopyToClipb_Click);
            // 
            // MenuItemExport
            // 
            this.MenuItemExport.Name = "MenuItemExport";
            this.MenuItemExport.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.MenuItemExport.Size = new System.Drawing.Size(205, 22);
            this.MenuItemExport.Text = "&Save to file";
            this.MenuItemExport.Click += new System.EventHandler(this.MenuItemExport_Click);
            // 
            // MenuItemOpen
            // 
            this.MenuItemOpen.Name = "MenuItemOpen";
            this.MenuItemOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.MenuItemOpen.Size = new System.Drawing.Size(205, 22);
            this.MenuItemOpen.Text = "&Open in text editor";
            this.MenuItemOpen.Click += new System.EventHandler(this.MenuItemOpen_Click);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "txt";
            this.saveFileDialog.RestoreDirectory = true;
            // 
            // EventLogViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Grid);
            this.Controls.Add(this.toolStrip);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "EventLogViewer";
            this.Size = new System.Drawing.Size(1024, 426);
            this.Load += new System.EventHandler(this.EventLogViewer_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem MenuItemCopyToClipb;
        private System.Windows.Forms.ToolStripMenuItem MenuItemExport;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ToolStripMenuItem MenuItemOpen;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox RecCount;
        private System.Windows.Forms.ToolStripSeparator SourceSeparator;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
    }
}
