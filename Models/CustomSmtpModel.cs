using System.Xml.Serialization;

namespace Oshyn.Modules.Forms.EmailSubmitAction.Models
{
    [XmlRoot("CustomSmtpModel")]
    public class CustomSmtpModel
    {
        [XmlElement]
        public string Host { get; set; }

        [XmlElement(typeof(int))]
        public int Port { get; set; } = 0;

        [XmlElement]
        public string Login { get; set; }

        [XmlElement]
        public string Password { get; set; }

        [XmlElement(typeof(bool?))]
        public bool? EnableSsl { get; set; }
    }
}