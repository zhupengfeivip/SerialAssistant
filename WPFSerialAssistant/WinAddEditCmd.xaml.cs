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




        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {
            if (OpType == 1)
            {
                MainWindow.Config.batchCmd.Add(new BatchSendCmd()
                {
                    Name = "测试二",
                    SendBuff = "11 22 33",
                    DelayMs = 100
                });
            }
            else
            {
                
            }
        }
    }
}
