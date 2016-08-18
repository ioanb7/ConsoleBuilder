using Microsoft.Build.BuildEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace ConsoleBuilder
{
    public class ModuleListing{
        public bool IsEnabled { get; set; }
        public string User { get; set; }
        public string Module { get; set; }
        public string Path { get; set; } // To be hidden from UI
        public bool HasDependencies { get; set; }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<ModuleListing> modules = new List<ModuleListing>();
        string dirPath = @"C:\Users\484327\Documents\GitHub\ExcelsiorConsole\Users";
        string csProj = @"C:\Users\484327\Documents\GitHub\ExcelsiorConsole\ExcelsiorConsole.csproj";
        string bubbleFile = @"C:\Users\484327\Documents\GitHub\ExcelsiorConsole\Bubble\bubble.cs";
        //string pipPath = @"C:\Users\484327\AppData\Local\Programs\Python\Python35-32\Scripts\pip.exe";
        string pipPath = @"C:\Python27\Scripts\pip.exe";
        public MainWindow()
        {
            InitializeComponent();
        }

        public void UpdateXMLProject(string csproj, List<string> linksToAdd)
        {
            string condition = "13 != 37";

            XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument projDefinition;
            try
            {
                projDefinition = XDocument.Load(csproj);
            }
            catch (System.Xml.XmlException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Exception object Line, pos: (" + e.LineNumber + "," + e.LinePosition + ")");

                MessageBox.Show("Invalid XML Exception on line: " + e.LineNumber + ", pos: " + e.LinePosition + ".");

                return;
            }

            var project = projDefinition
                .Element(msbuild + "Project");
            
            var itemGroup = project
                .Elements(msbuild + "ItemGroup")
                .FirstOrDefault(item => item.Attribute("Condition") != null && item.Attribute("Condition").Value.ToString() == condition);

            if (itemGroup != null)
                itemGroup.RemoveNodes();

            if(itemGroup == null)
            {
                itemGroup = new XElement(msbuild + "ItemGroup");
                itemGroup.SetAttributeValue("Condition", condition);
                project.Add(itemGroup);
            }

            foreach(var link in linksToAdd) {
                var linkElement = new XElement(msbuild + "Compile");
                linkElement.SetAttributeValue("Include", link);
                itemGroup.Add(linkElement);

                //read and add dependencies

                string jsonDependencyPath = dirPath + "\\" + link.Substring(link.IndexOf("\\") + 1).Replace("Cmd.cs", "Cmd.json");
                if (System.IO.File.Exists(jsonDependencyPath))
                {
                    DependencyJObject dependency = JsonConvert.DeserializeObject<DependencyJObject>(System.IO.File.ReadAllText(jsonDependencyPath));
                    foreach (string dependencyPath2 in dependency.Dependencies)
                    {
                        string dependencyPath = dirPath + "\\" + link.Substring(link.IndexOf("\\") + 1).Substring(0, link.IndexOf("\\") + 1) + "\\" + dependencyPath2;
                        string dependencyNameWithoutDirectory = dependencyPath.Substring(dependencyPath.LastIndexOf('\\') + 1);
                        if (dependencyPath.EndsWith(".Designer.cs"))
                        {
                            var compileIncludeTag = new XElement(msbuild + "Compile");
                            compileIncludeTag.SetAttributeValue("Include", dependencyPath);

                            var dependentUponTag = new XElement(msbuild + "DependentUpon", dependencyNameWithoutDirectory.Replace(".Designer.cs", ".cs"));

                            compileIncludeTag.Add(dependentUponTag);
                            itemGroup.Add(compileIncludeTag);
                        }

                        if (dependencyPath.EndsWith(".resx"))
                        {
                            var embeddedResourceTag = new XElement(msbuild + "EmbeddedResource");
                            embeddedResourceTag.SetAttributeValue("Include", dependencyPath);

                            var dependentUponTag = new XElement(msbuild + "DependentUpon", dependencyNameWithoutDirectory.Replace(".resx", ".cs"));

                            embeddedResourceTag.Add(dependentUponTag);
                            itemGroup.Add(embeddedResourceTag);
                        }
                        if (dependencyPath.EndsWith("Form.cs"))
                        {
                            var compileIncludeTag = new XElement(msbuild + "Compile");
                            compileIncludeTag.SetAttributeValue("Include", dependencyPath);

                            var subtypeTag = new XElement(msbuild + "SubType", "Form");

                            compileIncludeTag.Add(subtypeTag);
                            itemGroup.Add(compileIncludeTag);
                        }
                    }
                }

            }

            projDefinition.Save(csproj);
        }
        
        public List<ModuleListing> GetModules()
        {
            var modules = new List<ModuleListing>();
            try
            {

                List<string> dirs = new List<string>(Directory.EnumerateDirectories(dirPath));

                foreach (var dir in dirs)
                {
                    var fileExtension = "Cmd.cs";
                    var files = Directory.EnumerateFiles(dir, "*" + fileExtension, SearchOption.AllDirectories);

                    foreach (string currentFile in files)
                    {
                        ModuleListing moduleListing = new ModuleListing();
                        moduleListing.User = dir.Substring(dir.LastIndexOf("\\") + 1);
                        string fileName = currentFile.Substring(dir.Length + 1);
                        moduleListing.Module = fileName.Substring(0, fileName.Length - fileExtension.Length);
                        moduleListing.Path = "Users\\" + moduleListing.User + "\\" + fileName;
                        string jsonDependencyPath = currentFile.Replace("Cmd.cs", "Cmd.json");
                        if (System.IO.File.Exists(jsonDependencyPath))
                            moduleListing.HasDependencies = true;
                        else
                            moduleListing.HasDependencies = false;

                        modules.Add(moduleListing);
                    }

                }
                Console.WriteLine("{0} directories found.",  dirs.Count);
            }
            catch (UnauthorizedAccessException UAEx)
            {
                Console.WriteLine(UAEx.Message);
            }
            catch (PathTooLongException PathEx)
            {
                Console.WriteLine(PathEx.Message);
            }

            return modules;
        }

        private void MakeLinker(string bubbleFile, string text2)
        {
            var text = "using System.Collections.Generic;\r\nnamespace ExcelsiorConsole\r\n{\r\npublic static class CommandsGenerator\r\n{\r\npublic static List<Command> GetCommands(ConsoleWindow cw)\r\n{\r\nList<Command> commands = new List<Command>();\r\n";
            text += text2.Split(new char[] { '\r', '\n' }).Select(line => line == "" ? "" : "commands.Add(new ExcelsiorConsole.Users." + line.Split(':')[0] + "." + line.Split(':')[1] + "Cmd(cw));").ToList().Aggregate((a, b) => a + "\r\n\t\t" + b);
            text += "\r\nreturn commands;\r\n}\r\n}\r\n}";
            System.IO.File.WriteAllText(bubbleFile, text);
        }

        public string RunProcess(string filename, string arguments)
        {
            Process process = new Process();
            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = arguments; // Note the /c command (*)
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            Console.WriteLine(err);
            process.WaitForExit();

            return output;
        }

        private void Install(ModuleListing module)
        {
            string jsonDependencyPath = dirPath + "\\" + module.Path.Substring(module.Path.IndexOf("\\") + 1).Replace("Cmd.cs", "Cmd.json");
            if (System.IO.File.Exists(jsonDependencyPath))
            {
                DependencyJObject dependency = JsonConvert.DeserializeObject<DependencyJObject>(System.IO.File.ReadAllText(jsonDependencyPath));

                if (dependency.pip != null && dependency.pip.Count > 0)
                {
                    string pipInstalled = RunProcess("cmd.exe", "/c " + pipPath + " list");
                    foreach (string pipDependency in dependency.pip)
                    {
                        if (pipInstalled.ToLower().Contains(pipDependency.ToLower()))
                        {
                            // dependency installed already=
                        }
                        else
                        {
                            //install dependency
                            string result = RunProcess("cmd.exe", "/c "+pipPath+" install " + pipDependency);
                            pipInstalled += "\r\n" + pipDependency;
                        }
                    }
                }
            }
        }

        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            var links = new List<string>();
            var modulesToAdd = new List<string>();
            foreach(var module in modules) {
                if(module.IsEnabled == true) {
                    links.Add(module.Path);
                    modulesToAdd.Add(module.User + ":" + module.Module);
                }
            }

            UpdateXMLProject( csProj, links );
            string text = modulesToAdd.Count == 0 ? "" : modulesToAdd.Aggregate((a, b) => a + "\r\n" + b);
            MakeLinker(bubbleFile, text);

        }
        private void RemoveAllDependenciesButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateXMLProject(csProj, new List<string> { });
            MakeLinker(bubbleFile, "");
        }

        private void ShowCommandsButton_Click(object sender, RoutedEventArgs e)
        {
            modules = GetModules();

            CommandsDataGrid.ItemsSource = modules;
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {

            foreach (var module in modules)
            {
                if (module.HasDependencies == true)
                {
                    Install(module);
                }
            }
        }
    }
}
