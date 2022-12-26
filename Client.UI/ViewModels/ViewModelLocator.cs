using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Windows;

namespace GZKL.Client.UI.ViewsModels
{
    public class ViewModelLocator
    {
        /// <summary>
        /// 嘿巴扎嘿
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<LoginViewModel>();
            SimpleIoc.Default.Register<HomeViewModel>();
            SimpleIoc.Default.Register<ConfigViewModel>();
        }

        #region 实例化
        public static ViewModelLocator Instance = new Lazy<ViewModelLocator>(() =>
           Application.Current.TryFindResource("Locator") as ViewModelLocator).Value;

        public MainViewModel Main => SimpleIoc.Default.GetInstance<MainViewModel>();
        public LoginViewModel Login => ServiceLocator.Current.GetInstance<LoginViewModel>();
        public HomeViewModel Home => ServiceLocator.Current.GetInstance<HomeViewModel>();

        #region 系统管理
        public ConfigViewModel Config => ServiceLocator.Current.GetInstance<ConfigViewModel>();

        #endregion


        #endregion

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}
