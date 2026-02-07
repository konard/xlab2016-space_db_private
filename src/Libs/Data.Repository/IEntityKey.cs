using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository
{
    public interface IEntityKey<TKey>
    {
        TKey Id { get; set; }
    }
}
