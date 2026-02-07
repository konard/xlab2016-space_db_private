using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository.Models.Rest

{
    public abstract class RestMethodResponseBase : IRestMethodResponse
    {
        public int Affected { get; set; }
    }
}
