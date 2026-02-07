using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Recognize
{
    public class StringTable
    {
        [JsonProperty("rows")]
        public List<TableRow> Rows { get; set; }

        public class TableRow : List<string>
        {

        }
    }
}
