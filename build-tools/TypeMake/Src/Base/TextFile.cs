using System;
using System.Text;
using System.IO;

namespace TypeMake
{
    public static class TextFile
    {
        public static void WriteToFile(PathString FilePath, String Content, Encoding Encoding, bool WriteOnlyOnModified)
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
            var Dir = FilePath.Parent;
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
            File.WriteAllText(FilePath, Content, Encoding);
        }
    }
}
