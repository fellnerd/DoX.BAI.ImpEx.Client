using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DoX.BAI.ImpEx.Shared;

namespace DoX.BAI.ImpEx.Client
{
    public partial class LoginForm : Form
    {
        internal ClientConfig Config { get; set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            textBoxServiceUsername.DataBindings.Add("Text", Config, "Username", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxServicePassword.DataBindings.Add("Text", Config, "Password", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxServiceUrl.DataBindings.Add("Text", Config, "ServiceUrl", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxIntegrationClientUrl.DataBindings.Add("Text", Config, "IntegrationClientUrl", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxIntegrationClientToken.DataBindings.Add("Text", Config, "IntegrationClientToken", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxAddressPort.DataBindings.Add("Text", Config, "ProxyAddress", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxProxyUsername.DataBindings.Add("Text", Config, "ProxyUsername", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxProxyPassword.DataBindings.Add("Text", Config, "ProxyPassword", false, DataSourceUpdateMode.OnPropertyChanged);
            textBoxProxyDomain.DataBindings.Add("Text", Config, "ProxyDomain", false, DataSourceUpdateMode.OnPropertyChanged);
            
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            toolStripButtonCancel.PerformClick();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            toolStripButtonOK.PerformClick();
        }

        private void toolStripButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void toolStripButtonOK_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Config.Username))
            {
                MessageBox.Show(labelServiceUsername.Text + " is empty!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (String.IsNullOrEmpty(Config.Password))
            {
                if (MessageBox.Show(labelServicePassword.Text + " is empty!\r\nContinue anyway?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Error) != System.Windows.Forms.DialogResult.Yes)
                    return;
            }

            if (String.IsNullOrEmpty(Config.ServiceUrl))
            {
                MessageBox.Show(labelServiceUrl.Text + " is empty!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (String.IsNullOrEmpty(Config.IntegrationClientUrl))
            {
                MessageBox.Show("Client URL is empty!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            this.Close();
        }

    }
}
