using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CsharpCodeAnalyzer
{
    /// <summary>
    /// 変換対象のC#プロジェクトの情報クラス
    /// </summary>
    public class ParsedProject
    {
        /// <summary>各名前空間の辞書型</summary>
        public Dictionary<string, NamespaceModel> Namespaces;
    }

    /// <summary>
    /// 変換対象のファイル情報クラス
    /// </summary>
    public class ParsedFile
    {
        /// <summary>対象のファイルパス</summary>
        public string FilePath;
        /// <summary>ファイルに含まれる名前空間の情報リスト</summary>
        public List<NamespaceModel> Namespaces;
    }

    /// <summary>
    /// 名前空間に関する構造情報クラス
    /// </summary>
    public class NamespaceModel
    {
        /// <summary>名称</summary>
        public string Name;
        /// <summary>内部のクラスや構造体の情報リスト</summary>
        public List<TypeModel> Types;
    }

    /// <summary>
    /// クラスや構造体の構造情報クラス
    /// </summary>
    public class TypeModel
    {
        /// <summary>名称</summary>
        public string Name;
        /// <summary>クラスや構造の種類</summary>
        public TypeName Kind;
        /// <summary>アクセス修飾子</summary>
        public string Accessibility;
        /// <summary>xmlタグのコメント</summary>
        public string XmlComment;
        /// <summary>内部のメソッドやフィールド情報リスト</summary>
        public List<MemberModel> Members;
        /// <summary>Partialクラスの場合はtrue</summary>
        public bool IsPartial;

        /// <summary>クラスや構造体の名称リスト</summary>
        public enum TypeName
        {
            Class,
            Struct,
            Enum,
            Interface,
            Unknown
        }
    }

    /// <summary>
    /// メソッドやフィールドの構造情報クラス
    /// </summary>
    public class MemberModel
    {
        /// <summary>メソッドやフィールド名</summary>
        public string Name;
        /// <summary>method, field, enumMember</summary>
        public TypeName Kind;
        public string Type;
        /// <summary>アクセス修飾子</summary>
        public string Accessibility;
        /// <summary>xmlコメント</summary>
        public string XmlComment;

        /// <summary>メソッドやフィールドの種類リスト</summary>
        public enum TypeName
        {
            Method,
            Field,
            EnumMember,
            Unknown
        }
    }

}
