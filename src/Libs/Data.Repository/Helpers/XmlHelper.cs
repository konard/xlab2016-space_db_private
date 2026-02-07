using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace Data.Repository.Helpers
{
    public static class XmlHelper
    {
        public static string SerializeAsXml(this object source, Type[] extraTypes = null)
        {
            var serializer = new XmlSerializer(source.GetType(), extraTypes);
            using var ms = new MemoryStream();
            using var sw = XmlWriter.Create(ms, new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });
            serializer.Serialize(sw, source);
            var result = Encoding.UTF8.GetString(ms.ToArray());

            if (result.Length > 0 && (int)result[0] == 65279)
                result = result.Substring(1);

            return result;
        }

        public static T DeserializeXml<T>(this string source, Type[] extraTypes = null)
        {
            var serializer = new XmlSerializer(typeof(T), extraTypes);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(source));
            using var sr = XmlReader.Create(ms);
            return (T)serializer.Deserialize(sr);
        }
    }
}
