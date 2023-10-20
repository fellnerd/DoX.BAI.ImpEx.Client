using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using DoX.BAI.ImpEx.Shared;
using DoX.FX.API.ClientServer;
using DoX.FX.Util;

namespace DoX.BAI.ImpEx.Client
{
    internal class DummyLogTraceProvider : ILogTraceProvider
    {
        private static IClientController _BAIClient;

        public DummyLogTraceProvider(IClientController baiClient)
        {
            _BAIClient = baiClient;
        }

        public void Audit(string argSubjectText, string argDisplayText, string[] argTextParams, string argDetailInfo, AuditTypes argAuditType)
        {
            
        }

        public void Audit(string argSubjectText, string argDisplayText, string argTextParam0, string argDetailInfo, AuditTypes argAuditType)
        {
            
        }

        public void Audit(string argSubjectText, string argDisplayText, string argDetailInfo, AuditTypes argAuditType)
        {
            
        }

        public void Audit(string argSubjectText, string argDisplayText, AuditTypes argAuditType)
        {
            
        }

        public List<TraceInfo> GetTraceBuffer()
        {
            return new List<TraceInfo>();
        }

        public void Log(Exception argEx, bool argSetAlert)
        {
            //_BAIClient.WriteLogEntry(MethodBase.GetCurrentMethod(), argEx.ToString(), EventLogEntryType.Information);
        }

        public void Log(int argErrorId, string argText, LogTypes argType)
        {
            //_BAIClient.WriteLogEntry(MethodBase.GetCurrentMethod(), argText, EventLogEntryType.Information);
        }

        public void Log(string argText, LogTypes argType)
        {
            //EventLogEntryType type = EventLogEntryType.Information;
            //if (argType == Enums.LogTypes.Error)
            //    type = EventLogEntryType.Error;
            //else if (argType == Enums.LogTypes.Warning)
            //    type = EventLogEntryType.Warning;
            
            //_BAIClient.WriteLogEntry(MethodBase.GetCurrentMethod(), argText, type);
        }

        public void Log(string argText)
        {
            _BAIClient.WriteLogEntry(MethodBase.GetCurrentMethod(), argText, EventLogEntryType.Information);
        }

        public void ResetAlerts(Type argAlertType)
        {
            
        }

        public void ResetAlerts(AlertCategories argCategory)
        {
            
        }

        public void ResetAlerts()
        {
            
        }

        public void SetAlert<T>(string argSubjectText, string argDisplayText, string[] argTextParams, string argDetailInfo) where T : CAlertBase
        {
            
        }

        public void SetAlert<T>(string argSubjectText, string argDisplayText, string argtextParam0, string argDetailInfo) where T : CAlertBase
        {
            
        }

        public void SetAlert<T>(string argSubjectText, string argDisplayText, string argDetailInfo) where T : CAlertBase
        {
            
        }

        public void SetAlert<T>(string argSubjectText, string argDisplayText) where T : CAlertBase
        {
            
        }

        public ServiceProviderStatus GetStatus()
        {
            return new ServiceProviderStatus();
        }


        public void Log(string argSubjectText, string argDisplayText, string[] argTextParams, string argDetailInfo, AuditTypes argAuditType)
        {
        }

        public void Log(string argSubjectText, string argDisplayText, string argTextParam0, string argDetailInfo, AuditTypes argAuditType)
        {
        }

        public void Log(string argSubjectText, string argDisplayText, string argDetailInfo, AuditTypes argAuditType)
        {
        }

        public void Log(string argSubjectText, string argDisplayText, AuditTypes argAuditType)
        {
        }

        public string GetLogDirectory()
        {
            throw new NotImplementedException();
        }
    }
}
