using AI.Recognize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class RecognizeWordResponse
    {
        public List<StringTable>? Tables {  get; set; }
        public List<Picture>? Pictures {  get; set; }
        public List<string>? Text {  get; set; }
    }
}
