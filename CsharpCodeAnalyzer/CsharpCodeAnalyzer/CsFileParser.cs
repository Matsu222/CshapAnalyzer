using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace CsharpCodeAnalyzer
{
    class CsFileParser
    {
        static public ParsedProject ParseFilesToProject(IEnumerable<string> filePaths)
        {
            var project = new ParsedProject()
            {
                Namespaces = new Dictionary<string, NamespaceModel>()
            };

            foreach (var filePath in filePaths)
            {
                var parsedFile = ParseFile(filePath);

                foreach (var ns in parsedFile.Namespaces)
                {
                    // 同じ namespace があれば取得、なければ作成
                    if (!project.Namespaces.TryGetValue(ns.Name, out var existingNamespace))
                    {
                        existingNamespace = new NamespaceModel
                        {
                            Name = ns.Name,
                            Types = new List<TypeModel>()
                        };
                        project.Namespaces[ns.Name] = existingNamespace;
                    }

                    foreach (var type in ns.Types)
                    {
                        // 同じ名前・種類（class/enumなど）の型がすでに存在するか確認
                        var existingType = existingNamespace.Types
                            .FirstOrDefault(t => t.Name == type.Name && t.Kind == type.Kind);

                        // もし partial 同士ならマージ
                        if (existingType != null && type.IsPartial && existingType.IsPartial)
                        {
                            existingType.Members.AddRange(type.Members);

                            // XMLコメントなどのマージは要件に応じて（ここでは保持されてるものを優先）
                            if (string.IsNullOrEmpty(existingType.XmlComment) && !string.IsNullOrEmpty(type.XmlComment))
                            {
                                existingType.XmlComment = type.XmlComment;
                            }
                        }
                        else
                        {
                            // 新しい型として追加
                            existingNamespace.Types.Add(type);
                        }
                    }
                }
            }

            return project;
        }

        public static ParsedFile ParseFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var parsedFile = new ParsedFile
            {
                FilePath = filePath,
                Namespaces = new List<NamespaceModel>()
            };

            NamespaceModel currentNamespace = null;
            TypeModel currentType = null;
            var xmlCommentBuffer = new List<string>();
            bool insideEnum = false;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                if (IsXmlComment(trimmed))
                {
                    BufferXmlComment(trimmed, xmlCommentBuffer);
                    continue;
                }

                if (IsLineComment(trimmed)) continue;

                if (TryParseNamespace(line, parsedFile, out var newNamespace))
                {
                    currentNamespace = newNamespace;
                    currentType = null;
                    xmlCommentBuffer.Clear();
                    insideEnum = false;
                    continue;
                }

                if (TryParseTypeDeclaration(line, xmlCommentBuffer, out var newType, ref insideEnum))
                {
                    currentType = newType;
                    currentNamespace?.Types.Add(currentType);
                    continue;
                }

                if (insideEnum && currentType != null)
                {
                    if (trimmed.StartsWith("}"))
                    {
                        insideEnum = false;
                        continue;
                    }

                    TryParseEnumMember(line, currentType, xmlCommentBuffer);
                    continue;
                }

                if (currentType != null)
                {
                    TryParseMember(line, currentType, xmlCommentBuffer);
                }
            }

            return parsedFile;
        }

        private static bool IsXmlComment(string line) => line.StartsWith("///");

        private static void BufferXmlComment(string line, List<string> buffer)
        {
            buffer.Add(line.Substring(3).Trim());
        }

        private static bool IsLineComment(string line) => line.StartsWith("//");

        private static bool TryParseNamespace(string line, ParsedFile file, out NamespaceModel ns)
        {
            var regex = new Regex(@"^\s*namespace\s+([a-zA-Z0-9_.]+)");
            var match = regex.Match(line);

            if (match.Success)
            {
                ns = new NamespaceModel
                {
                    Name = match.Groups[1].Value,
                    Types = new List<TypeModel>()
                };
                file.Namespaces.Add(ns);
                return true;
            }

            ns = null;
            return false;
        }

        private static bool TryParseTypeDeclaration(string line, List<string> xmlCommentBuffer, out TypeModel type, ref bool isEnum)
        {
            var regex = new Regex(@"^\s*(public|protected|internal|private|static|readonly|virtual|abstract|\s)*\s*(partial\s+)?(class|struct|enum)\s+([a-zA-Z0-9_]+)");
            var match = regex.Match(line);

            if (match.Success)
            {
                var access = string.IsNullOrEmpty(match.Groups[1].Value) ? "internal" : match.Groups[1].Value;
                var kind = match.Groups[3].Value;
                var name = match.Groups[4].Value;

                type = new TypeModel
                {
                    Name = name,
                    Kind = kind,
                    Accessibility = access,
                    Members = new List<MemberModel>(),
                    XmlComment = string.Join(" ", xmlCommentBuffer),
                    IsPartial = line.Contains("partial")
                };

                xmlCommentBuffer.Clear();
                isEnum = kind == "enum";
                return true;
            }

            type = null;
            return false;
        }

        private static void TryParseEnumMember(string line, TypeModel currentType, List<string> xmlCommentBuffer)
        {
            var regex = new Regex(@"^\s*([a-zA-Z0-9_]+)\s*(=\s*[^,]+)?\s*,?");
            var match = regex.Match(line);

            if (match.Success)
            {
                var name = match.Groups[1].Value;

                currentType.Members.Add(new MemberModel
                {
                    Name = name,
                    Kind = "enumMember",
                    Type = currentType.Name,
                    Accessibility = "public",
                    XmlComment = string.Join(" ", xmlCommentBuffer)
                });

                xmlCommentBuffer.Clear();
            }
        }

        private static void TryParseMember(string line, TypeModel currentType, List<string> xmlCommentBuffer)
        {
            //var regex = new Regex(@"^\s*(public|protected|internal|private)\s+([a-zA-Z0-9_<>,\[\]\s]+)\s+([a-zA-Z0-9_]+)\s*(\(|\{|;)?");
            var regex = new Regex(@"^\s*(public|protected|internal|private)\s+([a-zA-Z0-9_<>,\[\]\s]+)\s+([a-zA-Z0-9_]+)\s*(\(([^)]*)\)|\{|;)");
            var argsRegex = new Regex(@"\s*\(([^)]*)\)");

            var match = regex.Match(line);
            if (match.Success)
            {
                var access = match.Groups[1].Value;
                var type = match.Groups[2].Value.Trim();
                var name = match.Groups[3].Value.Trim() + match.Groups[4].Value;
                //var isMethod = match.Groups[4].Value == "(";
                var isMethod = argsRegex.Match(match.Groups[4].Value).Success;

                currentType.Members.Add(new MemberModel
                {
                    Name = name,
                    Kind = isMethod ? "method" : "field",
                    Type = type,
                    Accessibility = access,
                    XmlComment = string.Join(" ", xmlCommentBuffer)
                });

                xmlCommentBuffer.Clear();
            }
        }

        public static void RemovePrivateMembersAndComments(ParsedProject project)
        {
            foreach (var ns in project.Namespaces.Values)
            {
                foreach (var type in ns.Types)
                {
                    type.Members = type.Members
                        .Where(m => m.Accessibility == "public" || m.Accessibility == "protected")
                        .ToList();
                }
            }
        }
    }
}
