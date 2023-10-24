namespace DoX.BAI.ImpEx.Client
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBoxServicePassword = new System.Windows.Forms.TextBox();
            this.textBoxServiceUsername = new System.Windows.Forms.TextBox();
            this.labelServiceUsername = new System.Windows.Forms.Label();
            this.labelServicePassword = new System.Windows.Forms.Label();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonCancel = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonOK = new System.Windows.Forms.ToolStripButton();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.textBoxServiceUrl = new System.Windows.Forms.TextBox();
            this.labelServiceUrl = new System.Windows.Forms.Label();
            this.groupBoxProxy = new System.Windows.Forms.GroupBox();
            this.textBoxProxyDomain = new System.Windows.Forms.TextBox();
            this.labelProxyDomain = new System.Windows.Forms.Label();
            this.textBoxProxyUsername = new System.Windows.Forms.TextBox();
            this.labelProxyPassword = new System.Windows.Forms.Label();
            this.textBoxAddressPort = new System.Windows.Forms.TextBox();
            this.labelProxyUsername = new System.Windows.Forms.Label();
            this.labelAddressPort = new System.Windows.Forms.Label();
            this.textBoxProxyPassword = new System.Windows.Forms.TextBox();
            this.groupBoxWebservice = new System.Windows.Forms.GroupBox();
            this.textBoxIntegrationClientUrl = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxIntegrationClientToken = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.toolStrip1.SuspendLayout();
            this.groupBoxProxy.SuspendLayout();
            this.groupBoxWebservice.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBoxServicePassword
            // 
            this.textBoxServicePassword.Location = new System.Drawing.Point(125, 55);
            this.textBoxServicePassword.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxServicePassword.Name = "textBoxServicePassword";
            this.textBoxServicePassword.Size = new System.Drawing.Size(231, 23);
            this.textBoxServicePassword.TabIndex = 25;
            this.textBoxServicePassword.UseSystemPasswordChar = true;
            // 
            // textBoxServiceUsername
            // 
            this.textBoxServiceUsername.Location = new System.Drawing.Point(125, 23);
            this.textBoxServiceUsername.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxServiceUsername.Name = "textBoxServiceUsername";
            this.textBoxServiceUsername.Size = new System.Drawing.Size(231, 23);
            this.textBoxServiceUsername.TabIndex = 24;
            // 
            // labelServiceUsername
            // 
            this.labelServiceUsername.AutoSize = true;
            this.labelServiceUsername.Location = new System.Drawing.Point(19, 26);
            this.labelServiceUsername.Name = "labelServiceUsername";
            this.labelServiceUsername.Size = new System.Drawing.Size(65, 16);
            this.labelServiceUsername.TabIndex = 26;
            this.labelServiceUsername.Text = "Username";
            // 
            // labelServicePassword
            // 
            this.labelServicePassword.AutoSize = true;
            this.labelServicePassword.Location = new System.Drawing.Point(19, 58);
            this.labelServicePassword.Name = "labelServicePassword";
            this.labelServicePassword.Size = new System.Drawing.Size(62, 16);
            this.labelServicePassword.TabIndex = 27;
            this.labelServicePassword.Text = "Password";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonCancel,
            this.toolStripButtonOK});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(368, 25);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonCancel
            // 
            this.toolStripButtonCancel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCancel.Image = global::DoX.BAI.ImpEx.Client.Properties.Resources.cancel;
            this.toolStripButtonCancel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCancel.Name = "toolStripButtonCancel";
            this.toolStripButtonCancel.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonCancel.ToolTipText = "Cancel";
            this.toolStripButtonCancel.Click += new System.EventHandler(this.toolStripButtonCancel_Click);
            // 
            // toolStripButtonOK
            // 
            this.toolStripButtonOK.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonOK.Image = global::DoX.BAI.ImpEx.Client.Properties.Resources.accept;
            this.toolStripButtonOK.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonOK.Name = "toolStripButtonOK";
            this.toolStripButtonOK.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonOK.ToolTipText = "OK";
            this.toolStripButtonOK.Click += new System.EventHandler(this.toolStripButtonOK_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.BackColor = System.Drawing.Color.White;
            this.buttonOK.Location = new System.Drawing.Point(552, 265);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(22, 13);
            this.buttonOK.TabIndex = 31;
            this.buttonOK.UseVisualStyleBackColor = false;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.BackColor = System.Drawing.Color.White;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(580, 264);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(22, 13);
            this.buttonCancel.TabIndex = 32;
            this.buttonCancel.UseVisualStyleBackColor = false;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // textBoxServiceUrl
            // 
            this.textBoxServiceUrl.Location = new System.Drawing.Point(125, 86);
            this.textBoxServiceUrl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxServiceUrl.Name = "textBoxServiceUrl";
            this.textBoxServiceUrl.Size = new System.Drawing.Size(231, 23);
            this.textBoxServiceUrl.TabIndex = 33;
            // 
            // labelServiceUrl
            // 
            this.labelServiceUrl.AutoSize = true;
            this.labelServiceUrl.Location = new System.Drawing.Point(19, 89);
            this.labelServiceUrl.Name = "labelServiceUrl";
            this.labelServiceUrl.Size = new System.Drawing.Size(29, 16);
            this.labelServiceUrl.TabIndex = 34;
            this.labelServiceUrl.Text = "URL";
            // 
            // groupBoxProxy
            // 
            this.groupBoxProxy.Controls.Add(this.textBoxProxyDomain);
            this.groupBoxProxy.Controls.Add(this.labelProxyDomain);
            this.groupBoxProxy.Controls.Add(this.textBoxProxyUsername);
            this.groupBoxProxy.Controls.Add(this.labelProxyPassword);
            this.groupBoxProxy.Controls.Add(this.textBoxAddressPort);
            this.groupBoxProxy.Controls.Add(this.labelProxyUsername);
            this.groupBoxProxy.Controls.Add(this.labelAddressPort);
            this.groupBoxProxy.Controls.Add(this.textBoxProxyPassword);
            this.groupBoxProxy.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBoxProxy.Location = new System.Drawing.Point(0, 210);
            this.groupBoxProxy.Name = "groupBoxProxy";
            this.groupBoxProxy.Size = new System.Drawing.Size(368, 155);
            this.groupBoxProxy.TabIndex = 2;
            this.groupBoxProxy.TabStop = false;
            this.groupBoxProxy.Text = "Proxy";
            // 
            // textBoxProxyDomain
            // 
            this.textBoxProxyDomain.Location = new System.Drawing.Point(125, 117);
            this.textBoxProxyDomain.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxProxyDomain.Name = "textBoxProxyDomain";
            this.textBoxProxyDomain.Size = new System.Drawing.Size(231, 23);
            this.textBoxProxyDomain.TabIndex = 42;
            // 
            // labelProxyDomain
            // 
            this.labelProxyDomain.AutoSize = true;
            this.labelProxyDomain.Location = new System.Drawing.Point(16, 120);
            this.labelProxyDomain.Name = "labelProxyDomain";
            this.labelProxyDomain.Size = new System.Drawing.Size(50, 16);
            this.labelProxyDomain.TabIndex = 42;
            this.labelProxyDomain.Text = "Domain";
            // 
            // textBoxProxyUsername
            // 
            this.textBoxProxyUsername.Location = new System.Drawing.Point(125, 54);
            this.textBoxProxyUsername.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxProxyUsername.Name = "textBoxProxyUsername";
            this.textBoxProxyUsername.Size = new System.Drawing.Size(231, 23);
            this.textBoxProxyUsername.TabIndex = 40;
            // 
            // labelProxyPassword
            // 
            this.labelProxyPassword.AutoSize = true;
            this.labelProxyPassword.Location = new System.Drawing.Point(16, 89);
            this.labelProxyPassword.Name = "labelProxyPassword";
            this.labelProxyPassword.Size = new System.Drawing.Size(62, 16);
            this.labelProxyPassword.TabIndex = 38;
            this.labelProxyPassword.Text = "Password";
            // 
            // textBoxAddressPort
            // 
            this.textBoxAddressPort.Location = new System.Drawing.Point(125, 23);
            this.textBoxAddressPort.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxAddressPort.Name = "textBoxAddressPort";
            this.textBoxAddressPort.Size = new System.Drawing.Size(231, 23);
            this.textBoxAddressPort.TabIndex = 39;
            // 
            // labelProxyUsername
            // 
            this.labelProxyUsername.AutoSize = true;
            this.labelProxyUsername.Location = new System.Drawing.Point(16, 57);
            this.labelProxyUsername.Name = "labelProxyUsername";
            this.labelProxyUsername.Size = new System.Drawing.Size(65, 16);
            this.labelProxyUsername.TabIndex = 37;
            this.labelProxyUsername.Text = "Username";
            // 
            // labelAddressPort
            // 
            this.labelAddressPort.AutoSize = true;
            this.labelAddressPort.Location = new System.Drawing.Point(16, 26);
            this.labelAddressPort.Name = "labelAddressPort";
            this.labelAddressPort.Size = new System.Drawing.Size(81, 16);
            this.labelAddressPort.TabIndex = 40;
            this.labelAddressPort.Text = "Address:Port";
            // 
            // textBoxProxyPassword
            // 
            this.textBoxProxyPassword.Location = new System.Drawing.Point(125, 86);
            this.textBoxProxyPassword.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxProxyPassword.Name = "textBoxProxyPassword";
            this.textBoxProxyPassword.Size = new System.Drawing.Size(231, 23);
            this.textBoxProxyPassword.TabIndex = 41;
            this.textBoxProxyPassword.UseSystemPasswordChar = true;
            // 
            // groupBoxWebservice
            // 
            this.groupBoxWebservice.Controls.Add(this.label2);
            this.groupBoxWebservice.Controls.Add(this.textBoxIntegrationClientToken);
            this.groupBoxWebservice.Controls.Add(this.textBoxIntegrationClientUrl);
            this.groupBoxWebservice.Controls.Add(this.label1);
            this.groupBoxWebservice.Controls.Add(this.textBoxServiceUsername);
            this.groupBoxWebservice.Controls.Add(this.labelServicePassword);
            this.groupBoxWebservice.Controls.Add(this.textBoxServiceUrl);
            this.groupBoxWebservice.Controls.Add(this.labelServiceUsername);
            this.groupBoxWebservice.Controls.Add(this.labelServiceUrl);
            this.groupBoxWebservice.Controls.Add(this.textBoxServicePassword);
            this.groupBoxWebservice.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxWebservice.Location = new System.Drawing.Point(0, 25);
            this.groupBoxWebservice.Name = "groupBoxWebservice";
            this.groupBoxWebservice.Size = new System.Drawing.Size(368, 179);
            this.groupBoxWebservice.TabIndex = 1;
            this.groupBoxWebservice.TabStop = false;
            this.groupBoxWebservice.Text = "Webservice";
            // 
            // textBoxIntegrationClientUrl
            // 
            this.textBoxIntegrationClientUrl.Location = new System.Drawing.Point(125, 117);
            this.textBoxIntegrationClientUrl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxIntegrationClientUrl.Name = "textBoxIntegrationClientUrl";
            this.textBoxIntegrationClientUrl.Size = new System.Drawing.Size(231, 23);
            this.textBoxIntegrationClientUrl.TabIndex = 35;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 120);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 16);
            this.label1.TabIndex = 36;
            this.label1.Text = "DB Client URL";
            // 
            // textBoxIntegrationClientToken
            // 
            this.textBoxIntegrationClientToken.Location = new System.Drawing.Point(125, 148);
            this.textBoxIntegrationClientToken.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxIntegrationClientToken.Name = "textBoxIntegrationClientToken";
            this.textBoxIntegrationClientToken.Size = new System.Drawing.Size(231, 23);
            this.textBoxIntegrationClientToken.TabIndex = 37;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 151);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 16);
            this.label2.TabIndex = 38;
            this.label2.Text = "Token";
            // 
            // LoginForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(368, 365);
            this.Controls.Add(this.groupBoxWebservice);
            this.Controls.Add(this.groupBoxProxy);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.toolStrip1);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "LoginForm";
            this.Text = "Login";
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.groupBoxProxy.ResumeLayout(false);
            this.groupBoxProxy.PerformLayout();
            this.groupBoxWebservice.ResumeLayout(false);
            this.groupBoxWebservice.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelServiceUsername;
        private System.Windows.Forms.Label labelServicePassword;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonOK;
        private System.Windows.Forms.ToolStripButton toolStripButtonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label labelServiceUrl;
        private System.Windows.Forms.GroupBox groupBoxProxy;
        private System.Windows.Forms.GroupBox groupBoxWebservice;
        private System.Windows.Forms.Label labelProxyPassword;
        private System.Windows.Forms.Label labelProxyUsername;
        private System.Windows.Forms.Label labelAddressPort;
        private System.Windows.Forms.Label labelProxyDomain;
        private System.Windows.Forms.TextBox textBoxServicePassword;
        private System.Windows.Forms.TextBox textBoxServiceUsername;
        private System.Windows.Forms.TextBox textBoxServiceUrl;
        private System.Windows.Forms.TextBox textBoxProxyUsername;
        private System.Windows.Forms.TextBox textBoxAddressPort;
        private System.Windows.Forms.TextBox textBoxProxyPassword;
        private System.Windows.Forms.TextBox textBoxProxyDomain;
        private System.Windows.Forms.TextBox textBoxIntegrationClientUrl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxIntegrationClientToken;
    }
}