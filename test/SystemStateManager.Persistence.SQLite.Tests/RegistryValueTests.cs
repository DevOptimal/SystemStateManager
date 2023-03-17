using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevOptimal.SystemStateManager.Persistence.SQLite.Tests
{
    [TestClass]
    public class RegistryValueTests
    {
        [TestMethod]
        public void ConvertToByteArray()
        {
            object value = "foobar";
            byte[] actualBytes;

            switch (value)
            {
                case string stringValue:
                    actualBytes = Encoding.ASCII.GetBytes(stringValue);
                    break;
                case int intValue:
                    actualBytes = BitConverter.GetBytes(intValue);
                    break;
                case long longValue:
                    actualBytes = BitConverter.GetBytes(longValue);
                    break;
                case byte[] byteValue:
                    actualBytes = byteValue;
                    break;
                default:
                    throw new NotSupportedException($"{value.GetType().Name} is not a supported registry type");
            }
        }
    }
}
