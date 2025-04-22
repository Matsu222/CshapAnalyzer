using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSample
{
    /// <summary>
    /// テスト用クラス1
    /// </summary>
    public class Class1
    {
        /// <summary>公開フィールド1</summary>
        public double a;
        /// <summary>公開フィールド2</summary>
        public double b;

        /// <summary>
        /// 公開メソッド1
        /// </summary>
        /// <param name="a">引数1</param>
        /// <param name="b">引数2</param>
        /// <returns>戻り値</returns>
        static public bool Method1(double a, double b)
        {
            return false;
        }

        /// <summary>
        /// 公開メソッド2
        /// </summary>
        /// <returns>戻り値</returns>
        public static string Method2()
        {
            return "test";
        }

        /// <summary>
        /// 非公開メソッド3
        /// </summary>
        /// <param name="s">引数</param>
        /// <returns>戻り値</returns>
        private double Method3(string s)
        {
            return 0;
        }

        /// <summary>
        /// Test用enum
        /// </summary>
        public enum Test
        {
            /// <summary>1つ目</summary>
            enum1,
            /// <summary>2つ目</summary>
            enum2
        }

        /// <summary>
        /// テスト用構造体
        /// </summary>
        public struct TestStr
        {
            /// <summary>構造体メンバー1</summary>
            public double a;
            /// <summary>構造体メンバー2</summary>
            public string s;
        }
    }
}
