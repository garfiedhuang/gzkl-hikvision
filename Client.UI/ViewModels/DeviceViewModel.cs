using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GZKL.Client.UI.Common;
using GZKL.Client.UI.Models;
using HandyControl.Data;
using MessageBox = HandyControl.Controls.MessageBox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;

namespace GZKL.Client.UI.ViewsModels
{
    public class DeviceViewModel : ViewModelBase
    {

        public DeviceViewModel() {

            RbSelectedChangedCmd = new RelayCommand<object>(this.RbSelectedChanged);
        }

        #region Command

        /// <summary>
        /// 注册
        /// </summary>
        public RelayCommand<object> RbSelectedChangedCmd { get; set; }

        #endregion

        #region =====data

        /// <summary>
        /// 设备模型
        /// </summary>
        private DeviceModel model;
        public DeviceModel Model
        {
            get { return model; }
            set { model = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 用户名和密码可见性
        /// </summary>
        private Visibility userNameAndPasswordVisable;
        public Visibility UserNameAndPasswordVisable
        {
            get { return userNameAndPasswordVisable; }
            set { userNameAndPasswordVisable = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 设备端口或通道号标题
        /// </summary>
        private string devicePortOrChannelNoContent="设备端口";
        public string DevicePortOrChannelNoContent
        {
            get { return devicePortOrChannelNoContent; }
            set { devicePortOrChannelNoContent = value; RaisePropertyChanged(); }
        }

        #endregion

        #region =====methods

        private void RbSelectedChanged(object commandParameter)
        {
            var param = commandParameter?.ToString();

            switch (param)
            {
                case "VR"://录像机
                    devicePortOrChannelNoContent = "设备端口";
                    this.userNameAndPasswordVisable = Visibility.Visible;

                    break;
                case "VC"://摄像机
                    devicePortOrChannelNoContent = "通道号";
                    this.userNameAndPasswordVisable = Visibility.Hidden;

                    break;
            }
        }

        #endregion

    }
}
