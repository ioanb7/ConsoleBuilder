using Microsoft.Build.BuildEngine;
using System;
using System.Collections.Generic;
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

            var itemGroups = project
                .Elements(msbuild + "ItemGroup")
                .Where(item => item.Attribute("Condition") != null).ToList();

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

    }
}
