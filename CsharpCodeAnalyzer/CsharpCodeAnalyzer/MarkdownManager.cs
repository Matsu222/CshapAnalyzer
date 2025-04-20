using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace CsharpCodeAnalyzer
{
    class MarkdownManager
    {
        public static void OutputToMarkdown(ParsedProject project)
        {
            var namespaceList = new StringBuilder();
            var outputDirectory = "output"; // 出力先のディレクトリ（任意で変更）

            // 出力先ディレクトリがあれば、.mdファイルを削除
            if (Directory.Exists(outputDirectory))
            {
                var existingFiles = Directory.GetFiles(outputDirectory, "*.md");
                foreach (var file in existingFiles)
                {
                    File.Delete(file);  // ファイルを削除
                }
            }
            else
            {
                // ディレクトリが存在しない場合は作成
                Directory.CreateDirectory(outputDirectory);
            }

            // 名前空間ごとにマークダウンファイルを分割して作成
            foreach (var ns in project.Namespaces.Values)
            {
                var namespaceMarkdownBuilder = new StringBuilder();
                namespaceMarkdownBuilder.AppendLine($"# Namespace: {ns.Name}\n");

                // グループ分け
                var classOrStructs = ns.Types
                    .Where(t => t.Kind == "class")
                    .OrderBy(t => t.Name)
                    .ToList();

                var enums = ns.Types
                    .Where(t => t.Kind == "enum")
                    .OrderBy(t => t.Name)
                    .ToList();
                var structs = ns.Types
                    .Where(t => t.Kind == "struct")
                    .OrderBy(t => t.Name)
                    .ToList();


                // クラス・構造体の見出し
                if (classOrStructs.Any())
                {
                    namespaceMarkdownBuilder.AppendLine("## Classes");
                    foreach (var type in classOrStructs)
                    {
                        WriteTypeMarkdown(type, ns.Name, outputDirectory);
                        namespaceMarkdownBuilder.AppendLine($"- [{type.Name}](./{ns.Name}_{type.Name}.md)");
                    }
                    namespaceMarkdownBuilder.AppendLine();
                }

                // Enum の見出し
                if (enums.Any())
                {
                    namespaceMarkdownBuilder.AppendLine("## Enums");
                    foreach (var type in enums)
                    {
                        WriteTypeMarkdown(type, ns.Name, outputDirectory);
                        namespaceMarkdownBuilder.AppendLine($"- [{type.Name}](./{ns.Name}_{type.Name}.md)");
                    }
                    namespaceMarkdownBuilder.AppendLine();
                }

                // Structの見出し
                if (structs.Any())
                {
                    namespaceMarkdownBuilder.AppendLine("## Structs");
                    foreach (var type in structs)
                    {
                        WriteTypeMarkdown(type, ns.Name, outputDirectory);
                        namespaceMarkdownBuilder.AppendLine($"- [{type.Name}](./{ns.Name}_{type.Name}.md)");
                    }
                    namespaceMarkdownBuilder.AppendLine();
                }


                // 名前空間ごとのマークダウンファイルを保存
                var namespaceFilePath = Path.Combine(outputDirectory, $"{ns.Name}.md");
                WriteMarkdownWithToc(namespaceFilePath, namespaceMarkdownBuilder);
            }

            // 名前空間の一覧ファイルを作成
            var namespaceListFilePath = Path.Combine(outputDirectory, "Namespaces.md");
            var namespacesList = new StringBuilder();
            namespacesList.AppendLine("# Namespaces\n");

            // 名前空間一覧を作成
            foreach (var ns in project.Namespaces.Values)
            {
                namespacesList.AppendLine($"- [{ns.Name}](./{ns.Name}.md)");
            }

            // 名前空間一覧ファイルを保存
            WriteMarkdownWithToc(namespaceListFilePath, namespacesList);

            Console.WriteLine("Markdown files have been generated.");
        }

        private static void WriteTypeMarkdown(TypeModel type, string namespaceName, string outputDirectory)
        {
            var typeMarkdownBuilder = new StringBuilder();
            typeMarkdownBuilder.AppendLine($"# {type.Accessibility} {type.Kind} {type.Name}");

            if (!string.IsNullOrEmpty(type.XmlComment))
                typeMarkdownBuilder.AppendLine(XmlParser.ToMarkdown(type.XmlComment) + "\n");

            if (type.Kind == "enum")
            {
                var enumMembers = type.Members.OrderBy(m => m.Name).ToList();

                if (enumMembers.Any())
                {
                    typeMarkdownBuilder.AppendLine("## Members");
                    foreach (var member in enumMembers)
                    {
                        typeMarkdownBuilder.AppendLine($" - **{member.Name}**");
                        if (!string.IsNullOrEmpty(member.XmlComment))
                            typeMarkdownBuilder.AppendLine(XmlParser.ToMarkdown(member.XmlComment));
                    }
                }
            }
            else
            {
                // メンバーをフィールドとメソッドに分けて表示
                var fields = type.Members
                    .Where(m => m.Kind == "field")
                    .OrderBy(m => m.Name)
                    .ToList();

                var methods = type.Members
                    .Where(m => m.Kind == "method")
                    .OrderBy(m => m.Name)
                    .ToList();

                if (fields.Any())
                {
                    typeMarkdownBuilder.AppendLine("## Fields");
                    foreach (var field in fields)
                    {
                        typeMarkdownBuilder.AppendLine($"### **{field.Type} {field.Name}**");
                        if (!string.IsNullOrEmpty(field.XmlComment))
                            typeMarkdownBuilder.AppendLine(XmlParser.ToMarkdown(field.XmlComment));
                    }
                    typeMarkdownBuilder.AppendLine();
                }

                if (methods.Any())
                {
                    typeMarkdownBuilder.AppendLine("## Methods");
                    foreach (var method in methods)
                    {
                        typeMarkdownBuilder.AppendLine($"### **{method.Type} {method.Name}**");
                        if (!string.IsNullOrEmpty(method.XmlComment))
                            typeMarkdownBuilder.AppendLine(XmlParser.ToMarkdown(method.XmlComment));
                    }
                }
            }

            var typeFilePath = Path.Combine(outputDirectory, $"{namespaceName}_{type.Name}.md");
            WriteMarkdownWithToc(typeFilePath, typeMarkdownBuilder);
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

            toc.AppendLine(); // 空行
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
