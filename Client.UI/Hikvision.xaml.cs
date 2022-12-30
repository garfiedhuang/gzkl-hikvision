using GZKL.Client.UI.ViewsModels;
using GZKL.Client.UI.Models;
using System.Windows;
using System.Windows.Input;
using GZKL.Client.UI.Views.SystemMgt.Device;

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

            //this.txtTitle.Title = $"XXXXXX系统({GetEdition()})";
            

            this.DataContext = new HikvisionViewModel(loginSuccessModel);
            /*
            Messenger.Default.Register<string>(this, "ExpandMenu", arg =>
            {
                if (this.menu.Width < 200)
                {
                    this.xpUserInfo.Visibility = Visibility.Visible;
                    AnimationHelper.CreateWidthChangedAnimation(this.menu, 60, 200, new TimeSpan(0, 0, 0, 0, 300));
                }
                else
                {
                    this.xpUserInfo.Visibility = Visibility.Collapsed;
                    AnimationHelper.CreateWidthChangedAnimation(this.menu, 200, 60, new TimeSpan(0, 0, 0, 0, 300));
                }

                //由于...
                var template = this.IC.ItemTemplateSelector;
                this.IC.ItemTemplateSelector = null;
                this.IC.ItemTemplateSelector = template;              
            });*/
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
            this.Close();
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            var window = new Device();
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
    }
}
