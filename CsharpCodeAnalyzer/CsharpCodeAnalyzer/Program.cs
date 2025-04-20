using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace CsharpCodeAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string rootDir = @"D:\Git_Local\DllXml2Markdown\CsharpCodeAnalyzer\TestSample";
            var files = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories);
            var project = CsFileParser.ParseFilesToProject(files);

            CsFileParser.RemovePrivateMembersAndComments(project);
            MarkdownManager.OutputToMarkdown(project);
        }
    }
}