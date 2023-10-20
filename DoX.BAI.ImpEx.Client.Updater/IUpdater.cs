using System;
using System.Collections.Generic;
using System.Text;

namespace DoX.BAI.ImpEx.Client
{
    public interface IUpdater
    {
        Boolean Update(String clientName, String programDir, String updateDir, String callingProc);
    }
}
