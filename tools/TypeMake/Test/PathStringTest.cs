using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeMake;

namespace TypeMakeTest
{
    [TestClass]
    public class PathStringTest
    {
        [TestMethod]
        public void TestConversion()
        {
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().ToString(PathStringStyle.Windows), @"C:\Windows\notepad.exe");
            Assert.AreEqual("/dev/null".AsPath().ToString(PathStringStyle.Unix), "/dev/null");

            Assert.AreEqual(@"C:\Windows\".AsPath().ToString(PathStringStyle.Windows), @"C:\Windows");
            Assert.AreEqual("/dev/".AsPath().ToString(PathStringStyle.Unix), "/dev");

            Assert.AreEqual(@"C:".AsPath().ToString(PathStringStyle.Windows), @"C:\");
            Assert.AreEqual("/".AsPath().ToString(PathStringStyle.Unix), "/");

            Assert.AreEqual(((String)(null)).AsPath(), null);
        }

        [TestMethod]
        public void TestParent()
        {
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Parent.ToString(PathStringStyle.Windows), @"C:\Windows");
            Assert.AreEqual("/dev/null".AsPath().Parent.ToString(PathStringStyle.Unix), "/dev");

            Assert.AreEqual(@"C:\Windows\".AsPath().Parent.ToString(PathStringStyle.Windows), @"C:\");
            Assert.AreEqual("/dev/".AsPath().Parent.ToString(PathStringStyle.Unix), "/");

            Assert.AreEqual(@"C:".AsPath().Parent, "C:");
            Assert.AreEqual("/".AsPath().Parent, "/");

            Assert.AreEqual(".".AsPath().Parent, "..");
            Assert.AreEqual("..".AsPath().Parent, "../..");
            Assert.AreEqual("../..".AsPath().Parent, "../../..");

            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().GetAccestor(0).ToString(PathStringStyle.Windows), @"C:\Windows\notepad.exe");
            Assert.AreEqual("/dev/null".AsPath().GetAccestor(0).ToString(PathStringStyle.Unix), "/dev/null");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().GetAccestor(1).ToString(PathStringStyle.Windows), @"C:\Windows");
            Assert.AreEqual("/dev/null".AsPath().GetAccestor(1).ToString(PathStringStyle.Unix), "/dev");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().GetAccestor(2).ToString(PathStringStyle.Windows), @"C:\");
            Assert.AreEqual("/dev/null".AsPath().GetAccestor(2).ToString(PathStringStyle.Unix), "/");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().GetAccestor(3).ToString(PathStringStyle.Windows), @"C:\");
            Assert.AreEqual("/dev/null".AsPath().GetAccestor(3).ToString(PathStringStyle.Unix), "/");
        }

