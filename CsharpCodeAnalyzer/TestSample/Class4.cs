using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TestSampleOther
{
    /// <summary>
    /// テスト用クラス3
    /// </summary>
    public partial class Class3
    {
        /// <summary>
        /// 公開メソッド4
        /// </summary>
        public void Method4()
        {
        }

        /// <summary>
        /// 公開メソッド5
        /// </summary>
        /// <param name="a">引数1</param>
        /// <param name="s">out引数</param>
        public void Method5(double a, out string s)
        {
            s = "test";
        }

        /// <summary>
        /// 非公開メソッド6
        /// </summary>
        /// <param name="a">引数1</param>
        /// <param name="s">ref引数</param>
        private void Method6(int a, ref string s)
        {

        }
    }
}
