using CommonServiceLocator;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Windows;
using System.Windows.Input;

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
            SimpleIoc.Default.Register<HikvisionViewModel>();
            SimpleIoc.Default.Register<LoginViewModel>();
            SimpleIoc.Default.Register<DeviceViewModel>();
            SimpleIoc.Default.Register<ConfigViewModel>();
        }

        #region 实例化
        public static ViewModelLocator Instance = new Lazy<ViewModelLocator>(() =>
           Application.Current.TryFindResource("Locator") as ViewModelLocator).Value;

        public HikvisionViewModel Main => SimpleIoc.Default.GetInstance<HikvisionViewModel>();
        public LoginViewModel Login => ServiceLocator.Current.GetInstance<LoginViewModel>();
        public DeviceViewModel Home => ServiceLocator.Current.GetInstance<DeviceViewModel>();

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
