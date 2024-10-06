namespace ProcessMonitoringService
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class ProcessToCheck
    {

        private string[] processNameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ProcessName")]
        public string[] ProcessName
        {
            get
            {
                return this.processNameField;
            }
            set
            {
                this.processNameField = value;
            }
        }
    }


}
