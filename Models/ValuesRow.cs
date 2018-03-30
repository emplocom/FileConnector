using System.Collections.Generic;

namespace EmploFileImport.Models
{
    public class ValuesRow
    {
        private List<ValueHeaderName> _values { get; set; } = new List<ValueHeaderName>();

        public void Add(ValueHeaderName valueHeaderName)
        {
            _values.Add(valueHeaderName);
        }

        public List<ValueHeaderName> Get()
        {
            return _values;
        }
    }
}
