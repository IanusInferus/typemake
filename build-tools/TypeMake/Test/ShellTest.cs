using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TypeMake;

namespace TypeMakeTest
{
    [TestClass]
    public class ShellTest
    {
        [TestMethod]
        public void TestCmdDoubleQuoteEscaping()
        {
            Assert.AreEqual("plain", Shell.EscapeArgumentForShell("plain", Shell.ShellArgumentStyle.CMD));
            Assert.AreEqual("\"\"", Shell.EscapeArgumentForShell("", Shell.ShellArgumentStyle.CMD));
            Assert.AreEqual("\"a\"\"b & c\"", Shell.EscapeArgumentForShell("a\"b & c", Shell.ShellArgumentStyle.CMD));
            Assert.AreEqual("\"a\\\\\"\"b\"", Shell.EscapeArgumentForShell("a\\\"b", Shell.ShellArgumentStyle.CMD));
            Assert.AreEqual("\"" + "a " + @"\\" + "\"", Shell.EscapeArgumentForShell("a \\", Shell.ShellArgumentStyle.CMD));
        }

        [TestMethod]
        public void TestCmdInteractiveDoubleQuoteEscaping()
        {
            Assert.AreEqual("\"a\"\"b & c\"", Shell.EscapeArgumentForShell("a\"b & c", Shell.ShellArgumentStyle.CMD_Interactive));
            Assert.AreEqual("\"a\\\\\"\"b\"", Shell.EscapeArgumentForShell("a\\\"b", Shell.ShellArgumentStyle.CMD_Interactive));
        }

        [TestMethod]
        public void TestCmdSetEscaping()
        {
            var Assignment = "Name=a\"b&c^d%%";
            var Escaped = Shell.EscapeArgumentForCmdSet(Assignment);
            Assert.AreEqual("Name=a^\"b^&c^^d%%%%", Escaped);
            Assert.AreEqual(Assignment, Shell.ParseArgumentsForCmdSet(Escaped));
        }

        [TestMethod]
        public void TestParseArgumentsForShell()
        {
            foreach (var Style in new[] { Shell.ShellArgumentStyle.CMD, Shell.ShellArgumentStyle.CMD_Interactive, Shell.ShellArgumentStyle.Bash })
            {
                var Arguments = new[] { "plain", "a\"b & c", "a\\\"b", "100%" };
                var CommandLine = String.Join(" ", Arguments.Select(a => Shell.EscapeArgumentForShell(a, Style)));
                CollectionAssert.AreEqual(
                    new List<string>(Arguments),
                    Shell.ParseArgumentsForShell(CommandLine, Style));
            }
        }

        [TestMethod]
        public void TestParseArguments()
        {
            CollectionAssert.AreEqual(
                new List<string> { "--sysroot=C:\\Program Files\\Toolchain", "-DNAME=a b", "", "literal\"quote" },
                Shell.ParseArguments("--sysroot=\"C:\\Program Files\\Toolchain\" -DNAME=a\\ b \"\" literal\\\"quote", Shell.ArgumentStyle.Unix));
            Assert.ThrowsException<System.ArgumentException>(() => Shell.ParseArguments("\"unterminated", Shell.ArgumentStyle.Unix));

            var WindowsArgument = "a\\\"b c\\";
            var WindowsCommandLine = Shell.EscapeArgument(WindowsArgument, Shell.ArgumentStyle.Windows);
            CollectionAssert.AreEqual(
                new List<string> { WindowsArgument },
                Shell.ParseArguments(WindowsCommandLine, Shell.ArgumentStyle.Windows));
            CollectionAssert.AreEqual(
                new List<string> { "" },
                Shell.ParseArguments("\"\"", Shell.ArgumentStyle.Windows));
        }
    }
}
