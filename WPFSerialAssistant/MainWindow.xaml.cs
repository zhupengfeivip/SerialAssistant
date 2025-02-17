﻿using Bonn.Helper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NLog;

namespace WPFSerialAssistant
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region 变量私有方法

        ///// <summary>
        ///// 
        ///// </summary>
        //private string configFile = "default.conf";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ConfigPath = Environment.CurrentDirectory + "\\config.xml";

        /// <summary>
        /// 
        /// </summary>
        public static Config Config = new Config();

        /// <summary>
        /// 数据接收缓冲区
        /// </summary>
        private List<byte> receiveBuffer = new List<byte>();

        /// <summary>
        /// 自动回复
        /// </summary>
        private bool autoReply = false;

        /// <summary>
        /// 更新时间信息
        /// </summary>
        private void UpdateTimeDate()
        {
            string timeDateString = "";
            DateTime now = DateTime.Now;
            timeDateString = string.Format("{0}年{1}月{2}日 {3}:{4}:{5}",
                now.Year,
                now.Month.ToString("00"),
                now.Day.ToString("00"),
                now.Hour.ToString("00"),
                now.Minute.ToString("00"),
                now.Second.ToString("00"));

            timeDateTextBlock.Text = timeDateString;
        }

        /// <summary>
        /// 警告信息提示（一直提示）
        /// </summary>
        /// <param name="message">提示信息</param>
        private void Alert(string message)
        {
            // #FF68217A
            statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x21, 0x2A));
            statusInfoTextBlock.Text = message;
        }

        /// <summary>
        /// 普通状态信息提示
        /// </summary>
        /// <param name="message">提示信息</param>
        private void Information(string message)
        {
            if (serialPort.IsOpen)
            {
                // #FFCA5100
                statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xCA, 0x51, 0x00));
            }
            else
            {
                // #FF007ACC
                statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC));
            }
            statusInfoTextBlock.Text = message;
        }

        /// <summary>
        /// SerialPort对象
        /// </summary>
        private SerialPort serialPort = new SerialPort();

        // 需要一个定时器用来，用来保证即使缓冲区没满也能够及时将数据处理掉，防止一直没有到达
        // 阈值，而导致数据在缓冲区中一直得不到合适的处理。
        private DispatcherTimer checkTimer = new DispatcherTimer();

        /// <summary>
        /// 用于更新时间的定时器
        /// </summary>
        private DispatcherTimer clockTimer = new DispatcherTimer();

        /// <summary>
        /// 用于自动发送串口数据的定时器
        /// </summary>
        private DispatcherTimer autoSendDataTimer = new DispatcherTimer();

        // 保存面板的显示状态
        private Stack<Visibility> panelVisibilityStack = new Stack<Visibility>(3);

        // 一个阈值，当接收的字节数大于这么多字节数之后，就将当前的buffer内容交由数据处理的线程
        // 分析。这里存在一个问题，假如最后一次传输之后，缓冲区并没有达到阈值字节数，那么可能就
        // 没法启动数据处理的线程将最后一次传输的数据处理了。这里应当设定某种策略来保证数据能够
        // 在尽可能短的时间内得到处理。
        private const int THRESH_VALUE = 128;

        private bool shouldClear = true;

        //private string appendContent = "\n";

        /// <summary>
        /// 接收并显示的方式
        /// </summary>
        private ReceiveMode receiveMode = ReceiveMode.Character;

        /// <summary>
        /// 发送的方式
        /// </summary>
        private SendMode _sendMode = SendMode.Character;

        /// <summary>
        /// 
        /// </summary>
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private void LogSend(string byteStr)
        {
            BuffAppendRichTextBox(byteStr, 1);
        }

        private void LogSend(byte[] bytes)
        {
            string byteStr = Utilities.BytesToText(bytes.ToList(), receiveMode, serialPort.Encoding);
            BuffAppendRichTextBox(byteStr, 1);
        }

        private void LogRx(byte[] bytes)
        {
            string byteStr = Utilities.BytesToText(bytes.ToList(), receiveMode, serialPort.Encoding);
            BuffAppendRichTextBox(byteStr, 2);
        }

        private void LogRx(string byteStr)
        {
            BuffAppendRichTextBox(byteStr, 2);
        }

        private void LogError(string byteStr)
        {
            BuffAppendRichTextBox(byteStr, 99);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="buff"></param>
        private void BuffAppendRichTextBox(string byteStr, byte dataType = 3)
        {
            string typeStr;
            Color showColor;
            if (dataType == 1)
            {
                // 发送
                typeStr = "发送";
                showColor = Colors.Green;
            }
            else if (dataType == 2)
            {
                typeStr = "接收";
                showColor = Colors.Blue;
            }
            else if (dataType == 99)
            {
                typeStr = "异常";
                showColor = Colors.Red;
            }
            else
            {
                typeStr = "普通";
                showColor = Colors.Black;
            }
            log.Debug($"[{typeStr}] {byteStr}");

            Dispatcher.Invoke(new Action(() =>
            {
                // 根据显示模式显示接收到的字节.
                string msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}][{typeStr}] {byteStr}";

                Paragraph p = new Paragraph(new Run(msg));
                p.FontSize = Config.LogFontSize;
                // p.LineHeight = 20;
                p.Margin = new Thickness(0);
                p.Foreground = new SolidColorBrush(showColor);
                RichTextBoxMsg.Document.Blocks.Add(p);
                RichTextBoxMsg.ScrollToEnd();
            }));
        }

        private void AutoSendData()
        {
            //bool ret = SendData();

            //if (ret == false)
            //{
            //    return;
            //}

            // 启动自动发送定时器
            StartAutoSendDataTimer(GetAutoSendDataInterval());

            // 提示处于自动发送状态
            progressBar.Visibility = Visibility.Visible;
            Information("串口自动发送数据中...");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private int GetAutoSendDataInterval()
        {
            int interval = 1000;

            if (int.TryParse(autoSendIntervalTextBox.Text.Trim(), out interval) == true)
            {
                string select = timeUnitComboBox.Text.Trim();

                switch (select)
                {
                    case "毫秒":
                        break;
                    case "秒钟":
                        interval *= 1000;
                        break;
                    case "分钟":
                        interval = interval * 60 * 1000;
                        break;
                    default:
                        break;
                }
            }

            return interval;
        }

        /// <summary>
        /// 
        /// </summary>
        private void BuildBatchCmdControl()
        {
            for (int i = 0; i < Config.batchCmd.Count; i++)
            {
                BuildBatchCmdControl(i, Config.batchCmd[i]);
            }
        }

        private void AddBatchCmdControl(BatchSendCmd batchCmd)
        {
            BuildBatchCmdControl(Config.AutoBackRule.Count - 1, batchCmd);
        }

        /// <summary>
        /// 
        /// </summary>
        private void BuildBatchCmdControl(int i, BatchSendCmd batchCmd)
        {
            RowDefinition row1 = new RowDefinition();   //实例化一个Grid行
            row1.Height = new GridLength(30);
            GdBatchCmd.RowDefinitions.Add(row1);

            Thickness tn = new Thickness(2, 5, 10, 5);
            int colIndex = 0;
            int rowIndex = i;

            TextBlock lbl = new TextBlock();
            lbl.Text = i + 1 + ". ";
            lbl.Padding = tn;
            Grid.SetRow(lbl, rowIndex);
            Grid.SetColumn(lbl, colIndex++);
            GdBatchCmd.Children.Add(lbl);

            CheckBox cbx = new CheckBox();
            Grid.SetRow(cbx, rowIndex);
            Grid.SetColumn(cbx, colIndex++);
            GdBatchCmd.Children.Add(cbx);

            TextBox tbx = new TextBox();
            tbx.Text = batchCmd.SendBuff;
            tbx.Padding = tn;
            tbx.Tag = i;
            tbx.MouseDoubleClick += TbxEditBatchCmd_MouseDoubleClick;
            Grid.SetRow(tbx, rowIndex);
            Grid.SetColumn(tbx, colIndex++);
            GdBatchCmd.Children.Add(tbx);

            Button btn = new Button();
            btn.Name = $"btnBatchCmd{i}";
            btn.Content = batchCmd.Name;
            btn.Padding = tn;
            btn.Tag = Config.batchCmd[i];
            btn.Click += batchSendCmd_Click;
            Grid.SetRow(btn, rowIndex);
            Grid.SetColumn(btn, colIndex++);
            GdBatchCmd.Children.Add(btn);

            tbx = new TextBox();
            tbx.Text = batchCmd.OrderNo.ToString();
            tbx.Padding = tn;
            Grid.SetRow(tbx, rowIndex);
            Grid.SetColumn(tbx, colIndex++);
            GdBatchCmd.Children.Add(tbx);

            tbx = new TextBox();
            tbx.Text = batchCmd.DelayMs.ToString();
            tbx.Padding = tn;
            Grid.SetRow(tbx, rowIndex);
            Grid.SetColumn(tbx, colIndex++);
            GdBatchCmd.Children.Add(tbx);
        }

        private void TbxEditBatchCmd_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox tbx = sender as TextBox;
            if (tbx == null) return;

            int index = Convert.ToInt32(tbx.Tag);

            WinAddEditCmd win = new WinAddEditCmd();
            win.CmdIndex = index;
            win.TbxName.Text = Config.batchCmd[index].Name;
            win.TbxSendBuff.Text = Config.batchCmd[index].SendBuff;
            win.TbxOrderNo.Text = Config.batchCmd[index].OrderNo.ToString();
            win.TbxDelayMs.Text = Config.batchCmd[index].DelayMs.ToString();
            win.RadioHex.IsChecked = Config.batchCmd[index].DataType == 2;
            win.RadioString.IsChecked = Config.batchCmd[index].DataType == 1;
            win.Title = "修改批量发送命令";
            win.Owner = this;
            win.ShowDialog();

            tbx.Text = Config.batchCmd[index].SendBuff;
        }


        /// <summary>
        /// 
        /// </summary>
        private void BuildAutoRelayCmdControl()
        {
            for (int i = 0; i < Config.AutoBackRule.Count; i++)
            {
                BuildAutoRelayCmdControl(i, Config.AutoBackRule[i]);
            }
        }

        private void AddAutoCmdControl(AutoBackRule cmd)
        {
            BuildAutoRelayCmdControl(Config.AutoBackRule.Count - 1, cmd);
        }

        /// <summary>
        /// 
        /// </summary>
        private void BuildAutoRelayCmdControl(int i, AutoBackRule cmd)
        {
            RowDefinition row1 = new RowDefinition();   //实例化一个Grid行
            row1.Height = new GridLength(30);
            GdAutoRelay.RowDefinitions.Add(row1);

            Thickness tn = new Thickness(2, 5, 10, 5);
            int colIndex = 0;
            int rowIndex = i + 0;

            TextBlock lbl = new TextBlock();
            lbl.Text = i + 1 + ". ";
            lbl.Padding = tn;
            Grid.SetRow(lbl, rowIndex);
            Grid.SetColumn(lbl, colIndex++);
            GdAutoRelay.Children.Add(lbl);

            CheckBox cbx = new CheckBox();
            Grid.SetRow(cbx, rowIndex);
            Grid.SetColumn(cbx, colIndex++);
            GdAutoRelay.Children.Add(cbx);

            TextBox tbx = new TextBox();
            tbx.Text = cmd.RecvBuff;
            tbx.ToolTip = cmd.Name;
            tbx.IsReadOnly = true;
            tbx.Padding = tn;
            tbx.Tag = i;
            tbx.MouseDoubleClick += TbxEditAutoCmd_MouseDoubleClick;
            Grid.SetRow(tbx, rowIndex);
            Grid.SetColumn(tbx, colIndex++);
            GdAutoRelay.Children.Add(tbx);
        }

        private void TbxEditAutoCmd_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox tbx = sender as TextBox;
            if (tbx == null) return;

            int index = Convert.ToInt32(tbx.Tag);

            WinAddEditCmd win = new WinAddEditCmd();
            win.TbxName.Text = Config.AutoBackRule[index].Name;
            win.Title = "修改自动回复命令";
            win.Owner = this;
            win.ShowDialog();
        }


        #endregion 变量私有方法

        #region 按钮事件

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    // 配置文件存在
                    Config = XmlHelper.XmlDeserializeFromFile<Config>(ConfigPath);
                }
                else
                {
                    // 配置不文件存在
                    XmlHelper.XmlSerializeToFile(Config, ConfigPath);
                }

                if (Config.FoupCmd.Count == 0)
                {
                    Config.FoupCmd.Add(new BatchSendCmd() { Name = "读取版本信息", SendBuff = "GET:LIOI;" });

                    Config.FoupCmd.Add(new BatchSendCmd() { Name = "MOVEORGN", SendBuff = "MOV:ORGN;" });
                }

                if (Config.batchCmd.Count == 0)
                {
                    // 增加示例数据
                    for (int i = 0; i < 50; i++)
                    {
                        Config.batchCmd.Add(new BatchSendCmd()
                        {
                            Name = "测试一",
                            SendBuff = "11 22 33",
                            DelayMs = 100
                        });
                    }
                }

                if (Config.AutoBackRule.Count == 0)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        Config.AutoBackRule.Add(new AutoBackRule()
                        {
                            Name = "测试自动回复",
                            Description = "测试自动回复",
                            BackBuff = "AA BB CC",
                            RecvBuff = "CC BB AA"
                        });
                    }
                }

                CbxPort.Text = Config.PortName;
                CbxBaudRate.Text = Config.BaudRate.ToString();
                TbxSendData1.Text = Config.SendData1;
                tbxSendData2.Text = Config.SendData2;
                tbxSendData3.Text = Config.SendData3;
                parityComboBox.SelectedIndex = Config.ParityIndex;
                dataBitsComboBox.SelectedIndex = Config.DataBitsIndex;
                stopBitsComboBox.SelectedIndex = Config.StopBitsIndex;
                encodingComboBox.SelectedIndex = Config.EncodingIndex;
                autoSendIntervalTextBox.Text = Config.AutoSendInterval.ToString();
                timeUnitComboBox.SelectedIndex = Config.TimeUnitIndex;
                WindowState = Config.WindowState;
                Width = Config.WindowWidth;
                Height = Config.WindowHeight;
                Top = Config.WindowTop;
                Left = Config.WindowLeft;

                // 面板显示状态
                if (Config.SerialPortConfigPanelVisible)
                {
                    serialSettingViewMenuItem.IsChecked = true;
                    serialPortConfigPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    serialSettingViewMenuItem.IsChecked = false;
                    serialPortConfigPanel.Visibility = Visibility.Collapsed;
                }

                if (Config.AutoSendConfigPanelVisible)
                {
                    autoSendDataSettingViewMenuItem.IsChecked = true;
                    autoSendConfigPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    autoSendDataSettingViewMenuItem.IsChecked = false;
                    autoSendConfigPanel.Visibility = Visibility.Collapsed;
                }

                cbxRcvShowType.SelectedIndex = Config.ReceiveMode;

                _sendMode = Config.SendMode;
                switch (_sendMode)
                {
                    case SendMode.Character:
                        RbtnSendAsc.IsChecked = true;
                        break;
                    case SendMode.Hex:
                        RbtnSendHex.IsChecked = true;
                        break;
                    default:
                        break;
                }
                switch (Config.AppendContent)
                {
                    case "":
                        appendNoneRadioButton.IsChecked = true;
                        break;
                    case "\r":
                        appendReturnRadioButton.IsChecked = true;
                        break;
                    case "\n":
                        appednNewLineRadioButton.IsChecked = true;
                        break;
                    case "\r\n":
                        appendReturnNewLineRadioButton.IsChecked = true;
                        break;
                    default:
                        break;
                }


                // 加载配置信息
                // LoadConfig();

                // 其他模块初始化
                InitClockTimer();
                InitAutoSendDataTimer();
                InitSerialPort();

                // 查找可以使用的端口号
                FindPorts();

                if (Config.ConnOnStart)
                    btnOpenClose.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                // 动态生成按钮
                BuildBatchCmdControl();
                BuildAutoRelayCmdControl();

                log.Debug("System Start...");
            }
            catch (Exception ex)
            {
                log.Error(ex);
                MessageBox.Show(this, ex.Message);
            }
        }

        private void saveSerialDataMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void saveConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //SaveConfig();
            // 状态栏显示保存成功
            Information("配置信息保存成功。");
        }


        private void loadConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //LoadConfig();
            // 状态栏显示加载成功
            Information("配置信息加载成功。");
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void serialSettingViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool state = serialSettingViewMenuItem.IsChecked;

            if (state == false)
            {
                serialPortConfigPanel.Visibility = Visibility.Visible;
            }
            else
            {
                serialPortConfigPanel.Visibility = Visibility.Collapsed;
                if (IsCompactViewMode())
                {
                    serialPortConfigPanel.Visibility = Visibility.Visible;
                    EnterCompactViewMode();
                }
            }

            serialSettingViewMenuItem.IsChecked = !state;
        }

        private void autoSendDataSettingViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool state = autoSendDataSettingViewMenuItem.IsChecked;

            if (state == false)
            {
                autoSendConfigPanel.Visibility = Visibility.Visible;
            }
            else
            {
                autoSendConfigPanel.Visibility = Visibility.Collapsed;
                if (IsCompactViewMode())
                {
                    autoSendConfigPanel.Visibility = Visibility.Visible;
                    EnterCompactViewMode();
                }
            }

            autoSendDataSettingViewMenuItem.IsChecked = !state;
        }

        private void serialCommunicationSettingViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool state = serialCommunicationSettingViewMenuItem.IsChecked;

            if (state == false)
            {
            }
            else
            {

                if (IsCompactViewMode())
                {
                    EnterCompactViewMode();
                }
            }

            serialCommunicationSettingViewMenuItem.IsChecked = !state;
        }

        private void compactViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsCompactViewMode())
            {
                RestoreViewMode();
            }
            else
            {
                EnterCompactViewMode();
            }
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            WPFSerialAssistant.About about = new About();
            about.ShowDialog();
        }

        private void helpMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void openClosePortButton_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                if (ClosePort())
                {
                    btnOpenClose.Content = "打开";
                }
            }
            else
            {
                if (OpenPort())
                {
                    btnOpenClose.Content = "关闭";
                }
            }
        }

        private void findPortButton_Click(object sender, RoutedEventArgs e)
        {
            FindPorts();
        }

        private void autoSendEnableCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (autoSendEnableCheckBox.IsChecked == true)
            {
                Information($"使能串口自动发送功能，发送间隔：{autoSendIntervalTextBox.Text} {timeUnitComboBox.Text.Trim()}。");
            }
            else
            {
                Information("禁用串口自动发送功能。");
                StopAutoSendDataTimer();
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void sendDataButton_Click(object sender, RoutedEventArgs e)
        {
            string sendText = "";
            Button btn = sender as Button;
            if (btn == null) return;
            if (btn.Name.IndexOf("1") >= 0)
                sendText = TbxSendData1.Text.Trim();
            else if (btn.Name.IndexOf("2") >= 0)
                sendText = tbxSendData2.Text.Trim();
            else if (btn.Name.IndexOf("3") >= 0)
                sendText = tbxSendData3.Text.Trim();

            if (_sendMode == SendMode.Character)
                sendText += Config.AppendContent;
            SerialPortWrite(sendText, _sendMode);
        }

        /// <summary>
        /// 自定义指令发送
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void batchSendCmd_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;
            BatchSendCmd sendCmd = btn.Tag as BatchSendCmd;
            if (sendCmd == null) return;

            SerialPortWrite(sendCmd);
        }

        //private void sendFoupCmd_Click(object sender, RoutedEventArgs e)
        //{
        //    Button btn = sender as Button;
        //    if (btn == null) return;
        //    BatchSendCmd sendCmd = btn.Tag as BatchSendCmd;
        //    if (sendCmd == null) return;

        //    LogSend($"{sendCmd.Name} {sendCmd.SendBuff}");
        //    byte[] newCmd = new FoupHelper().GetCmdBuff(sendCmd.SendBuff);
        //    SerialPortWrite(newCmd);
        //}


        private void saveRecvDataButton_Click(object sender, RoutedEventArgs e)
        {
            SaveData(GetSaveDataPath());
        }

        private void clearRecvDataBoxButton_Click(object sender, RoutedEventArgs e)
        {
            RichTextBoxMsg.Document.Blocks.Clear();
        }

        private void recvModeButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (RichTextBoxMsg == null)
            {
                return;
            }

            if (rb != null)
            {
                //
                // TO-DO:
                // 可以将已经存在在文本框中的内容全部转换成指定形式显示，而不是简单地清空
                //
                RichTextBoxMsg.Document.Blocks.Clear();

                switch (rb.Tag.ToString())
                {
                    case "char":
                        receiveMode = ReceiveMode.Character;
                        Information("提示：字符显示模式。");
                        break;
                    case "hex":
                        receiveMode = ReceiveMode.Hex;
                        Information("提示：十六进制显示模式。");
                        break;
                    case "dec":
                        receiveMode = ReceiveMode.Decimal;
                        Information("提示：十进制显示模式。");
                        break;
                    case "oct":
                        receiveMode = ReceiveMode.Octal;
                        Information("提示：八进制显示模式。");
                        break;
                    case "bin":
                        receiveMode = ReceiveMode.Binary;
                        Information("提示：二进制显示模式。");
                        break;
                    default:
                        break;
                }
            }
        }

        ///// <summary>
        ///// 显示接收数据
        ///// </summary>
        //private bool showReceiveData = true;

        //private void showRecvDataCheckBox_Click(object sender, RoutedEventArgs e)
        //{
        //    showReceiveData = (bool)showRecvDataCheckBox.IsChecked;
        //}

        private void sendDataModeRadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb != null)
            {
                switch (rb.Tag.ToString())
                {
                    case "char":
                        _sendMode = SendMode.Character;
                        Information("提示：发送字符文本。");
                        // 将文本框中内容转换成char
                        TbxSendData1.Text = Utilities.ToSpecifiedText(TbxSendData1.Text, SendMode.Character, serialPort.Encoding);
                        break;
                    case "hex":
                        // 将文本框中的内容转换成hex
                        _sendMode = SendMode.Hex;
                        Information("提示：发送十六进制。输入十六进制数据之间用空格隔开，如：1D 2A 38。");
                        TbxSendData1.Text = Utilities.ToSpecifiedText(TbxSendData1.Text, SendMode.Hex, serialPort.Encoding);
                        break;
                    default:
                        break;
                }
            }
        }

        private void manualInputRadioButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void loadFileRadioButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void clearSendDataTextBox_Click(object sender, RoutedEventArgs e)
        {
            TbxSendData1.Clear();
        }

        private void appendRadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null) return;

            Config.AppendContent = GetAppend(rb.Tag.ToString());
            Information("发送追加：" + rb.Content);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetAppend(string type)
        {
            switch (type)
            {
                case "return":
                    Config.AppendContent = "\r";
                    break;
                case "newline":
                    Config.AppendContent = "\n";
                    break;
                case "retnewline":
                    Config.AppendContent = "\r\n";
                    break;
                case "none":
                default:
                    Config.AppendContent = "";
                    break;
            }
            return Config.AppendContent;
        }

        #endregion

        #region Event handler for timers
        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateTimeDate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoSendDataTimer_Tick(object sender, EventArgs e)
        {
            //bool ret = false;
            //ret = SendData();

            //if (ret == false)
            //{
            //    StopAutoSendDataTimer();
            //}
        }

        /// <summary>
        /// 窗口关闭前拦截
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (MessageBox.Show("确认要退出吗？", "小贴士", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
                    e.Cancel = true;    // 取消关闭操作

                // 释放没有关闭的端口资源
                if (serialPort.IsOpen)
                    ClosePort();

                //SaveConfig();

                //写配置文件
                Config.PortName = CbxPort.Text;
                Config.BaudRate = int.Parse(CbxBaudRate.Text);
                Config.ParityIndex = parityComboBox.SelectedIndex;
                Config.DataBitsIndex = dataBitsComboBox.SelectedIndex;
                Config.StopBitsIndex = stopBitsComboBox.SelectedIndex;
                Config.EncodingIndex = encodingComboBox.SelectedIndex;
                Config.AutoSendInterval = autoSendIntervalTextBox.Text.ToInt();
                Config.TimeUnitIndex = timeUnitComboBox.SelectedIndex;
                Config.WindowState = WindowState;
                Config.WindowWidth = Width;
                Config.WindowHeight = Height;
                Config.WindowTop = Top;
                Config.WindowLeft = Left;
                Config.SerialPortConfigPanelVisible = serialSettingViewMenuItem.IsChecked;
                Config.AutoSendConfigPanelVisible = autoSendDataSettingViewMenuItem.IsChecked;
                Config.ReceiveMode = cbxRcvShowType.SelectedIndex;
                Config.SendMode = _sendMode;
                Config.SendData1 = TbxSendData1.Text.Trim();
                Config.SendData2 = tbxSendData2.Text.Trim();
                Config.SendData3 = tbxSendData3.Text.Trim();

                XmlHelper.XmlSerializeToFile(Config, ConfigPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 捕获窗口按键。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+S保存数据
            if (e.Key == Key.S && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
            {
                SaveData(GetSaveDataPath());
            }

            // Ctrl+Enter 进入/退出简洁视图模式
            if (e.Key == Key.Enter && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
            {
                if (IsCompactViewMode())
                {
                    RestoreViewMode();
                }
                else
                {
                    EnterCompactViewMode();
                }
            }

            // Enter发送数据
            if (e.Key == Key.Enter)
            {
                //SendData();
            }
        }

        #endregion

        #region 数据处理

        private void CheckTimer_Tick(object sender, EventArgs e)
        {
            // 触发了就把定时器关掉，防止重复触发。
            StopCheckTimer();

            // 只有没有到达阈值的情况下才会强制其启动新的线程处理缓冲区数据。
            if (receiveBuffer.Count < THRESH_VALUE)
            {
                //recvDataRichTextBox.AppendText("Timeout!\n");
                // 进行数据处理，采用新的线程进行处理。
                Thread dataHandler = new Thread(ReceivedDataHandler);
                dataHandler.Start(receiveBuffer);
            }
        }


        private void ReceivedDataHandler(object obj)
        {
            List<byte> recvBuffer = new List<byte>();
            recvBuffer.AddRange((List<byte>)obj);

            if (recvBuffer.Count == 0) return;

            // 必须应当保证全局缓冲区的数据能够被完整地备份出来，这样才能进行进一步的处理。
            shouldClear = true;

            LogRx(recvBuffer.ToArray());

            // TO-DO：
            // 处理数据，比如解析指令等等
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetSaveDataPath()
        {
            string path = @"data.txt";

            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Title = "选择存储数据的路径...";
            sfd.FileName = string.Format("数据{0}", DateTime.Now.ToString("yyyyMdHHMMss"));
            sfd.Filter = "文本文件|*.txt";

            if (sfd.ShowDialog() == true)
            {
                path = sfd.FileName;
            }

            return path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        private void SaveData(string path)
        {
            try
            {
                using (System.IO.StreamWriter sr = new StreamWriter(path))
                {
                    string text = (new TextRange(RichTextBoxMsg.Document.ContentStart, RichTextBoxMsg.Document.ContentEnd)).Text;

                    sr.Write(text);

                    Information($"成功保存数据到{path}");
                }
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
            }
        }

        #region 串口处理

        /// <summary>
        /// 
        /// </summary>
        private void InitSerialPort()
        {
            serialPort.DataReceived += SerialPort_DataReceived;
            InitCheckTimer();
        }

        /// <summary>
        /// 查找端口
        /// </summary>
        private void FindPorts()
        {
            string[] portList = SerialPort.GetPortNames();
            Array.Sort(portList);
            CbxPort.ItemsSource = portList;
            if (CbxPort.Items.Count > 0)
            {
                if (portList.Contains(Config.PortName))
                {
                    // 取配置文件串口号
                    CbxPort.SelectedValue = Config.PortName;
                }
                else
                {
                    // 没找到时默认选择第一个
                    CbxPort.SelectedIndex = 0;
                }


                CbxPort.IsEnabled = true;
                Information($"查找到可以使用的端口{CbxPort.Items.Count.ToString()}个。");
            }
            else
            {
                CbxPort.IsEnabled = false;
                Alert("Oops，没有查找到可用端口；您可以点击“查找”按钮手动查找。");
            }
        }

        private bool OpenPort()
        {
            bool flag = false;
            ConfigurePort();

            try
            {
                serialPort.Open();
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                Information($"成功打开端口{serialPort.PortName}, 波特率{serialPort.BaudRate.ToString()}。");
                flag = true;

                Title = $"小朱串口调试工具({serialPort.PortName},{serialPort.BaudRate})";
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
            }

            return flag;
        }

        private bool ClosePort()
        {
            bool flag = false;

            try
            {
                StopAutoSendDataTimer();
                progressBar.Visibility = Visibility.Collapsed;
                serialPort.Close();
                Information($"成功关闭端口{serialPort.PortName}。");
                flag = true;

                Title = $"小朱串口调试工具";
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
            }

            return flag;
        }

        private void ConfigurePort()
        {
            serialPort.PortName = CbxPort.Text;
            serialPort.BaudRate = int.Parse(CbxBaudRate.Text);
            serialPort.Parity = GetSelectedParity();
            serialPort.DataBits = GetSelectedDataBits();
            serialPort.StopBits = GetSelectedStopBits();
            serialPort.Encoding = GetSelectedEncoding();
        }

        private Parity GetSelectedParity()
        {
            string select = parityComboBox.Text;

            Parity p = Parity.None;
            if (select.Contains("Odd"))
            {
                p = Parity.Odd;
            }
            else if (select.Contains("Even"))
            {
                p = Parity.Even;
            }
            else if (select.Contains("Space"))
            {
                p = Parity.Space;
            }
            else if (select.Contains("Mark"))
            {
                p = Parity.Mark;
            }

            return p;
        }

        private int GetSelectedDataBits()
        {
            int dataBits = 8;
            int.TryParse(dataBitsComboBox.Text, out dataBits);

            return dataBits;
        }

        private StopBits GetSelectedStopBits()
        {
            StopBits stopBits = StopBits.None;
            string select = stopBitsComboBox.Text.Trim();

            if (select.Equals("1"))
            {
                stopBits = StopBits.One;
            }
            else if (select.Equals("1.5"))
            {
                stopBits = StopBits.OnePointFive;
            }
            else if (select.Equals("2"))
            {
                stopBits = StopBits.Two;
            }

            return stopBits;
        }

        private Encoding GetSelectedEncoding()
        {
            string select = encodingComboBox.Text;
            Encoding enc = Encoding.Default;

            if (select.Contains("UTF-8"))
            {
                enc = Encoding.UTF8;
            }
            else if (select.Contains("ASCII"))
            {
                enc = Encoding.ASCII;
            }
            else if (select.Contains("Unicode"))
            {
                enc = Encoding.Unicode;
            }

            return enc;
        }

        private bool SerialPortWrite(BatchSendCmd cmdDto)
        {
            string newCmd = cmdDto.SendBuff + GetAppend(cmdDto.EndType);
            return SerialPortWrite(newCmd, (SendMode)cmdDto.DataType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textData"></param>
        /// <param name="sendMode"></param>
        /// <returns></returns>
        private bool SerialPortWrite(string textData, SendMode sendMode)
        {
            if (serialPort == null) return false;

            if (string.IsNullOrEmpty(textData))
            {
                Alert("要发送的内容不能为空！");
                return false;
            }
            if (serialPort.IsOpen == false)
            {
                Alert("串口未打开，无法发送数据。");
                return false;
            }

            try
            {
                if (sendMode == SendMode.Character)
                {
                    string sendMsg = textData;
                    serialPort.Write(sendMsg);

                    LogSend($"{sendMsg.Trim()} ({Encoding.UTF8.GetBytes(sendMsg).ByteToHexString()})");
                }
                else if (sendMode == SendMode.Hex)
                {
                    string[] grp = textData.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    List<byte> sendBuff = new List<byte>();

                    foreach (var item in grp)
                        sendBuff.Add(Convert.ToByte(item, 16));

                    serialPort.Write(sendBuff.ToArray(), 0, sendBuff.Count);
                    Trace.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff} 发送数据：{sendBuff.Count}");
                    LogSend(sendBuff.ToArray());
                }

                // 报告发送成功的消息，提示用户。
                Information($"成功发送：{textData.Trim()}");
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return false;
            }

            return true;
        }

        private bool SerialPortWrite(byte[] sendData)
        {
            if (serialPort == null) return false;

            if (sendData.Length == 0)
            {
                Alert("要发送的内容不能为空！");
                return false;
            }
            if (serialPort.IsOpen == false)
            {
                Alert("串口未打开，无法发送数据。");
                return false;
            }

            try
            {
                serialPort.Write(sendData, 0, sendData.Length);
                Trace.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff} 发送数据：{sendData}");
                LogSend(sendData);
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 更新：采用一个缓冲区，当有数据到达时，把字节读取出来暂存到缓冲区中，缓冲区到达定值
        /// 时，在显示区显示数据即可。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!(sender is SerialPort sp)) return;

            // 收到数据后等待100ms，让数据接收完
            Thread.Sleep(100);

            // 临时缓冲区将保存串口缓冲区的所有数据
            var bytesToRead = sp.BytesToRead;
            var readBuff = new byte[bytesToRead];

            // 将缓冲区所有字节读取出来
            sp.Read(readBuff, 0, bytesToRead);

            if (autoReply)
            {
                AutoBackRule backRule = getBackBuff(readBuff);
                if (backRule == null)
                {
                    LogRx(readBuff.ToArray());
                    LogError("未找到回复命令，请在配置文件中配置");
                }
                else
                {
                    LogRx($"{backRule.Name},{backRule.Description},{readBuff.ToArray().ByteToHexString()}");

                    var backBuff = backRule.BackBuff.Replace(" ", "").ToBytes();
                    sp.Write(backBuff, 0, backBuff.Length);
                    LogSend($"{backRule.Name},{backRule.Description},{backBuff.ToArray().ByteToHexString()}");
                }
            }
            else
            {
                LogRx(readBuff.ToArray());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recvBuff"></param>
        /// <returns></returns>
        private AutoBackRule getBackBuff(byte[] recvBuff)
        {
            string byteStr = ByteExp.ByteToString(recvBuff);
            byteStr = byteStr.Replace(" ", "");
            foreach (var item in Config.AutoBackRule)
            {
                if (byteStr == item.RecvBuff.Replace(" ", ""))
                {
                    return item;
                }
            }
            return null;
        }

        #endregion 串口处理

        #region 定时器
        /// <summary>
        /// 超时时间为50ms
        /// </summary>
        private const int TIMEOUT = 50;
        private void InitCheckTimer()
        {
            // 如果缓冲区中有数据，并且定时时间达到前依然没有得到处理，将会自动触发处理函数。
            checkTimer.Interval = new TimeSpan(0, 0, 0, 0, TIMEOUT);
            checkTimer.IsEnabled = false;
            checkTimer.Tick += CheckTimer_Tick;
        }

        private void StartCheckTimer()
        {
            checkTimer.IsEnabled = true;
            checkTimer.Start();
        }

        private void StopCheckTimer()
        {
            checkTimer.IsEnabled = false;
            checkTimer.Stop();
        }
        #endregion

        /// <summary>
        /// 定时器初始化
        /// </summary>
        private void InitClockTimer()
        {
            clockTimer.Interval = new TimeSpan(0, 0, 1);
            clockTimer.IsEnabled = true;
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();
        }
        private void InitAutoSendDataTimer()
        {
            autoSendDataTimer.IsEnabled = false;
            autoSendDataTimer.Tick += AutoSendDataTimer_Tick;
        }

        private void StartAutoSendDataTimer(int interval)
        {
            autoSendDataTimer.IsEnabled = true;
            autoSendDataTimer.Interval = TimeSpan.FromMilliseconds(interval);
            autoSendDataTimer.Start();
        }

        private void StopAutoSendDataTimer()
        {
            autoSendDataTimer.IsEnabled = false;
            autoSendDataTimer.Stop();
        }
        /// <summary>
        /// 判断是否处于简洁视图模式
        /// </summary>
        /// <returns></returns>
        private bool IsCompactViewMode()
        {
            return autoSendConfigPanel.Visibility == Visibility.Collapsed && autoSendConfigPanel.Visibility == Visibility.Collapsed;
        }

        /// <summary>
        /// 进入简洁视图模式
        /// </summary>
        private void EnterCompactViewMode()
        {
            // 首先需要保持panel的显示状态
            panelVisibilityStack.Push(serialPortConfigPanel.Visibility);
            panelVisibilityStack.Push(autoSendConfigPanel.Visibility);

            // 进入简洁视图模式
            serialPortConfigPanel.Visibility = Visibility.Collapsed;
            autoSendConfigPanel.Visibility = Visibility.Collapsed;

            // 把对应的菜单项取消选中
            serialSettingViewMenuItem.IsChecked = false;
            autoSendDataSettingViewMenuItem.IsChecked = false;
            serialCommunicationSettingViewMenuItem.IsChecked = false;

            // 此时无法视图模式，必须恢复到原先的视图模式才可以
            serialSettingViewMenuItem.IsEnabled = false;
            autoSendDataSettingViewMenuItem.IsEnabled = false;
            serialCommunicationSettingViewMenuItem.IsEnabled = false;

            // 切换至简洁视图模式，菜单项选中
            compactViewMenuItem.IsChecked = true;

            // 
            Information("进入简洁视图模式");
        }

        /// <summary>
        /// 恢复到原来的视图模式
        /// </summary>
        private void RestoreViewMode()
        {
            // 恢复面板显示状态
            autoSendConfigPanel.Visibility = panelVisibilityStack.Pop();
            serialPortConfigPanel.Visibility = panelVisibilityStack.Pop();

            // 恢复菜单选中状态
            if (serialPortConfigPanel.Visibility == Visibility.Visible)
            {
                serialSettingViewMenuItem.IsChecked = true;
            }

            if (autoSendConfigPanel.Visibility == Visibility.Visible)
            {
                autoSendDataSettingViewMenuItem.IsChecked = true;
            }

            serialSettingViewMenuItem.IsEnabled = true;
            autoSendDataSettingViewMenuItem.IsEnabled = true;
            serialCommunicationSettingViewMenuItem.IsEnabled = true;

            compactViewMenuItem.IsChecked = false;

            // 
            Information("恢复原来的视图模式");
        }

        private void showRecvDataCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void showRecvDataCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void cbxAutoReply_Unchecked(object sender, RoutedEventArgs e)
        {
            autoReply = false;
        }

        private void cbxAutoReply_Checked(object sender, RoutedEventArgs e)
        {
            autoReply = true;
        }

        private void cbxRcvShowType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbxRcvShowType.SelectedIndex)
            {
                case 0:
                    receiveMode = ReceiveMode.Character;
                    Information("提示：字符显示模式。");
                    break;
                case 1:
                    receiveMode = ReceiveMode.Hex;
                    Information("提示：十六进制显示模式。");
                    break;
                case 2:
                    receiveMode = ReceiveMode.Decimal;
                    Information("提示：十进制显示模式。");
                    break;
                case 3:
                    receiveMode = ReceiveMode.Octal;
                    Information("提示：八进制显示模式。");
                    break;
                case 4:
                    receiveMode = ReceiveMode.Binary;
                    Information("提示：二进制显示模式。");
                    break;
                default:
                    break;
            }
        }

        private void BtnAddBatchCmd_OnClick(object sender, RoutedEventArgs e)
        {
            WinAddEditCmd win = new WinAddEditCmd();
            win.Owner = this;
            win.Title = "新增命令";
            win.ShowDialog();

            // 保存配置信息到磁盘中
            XmlHelper.XmlSerializeToFile(Config, ConfigPath);

            BatchSendCmd cmd = new BatchSendCmd();

            Config.batchCmd.Add(cmd);
            AddBatchCmdControl(cmd);
        }

        private void BtnSaveConfig_OnClick(object sender, RoutedEventArgs e)
        {
            saveConfigMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
    }



}
