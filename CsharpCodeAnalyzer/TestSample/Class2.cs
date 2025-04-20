using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TestSample
{
    /// <summary>
    /// テスト用クラス2
    /// </summary>
    public class Class2
    {
        /// <summary>
        /// 公開メソッド1
        /// </summary>
        public void Method1()
        {
        }

        /// <summary>
        /// 公開メソッド2
        /// </summary>
        /// <param name="a">引数1</param>
        /// <param name="s">out引数</param>
        public void Method2(double a,out string s)
        {
            s = "test";
        }　

        /// <summary>
        /// 非公開メソッド3
        /// </summary>
        /// <param name="a">引数1</param>
        /// <param name="s">ref引数</param>
        private void Method3(int a,ref string s)
        {

        }
    }
}
