using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using DoX.BAI.ImpEx.Shared;
using System.Reflection.Emit;

namespace DoX.BAI.ImpEx.Client.ConsoleHost
{
    class Program
    {
        private static IClientController _Client;

        static void Main(string[] args)
        {

            // --- Assembly laden, die IClientController implementiert
            Type clientType = TypeBinder.GetTypeByInterface(typeof(IClientController));
            if (clientType != null)
            {
                PropertyInfo pi = clientType.GetProperty("Instance", clientType);
                _Client = (IClientController)pi.GetValue(null, null);
            }
            else
                throw new Exception("Im Verzeichnis " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + " wurde keine Assembly gefunden, die " + typeof(IClientController).Name + " implementiert");

            Console.WriteLine("<Enter> beendet den Client");
            Console.WriteLine();

            Console.Title = _Client.ClientName;
            _Client.ClientStatusChanged += status =>
            {
                if ((status & ClientStatus.Running) == ClientStatus.Running)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(DateTime.Now.ToString() + "   BAI-Client " + _Client.ClientName + " gestartet");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(DateTime.Now.ToString() + "   BAI-Client " + _Client.ClientName + " gestoppt");
                }
            };

            _Client.Start();
            Console.ReadLine();

        }
    }
}
