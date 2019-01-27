using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TypeMake
{
    [DebuggerDisplay("Id = {Id}, Name = {Name}, VirtualDir = {VirtualDir}, FilePath = {FilePath}")]
    public class ProjectReference
    {
        public String Id;
        public String Name;
        public PathString VirtualDir;
        public PathString FilePath;
    }
}
