using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpCodeAnalyzer
{
    /// <summary>
    /// 
    /// </summary>
    public class ParsedProject
    {
        public Dictionary<string, NamespaceModel> Namespaces { get; set; }
    }

    public class ParsedFile
    {
        public string FilePath { get; set; }
        public List<NamespaceModel> Namespaces { get; set; }
    }

    public class NamespaceModel
    {
        public string Name { get; set; }
        public List<TypeModel> Types { get; set; }
    }

    public class TypeModel
    {
        public string Name { get; set; }
        /// <summary>class, struct, enum</summary>
        public string Kind { get; set; }
        public string Accessibility { get; set; }
        public string XmlComment { get; set; }
        public List<MemberModel> Members { get; set; }
        public  bool IsPartial { get; set; }
    }

    public class MemberModel
    {
        public string Name { get; set; }
        /// <summary>method, field, enumMember</summary>
        public string Kind { get; set; }
        public string Type { get; set; }
        public string Accessibility { get; set; }
        public string XmlComment { get; set; }
    }

}
