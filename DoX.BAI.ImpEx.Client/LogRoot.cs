using System.Diagnostics;
using System.Xml.Serialization;

namespace DoX.BAI.ImpEx.Client
{
    [XmlRoot("Log")]
    public class LogRoot
    {
        public LogRoot()
        {
            Entries = new LogEntries();
        }

        [XmlElement("Entry")]
        public LogEntries Entries { get; set; }

        public void Append(string message, EventLogEntryType eventLogEntryType, string sendingUserName)
        {
            Entries.Insert(0, new LogEntry(message, eventLogEntryType, sendingUserName));
        }

        public void Shrink(int maxLogEntries)
        {
          
            int count = Entries.Count;
            while (count > maxLogEntries)
            {
                Entries.RemoveAt(count - 1);
                count = Entries.Count;
            }
        }
    }
}
