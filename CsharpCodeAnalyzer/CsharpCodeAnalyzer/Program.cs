using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics.Eventing.Reader;

namespace CsharpCodeAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string rootDir;
            if (args.Length > 0) rootDir = args[0];
            else rootDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var files = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories);
            var project = CsFileParser.ParseFilesToProject(files);

            MarkdownManager.OutputToMarkdown(project, rootDir + @"\Overall");
            CsFileParser.RemovePrivateMembersAndComments(project);
            MarkdownManager.OutputToMarkdown(project, rootDir + @"\ApiDoc");
        }
    }
}