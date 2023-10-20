using System;
using System.Collections.Generic;
using System.Text;

namespace DoX.BAI.ImpEx.Shared
{
    public interface IClientControllerCallback
    {
        void ClientStatusChanged(ClientStatus status);
    }

}
