using GZKL.Client.UI.ViewsModels;
using GZKL.Client.UI.Models;
using System.Windows;
using System.Windows.Input;
using GZKL.Client.UI.Views.SystemMgt.Device;
using HandyControl.Tools;
using GZKL.Client.UI.Common;
using System;

namespace GZKL.Client.UI
{
    /// <summary>
    /// Hikvision.xaml 的交互逻辑
    /// </summary>
    public partial class Hikvision : Window
    {
        public Hikvision(LoginSuccessModel loginSuccessModel)
        {
            InitializeComponent();

            this.tbTitle.Text = $"广州昆仑录像检测系统({GetEdition()})";
            this.DataContext = new HikvisionViewModel(loginSuccessModel);
        }

        public static string GetEdition()
        {
            return Application.ResourceAssembly.GetName().Version.ToString();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private void MinWin_click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaxWin_click(object sender, RoutedEventArgs e)
        {
            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseWin_click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as HikvisionViewModel;

            viewModel.Exit();

            //this.Close();
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            var window = new Device();
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void cmbShootingChannel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedValue = this.cmbShootingChannel.SelectedValue.ToString();
            if (!string.IsNullOrEmpty(selectedValue))
            {
                HikvisionHelper.m_lChannel = Convert.ToInt32(selectedValue);
                HikvisionHelper.Preview(this.mePreview.GetHandle());
            }
        }

        /// <summary>
        /// 开始录像
        /// </summary>
        private void btnStartShooting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(this.cmbShootingChannel.SelectedValue.ToString()))
                {
                    HandyControl.Controls.Growl.Warning("请选择通道列表！");
                    return;
                }

                if (string.IsNullOrEmpty(this.txtShootingTestNo.Text))
                {
                    HandyControl.Controls.Growl.Warning("请输入检测编号！");
                    return;
                }

                //设置屏幕窗口显示的字符串
                HikvisionHelper.SetShowString(this.txtShootingTestNo.Text);

                //开始录制视频
                HikvisionHelper.StartDvrRecord();

            }
            catch (Exception ex)
            {
                LogHelper.Error($"开始录像失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 结束录像
        /// </summary>
        private void btnStopShooting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //设置屏幕窗口显示的字符串
                HikvisionHelper.SetShowString(string.Empty);

                //停止录制视频
                var testNo =this.txtShootingTestNo.Text.Trim();
                var nvrName = this.cmbShootingChannel.SelectedItem.ToString();
                var intPtr = this.mePreview.GetHandle();

                HikvisionHelper.StopDvrRecord(testNo,nvrName, intPtr);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"停止录像失败，{ex?.Message}");
            }
        }
    }
}
