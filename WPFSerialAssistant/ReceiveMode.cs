using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFSerialAssistant
{
    public enum ReceiveMode
    {
        /// <summary>
        /// 字符显示
        /// </summary>
        Character = 0,

        /// <summary>
        /// 十六进制
        /// </summary>
        Hex = 1,

        /// <summary>
        /// 十进制
        /// </summary>
        Decimal = 2,

        /// <summary>
        /// 八进制
        /// </summary>
        Octal = 3,

        /// <summary>
        /// 二进制
        /// </summary>
        Binary = 4
    }
}
