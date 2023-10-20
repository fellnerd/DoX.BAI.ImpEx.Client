using System;
using System.Collections.Generic;
using System.Text;
using DoX.BAI.ImpEx.Shared;
using System.Windows.Forms;

namespace DoX.BAI.ImpEx.Client
{
    public class BAIClientCallback : MarshalByRefObject, IClientControllerCallback
    {

        public BAIClientCallback()
        {

        }
        
        #region IClientControllerCallback Members

        public void ClientStatusChanged(ClientStatus status)
        {
            foreach (Form item in Application.OpenForms)
            {
                // --- Haupt-Formular suchen
                MainForm mainForm = item as MainForm;
                if (mainForm != null)
                {
                    // --- Statuswechsel im GUI-Thread aufrufen
                    mainForm.Invoke(new ClientStatusChangedDelegate(mainForm.SetStatus), new Object[] { status });
                }
            }
        }

        #endregion
    }
}
