using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeMake;

namespace TypeMakeTest
{
    [TestClass]
    public class RetypemakeScriptReaderTest
    {
        [TestMethod]
        public void TestCmdSetEscaping()
        {
            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cmd");
            const String Name = "TYPEMAKE_TEST_CMD_SET";
            try
            {
                File.WriteAllLines(Path, new[]
                {
                    "set " + Shell.EscapeArgumentForCmdSet(Name + "=a\"b&c^d%"),
                    "SET \"TYPEMAKE_TEST_LEGACY=legacy value\""
                });
                RetypemakeScriptReader.Read(Path.AsPath());
                Assert.AreEqual("a\"b&c^d%", Environment.GetEnvironmentVariable(Name));
                Assert.AreEqual("legacy value", Environment.GetEnvironmentVariable("TYPEMAKE_TEST_LEGACY"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(Name, null);
                Environment.SetEnvironmentVariable("TYPEMAKE_TEST_LEGACY", null);
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
        }
    }
}
