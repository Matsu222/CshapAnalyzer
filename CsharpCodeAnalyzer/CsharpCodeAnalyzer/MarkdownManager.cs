using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace CsharpCodeAnalyzer
{
    class MarkdownManager
    {
        public static void OutputToMarkdown(ParsedProject project, string outputDirectory = "output")
        {
            PrepareOutputDirectory(outputDirectory);

            foreach (var ns in project.Namespaces.Values)
            {
                var namespaceMarkdown = GenerateNamespaceMarkdown(ns, outputDirectory);
                var namespaceFilePath = Path.Combine(outputDirectory, $"{ns.Name}.md");
                WriteMarkdownWithToc(namespaceFilePath, namespaceMarkdown);
            }

            WriteNamespaceList(project, outputDirectory);

            Console.WriteLine("Markdown files have been generated.");
        }

        private static void PrepareOutputDirectory(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
            {
                foreach (var file in Directory.GetFiles(outputDirectory, "*.md"))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(outputDirectory);
            }
        }

        private static StringBuilder GenerateNamespaceMarkdown(NamespaceModel ns, string outputDirectory)
        {
            var sb = new StringBuilder();
            AppendBackLink(sb, "Namespaces");
            sb.AppendLine($"# Namespace: {ns.Name}\n");

            WriteTypeSection(sb, ns.Types, "class", "Classes", ns.Name, outputDirectory);
            WriteTypeSection(sb, ns.Types, "interface", "Interfaces", ns.Name, outputDirectory);
            WriteTypeSection(sb, ns.Types, "enum", "Enums", ns.Name, outputDirectory);
            WriteTypeSection(sb, ns.Types, "struct", "Structs", ns.Name, outputDirectory);

            AppendBackLink(sb, "Namespaces");
            return sb;
        }

        private static void WriteTypeSection(StringBuilder sb, List<TypeModel> types, string kind, string header, string nsName, string outputDirectory)
        {
            var filtered = types.Where(t => t.Kind == kind).OrderBy(t => t.Name).ToList();

            if (filtered.Any())
            {
                sb.AppendLine($"## {header}");
                foreach (var type in filtered)
                {
                    WriteTypeMarkdown(type, nsName, outputDirectory);
                    sb.AppendLine($"- [{type.Name}](./{nsName}_{type.Name}.md)");
                }
                sb.AppendLine();
            }
        }

        private static void WriteNamespaceList(ParsedProject project, string outputDirectory)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Namespaces\n");

            foreach (var ns in project.Namespaces.Values)
            {
                sb.AppendLine($"- [{ns.Name}](./{ns.Name}.md)");
            }

            var path = Path.Combine(outputDirectory, "Namespaces.md");
            WriteMarkdownWithToc(path, sb);
        }

        private static void WriteTypeMarkdown(TypeModel type, string namespaceName, string outputDirectory)
        {
            var sb = new StringBuilder();

            AppendBackLink(sb, namespaceName);

            sb.AppendLine($"# {type.Accessibility} {type.Kind} {type.Name}");
            if (!string.IsNullOrEmpty(type.XmlComment)) sb.AppendLine(XmlParser.ToMarkdown(type.XmlComment) + "\n");
            if (type.Kind == "enum") AppendEnumMembers(sb, type.Members);
            else AppendFieldsAndMethods(sb, type.Members);

            AppendBackLink(sb, namespaceName);

            var filePath = Path.Combine(outputDirectory, $"{namespaceName}_{type.Name}.md");
            WriteMarkdownWithToc(filePath, sb);
        }

        private static void AppendEnumMembers(StringBuilder sb, List<MemberModel> members)
        {
            var enumMembers = members.OrderBy(m => m.Name).ToList();

            if (enumMembers.Any())
            {
                sb.AppendLine("## Members");
                foreach (var member in enumMembers)
                {
                    sb.AppendLine($" - **{member.Name}**");
                    if (!string.IsNullOrEmpty(member.XmlComment))
                        sb.AppendLine(XmlParser.ToMarkdown(member.XmlComment));
                }
            }
        }

        private static void AppendFieldsAndMethods(StringBuilder sb, List<MemberModel> members)
        {
            var fields = members
                .Where(m => m.Kind == "field")
                .OrderBy(m => m.Name)
                .ToList();

            var methods = members
                .Where(m => m.Kind == "method")
                .OrderBy(m => m.Name)
                .ToList();

            if (fields.Any())
            {
                sb.AppendLine("## Fields");
                foreach (var field in fields)
                {
                    sb.AppendLine($"### **{field.Type} {field.Name}**");
                    if (!string.IsNullOrEmpty(field.XmlComment))
                        sb.AppendLine(XmlParser.ToMarkdown(field.XmlComment));
                }
                sb.AppendLine();
            }

            if (methods.Any())
            {
                sb.AppendLine("## Methods");
                foreach (var method in methods)
                {
                    sb.AppendLine($"### **{method.Type} {method.Name}**");
                    if (!string.IsNullOrEmpty(method.XmlComment))
                        sb.AppendLine(XmlParser.ToMarkdown(method.XmlComment));
                }
            }
        }

        private static void AppendBackLink(StringBuilder sb, string BackFilename)
        {
            sb.AppendLine($"---\n");
            sb.AppendLine($"---\n");
            sb.AppendLine($"[← Back to {BackFilename}](./{BackFilename}.md)\n");
        }

        private static void WriteMarkdownWithToc(string filePath, StringBuilder originalContent)
        {
            var lines = originalContent.ToString().Split('\n').ToList();
            var toc = GenerateTocFromLines(lines);

            var finalBuilder = new StringBuilder();

            bool inserted = false;
            for (int i = 0; i < lines.Count; i++)
            {
                // 最初の "##" セクションの次に TOC を挿入
                //if (!inserted && lines[i].StartsWith("## "))
                if (!inserted)
                {
                    finalBuilder.AppendLine();
                    finalBuilder.AppendLine(toc);
                    inserted = true;
                }
                finalBuilder.AppendLine(lines[i].TrimEnd());
            }

            File.WriteAllText(filePath, finalBuilder.ToString());
        }


        private static string GenerateTocFromLines(IEnumerable<string> markdownLines)
        {
            var toc = new StringBuilder();
            toc.AppendLine("# Table of Contents");

            foreach (var line in markdownLines)
            {
                if (line.StartsWith("## "))
                {
                    var title = line.Substring(3).Trim();
                    var anchor = GenerateMarkdownAnchor(title);
                    toc.AppendLine($"- [{title}](#{anchor})");
                }
                else if (line.StartsWith("### "))
                {
                    var title = line.Substring(4).Trim();
                    var anchor = GenerateMarkdownAnchor(title);
                    toc.AppendLine($"  - [{title}](#{anchor})");
                }
            }

            toc.AppendLine();
            return toc.ToString();
        }

        private static string GenerateMarkdownAnchor(string title)
        {
            // GitHub形式のアンカーを生成：小文字、空白→-、記号削除
            var anchor = title.ToLowerInvariant();
            anchor = Regex.Replace(anchor, @"[^\w\s\-]", ""); // 記号除去
            anchor = Regex.Replace(anchor, @"\s+", "-");      // 空白→-
            return anchor;
        }
    }
}
