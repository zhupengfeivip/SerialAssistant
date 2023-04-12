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
        public string Name;

        /// <summary>
        /// 
        /// </summary>
        public string sendBuff;

        /// <summary>
        /// 指令类型 
        /// </summary>
        public int dataType = 1;

        /// <summary>
        /// 
        /// </summary>
        public string endType = "none";

        /// <summary>
        /// 延迟时间
        /// </summary>
        public int delayMs;

    }
}
