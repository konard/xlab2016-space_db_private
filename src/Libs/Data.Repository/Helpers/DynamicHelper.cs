using System;
using System.Dynamic;

namespace Data.Repository.Helpers
{
    public static class DynamicHelper
    {
        public static dynamic CreateDynamic(Action<ExpandoObject> builder)
        {
            var result = new ExpandoObject();
            builder?.Invoke(result);
            return result;
        }
    }
}
