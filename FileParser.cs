using EmploFileImport.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmploFileImport
{
    public class FileParser
    {
        private readonly string _filePath;
        private readonly char _separator;
        private readonly int _headerLineNumber;

        private List<string> _headers = new List<string> { };
        private List<ValuesRow> _rows = new List<ValuesRow>();

        public FileParser(string filePath, char separator, int headerLineNumber)
        {
            _filePath = filePath;
            _separator = separator;
            _headerLineNumber = headerLineNumber;
        }

        public List<ValuesRow> ReadValues()
        {
            int lineNumber = 0;
            string line;

            if(!_rows.Any())
            {
                using(var reader = new StreamReader(_filePath))
                {
                    while(!reader.EndOfStream)
                    {
                        line = reader.ReadLine();
                        lineNumber++;

                        string[] data = GetDataFromRowText(line, lineNumber);
                        if(data == null)
                            continue;

                        ParseRowData(data, lineNumber);
                    }
                }
            }

            return _rows;
        }

        private string[] GetDataFromRowText(string rowText, int lineNumber)
        {
            string[] data = rowText.Split(_separator);

            if(lineNumber != _headerLineNumber && data.Length != _headers.Count)
                return null;

            return data;
        }

        private void ParseRowData(string[] data, int lineNumber)
        {
            if(lineNumber == _headerLineNumber)
            {
                _headers.AddRange(data);
            }
            else if(data.Length == _headers.Count)
            {
                var valuesRow = new ValuesRow();
                for(int i = 0; i < data.Length; i++)
                {
                    var valueHeaderName = new ValueHeaderName()
                    {
                        HeaderName = _headers[i],
                        Value = data[i]
                    };
                    if(valueHeaderName.HeaderName != string.Empty)
                        valuesRow.Add(valueHeaderName);
                }
                _rows.Add(valuesRow);
            }
        }
    }
}
