using ReadWriteIni.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WPFSerialAssistant
{
    /// <summary>
    /// 
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 串口号
        /// </summary>
        [Group(Group = "system", Comment = "串口号")]
        public string PortName { get; set; }

        /// <summary>
        /// 小票打印机
        /// </summary>
        [Group(Group = "system", Comment = "小票打印机")]
        public string NotePrinter { get; set; }
    }
}
