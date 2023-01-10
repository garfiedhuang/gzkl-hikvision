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
    }
}
