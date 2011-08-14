using System.Xml.Serialization;

namespace KFA.Disks {
    public class MasterBootRecordAttributes : Attributes, IDescribable {
        public MasterBootRecordAttributes() { }

        public MasterBootRecordAttributes(string description) {
            m_Description = description;
        }

        private string m_Description;
        [XmlIgnore]
        public override string TextDescription {
            get { return m_Description; }
        }
    }
}
