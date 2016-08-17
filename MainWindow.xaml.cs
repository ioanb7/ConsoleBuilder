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
        string modulesTxt = @"C:\Users\484327\Documents\GitHub\ExcelsiorConsole\Bubble\modules.txt";
        public MainWindow()
        {
            InitializeComponent();
        }

        public void UpdateXMLProject(string csproj, List<string> linksToAdd)
        {
            string condition = "13 != 37";

            XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument projDefinition = XDocument.Load(csproj);
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
            System.IO.File.WriteAllText(modulesTxt, text);
        }
        private void RemoveAllDependenciesButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateXMLProject(csProj, new List<string> { });
            System.IO.File.WriteAllText(modulesTxt, "");
        }

        private void ShowCommandsButton_Click(object sender, RoutedEventArgs e)
        {
            modules = GetModules();

            CommandsDataGrid.ItemsSource = modules;
        }

    }
}
