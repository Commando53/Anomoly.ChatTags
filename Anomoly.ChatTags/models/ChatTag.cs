using System;
using System.Xml.Serialization;

namespace Anomoly.ChatTags.Models
{
    [Serializable]
    public class ChatTag
    {
        [XmlAttribute("permission")]
        public string Permission { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
    }
}
