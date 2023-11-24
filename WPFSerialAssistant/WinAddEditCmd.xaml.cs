using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Bonn.Helper;
using NLog;
using NLog.Fluent;

namespace WPFSerialAssistant
{
    /// <summary>
    /// WiinAddEditCmd.xaml 的交互逻辑
    /// </summary>
    public partial class WinAddEditCmd : Window
    {
        public WinAddEditCmd()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 1 新增 2 修改
        /// </summary>
        public int OpType
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int CmdIndex;

        /// <summary>
        /// 
        /// </summary>
        private readonly Logger log = LogManager.GetCurrentClassLogger();


        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (OpType == 1)
                {
                    // 新增
                    MainWindow.Config.batchCmd.Add(new BatchSendCmd()
                    {
                        Name = "测试二",
                        SendBuff = "11 22 33",
                        DelayMs = 100
                    });
                }
                else
                {
                    // 修改
                    MainWindow.Config.batchCmd[CmdIndex].Name = TbxName.Text.Trim();
                    MainWindow.Config.batchCmd[CmdIndex].SendBuff = TbxSendBuff.Text.Trim();
                    if (RadioHex.IsChecked.HasValue && RadioHex.IsChecked.Value)
                        MainWindow.Config.batchCmd[CmdIndex].DataType = 2;
                    else
                        MainWindow.Config.batchCmd[CmdIndex].DataType = 1;
                    MainWindow.Config.batchCmd[CmdIndex].OrderNo = TbxOrderNo.Text.Trim().ToInt();
                    MainWindow.Config.batchCmd[CmdIndex].DelayMs = TbxDelayMs.Text.Trim().ToInt();

                    XmlHelper.XmlSerializeToFile(MainWindow.Config, MainWindow.ConfigPath);

                    Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                MessageBox.Show(this, ex.Message);
            }

        }
    }
}
