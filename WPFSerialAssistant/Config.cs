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
        public string PortName = "COM1";

        /// <summary>
        /// 
        /// </summary>
        public string SendData1 = "";

        /// <summary>
        /// 
        /// </summary>
        public string SendData2 = "";

        /// <summary>
        /// 
        /// </summary>
        public string SendData3 = "";
    }
}
