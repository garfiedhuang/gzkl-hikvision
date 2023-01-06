using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GZKL.Client.UI.Models;
using GZKL.Client.UI.Common;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace GZKL.Client.UI.ViewsModels
{
    public class HikvisionViewModel : ViewModelBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="loginSuccessModel"></param>
        public HikvisionViewModel(LoginSuccessModel loginSuccessModel)
        {
            this.TimeSetCmd = new RelayCommand(this.TimeSet);
            this.RegisterCmd = new RelayCommand(this.Register);
            this.ExitCmd = new RelayCommand(this.Exit);

            this.StartShootingCmd = new RelayCommand(this.StartShooting);
            this.StopShootingCmd = new RelayCommand(this.StopShooting);

            //初始化数据
            InitData(loginSuccessModel);

            //初始化DVR
            InitDvr();

            //登录NVR
            LoginDvr();
        }

        #region =====data

        /// <summary>
        /// NVR模型
        /// </summary>
        private ObservableCollection<NvrData> nvrData;
        public ObservableCollection<NvrData> NvrData
        {
            get { return nvrData; }
            set { nvrData = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// DVR模型
        /// </summary>
        private ObservableCollection<DvrData> dvrData;
        public ObservableCollection<DvrData> DvrData
        {
            get { return dvrData; }
            set { dvrData = value; RaisePropertyChanged(); }
        }

        #endregion

        #region ====cmd

        public RelayCommand TimeSetCmd { get; set; }
        public RelayCommand RegisterCmd { get; set; }
        public RelayCommand ExitCmd { get; set; }


        public RelayCommand StartShootingCmd { get; set; }
        public RelayCommand StopShootingCmd { get; set; }

        #endregion

        /// <summary>
        /// 初始化数据
        /// </summary>
        /// <param name="loginSuccessModel"></param>
        public void InitData(LoginSuccessModel loginSuccessModel)
        {
            try
            {
                SessionInfo.Instance.UserInfo = loginSuccessModel.User;

                //异步获取应用信息：电脑配置和注册信息
                this.GetAppInfo();

                //加载录像机和摄像机配置
                var deviceViewModel = ServiceLocator.Current.GetInstance<DeviceViewModel>();

                deviceViewModel.QueryDevice();

                this.DvrData = deviceViewModel.DvrData;
                this.NvrData = deviceViewModel.NvrData;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"初始化数据失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 初始化DVR
        /// </summary>
        public void InitDvr()
        {
            try
            {
                HikvisionHelper.InitDvr();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"初始化DVR失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 登录DVR
        /// </summary>
        public void LoginDvr()
        {
            try
            {
                var nvrConfig = NvrData.FirstOrDefault();
                if (nvrConfig != null)
                {
                    if (string.IsNullOrEmpty(nvrConfig.DeviceIp))
                    {
                        HandyControl.Controls.Growl.Warning("请先配置录像机设备IP！");
                        return;
                    }
                    if (nvrConfig.DevicePort < 1)
                    {
                        HandyControl.Controls.Growl.Warning("请先配置录像机设备端口！");
                        return;
                    }
                    if (string.IsNullOrEmpty(nvrConfig.UserName))
                    {
                        HandyControl.Controls.Growl.Warning("请先配置录像机用户名！");
                        return;
                    }
                    if (string.IsNullOrEmpty(nvrConfig.Password))
                    {
                        HandyControl.Controls.Growl.Warning("请先配置录像机密码！");
                        return;
                    }
                    HikvisionHelper.LoginDvr(nvrConfig);
                }
                else
                {
                    HandyControl.Controls.Growl.Warning("请先配置录像机设备！");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"登录NVR失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 退出
        /// </summary>
        public void Exit()
        {
            try
            {
                HikvisionHelper.LogoutDvr();

                HikvisionHelper.Dispose();

                Environment.Exit(0);//强制退出
            }
            catch (Exception ex)
            {
                LogHelper.Error($"退出失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 注册
        /// </summary>
        public void Register()
        {
            try
            {
                string msg = string.Empty;
                string fullName = $"{SessionInfo.Instance.ComputerInfo.HostName}-{SessionInfo.Instance.ComputerInfo.CPU}";

                if (string.IsNullOrEmpty(fullName))
                {
                    msg = "电脑信息读取失败，请稍后再试！";
                    LogHelper.Warn(msg);
                    HandyControl.Controls.Growl.Warning(msg);

                    return;
                }

                string sql = "";
                int rowCount = 0;

                //判断是否存在注册信息？
                sql = $"SELECT COUNT(1) FROM [sys_config] WHERE [category]='System-{fullName}' AND [value]='Register' AND [is_deleted]=0";

                rowCount = Convert.ToInt32(OleDbHelper.ExecuteScalar(sql) ?? "0");

                if (rowCount > 0)
                {
                    msg = $"当前电脑【{fullName}】已存在注册记录，请勿重复注册";
                    LogHelper.Warn(msg);
                    HandyControl.Controls.Growl.Warning(msg);

                    return;
                }

                var registerCode = SecurityHelper.DESEncrypt(fullName);
                var registerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var value = string.Empty;
                var text = string.Empty;
                var remark = string.Empty;

                //注册信息写入数据库
                sql = @"INSERT INTO [sys_config]
           ([category],[value],[text],[remark],[is_deleted],[create_dt],[create_user_id],[update_dt],[update_user_id])
     VALUES
           ({0},{1},{2},{3},0,Now(),{4},Now(),{4})";

                for (var i = 0; i < 3; i++)
                {
                    switch (i)
                    {
                        case 0:
                            value = "Register";
                            text = registerCode;
                            remark = registerTime;
                            break;
                        case 1:
                            value = "HostName";
                            text = SessionInfo.Instance.ComputerInfo.HostName;
                            remark = "系统信息";
                            break;
                        case 2:
                            value = "CPU";
                            text = SessionInfo.Instance.ComputerInfo.CPU;
                            remark = "系统信息";
                            break;
                    }

                    sql = string.Format(sql, $"System-{fullName}", value, text, remark, SessionInfo.Instance.UserInfo.Id);

                    OleDbHelper.ExcuteSql(sql);
                }

                msg = $"当前电脑{SessionInfo.Instance.ComputerInfo.HostName}注册成功";

                LogHelper.Info(msg);
                HandyControl.Controls.Growl.Info(msg);

            }
            catch (Exception ex)
            {
                LogHelper.Error(ex?.Message);
                HandyControl.Controls.Growl.Error(ex?.Message);
            }
        }

        /// <summary>
        /// 校时
        /// </summary>
        public void TimeSet()
        {
            try
            {
                HikvisionHelper.TimeSet();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"校时失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 开始录像
        /// </summary>
        public void StartShooting()
        {

        }

        /// <summary>
        /// 结束录像
        /// </summary>
        public void StopShooting()
        {

        }


        #region 私有方法

        /// <summary>
        /// 获取应用信息
        /// </summary>
        private void GetAppInfo()
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    SessionInfo.Instance.ComputerInfo = ComputerInfo.GetInstance().ReadComputerInfo();

                    var fullName = $"{SessionInfo.Instance.ComputerInfo.HostName}-{SessionInfo.Instance.ComputerInfo.CPU}";

                    SessionInfo.Instance.RegisterInfo = RegisterInfo.GetInstance().GetRegisterInfo(fullName);
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"获取应用信息失败，{ex?.Message}");
                }
            }));
        }


        #endregion

    }
}