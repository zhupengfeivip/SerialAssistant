﻿using System.Collections.Generic;
using System.Windows;

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
        /// 串口号
        /// </summary>
        public int BaudRate = 9600;

        /// <summary>
        /// 
        /// </summary>
        public int ParityIndex;

        /// <summary>
        /// 
        /// </summary>
        public int DataBitsIndex;

        /// <summary>
        /// 
        /// </summary>
        public int StopBitsIndex;

        /// <summary>
        /// 
        /// </summary>
        public int EncodingIndex;

        /// <summary>
        /// 获取自动发送数据时间间隔，单位 毫秒
        /// </summary>
        public int AutoSendInterval = 1000;

        /// <summary>
        /// 
        /// </summary>
        public int TimeUnitIndex;

        /// <summary>
        /// 
        /// </summary>
        public WindowState WindowState = WindowState.Normal;

        /// <summary>
        /// 
        /// </summary>
        public double WindowWidth;

        /// <summary>
        /// 
        /// </summary>
        public double WindowHeight;

        /// <summary>
        /// 
        /// </summary>
        public double WindowTop;

        /// <summary>
        /// 
        /// </summary>
        public double WindowLeft;

        /// <summary>
        /// 
        /// </summary>
        public bool SerialPortConfigPanelVisible = true;

        /// <summary>
        /// 
        /// </summary>
        public bool AutoSendConfigPanelVisible = true;

        /// <summary>
        /// 
        /// </summary>
        public int ReceiveMode;

        /// <summary>
        /// 
        /// </summary>
        public SendMode SendMode = SendMode.Hex;

        /// <summary>
        /// 
        /// </summary>
        public string AppendContent = "";

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
        public bool ConnOnStart = true;

        /// <summary>
        /// 显示16进制发送日志，当显示ASCII码发送数据时有效
        /// </summary>
        public bool SendLogType = true;

        /// <summary>
        /// 
        /// </summary>
        public int LogFontSize = 18;

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
