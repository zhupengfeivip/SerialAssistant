using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            InitCore();
        }


        /// <summary>
        /// 
        /// </summary>
        private string configFile = "default.conf";

        /// <summary>
        /// 
        /// </summary>
        string configPath = Environment.CurrentDirectory + "\\config.ini";

        /// <summary>
        /// 
        /// </summary>
        public static Config Config = new Config();

        /// <summary>
        /// 
        /// </summary>
        private ReadWriteIni.v1.IniHelper ini;

        /// <summary>
        /// 数据接收缓冲区
        /// </summary>
        private List<byte> receiveBuffer = new List<byte>();

        /// <summary>
        /// 核心初始化
        /// </summary>
        private void InitCore()
        {
            ini = new ReadWriteIni.v1.IniHelper(configPath);
            ini.Deserialize(ref Config);

            tbxSendData1.Text = Config.SendData1;
            tbxSendData2.Text = Config.SendData2;
            tbxSendData3.Text = Config.SendData3;

            // 加载配置信息
            LoadConfig();

            // 其他模块初始化
            InitClockTimer();
            InitAutoSendDataTimer();
            InitSerialPort();

            // 查找可以使用的端口号
            FindPorts();
        }

        #region 状态栏
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

        #endregion

        /// <summary>
        /// 显示接收数据
        /// </summary>
        private bool showSendData = true;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="buff"></param>
        private void BuffAppendRichTextBox(byte dataType, byte[] buff)
        {
            string typeStr;
            Color showColor;
            if (dataType == 1)
            {
                if (!showSendData) return;

                // 发送
                typeStr = "发送";
                showColor = Colors.Blue;
            }
            else
            {
                typeStr = "接收";
                showColor = Colors.Red;
            }

            Dispatcher.Invoke(new Action(() =>
            {
                // 根据显示模式显示接收到的字节.
                string byteStr = Utilities.BytesToText(buff.ToList(), receiveMode, serialPort.Encoding);
                string msg = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff")}][{typeStr}]   {byteStr}{Environment.NewLine}";

                Paragraph p = new Paragraph(new Run(msg));
                p.FontSize = 14;
                p.LineHeight = 1;
                p.Foreground = new SolidColorBrush(showColor);
                recvDataRichTextBox.Document.Blocks.Add(p);
                recvDataRichTextBox.ScrollToEnd();

                dataRecvStatusBarItem.Visibility = Visibility.Collapsed;
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool SendData(string sendText)
        {
            if (string.IsNullOrEmpty(sendText))
            {
                Alert("要发送的内容不能为空！");
                return false;
            }

            if (autoSendEnableCheckBox.IsChecked == true)
            {
                return SerialPortWrite(sendText, false);
            }
            else
            {
                return SerialPortWrite(sendText);
            }
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

        #region 配置信息
        // 
        // 目前保存的配置信息如下：
        // 1. 波特率
        // 2. 奇偶校验位
        // 3. 数据位
        // 4. 停止位
        // 5. 字节编码
        // 6. 发送区文本内容
        // 7. 自动发送时间间隔
        // 8. 窗口状态：最大化|高度+宽度
        // 9. 面板显示状态
        // 10. 接收数据模式
        // 11. 是否显示接收数据
        // 12. 发送数据模式
        // 13. 发送追加内容
        //

        /// <summary>
        /// 保存配置信息
        /// </summary>
        private void SaveConfig()
        {
            // 配置对象实例
            Configuration config = new Configuration();

            // 保存波特率
            AddBaudRate(config);

            // 串口号
            config.Add("port", portsComboBox.Text);

            // 保存奇偶校验位
            config.Add("parity", parityComboBox.SelectedIndex);

            // 保存数据位
            config.Add("dataBits", dataBitsComboBox.SelectedIndex);

            // 保存停止位
            config.Add("stopBits", stopBitsComboBox.SelectedIndex);

            // 字节编码
            config.Add("encoding", encodingComboBox.SelectedIndex);

            // 保存发送区文本内容
            config.Add("sendDataTextBoxText", tbxSendData1.Text);

            // 自动发送时间间隔
            config.Add("autoSendDataInterval", autoSendIntervalTextBox.Text);
            config.Add("timeUnit", timeUnitComboBox.SelectedIndex);

            // 窗口状态信息
            config.Add("maxmized", this.WindowState == WindowState.Maximized);
            config.Add("windowWidth", this.Width);
            config.Add("windowHeight", this.Height);
            config.Add("windowLeft", this.Left);
            config.Add("windowTop", this.Top);

            // 面板显示状态
            config.Add("serialPortConfigPanelVisible", serialPortConfigPanel.Visibility == Visibility.Visible);
            config.Add("autoSendConfigPanelVisible", autoSendConfigPanel.Visibility == Visibility.Visible);
            config.Add("serialCommunicationConfigPanelVisible", serialCommunicationConfigPanel.Visibility == Visibility.Visible);

            // 保存接收模式
            config.Add("receiveMode", receiveMode);
            config.Add("showReceiveData", showReceiveData);

            // 保存发送模式
            config.Add("sendMode", sendMode);

            // 保存发送追加
            config.Add("appendContent", appendContent);


            // 保存配置信息到磁盘中
            Configuration.Save(config, configFile);
        }

        /// <summary>
        /// 将波特率列表添加进去
        /// </summary>
        /// <param name="conf"></param>
        private void AddBaudRate(Configuration conf)
        {
            conf.Add("baudRate", baudRateComboBox.Text);
        }

        /// <summary>
        /// 加载配置信息
        /// </summary>
        private bool LoadConfig()
        {
            Configuration config = Configuration.Read(configFile);

            if (config == null)
            {
                return false;
            }

            portsComboBox.Text = config.GetString("port");

            // 获取波特率
            string baudRateStr = config.GetString("baudRate");
            baudRateComboBox.Text = baudRateStr;

            // 获取奇偶校验位
            int parityIndex = config.GetInt("parity");
            parityComboBox.SelectedIndex = parityIndex;

            // 获取数据位
            int dataBitsIndex = config.GetInt("dataBits");
            dataBitsComboBox.SelectedIndex = dataBitsIndex;

            // 获取停止位
            int stopBitsIndex = config.GetInt("stopBits");
            stopBitsComboBox.SelectedIndex = stopBitsIndex;

            // 获取编码
            int encodingIndex = config.GetInt("encoding");
            encodingComboBox.SelectedIndex = encodingIndex;

            //// 获取发送区内容
            //string sendDataText = config.GetString("sendDataTextBoxText");
            //tbxSendData1.Text = sendDataText;

            // 获取自动发送数据时间间隔
            string interval = config.GetString("autoSendDataInterval");
            int timeUnitIndex = config.GetInt("timeUnit");
            autoSendIntervalTextBox.Text = interval;
            timeUnitComboBox.SelectedIndex = timeUnitIndex;

            // 窗口状态
            if (config.GetBool("maxmized"))
            {
                this.WindowState = WindowState.Maximized;
            }
            double width = config.GetDouble("windowWidth");
            double height = config.GetDouble("windowHeight");
            double top = config.GetDouble("windowTop");
            double left = config.GetDouble("windowLeft");
            this.Width = width;
            this.Height = height;
            this.Top = top;
            this.Left = left;

            // 面板显示状态
            if (config.GetBool("serialPortConfigPanelVisible"))
            {
                serialSettingViewMenuItem.IsChecked = true;
                serialPortConfigPanel.Visibility = Visibility.Visible;
            }
            else
            {
                serialSettingViewMenuItem.IsChecked = false;
                serialPortConfigPanel.Visibility = Visibility.Collapsed;
            }

            if (config.GetBool("autoSendConfigPanelVisible"))
            {
                autoSendDataSettingViewMenuItem.IsChecked = true;
                autoSendConfigPanel.Visibility = Visibility.Visible;
            }
            else
            {
                autoSendDataSettingViewMenuItem.IsChecked = false;
                autoSendConfigPanel.Visibility = Visibility.Collapsed;
            }

            if (config.GetBool("serialCommunicationConfigPanelVisible"))
            {
                serialCommunicationSettingViewMenuItem.IsChecked = true;
                serialCommunicationConfigPanel.Visibility = Visibility.Visible;
            }
            else
            {
                serialCommunicationSettingViewMenuItem.IsChecked = false;
                serialCommunicationConfigPanel.Visibility = Visibility.Collapsed;
            }

            // 加载接收模式
            receiveMode = (ReceiveMode)config.GetInt("receiveMode");

            switch (receiveMode)
            {
                case ReceiveMode.Character:
                    recvCharacterRadioButton.IsChecked = true;
                    break;
                case ReceiveMode.Hex:
                    recvHexRadioButton.IsChecked = true;
                    break;
                case ReceiveMode.Decimal:
                    recvDecRadioButton.IsChecked = true;
                    break;
                case ReceiveMode.Octal:
                    recvOctRadioButton.IsChecked = true;
                    break;
                case ReceiveMode.Binary:
                    recvBinRadioButton.IsChecked = true;
                    break;
                default:
                    break;
            }

            showReceiveData = config.GetBool("showReceiveData");
            showRecvDataCheckBox.IsChecked = showReceiveData;

            // 加载发送模式
            sendMode = (SendMode)config.GetInt("sendMode");

            switch (sendMode)
            {
                case SendMode.Character:
                    sendCharacterRadioButton.IsChecked = true;
                    break;
                case SendMode.Hex:
                    sendHexRadioButton.IsChecked = true;
                    break;
                default:
                    break;
            }

            //加载追加内容
            appendContent = config.GetString("appendContent");

            switch (appendContent)
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
            return true;
        }
        #endregion


        #region Global
        // 接收并显示的方式
        private ReceiveMode receiveMode = ReceiveMode.Character;

        // 发送的方式
        private SendMode sendMode = SendMode.Character;

        #endregion

        #region Event handler for menu items
        private void saveSerialDataMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void saveConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            // 状态栏显示保存成功
            Information("配置信息保存成功。");
        }


        private void loadConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
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
                serialCommunicationConfigPanel.Visibility = Visibility.Visible;
            }
            else
            {
                serialCommunicationConfigPanel.Visibility = Visibility.Collapsed;

                if (IsCompactViewMode())
                {
                    serialCommunicationConfigPanel.Visibility = Visibility.Visible;
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
        #endregion

        #region 按钮事件

        private void openClosePortButton_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                if (ClosePort())
                {
                    openClosePortButton.Content = "打开";
                }
            }
            else
            {
                if (OpenPort())
                {
                    openClosePortButton.Content = "关闭";
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
                Information(string.Format("使能串口自动发送功能，发送间隔：{0} {1}。", autoSendIntervalTextBox.Text, timeUnitComboBox.Text.Trim()));
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
            if(btn.Name.IndexOf("1") >= 0)
                sendText = tbxSendData1.Text.Trim();
            else if (btn.Name.IndexOf("2") >= 0)
                sendText = tbxSendData2.Text.Trim();
            else if (btn.Name.IndexOf("3") >= 0)
                sendText = tbxSendData3.Text.Trim();
            SendData(sendText);
        }

        private void saveRecvDataButton_Click(object sender, RoutedEventArgs e)
        {
            SaveData(GetSaveDataPath());
        }

        private void clearRecvDataBoxButton_Click(object sender, RoutedEventArgs e)
        {
            recvDataRichTextBox.Document.Blocks.Clear();
        }

        private void recvModeButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (recvDataRichTextBox == null)
            {
                return;
            }

            if (rb != null)
            {
                //
                // TO-DO:
                // 可以将已经存在在文本框中的内容全部转换成指定形式显示，而不是简单地清空
                //
                recvDataRichTextBox.Document.Blocks.Clear();

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

        /// <summary>
        /// 显示接收数据
        /// </summary>
        private bool showReceiveData = true;

        private void showRecvDataCheckBox_Click(object sender, RoutedEventArgs e)
        {
            showReceiveData = (bool)showRecvDataCheckBox.IsChecked;
        }

        private void sendDataModeRadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb != null)
            {
                switch (rb.Tag.ToString())
                {
                    case "char":
                        sendMode = SendMode.Character;
                        Information("提示：发送字符文本。");
                        // 将文本框中内容转换成char
                        tbxSendData1.Text = Utilities.ToSpecifiedText(tbxSendData1.Text, SendMode.Character, serialPort.Encoding);
                        break;
                    case "hex":
                        // 将文本框中的内容转换成hex
                        sendMode = SendMode.Hex;
                        Information("提示：发送十六进制。输入十六进制数据之间用空格隔开，如：1D 2A 38。");
                        tbxSendData1.Text = Utilities.ToSpecifiedText(tbxSendData1.Text, SendMode.Hex, serialPort.Encoding);
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
            tbxSendData1.Clear();
        }

        private void appendRadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb != null)
            {
                switch (rb.Tag.ToString())
                {
                    case "none":
                        appendContent = "";
                        break;
                    case "return":
                        appendContent = "\r";
                        break;
                    case "newline":
                        appendContent = "\n";
                        break;
                    case "retnewline":
                        appendContent = "\r\n";
                        break;
                    default:
                        break;
                }
                Information("发送追加：" + rb.Content.ToString());
            }
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
                // 释放没有关闭的端口资源
                if (serialPort.IsOpen)
                {
                    ClosePort();
                }

                // 提示是否需要保存配置到文件中
                if (MessageBox.Show("是否在退出前保存软件配置？", "小贴士", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    SaveConfig();

                    //写配置文件
                    Config.PortName = portsComboBox.Text;
                    Config.SendData1 = tbxSendData1.Text.Trim();
                    Config.SendData2 = tbxSendData2.Text.Trim();
                    Config.SendData3= tbxSendData3.Text.Trim();
                    ini.SerializeToFile(Config);
                }
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

        #region EventHandler for serialPort
        // 一个阈值，当接收的字节数大于这么多字节数之后，就将当前的buffer内容交由数据处理的线程
        // 分析。这里存在一个问题，假如最后一次传输之后，缓冲区并没有达到阈值字节数，那么可能就
        // 没法启动数据处理的线程将最后一次传输的数据处理了。这里应当设定某种策略来保证数据能够
        // 在尽可能短的时间内得到处理。
        private const int THRESH_VALUE = 128;

        private bool shouldClear = true;

        /// <summary>
        /// 更新：采用一个缓冲区，当有数据到达时，把字节读取出来暂存到缓冲区中，缓冲区到达定值
        /// 时，在显示区显示数据即可。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = sender as System.IO.Ports.SerialPort;

            if (sp == null) return;

            // 临时缓冲区将保存串口缓冲区的所有数据
            int bytesToRead = sp.BytesToRead;
            byte[] tempBuffer = new byte[bytesToRead];

            Trace.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff} 接收数据：{bytesToRead}");

            // 将缓冲区所有字节读取出来
            sp.Read(tempBuffer, 0, bytesToRead);

            BuffAppendRichTextBox(2, tempBuffer.ToArray());

            //// 检查是否需要清空全局缓冲区先
            //if (shouldClear)
            //{
            //    receiveBuffer.Clear();
            //    shouldClear = false;
            //}

            // 暂存缓冲区字节到全局缓冲区中等待处理
            //receiveBuffer.AddRange(tempBuffer);

            //if (receiveBuffer.Count >= THRESH_VALUE)
            //{
            //    //Dispatcher.Invoke(new Action(() =>
            //    //{
            //    //    recvDataRichTextBox.AppendText("Process data.\n");
            //    //}));
            //    // 进行数据处理，采用新的线程进行处理。
            //    Thread dataHandler = new Thread(new ParameterizedThreadStart(ReceivedDataHandler));
            //    dataHandler.Start(receiveBuffer);
            //}

            //// 启动定时器，防止因为一直没有到达缓冲区字节阈值，而导致接收到的数据一直留存在缓冲区中无法处理。
            //StartCheckTimer();

            //this.Dispatcher.Invoke(new Action(() =>
            //{
            //    if (autoSendEnableCheckBox.IsChecked == false)
            //    {
            //        Information("");
            //    }
            //    dataRecvStatusBarItem.Visibility = Visibility.Visible;
            //}));
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
                Thread dataHandler = new Thread(new ParameterizedThreadStart(ReceivedDataHandler));
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

            BuffAppendRichTextBox(2, recvBuffer.ToArray());

            // TO-DO：
            // 处理数据，比如解析指令等等
        }
        #endregion
    }
}
