using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace DoX.BAI.ImpEx.Client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[] 
			{ 
				new BAIClientSvc() 
			};
            ServiceBase.Run(servicesToRun);
        }
    }
}
