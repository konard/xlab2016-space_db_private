using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Data.Repository.Helpers
{
    public static class JsonHelper
    {
        public static JsonDocument Convert(object obj)
        {
            if (obj == null)
                return null;

            var json = JsonConvert.SerializeObject(obj);
            return JsonDocument.Parse(json);
        }

        public static T Convert<T>(JsonDocument source)
        {
            if (source == null)
                return default(T);

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            source.WriteTo(writer);
            writer.Flush();
            var json = Encoding.UTF8.GetString(stream.ToArray());

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static U Extend<T, U>(T source, Action<T, U> copy)
            where U : T, new()
        {
            var result = new U();
            copy(source, result);

            return result;
        }

        public static bool TryDeserialize<T>(string s, out T result)
        {
            result = default(T);

            try
            {
                result = JsonConvert.DeserializeObject<T>(s);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static string Normalize(string source)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            if (source.IndexOf('"') == 0)
            {
                while (source.IndexOf('"') == 0)
                {
                    source = source.Replace("\\\\", "\\");
                    source = source.Substring(1, source.Length - 2);
                    if (source.Length > 1 && source[0] == '\\')
                    {
                        source = source.Substring(1, source.Length - 2);
                    }
                }
                
                source = source.Replace("\\\"", "\"");
                if (source.IndexOf('"') == 0)
                {
                    source = source.Substring(1, source.Length - 2);
                }
            }

            return source;
        }

        public static string NormalizeSafe(string source)
        {
            try
            {
                return Normalize(source);
            }
            catch
            {
                // Safe
            }

            return source;
        }
    }
}
