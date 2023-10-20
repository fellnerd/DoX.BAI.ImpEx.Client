using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace DoX.BAI.ImpEx.Client
{
    [Serializable]
    public class LogEntry
    {
        public LogEntry()
        {
            
        }

        public LogEntry(string message, EventLogEntryType eventLogEntryType, string sendingUserName)
        {
            Date = DateTime.Now.ToString("dd.MM.yyyy");
            Time = DateTime.Now.ToString("HH:mm:ss");
            Message = message;
            EventLogEntryType = eventLogEntryType;
            SendingUserName = sendingUserName;
        }

        [XmlAttribute]
        public string Date { get; set; }
        [XmlAttribute]
        public string Time { get; set; }
        [XmlAttribute]
        public EventLogEntryType EventLogEntryType { get; set; }
        [XmlAttribute]
        public string Message { get; set; }
        [XmlAttribute]
        public string SendingUserName { get; set; }
    }
}