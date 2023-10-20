using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using DoX.BAI.ImpEx.Shared;
using DoX.FX.API.ClientServer;
using DoX.FX.API.Util;
using DoX.FX.Contracts.Notification.Wcf;

namespace DoX.BAI.ImpEx.Client
{
    public class ClientNotificationService
    {
        private const string NOTIFICATION_UPDATE_CONFIG = "UPDATE_CONFIG";

        private static ICallBackService _CallbackService;
        private static ServiceHost _CallbackServiceHost;
        private static String _Address;
        private static DummyLogTraceProvider _LogTracer;

        private static IClientController _BAIClient;
        public static IClientController BAIClient
        {
            get { return _BAIClient; }
            set 
            { 
                _BAIClient = value;
                _LogTracer = new DummyLogTraceProvider(_BAIClient);
            }
        }

        public static Boolean Start(String address)
        {
            if (_CallbackService != null && _CallbackService.IsConnected && String.Compare(_Address, address, true) == 0)
                // --- Host für diese Addresse ist schon gestartet
                return false;

            Stop();

            _Address = address;

            var started = false;
            if (!String.IsNullOrEmpty(address))
            {
                //_SubscribeTimer = new Timer(SubscribeTimerElapsed, null, 30000, 30000);
                _CallbackService = GetCallBackService();
                _CallbackService.CallbackReceived += CallbackServiceCallbackReceived;
                _CallbackService.Connected += CallbackServiceConnected;
                _CallbackService.Disconnected += CallbackServiceDisconnected;
                started = true;
            }

            BAIClient.WriteLogEntry(MethodBase.GetCurrentMethod(), "Callback Service started in Thread " + Thread.CurrentThread.ManagedThreadId, EventLogEntryType.Information);

            return started;
        }

        public static Boolean Stop()
        {
            if (_CallbackService != null)
            {
                _CallbackService.Close();
                _CallbackService = null;
            }

            if (_CallbackServiceHost != null)
            {
                _CallbackServiceHost.Close();
                _CallbackServiceHost = null;
            }

            if (BAIClient != null)
                BAIClient.UnSubscribe();

            _Address = null;

            return true;
        }

        private static ICallBackService GetCallBackService()
        {
            ICallBackService result;
            CallbackEndpointTools.CallbackType callbacktype = CallbackEndpointTools.GetCallbackType(_Address);

            if (callbacktype == CallbackEndpointTools.CallbackType.WCF)
            {
                result = new WCFCallbackService(_LogTracer);
                System.ServiceModel.Channels.Binding binding = ServiceBindingFactory.GetBinding(_Address);
                _CallbackServiceHost = new ServiceHost(result);
                _CallbackServiceHost.AddServiceEndpoint(typeof(INotificationCallback), binding, _Address);
                _CallbackServiceHost.Open();
            }
            else if (callbacktype == CallbackEndpointTools.CallbackType.UDP)
            {
                string errormsg;
                int port = CallbackEndpointTools.GetUDPPort(_Address, out errormsg);
                if (port == -1)
                    throw new Exception(errormsg);

                result = new UDPCallbackService(port, _LogTracer);
                result.Open();
            }
            else
                throw new Exception("CallbackEndpoint " + _Address + " is invalid");

            return result;
        }

        private static void CallbackServiceDisconnected()
        {
            BAIClient.WriteLogEntry(MethodBase.GetCurrentMethod(), "Probiere Subscribe", EventLogEntryType.Information);
            BAIClient.Subscribe();
        }

        private static void CallbackServiceConnected()
        {
            BAIClient.WriteLogEntry(MethodBase.GetCurrentMethod(), "Verbunden mit Callback-Server", EventLogEntryType.Information);
        }

        private static void CallbackServiceCallbackReceived(NotificationCallbackInfo notificationinfo)
        {
            BAIClient.WriteLogEntry(MethodBase.GetCurrentMethod(), notificationinfo.Context + " Callback received in Thread " + Thread.CurrentThread.ManagedThreadId, EventLogEntryType.Information);

             var ctx = notificationinfo.Context;
            if (ctx != null && ctx.Equals(NOTIFICATION_UPDATE_CONFIG, StringComparison.InvariantCultureIgnoreCase))
                BAIClient.UpdateConfig();
            else
                BAIClient.Import();
        }
    }
}
