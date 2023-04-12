using System.Collections.Generic;

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


        /// <summary>
        /// 
        /// </summary>
        public List<BatchSendCmd> batchCmd = new List<BatchSendCmd>();

        /// <summary>
        /// foup命令列表
        /// </summary>
        public List<BatchSendCmd> FoupCmd = new List<BatchSendCmd>();

        /// <summary>
        /// 
        /// </summary>
        public List<AutoBackRule> AutoBackRule = new List<AutoBackRule>();


    }
}
