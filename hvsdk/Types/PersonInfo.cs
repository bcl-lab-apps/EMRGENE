// (c) Microsoft. All rights reserved
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using HealthVault.Foundation;

namespace HealthVault.Foundation.Types
{
    public class PersonInfo : IValidatable
    {
        [XmlElement("person-id", Order = 1)]
        public string PersonId
        {
            get; set;
        }

        [XmlElement("name", Order = 2)]
        public string Name
        {
            get; set;
        }
        
        [XmlElement("app-settings", Order = 3)]
        public XElement AppSettings
        {
            get; set;
        }

        [XmlElement("selected-record-id", Order = 4)]
        public string SelectedRecordID
        {
            get; set;
        }

        [XmlElement("more-records", Order = 5)]
        public bool HasMoreRecords
        {
            get; set;
        }

        [XmlElement("record", Order = 6)]
        public Record[] Records
        {
            get; set;
        }

        [XmlElement("groups", Order = 7)]
        public XElement Groups
        {
            get; set;
        }

        [XmlElement("preferred-culture", Order = 8)]
        public XElement PreferredCulture
        {
            get; set;
        }

        [XmlElement("preferred-uiculture", Order = 9)]
        public XElement PreferredUICulture
        {
            get; set;
        }

        [XmlElement("location", Order = 10)]
        public Location Location
        {
            get; set;
        }

        [XmlIgnore]
        public bool HasRecords
        {
            get { return !this.Records.IsNullOrEmpty();}
        }

        public string Serialize()
        {
            return this.ToXml();
        }

        public void Validate()
        {       
            this.PersonId.ValidateRequired("PersonId");     
            if (this.Records.IsNullOrEmpty())
            {
                throw new ArgumentException("Records");
            }
            foreach(Record record in this.Records)
            {
                record.ValidateRequired("Records");
            }

            Location.ValidateOptional();
        }
    }
}
