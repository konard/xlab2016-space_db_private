using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Graphics
{
    public class PeriodGroup
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Week { get; set; }
        public int? Day { get; set; }

        public DateTime ToDateTime()
        {
            if (Year == null)
                return DateTime.MinValue;

            var result = new DateTime(Year.Value, Month ?? 1, Day ?? 1);

            if (Week.HasValue)
                result = result.AddDays((Week.Value - 1) * 7 + 1 - 1);

            return result;
        }
    }
}