        [TestMethod]
        public void TestParts()
        {
            var Slash = Convert.ToString(Path.DirectorySeparatorChar);

            AreSequentialEqual(@"C:\Windows\notepad.exe".AsPath().Parts, new List<String> { "C:" + Slash, "Windows", "notepad.exe" });
            AreSequentialEqual("/dev/null".AsPath().Parts, new List<String> { Slash, "dev", "null" });

            AreSequentialEqual(@"C:\Windows\".AsPath().Parts, new List<String> { "C:" + Slash, "Windows" });
            AreSequentialEqual("/dev/".AsPath().Parts, new List<String> { Slash, "dev" });

            AreSequentialEqual(@"C:".AsPath().Parts, new List<String> { "C:" + Slash });
            AreSequentialEqual("/".AsPath().Parts, new List<String> { Slash });

            AreSequentialEqual("".AsPath().Parts, new List<String> { "." });
        }

        [TestMethod]
        public void TestReducedAndEqual()
        {
            Assert.AreEqual(@"C:\Windows\".AsPath().Reduced.ToString(PathStringStyle.Windows), @"C:\Windows");
            Assert.AreEqual(@"C:\Windows".AsPath().Reduced.ToString(PathStringStyle.Windows), @"C:\Windows");
            Assert.AreEqual("/usr/".AsPath().Reduced.ToString(PathStringStyle.Unix), "/usr");
            Assert.AreEqual("/usr".AsPath().Reduced.ToString(PathStringStyle.Unix), "/usr");

            Assert.AreEqual(@"C:\".AsPath().Reduced.ToString(PathStringStyle.Windows), @"C:\");
            Assert.AreEqual(@"C:".AsPath().Reduced.ToString(PathStringStyle.Windows), @"C:\");
            Assert.AreEqual("/".AsPath().Reduced.ToString(PathStringStyle.Unix), "/");

            Assert.AreEqual(".".AsPath().Reduced.ToString(), ".");
            Assert.AreEqual("".AsPath().Reduced.ToString(), ".");
            Assert.AreEqual("..".AsPath().Reduced.ToString(), "..");
            Assert.AreEqual(@"..\..".AsPath().Reduced.ToString(PathStringStyle.Windows), @"..\..");

            Assert.AreEqual(@"../../a/b/../c".AsPath().Reduced.ToString(PathStringStyle.Windows), @"..\..\a\c");
            Assert.AreEqual("../../a/../b/../c".AsPath().Reduced.ToString(PathStringStyle.Unix), "../../c");

            Assert.AreEqual(@"C:\Windows\..\Windows\System32\..\notepad.exe".AsPath(), @"C:\Windows\notepad.exe".AsPath());
            Assert.AreEqual("/usr/bin/a/b/../../ls".AsPath(), "/usr/bin/ls");

            Assert.AreEqual(@"C:\Windows\..\..\Windows\System32\..\notepad.exe".AsPath(), @"C:\Windows\notepad.exe".AsPath());
            Assert.AreEqual("/usr/../../usr/bin/ls".AsPath(), "/usr/bin/ls");
        }

        [TestMethod]
        public void TestRelative()
        {
            Assert.AreEqual(@"C:\Windows\".AsPath().RelativeTo(@"C:\Windows\").ToString(PathStringStyle.Windows), @".");
            Assert.AreEqual(@"C:\Windows".AsPath().RelativeTo(@"C:\Windows").ToString(PathStringStyle.Windows), @".");
            Assert.AreEqual(@"C:\Windows\".AsPath().RelativeTo(@"C:\Windows").ToString(PathStringStyle.Windows), @".");
            Assert.AreEqual(@"C:\Windows".AsPath().RelativeTo(@"C:\Windows\").ToString(PathStringStyle.Windows), @".");
            Assert.AreEqual("/usr/".AsPath().RelativeTo(@"/usr/").ToString(PathStringStyle.Unix), ".");
            Assert.AreEqual("/usr".AsPath().RelativeTo(@"/usr").ToString(PathStringStyle.Unix), ".");
            Assert.AreEqual("/usr/".AsPath().RelativeTo(@"/usr").ToString(PathStringStyle.Unix), ".");
            Assert.AreEqual("/usr".AsPath().RelativeTo(@"/usr/").ToString(PathStringStyle.Unix), ".");

            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().RelativeTo(@"C:\Windows").ToString(PathStringStyle.Windows), @"notepad.exe");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().RelativeTo(@"C:\Windows\System32").ToString(PathStringStyle.Windows), @"..\notepad.exe");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().RelativeTo(@"C:\Windows\System32\drivers").ToString(PathStringStyle.Windows), @"..\..\notepad.exe");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().RelativeTo(@"C:\Users").ToString(PathStringStyle.Windows), @"..\Windows\notepad.exe");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().RelativeTo(@"C:\Users\test").ToString(PathStringStyle.Windows), @"..\..\Windows\notepad.exe");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().RelativeTo(@"D:\Users\test").ToString(PathStringStyle.Windows), @"C:\Windows\notepad.exe");
            Assert.AreEqual("/home/test/1.txt".AsPath().RelativeTo(@"/home/test/1.txt").ToString(PathStringStyle.Unix), ".");
            Assert.AreEqual("/home/test/1.txt".AsPath().RelativeTo(@"/home/test").ToString(PathStringStyle.Unix), "1.txt");
            Assert.AreEqual("/home/test/1.txt".AsPath().RelativeTo(@"/home").ToString(PathStringStyle.Unix), "test/1.txt");
            Assert.AreEqual("/home/test/1.txt".AsPath().RelativeTo(@"/mnt/sdcard").ToString(PathStringStyle.Unix), "../../home/test/1.txt");
        }

        [TestMethod]
        public void TestEqualAndIn()
        {
            Assert.AreEqual(@"C:\Windows\".AsPath().Equals(@"C:\Windows\"), true);
            Assert.AreEqual(@"C:\Windows".AsPath().Equals(@"C:\Windows"), true);
            Assert.AreEqual(@"C:\Windows\".AsPath().Equals(@"C:\Windows"), true);
            Assert.AreEqual(@"C:\Windows".AsPath().Equals(@"C:\Windows\"), true);
            Assert.AreEqual("/usr/".AsPath().Equals("/usr/"), true);
            Assert.AreEqual("/usr".AsPath().Equals("/usr"), true);
            Assert.AreEqual("/usr/".AsPath().Equals("/usr"), true);
            Assert.AreEqual("/usr".AsPath().Equals("/usr/"), true);
            Assert.AreEqual(@"C:\Windows\".AsPath().Equals(@"c:\windows\", false), true);

            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"C:\Windows"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"C:\Windows\System32"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"C:\Windows\System32\drivers"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"C:\Users"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"C:\Users\test"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"D:\Users\test"), false);
            Assert.AreEqual(@"C:\Windows\..\Windows\notepad.exe".AsPath().Equals(@"C:\Windows"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"C:\Windows\..\Windows"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"C:\Windows\System32"), false);
            Assert.AreEqual("/home/test/1.txt".AsPath().Equals("/home/test/1.txt"), true);
            Assert.AreEqual("/home/test/1.txt".AsPath().Equals("/home/test"), false);
            Assert.AreEqual("/home/test/1.txt".AsPath().Equals("/home"), false);
            Assert.AreEqual("/home/test/1.txt".AsPath().Equals("/mnt/sdcard"), false);
            Assert.AreEqual("1.txt".AsPath().Equals(@"C:\test"), false);
            Assert.AreEqual("1.txt".AsPath().Equals("test"), false);
            Assert.AreEqual("1.txt".AsPath().Equals("1.txt"), true);
            Assert.AreEqual("1.txt".AsPath().Equals(""), false);
            Assert.AreEqual("1.txt".AsPath().Equals("."), false);
            Assert.AreEqual("1.txt".AsPath().Equals(".."), false);
            Assert.AreEqual(".".AsPath().Equals(@"C:\test"), false);
            Assert.AreEqual(".".AsPath().Equals("test"), false);
            Assert.AreEqual(".".AsPath().Equals("1.txt"), false);
            Assert.AreEqual(".".AsPath().Equals(""), true);
            Assert.AreEqual(".".AsPath().Equals("."), true);
            Assert.AreEqual(".".AsPath().Equals(".."), false);
            Assert.AreEqual("..".AsPath().Equals("."), false);
            Assert.AreEqual("..".AsPath().Equals(".."), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Equals(@"C:\Windows", false), false);

            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\Windows"), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\Windows\System32"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\Windows\System32\drivers"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\Users"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\Users\test"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"D:\Users\test"), false);
            Assert.AreEqual(@"C:\Windows\..\Windows\notepad.exe".AsPath().In(@"C:\Windows"), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\Windows\..\Windows"), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\Windows\System32"), false);
            Assert.AreEqual("/home/test/1.txt".AsPath().In("/home/test/1.txt"), false);
            Assert.AreEqual("/home/test/1.txt".AsPath().In("/home/test"), true);
            Assert.AreEqual("/home/test/1.txt".AsPath().In("/home"), true);
            Assert.AreEqual("/home/test/1.txt".AsPath().In("/mnt/sdcard"), false);
            Assert.AreEqual("1.txt".AsPath().In(@"C:\test"), false);
            Assert.AreEqual("1.txt".AsPath().In("test"), false);
            Assert.AreEqual("1.txt".AsPath().In("1.txt"), false);
            Assert.AreEqual("1.txt".AsPath().In(""), true);
            Assert.AreEqual("1.txt".AsPath().In("."), true);
            Assert.AreEqual("1.txt".AsPath().In(".."), true);
            Assert.AreEqual(".".AsPath().In(@"C:\test"), false);
            Assert.AreEqual(".".AsPath().In("test"), false);
            Assert.AreEqual(".".AsPath().In("1.txt"), false);
            Assert.AreEqual(".".AsPath().In(""), false);
            Assert.AreEqual(".".AsPath().In("."), false);
            Assert.AreEqual(".".AsPath().In(".."), true);
            Assert.AreEqual("..".AsPath().In("."), false);
            Assert.AreEqual("..".AsPath().In(".."), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\windows", false), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"c:\windows\system32", false), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().In(@"C:\windows", true), false);

            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\Windows"), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\Windows\System32"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\Windows\System32\drivers"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\Users"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\Users\test"), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"D:\Users\test"), false);
            Assert.AreEqual(@"C:\Windows\..\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\Windows"), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\Windows\..\Windows"), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\Windows\System32"), false);
            Assert.AreEqual("/home/test/1.txt".AsPath().EqualsOrIn("/home/test/1.txt"), true);
            Assert.AreEqual("/home/test/1.txt".AsPath().EqualsOrIn("/home/test"), true);
            Assert.AreEqual("/home/test/1.txt".AsPath().EqualsOrIn("/home"), true);
            Assert.AreEqual("/home/test/1.txt".AsPath().EqualsOrIn("/mnt/sdcard"), false);
            Assert.AreEqual("1.txt".AsPath().EqualsOrIn(@"C:\test"), false);
            Assert.AreEqual("1.txt".AsPath().EqualsOrIn("test"), false);
            Assert.AreEqual("1.txt".AsPath().EqualsOrIn("1.txt"), true);
            Assert.AreEqual("1.txt".AsPath().EqualsOrIn(""), true);
            Assert.AreEqual("1.txt".AsPath().EqualsOrIn("."), true);
            Assert.AreEqual("1.txt".AsPath().EqualsOrIn(".."), true);
            Assert.AreEqual(".".AsPath().EqualsOrIn(@"C:\test"), false);
            Assert.AreEqual(".".AsPath().EqualsOrIn("test"), false);
            Assert.AreEqual(".".AsPath().EqualsOrIn("1.txt"), false);
            Assert.AreEqual(".".AsPath().EqualsOrIn(""), true);
            Assert.AreEqual(".".AsPath().EqualsOrIn("."), true);
            Assert.AreEqual(".".AsPath().EqualsOrIn(".."), true);
            Assert.AreEqual("..".AsPath().EqualsOrIn("."), false);
            Assert.AreEqual("..".AsPath().EqualsOrIn(".."), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\windows", false), true);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"c:\windows\system32", false), false);
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().EqualsOrIn(@"C:\windows", true), false);
        }

        [TestMethod]
        public void TestExtension()
        {
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().Extension, "exe");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().FileName, "notepad.exe");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().FileNameWithoutExtension, "notepad");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().ChnageExtension(".txt"), @"C:\Windows\notepad.txt");
            Assert.AreEqual(@"C:\Windows\notepad.exe".AsPath().ChnageExtension("txt"), @"C:\Windows\notepad.txt");

