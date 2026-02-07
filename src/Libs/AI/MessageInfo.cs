using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class MessageInfo
    {
        public string Text { get; set; }
        public long ChatId { get; set; }
        public string Username { get; set; }
        public bool IsCallback { get; set; }

        public ContactInfo? Contact { get; set; }
        public FileInfo? File { get; set; }
    }
}
