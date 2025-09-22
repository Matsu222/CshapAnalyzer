using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CsharpCodeAnalyzer
{
    class XmlParser
    {
        /// <summary>
        /// xmlコメントをマークダウン形式の文字列へ変換
        /// </summary>
        /// <param name="xmlComment">xmlコメント文字列</param>
        /// <returns>マークダウン形式の文字列</returns>
        public static string ToMarkdown(string xmlComment)
        {
            if (string.IsNullOrWhiteSpace(xmlComment)) return string.Empty;

            try
            {
                var xml = "<doc>" + xmlComment + "</doc>"; // 擬似ルート
                var doc = XDocument.Parse(xml);
                var builder = new StringBuilder();

                var summary = doc.Descendants("summary").FirstOrDefault();
                if (summary != null)
                {
                    var summaryText = NormalizeXmlText(summary.Value);
                    builder.AppendLine($">{summaryText}");
                    builder.AppendLine();
                }

                foreach (var param in doc.Descendants("param"))
                {
                    var name = param.Attribute("name")?.Value;
                    var value = NormalizeXmlText(param.Value);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        builder.AppendLine($">- **{name}**: {value}");
                    }
                }

                var returns = doc.Descendants("returns").FirstOrDefault();
                if (returns != null)
                {
                    var retText = NormalizeXmlText(returns.Value);
                    builder.AppendLine($"\n>**Returns:** {retText}");
                }

                return builder.ToString().Trim();
            }
            catch
            {
                // XMLとしてパースできなかったらそのまま返す
                return xmlComment.Trim();
            }
        }

        /// <summary>
        /// 空白文字の削除
        /// </summary>
        /// <param name="text">対象文字列</param>
        /// <returns>削除後文字列</returns>
        private static string NormalizeXmlText(string text)
        {
            return Regex.Replace(text, @"\s+", " ").Trim();
        }
    }
}
