using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFSerialAssistant
{
    /// <summary>
    /// 
    /// </summary>
    public class BatchSendCmd
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name = "未命名";

        /// <summary>
        /// 
        /// </summary>
        public string SendBuff = "11 22 33";

        /// <summary>
        /// 指令类型 1 字符；2 HEX
        /// </summary>
        public int DataType = 1;

        /// <summary>
        /// 
        /// </summary>
        public string EndType = "none";

        /// <summary>
        /// 顺序，按从小到大顺序批量发送，0表示不参与批量发送，顺序号相同按前后顺序发送
        /// </summary>
        public int OrderNo = 0;

        /// <summary>
        /// 延迟时间，单位毫秒
        /// </summary>
        public int DelayMs = 1000;

    }
}
