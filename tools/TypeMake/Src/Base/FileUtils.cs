using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TypeMake
{
    public static class FileUtils
    {
        public static void Copy(PathString SourceFilePath, PathString DestinationFilePath, bool CopyOnlyOnModified)
        {
            if (CopyOnlyOnModified)
            {
                if (File.Exists(DestinationFilePath))
                {
                    var NewContent = File.ReadAllBytes(SourceFilePath);
                    var OriginalContent = File.ReadAllBytes(DestinationFilePath);
                    if (NewContent.SequenceEqual(OriginalContent))
                    {
                        return;
                    }
                }
            }
            var Dir = DestinationFilePath.Parent;
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
            File.Copy(SourceFilePath, DestinationFilePath, true);
        }
    }
}
