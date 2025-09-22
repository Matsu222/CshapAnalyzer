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
    /// <summary>
    /// マークダウンの生成用クラス
    /// </summary>
    class MarkdownManager
    {
        /// <summary>
        /// 対象プロジェクトのマークダウンファイルの出力
        /// </summary>
        /// <param name="project">対象のC#プロジェクト</param>
        /// <param name="outputDirectory">出力先のフォルダ</param>
        public static void OutputToMarkdown(ParsedProject project, string outputDirectory)
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

        /// <summary>
        /// 対象のフォルダを事前に作成、もしくはマークダウンを全削除
        /// </summary>
        /// <param name="outputDirectory">対象</param>
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

        /// <summary>
        /// 対象のフォルダに名前空間の情報についてマークダウンファイルを作成
        /// </summary>
        /// <param name="ns">対象の名前空間情報</param>
        /// <param name="outputDirectory">出力先のフォルダ</param>
        /// <returns></returns>
        private static StringBuilder GenerateNamespaceMarkdown(NamespaceModel ns, string outputDirectory)
        {
            var sb = new StringBuilder();
            AppendBackLink(sb, "Namespaces");
            sb.AppendLine($"# Namespace: {ns.Name}\n");

            WriteTypeSection(sb, ns.Types, TypeModel.TypeName.Class, "Classes", ns.Name, outputDirectory);
            WriteTypeSection(sb, ns.Types, TypeModel.TypeName.Interface, "Interfaces", ns.Name, outputDirectory);
            WriteTypeSection(sb, ns.Types, TypeModel.TypeName.Enum, "Enums", ns.Name, outputDirectory);
            WriteTypeSection(sb, ns.Types, TypeModel.TypeName.Struct, "Structs", ns.Name, outputDirectory);

            AppendBackLink(sb, "Namespaces");
            return sb;
        }

        /// <summary>
        /// 任意の項目を対象にして存在する項目を名前空間の情報として付与しマークダウンのリンクを作成<br/>
        /// 同時にそれぞれの項目の説明用マークダウンを下位フォルダへ生成
        /// </summary>
        /// <param name="sb">名前空間の説明用マークダウン文字列</param>
        /// <param name="types">付与する項目の種類</param>
        /// <param name="kind"></param>
        /// <param name="header">対象項目の見出し</param>
        /// <param name="nsName">対象とする名前空間の名称</param>
        /// <param name="outputDirectory">出力先フォルダ</param>
        private static void WriteTypeSection(StringBuilder sb, List<TypeModel> types, TypeModel.TypeName kind, string header, string nsName, string outputDirectory)
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

        /// <summary>
        /// 対象のフォルダに
        /// </summary>
        /// <param name="type"></param>
        /// <param name="namespaceName"></param>
        /// <param name="outputDirectory"></param>
        private static void WriteTypeMarkdown(TypeModel type, string namespaceName, string outputDirectory)
        {
            var sb = new StringBuilder();

            AppendBackLink(sb, namespaceName);

            sb.AppendLine($"# {type.Accessibility} {type.Kind} {type.Name}");
            if (!string.IsNullOrEmpty(type.XmlComment)) sb.AppendLine(XmlParser.ToMarkdown(type.XmlComment) + "\n");
            if (type.Kind == TypeModel.TypeName.Enum) AppendEnumMembers(sb, type.Members);
            else AppendFieldsAndMethods(sb, type.Members);

            AppendBackLink(sb, namespaceName);

            var filePath = Path.Combine(outputDirectory, $"{namespaceName}_{type.Name}.md");
            WriteMarkdownWithToc(filePath, sb);
        }

        /// <summary>
        /// 追加するメンバーリストからenumを抽出して順番にマークダウン文字列として追加
        /// </summary>
        /// <param name="sb">追加対象の文字列</param>
        /// <param name="members">追加するメンバーリスト</param>
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

        /// <summary>
        /// 追加するメンバーリストからフィールド変数とメソッドを抽出して順番にマークダウン文字列として追加
        /// </summary>
        /// <param name="sb">追加対象の文字列</param>
        /// <param name="members">追加するメンバーリスト</param>
        private static void AppendFieldsAndMethods(StringBuilder sb, List<MemberModel> members)
        {
            var fields = members
                .Where(m => m.Kind == MemberModel.TypeName.Field)
                .OrderBy(m => m.Name)
                .ToList();

            var methods = members
                .Where(m => m.Kind == MemberModel.TypeName.Method)
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

        /// <summary>
        /// 任意のファイルに戻るようなマークダウン用リンクを末尾に付与<br/>
        /// 対象のファイルは同じフォルダにあることが前提です
        /// </summary>
        /// <param name="sb">付与対象の文字列</param>
        /// <param name="BackFilename">付与対象のファイル文字列</param>
        private static void AppendBackLink(StringBuilder sb, string BackFilename)
        {
            sb.AppendLine($"---\n");
            sb.AppendLine($"---\n");
            sb.AppendLine($"[← Back to {BackFilename}](./{BackFilename}.md)\n");
        }

        /// <summary>
        /// 対象のファイルに保存対象のマークダウン文字列の目次を付与して保存
        /// </summary>
        /// <param name="filePath">保存先ファイルパス</param>
        /// <param name="originalContent">保存対象のマークダウン文字列</param>
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

        /// <summary>
        /// マークダウンの全文字列から目次を生成
        /// </summary>
        /// <param name="markdownLines">マークダウンの文字列</param>
        /// <returns>目次用文字列</returns>
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

        /// <summary>
        /// マークダウンのリンクを作成
        /// </summary>
        /// <param name="title">リンクのタイトル</param>
        /// <returns>リンク用アンカー文字列</returns>
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
