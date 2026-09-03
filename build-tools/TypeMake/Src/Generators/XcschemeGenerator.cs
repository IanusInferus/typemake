using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TypeMake.Cpp
{
    public class XcschemeGenerator
    {
        private Project Project;
        private PathString OutputDirectory;
        private String XcschemeTemplateText;
        private ConfigurationType ConfigurationType;

        public XcschemeGenerator(Project Project, PathString OutputDirectory, String XcschemeTemplateText, ConfigurationType ConfigurationType)
        {
            this.Project = Project;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.XcschemeTemplateText = XcschemeTemplateText;
            this.ConfigurationType = ConfigurationType;
        }

        public void Generate(bool ForceRegenerate, bool IgnoreOnExistence)
        {
            var XcschemePath = OutputDirectory / (Project.Name + ".xcodeproj") / "xcshareddata" / "xcschemes" / (Project.Name + ".xcscheme");
            var ProductName = !String.IsNullOrEmpty(Project.TargetName) ? Project.TargetName : Project.Name;
            String BuildableName;
            if (Project.TargetType == TargetType.Executable)
            {
                BuildableName = ProductName;
            }
            else if ((Project.TargetType == TargetType.StaticLibrary) || (Project.TargetType == TargetType.IntermediateStaticLibrary))
            {
                BuildableName = "lib" + ProductName + ".a";
            }
            else if (Project.TargetType == TargetType.DynamicLibrary)
            {
                BuildableName = "lib" + ProductName + ".dylib";
            }
            else if (Project.TargetType == TargetType.DarwinApplication)
            {
                BuildableName = ProductName + ".app";
            }
            else if (Project.TargetType == TargetType.DarwinSharedFramework)
            {
                BuildableName = ProductName + ".framework";
            }
            else
            {
                throw new NotSupportedException("Unsupported Xcode scheme target type: " + Project.TargetType);
            }

            var x = XDocument.Parse(XcschemeTemplateText);
            foreach (var BuildableReference in x.Descendants("BuildableReference"))
            {
                BuildableReference.SetAttributeValue("BuildableName", BuildableName);
                BuildableReference.SetAttributeValue("BlueprintName", Project.Name);
                BuildableReference.SetAttributeValue("ReferencedContainer", "container:" + Project.Name + ".xcodeproj");
            }
            x.Descendants("LaunchAction").Single().SetAttributeValue("buildConfiguration", ConfigurationType.ToString());
            if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DarwinApplication))
            {
                var BuildableReference = new XElement(x.Descendants("BuildableReference").First());
                x.Descendants("LaunchAction").Single().Add(
                    new XElement("BuildableProductRunnable",
                        new XAttribute("runnableDebuggingMode", "0"),
                        BuildableReference));

                var MacroExpansion = x.Descendants("MacroExpansion").Single();
                MacroExpansion.Name = "BuildableProductRunnable";
                MacroExpansion.SetAttributeValue("runnableDebuggingMode", "0");
            }
            if (IgnoreOnExistence && System.IO.File.Exists(XcschemePath))
            {
                return;
            }
            TextFile.WriteToFile(XcschemePath, @"<?xml version=""1.0"" encoding=""UTF-8""?>" + "\n" + x.ToString(), new UTF8Encoding(false), !ForceRegenerate);
        }
    }
}
