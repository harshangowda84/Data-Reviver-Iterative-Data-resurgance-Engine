using System.Xml.Serialization;

namespace KFA.Disks {
    public class UnallocatedDiskAreaAttributes : Attributes, IDescribable {

        public UnallocatedDiskAreaAttributes() {}

        [XmlIgnore]
        public override string TextDescription {
            get {
                return "";
            }
        }
    }
}
