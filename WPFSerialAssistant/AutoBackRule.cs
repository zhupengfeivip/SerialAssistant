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
    public class AutoBackRule
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable;

        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description;

        /// <summary>
        /// 接收到的数据包
        /// </summary>
        public string RecvBuff;

        /// <summary>
        /// 要返回的数据包
        /// </summary>
        public string BackBuff;

        /// <summary>
        /// 
        /// </summary>
        public string Remark;
    }
}
