using System;
using System.Text;
using System.IO;

namespace TypeMake
{
    public static class TextFile
    {
        public static void WriteToFile(String FilePath, String Content, Encoding Encoding, bool WriteOnlyOnModified)
        {
            if (WriteOnlyOnModified)
            {
                if (File.Exists(FilePath))
                {
                    var OriginalContent = File.ReadAllText(FilePath);
                    if (Content == OriginalContent)
                    {
                        return;
                    }
                }
            }
            var Dir = Path.GetDirectoryName(FilePath);
            if ((Dir != "") && !Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
            File.WriteAllText(FilePath, Content, Encoding);
        }
    }
}
