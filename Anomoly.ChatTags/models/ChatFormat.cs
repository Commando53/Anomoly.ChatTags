using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Anomoly.ChatTags.Models
{
    public class ChatFormat
    {
        [XmlAttribute("permission")]
        public string Permission { get; set; }
        public string Format { get; set; }

        public bool UseRichText { get; set; }
    }
}
