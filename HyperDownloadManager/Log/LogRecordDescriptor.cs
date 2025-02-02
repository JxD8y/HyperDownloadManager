using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using System.Xml.Serialization;

namespace HyperDownloadManager.Log
{
    [XmlType("Log")]
    public class LogViewModel
    {
        [XmlAttribute("Loglv")]
        public MessageLevel logLevel { get; set; }
        [XmlAttribute("Loglc")]
        public LogSection logsection { get; set; }
        [XmlText]
        public string? Log { get; set; }
        [XmlAttribute("Date")]
        public DateTime AccureTime { get; set; }
    }
}