            Assert.AreEqual(@"C:\Windows\notepad.txt.exe".AsPath().Extension, "exe");
            Assert.AreEqual(@"C:\Windows\notepad.txt.exe".AsPath().FileName, "notepad.txt.exe");
            Assert.AreEqual(@"C:\Windows\notepad.txt.exe".AsPath().FileNameWithoutExtension, "notepad.txt");
            Assert.AreEqual(@"C:\Windows\notepad.txt.exe".AsPath().ChnageExtension(".txt"), @"C:\Windows\notepad.txt.txt");
            Assert.AreEqual(@"C:\Windows\notepad.txt.exe".AsPath().ChnageExtension("txt"), @"C:\Windows\notepad.txt.txt");
        }

        private void AreSequentialEqual<T>(IEnumerable<T> Left, IEnumerable<T> Right)
        {
            var eLeft = Left.GetEnumerator();
            var eRight = Right.GetEnumerator();
            while (true)
            {
                var LeftHasValue = eLeft.MoveNext();
                var RightHasValue = eRight.MoveNext();
                if (LeftHasValue != RightHasValue)
                {
                    Assert.Fail();
                }
                if (!LeftHasValue) { break; }
                Assert.AreEqual(eLeft.Current, eRight.Current);
            }
        }
    }
}
