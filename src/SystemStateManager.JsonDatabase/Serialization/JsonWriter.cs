using System;
using System.Collections.Generic;
using System.IO;

namespace DevOptimal.SystemStateManager.JsonDatabase.Serialization
{
    /// <summary>
    /// Goal of this serializer is efficient resource usage by streaming arrays instead of loading all of them into memory at once.
    /// To accomplish this, we parse and yield return objects as we read them from the JSON array via the file stream.
    /// Reference: https://www.json.org/json-en.html
    /// </summary>
    internal class JsonWriter : StreamWriter
    {
        public JsonWriter(string path)
            : base(path) { }

        public JsonWriter(Stream stream)
            : base(stream) { }

        public void WriteString(string value)
        {
            var escapedValue = value
                .Replace("\"", "\\\"")
                .Replace("\\", "\\\\")
                .Replace("/", "\\/")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
            Write($"\"{escapedValue}\"");
        }

        public void WriteNumber(int value)
        {
            Write(value);
        }

        public void WriteNumber(long value)
        {
            Write(value);
        }

        public void WriteNumber(float value)
        {
            Write(value);
        }

        public void WriteNumber(double value)
        {
            Write(value);
        }

        public void WriteNumber(decimal value)
        {
            Write(value);
        }

        public void WriteValue(object value)
        {
            if (value == null)
            {
                Write("null");
            }
            else
            {
                switch (value)
                {
                    case string s:
                        WriteString(s);
                        break;
                    case int i:
                        WriteNumber(i);
                        break;
                    case long l:
                        WriteNumber(l);
                        break;
                    case float f:
                        WriteNumber(f);
                        break;
                    case double d:
                        WriteNumber(d);
                        break;
                    case decimal e:
                        WriteNumber(e);
                        break;
                    case IDictionary<string, object> o:
                        WriteObject(o);
                        break;
                    case IEnumerable<object> a:
                        WriteArray(a);
                        break;
                    case bool b:
                        Write(b ? "true" : "false");
                        break;
                    default: throw new Exception();
                }
            }
        }
        public void WriteArray(IEnumerable<object> value)
        {
            Write("[");

            var firstItem = true;
            foreach (var item in value)
            {
                if (firstItem)
                {
                    firstItem = false;
                }
                else
                {
                    Write(",");
                }

                Write(" ");

                WriteValue(item);
            }

            Write(" ]");
        }

        public void WriteObject(IDictionary<string, object> value)
        {
            Write("{");

            var firstItem = true;
            foreach (var item in value)
            {
                if (firstItem)
                {
                    firstItem = false;
                }
                else
                {
                    Write(",");
                }

                Write(" ");
                WriteString(item.Key);
                Write(": ");

                WriteValue(item.Value);
            }

            Write(" }");
        }
    }
}
