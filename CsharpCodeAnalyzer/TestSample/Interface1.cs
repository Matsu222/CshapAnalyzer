using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSample
{
    /// <summary>
    /// テスト用の公開インターフェース1
    /// </summary>
    public interface Interface1
    {
        /// <summary>
        /// テスト1
        /// </summary>
        /// <returns>結果</returns>
        bool Test1();

        /// <summary>
        /// テスト2
        /// </summary>
        void Test2();

        /// <summary>
        /// テスト3
        /// </summary>
        /// <param name="a">引数1</param>
        /// <param name="b">引数2</param>
        /// <returns>結果</returns>
        double Test3(double a, double b);
    }
}
