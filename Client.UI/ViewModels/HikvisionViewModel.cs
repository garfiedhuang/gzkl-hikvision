using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GZKL.Client.UI.Models;
using GZKL.Client.UI.Common;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data;
using GZKL.Client.UI.Views.SystemMgt.Config;

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
            this.QueryCmd = new RelayCommand(this.Query);

            this.StartPlayBackCmd = new RelayCommand(this.StartPlayBack);
            this.StopPlayBackCmd = new RelayCommand(this.StopPlayBack);
            this.DownloadCmd = new RelayCommand(this.Download);
            this.PausePlayBackCmd = new RelayCommand(this.PausePlayBack);
            this.RecoverPlayBackCmd = new RelayCommand(this.RecoverPlayBack);
            this.FastPlayBackCmd = new RelayCommand(this.FastPlayBack);
            this.SlowPlayBackCmd = new RelayCommand(this.SlowPlayBack);

            this.DataGridSelectionChangedCmd = new RelayCommand<object>(this.DataGridSelectionChanged);

            //初始化数据
            InitData(loginSuccessModel);

            //初始化DVR
            InitDvr();

            //登录NVR
            LoginDvr();
        }

        #region =====data

        /// <summary>
        /// 数据模型
        /// </summary>
        private HikvisionModel model;
        public HikvisionModel Model
        {
            get { return model; }
            set
            {
                model = value;
                RaisePropertyChanged();
                HikvisionHelper.m_lChannel = Convert.ToInt32(value);//设置当前选中的通道
            }
        }

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

        /// <summary>
        /// 检测数据模型
        /// </summary>
        private ObservableCollection<TestData> testData;
        public ObservableCollection<TestData> TestData
        {
            get { return testData; }
            set { testData = value; RaisePropertyChanged(); }
        }

        #endregion

        #region ====cmd

        public RelayCommand TimeSetCmd { get; set; }
        public RelayCommand RegisterCmd { get; set; }
        public RelayCommand ExitCmd { get; set; }
        public RelayCommand QueryCmd { get; set; }
        public RelayCommand StartPlayBackCmd { get; set; }
        public RelayCommand StopPlayBackCmd { get; set; }
        public RelayCommand DownloadCmd { get; set; }
        public RelayCommand PausePlayBackCmd { get; set; }
        public RelayCommand RecoverPlayBackCmd { get; set; }
        public RelayCommand FastPlayBackCmd { get; set; }
        public RelayCommand SlowPlayBackCmd { get; set; }
        public RelayCommand<object> DataGridSelectionChangedCmd { get; set; }

        #endregion

        #region 初始化

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

        #endregion

        #region 录像

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

                this.Model.IsRegister = false;

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

        #endregion

        #region 回放

        /// <summary>
        /// 查询
        /// </summary>
        public void Query()
        {
            try
            {
                if (string.IsNullOrEmpty(Model.TestNo))
                {
                    HandyControl.Controls.Growl.Warning("请输入检测编号！");
                    return;
                }

                var sql = $"SELECT * FROM JCRecord WHERE JCNO LIKE '%{Model.TestNo}%'";

                using (var dt = OleDbHelper.DataTable(sql))
                {
                    if (TestData == null)
                    {
                        TestData = new ObservableCollection<TestData>();
                    }
                    else
                    {
                        TestData.Clear();
                    }

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        return;
                    }

                    foreach (DataRow dr in dt.Rows)
                    {
                        TestData.Add(new Models.TestData()
                        {
                            ChannelNo = Convert.ToInt32(dr["ChannelNo"]),
                            StartDt = Convert.ToDateTime(dr["StartDt"]),
                            EndDt = Convert.ToDateTime(dr["EndDt"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"查询失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 开始回放
        /// </summary>
        private void StartPlayBack()
        {
            try
            {
                var intPtr = Model.MePreview;
                var startDt = Model.SelectedTestData.StartDt.AddSeconds(-0.00012);
                var endDt = Model.SelectedTestData.EndDt.AddSeconds(0.00012);

                var result = HikvisionHelper.StartPlayBack(startDt, endDt, intPtr);

                if (!result)
                {

                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"开始回放失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 停止回放
        /// </summary>
        private void StopPlayBack()
        {
            try
            {
                HikvisionHelper.StopPlayBack();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"停止回放失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 下载
        /// </summary>
        private void Download()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"下载失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 暂停回放
        /// </summary>
        private void PausePlayBack()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"暂停回放失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 恢复回放
        /// </summary>
        private void RecoverPlayBack()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"恢复回放失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 快速回放
        /// </summary>
        private void FastPlayBack()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"快速回放失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 慢速回放
        /// </summary>
        private void SlowPlayBack()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"慢速回放失败，{ex?.Message}");
            }
        }

        /// <summary>
        /// 网格选中事件
        /// </summary>
        /// <param name="selectedItem"></param>
        private void DataGridSelectionChanged(object selectedItem)
        {
            if (selectedItem == null)
            {
                HandyControl.Controls.Growl.Warning("请选择检测记录！");
                return;
            }
            var item = selectedItem as TestData;

            Model.SelectedTestData = item;
        }

        #endregion

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

                    if (!string.IsNullOrEmpty(SessionInfo.Instance.RegisterInfo.RegCode))//已注册
                    {
                        this.Model.IsRegister = false;
                    }
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