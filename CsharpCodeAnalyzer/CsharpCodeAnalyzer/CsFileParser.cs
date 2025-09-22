using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace CsharpCodeAnalyzer
{
    /// <summary>
    /// C#のソースコードの構造解析クラス
    /// </summary>
    class CsFileParser
    {
        /// <summary>
        /// 対象の全ファイルを1つのプロジェクトとして情報を解析する
        /// </summary>
        /// <param name="filePaths">対象のファイルパスのリスト</param>
        /// <returns>プロジェクト全体の情報管理インスタンス</returns>
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
                        var existingType = existingNamespace.Types.FirstOrDefault(t => t.Name == type.Name && t.Kind == type.Kind);

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

        /// <summary>
        /// 対象のファイルをファイル情報管理インスタンスへ変換
        /// </summary>
        /// <param name="filePath">対象のファイルパス</param>
        /// <returns>ファイル情報管理インスタンス</returns>
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
                    BufferXmlComment(trimmed, ref xmlCommentBuffer);
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

                if (TryParseTypeDeclaration(line, ref xmlCommentBuffer, out var newType, ref insideEnum))
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

                    TryParseEnumMember(line, ref currentType, ref xmlCommentBuffer);
                    continue;
                }

                if (currentType != null)
                {
                    TryParseMember(line, ref currentType, ref xmlCommentBuffer);
                }
            }

            return parsedFile;
        }

        /// <summary>
        /// 対象文字列がxml形式のコメント行かどうか確認する
        /// </summary>
        /// <param name="line">対象文字列</param>
        /// <returns>xml形式であればtrue</returns>
        private static bool IsXmlComment(string line) => line.StartsWith("///");

        /// <summary>
        /// 対象文字列コメントをバッファへ格納する
        /// </summary>
        /// <param name="line">対象文字列</param>
        /// <param name="buffer">格納先のバッファ</param>
        private static void BufferXmlComment(string line, ref List<string> buffer)
        {
            // コメントを想定するため先頭にあるスラッシュを削除して格納
            buffer.Add(line.Substring(3).Trim());
        }

        /// <summary>
        /// 1行コメントであるか確認する
        /// </summary>
        /// <param name="line">確認対象の文字列</param>
        /// <returns>1行コメントであればtrue</returns>
        private static bool IsLineComment(string line) => line.StartsWith("//");

        /// <summary>
        /// 対象の文字列が名前空間に関する記述であるか確認する<br/>
        /// 名前空間に関するものであれば情報格納用のインスタンスを生成
        /// </summary>
        /// <param name="line">対象の文字列</param>
        /// <param name="file">ファイルの情報管理用インスタンス</param>
        /// <param name="ns">名前空間の情報格納用インスタンス</param>
        /// <returns>名前空間に関する記述であればtrue</returns>
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

        /// <summary>
        /// 対象の文字列がクラスや構造体に関する記述であるか確認する<br/>
        /// クラスや構造体であれば情報格納用のインスタンスを生成
        /// </summary>
        /// <param name="line">対象文字列</param>
        /// <param name="xmlCommentBuffer">xmlコメントを事前に格納しておくバッファ</param>
        /// <param name="type">クラスや構造体の情報格納用インスタンス</param>
        /// <param name="isEnum">enumに関する記述であればtrue</param>
        /// <returns>クラスや構造体のに関する記述であればtrue</returns>
        private static bool TryParseTypeDeclaration(string line, ref List<string> xmlCommentBuffer, out TypeModel type, ref bool isEnum)
        {
            var regex = BuildTypeRegex();
            var match = regex.Match(line);

            if (match.Success)
            {
                var modifiers = match.Groups["modifier"].Captures.Cast<Capture>().Select(c => c.Value.Trim()).ToList();
                var accessModifiers = new[] { "public", "private", "protected", "internal" };

                // アクセス修飾子（public, protected, internal, private）がない場合はデフォルトで 'internal'
                string access = "internal";
                if (modifiers.Contains("protected") && modifiers.Contains("internal")) access = "protected internal";
                else access = modifiers.FirstOrDefault(m => accessModifiers.Contains(m));

                // 型の種類（class, struct, enum, interface）
                TypeModel.TypeName kind;
                switch (match.Groups[1].Value.ToLower())
                {
                    case "interface": kind = TypeModel.TypeName.Interface; break;
                    case "class": kind = TypeModel.TypeName.Class; break;
                    case "struce": kind = TypeModel.TypeName.Struct; break;
                    case "enum": kind = TypeModel.TypeName.Enum; break;
                    default: kind = TypeModel.TypeName.Unknown; break;
                }

                // クラス名
                var name = match.Groups[2].Value;

                // TypeModel の作成
                type = new TypeModel
                {
                    Name = name,
                    Kind = kind,
                    Accessibility = access,
                    Members = new List<MemberModel>(),
                    XmlComment = string.Join(" ", xmlCommentBuffer),
                    IsPartial = match.Groups[5].Success  // 'partial' が含まれていれば true
                };

                xmlCommentBuffer.Clear();
                isEnum = kind == TypeModel.TypeName.Enum;

                return true;
            }

            type = null;
            return false;
        }

        /// <summary>
        /// 対象の文字列がenumに関する記述であれば情報を追加する
        /// </summary>
        /// <param name="line">対象文字列</param>
        /// <param name="currentType">現在のクラスや構造体の情報インスタンス</param>
        /// <param name="xmlCommentBuffer">xmlコメントを事前に格納しておくバッファ</param>
        private static void TryParseEnumMember(string line, ref TypeModel currentType, ref List<string> xmlCommentBuffer)
        {
            var regex = new Regex(@"^\s*([a-zA-Z0-9_]+)\s*(=\s*[^,]+)?\s*,?");
            var match = regex.Match(line);

            if (match.Success)
            {
                var name = match.Groups[1].Value;

                currentType.Members.Add(new MemberModel
                {
                    Name = name,
                    Kind = MemberModel.TypeName.EnumMember,
                    Type = currentType.Name,
                    Accessibility = "public",
                    XmlComment = string.Join(" ", xmlCommentBuffer)
                });

                xmlCommentBuffer.Clear();
            }
        }

        /// <summary>
        /// 対象の文字列がメソッドやフィールド等に関する記述であれば情報を追加する
        /// </summary>
        /// <param name="line">対象文字列</param>
        /// <param name="currentType">現在のクラスや構造体の情報インスタンス</param>
        /// <param name="xmlCommentBuffer">xmlコメントを事前に格納しておくバッファ</param>
        private static void TryParseMember(string line, ref TypeModel currentType, ref List<string> xmlCommentBuffer)
        {
            var methodRegex = BuildMethodRegex();
            var fieldRegex = BuildFieldRegex();

            var methodMatch = methodRegex.Match(line);
            var fieldMatch = fieldRegex.Match(line);

            if (methodMatch.Success || fieldMatch.Success)
            {
                bool isMethod = methodMatch.Success;
                var match = isMethod ? methodMatch : fieldMatch;

                // デフォルトのアクセス修飾子（interface なら public、それ以外は private）
                string defAccess = currentType.Kind == TypeModel.TypeName.Interface ? "public" : "private";

                // 修飾子の抽出
                var modifiers = match.Groups["modifier"].Captures.Cast<Capture>().Select(c => c.Value.Trim()).ToList();

                var accessModifiers = new[] { "public", "private", "protected", "internal" };

                // アクセス修飾子の判定（protected internal の対応も含む）
                string access = defAccess;
                if (modifiers.Contains("protected") && modifiers.Contains("internal")) access = "protected internal";
                else access = modifiers.FirstOrDefault(m => accessModifiers.Contains(m)) ?? defAccess;

                // 型、名前、引数を抽出
                string type = match.Groups["type"].Value.Trim();
                string methodName = match.Groups["name"].Value.Trim();
                string parameterList = match.Groups["params"].Value.Trim();
                string name = isMethod ? $"{methodName}({parameterList})" : methodName;

                // MemberModel に追加
                currentType.Members.Add(new MemberModel
                {
                    Name = name,
                    Kind = isMethod ? MemberModel.TypeName.Method : MemberModel.TypeName.Field,
                    Type = type,
                    Accessibility = access,
                    XmlComment = string.Join(" ", xmlCommentBuffer)
                });

                xmlCommentBuffer.Clear();
            }
        }

        /// <summary>
        /// クラスや構造体用の正規表現インスタンスを作成
        /// </summary>
        /// <returns>正規表現インスタンス</returns>
        private static Regex BuildTypeRegex()
        {
            // 全修飾子を1つにまとめる
            var allModifiersPattern = @"(?:(?<modifier>public|protected|internal|private|static|readonly|sealed|virtual|abstract|async|extern|partial)\s+)*";
            // クラス、構造体、インターフェース、列挙型
            var typePattern = @"(class|struct|enum|interface)\s+";
            // 名前
            var namePattern = @"([a-zA-Z0-9_]+)";
            // 最終的な正規表現
            var regexPattern = $@"^\s*{allModifiersPattern}{typePattern}{namePattern}";
            return new Regex(regexPattern);
        }


        /// <summary>
        /// メソッド用の正規表現インスタンスを作成
        /// </summary>
        /// <returns>正規表現インスタンス</returns>
        private static Regex BuildMethodRegex()
        {
            var modifiersPattern = @"(?:(?<modifier>public|protected|internal|private|static|sealed|virtual|override|abstract|async|extern)\s+)*";
            var typePattern = @"(?<type>[a-zA-Z0-9_<>,\[\]\s]+)\s+";
            var methodNamePattern = @"(?<name>[a-zA-Z0-9_]+)\s*";
            var parameterPattern = @"\((?<params>[^)]*)\)\s*;?";
            var whitespace = @"\s*";

            var methodPattern = $"^{whitespace}{modifiersPattern}{typePattern}{methodNamePattern}{parameterPattern}";
            return new Regex(methodPattern);
        }

        /// <summary>
        /// フィールド変数用の正規表現インスタンスを作成
        /// </summary>
        /// <returns>正規表現インスタンス</returns>
        private static Regex BuildFieldRegex()
        {
            var modifiersPattern = @"(?:(?<modifier>public|protected|internal|private|static|sealed|virtual|override|abstract|async|extern|readonly|const)\s+)*";
            var typePattern = @"(?<type>[a-zA-Z0-9_<>,\[\]\s]+)\s+";
            var fieldNamePattern = @"(?<name>[a-zA-Z0-9_]+)";
            var initializerPattern = @"(?:\s*=\s*[^;]+)?";
            var whitespace = @"\s*";

            var fieldPattern = $"^{whitespace}{modifiersPattern}{typePattern}{fieldNamePattern}{initializerPattern};";
            return new Regex(fieldPattern);
        }

        /// <summary>
        /// 変換対象から非公開情報を削除
        /// </summary>
        /// <param name="project">変換対象</param>
        public static void RemovePrivateMembersAndComments(ref ParsedProject project)
        {
            foreach (var ns in project.Namespaces.Values)
            {
                ns.Types = ns.Types
                    .Where(m => m.Accessibility == "public" || m.Accessibility == "protected")
                    .ToList();
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
