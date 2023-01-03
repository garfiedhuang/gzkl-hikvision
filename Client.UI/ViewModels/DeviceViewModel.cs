using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GZKL.Client.UI.Common;
using static GZKL.Client.UI.Common.CHCNetSDK;
using GZKL.Client.UI.Models;
using System.Windows;
using System;
using System.Data;
using System.Linq;
using System.Collections.ObjectModel;

namespace GZKL.Client.UI.ViewsModels
{
    public class DeviceViewModel : ViewModelBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DeviceViewModel()
        {
            RbSelectedChangedCmd = new RelayCommand<object>(this.RbSelectedChanged);
            AddDeviceCmd = new RelayCommand(this.AddDevice);
            ResetDeviceCmd = new RelayCommand(this.ResetDevice);


            QueryDevice();
        }

        #region Command

        /// <summary>
        /// 设备类型选择改变命令
        /// </summary>
        public RelayCommand<object> RbSelectedChangedCmd { get; set; }

        /// <summary>
        /// 新增设备命令
        /// </summary>
        public RelayCommand AddDeviceCmd { get; set; }

        /// <summary>
        /// 清空设备命令
        /// </summary>
        public RelayCommand ResetDeviceCmd { get; set; }

        #endregion

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
        private string devicePortOrChannelNoContent = "设备端口";
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

        /// <summary>
        /// 新增设备
        /// </summary>
        private void AddDevice()
        {
            var msg = string.Empty;

            //参数赋值
            if (HikvisionHelper.m_lUserID >= 0)
            {
                NET_DVR_Logout(HikvisionHelper.m_lUserID);
            }

            HikvisionHelper.m_deviceIp = Model.DeviceIp;
            HikvisionHelper.m_devicePort = Model.DevicePortOrChannelNo;
            HikvisionHelper.m_UserName = Model.UserName;
            HikvisionHelper.m_Password = Model.Password;

            //登录设备
            HikvisionHelper.m_lUserID = NET_DVR_Login_V30(HikvisionHelper.m_deviceIp, HikvisionHelper.m_devicePort, HikvisionHelper.m_UserName, HikvisionHelper.m_Password,ref HikvisionHelper.m_struDeviceInfo);

            if (HikvisionHelper.m_lUserID < 0)
            {
                HikvisionHelper.dwRet = NET_DVR_GetLastError();

                msg = $"登录设备失败，错误号为{HikvisionHelper.dwRet}";
                LogHelper.Error(msg);
            }
            else
            {
                msg = Model.DeviceType == "VR" ? AddNvr() : AddDvr();
            }

            if (!string.IsNullOrEmpty(msg))
            {
                HandyControl.Controls.Growl.Warning(msg);
            }
            else
            {
                //刷新网格数据
               QueryDevice();

                //初始化设备
                var nvrId = NvrData.FirstOrDefault()?.ID;


                HandyControl.Controls.Growl.Info("保存成功");
            }
        }


        public string AddNvr()
        {
            var msg = string.Empty;

            try
            {
                var sql = $"SELECT * FROM NVRPara WHERE 1=1";
                using (var dt = OleDbHelper.DataTable(sql))
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        var tmpRow = dt.AsEnumerable().Count(w => w.Field<string>("IP") == Model.DeviceIp);
                        if (tmpRow == 1)
                        {
                            //更新
                            sql = $"UPDATE NVRPara SET Port={Model.DevicePortOrChannelNo},NVRName='{Model.DeviceName}',UserId='{Model.UserName}',pwd='{Model.Password}' WHERE IP='{HikvisionHelper.m_deviceIp}'";
                            OleDbHelper.ExcuteSql(sql);
                        }
                        else
                        {
                            //报错，录像机有且只有一个设备
                            throw new Exception("录像机有且只有一个设备");
                        }
                    }
                    else
                    {
                        //新增
                        sql = $"INSERT INTO NVRPara(NVRName,IP,Port,UserId,Psw) VALUES('{Model.DeviceName}','{Model.DeviceIp}',{Model.DevicePortOrChannelNo},'{Model.Password}')";
                        OleDbHelper.ExcuteSql(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex?.Message;
            }
            return msg;
        }

        public string AddDvr()
        {
            var msg = string.Empty;

            try
            {
                if (NvrData == null || NvrData.Count == 0)
                {
                    throw new Exception("请先新增硬盘录像机设备");
                }

                var pkId = NvrData.FirstOrDefault().ID;

                var sql = $"SELECT * FROM DVRPara WHERE 1=1 WHERE NVRID={pkId} AND IP='{Model.DeviceIp}'";
                using (var dt = OleDbHelper.DataTable(sql))
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        //更新
                        sql = $"UPDATE DVRPara SET DVRName='{Model.DeviceName}',Channel='{Model.DevicePortOrChannelNo}' WHERE NVRID={pkId} AND IP='{HikvisionHelper.m_deviceIp}'";
                        OleDbHelper.ExcuteSql(sql);
                    }
                    else
                    {
                        //新增
                        sql = $"INSERT INTO DVRPara(DVRName,IP,Channel,NVRID) VALUES('{Model.DeviceName}','{Model.DeviceIp}',{Model.DevicePortOrChannelNo},'{pkId}')";
                        OleDbHelper.ExcuteSql(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex?.Message;
            }
            return msg;
        }

        public void QueryDevice()
        {
            var sql = $"SELECT * FROM NVRPara WHERE 1=1";

            using (var dt = OleDbHelper.DataTable(sql))
            {
                if (NvrData == null)
                {
                    NvrData = new ObservableCollection<NvrData>();
                }
                else
                {
                    NvrData.Clear();
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        NvrData.Add(new Models.NvrData()
                        {
                            ID = Convert.ToInt32(dr["ID"]),
                            DeviceName = dr["NVRName"].ToString(),
                            DeviceIp = dr["IP"].ToString(),
                            DevicePort = Convert.ToInt32(dr["Port"]),
                            UserName = dr["UserName"].ToString(),
                            Password = dr["Password"].ToString()
                        });
                    }
                }
            }

            sql = $"SELECT * FROM DVRPara WHERE NVRID={NvrData.FirstOrDefault()?.ID}";
            using (var dt = OleDbHelper.DataTable(sql))
            {
                if (DvrData == null)
                {
                    DvrData = new ObservableCollection<DvrData>();
                }
                else
                {
                    DvrData.Clear();
                }
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        DvrData.Add(new Models.DvrData()
                        {
                            ID = Convert.ToInt32(dr["ID"]),
                            DeviceName = dr["NVRName"].ToString(),
                            DeviceIp = dr["IP"].ToString(),
                            DeviceChannelNo = Convert.ToInt32(dr["Channel"]),
                            NVRID = Convert.ToInt32(dr["NVRID"])
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 清空设备
        /// </summary>
        private void ResetDevice()
        {

        }

        #endregion

    }
}
