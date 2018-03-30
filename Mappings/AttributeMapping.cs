using System.Configuration;

namespace EmploFileImport.Mappings
{
    public class AttributeMapping : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new AttributeMappingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AttributeMappingElement)element).Name;
        }
    }
}
