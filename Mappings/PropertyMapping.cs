namespace EmploFileImport.Mappings
{
    public class PropertyMapping
    {
        public PropertyMapping(string emploPropertyName, string headerName)
        {
            EmploPropertyName =  emploPropertyName;
            FileHeaderName = headerName;
        }
        
        public string EmploPropertyName { get; private set; }
        public string FileHeaderName { get; private set; }
    }
}
