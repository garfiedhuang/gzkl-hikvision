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
using System.Windows.Interop;

namespace GZKL.Client.UI.ViewsModels
{
    public class DeviceViewModel : ViewModelBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DeviceViewModel()
        {
            AddDeviceCmd = new RelayCommand(this.AddDevice);
            ResetDeviceCmd = new RelayCommand(this.ResetDevice);

            this.QueryDevice();
        }

        #region Command

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

        #endregion

        #region =====methods

        /// <summary>
        /// 新增设备
        /// </summary>
        public void AddDevice()
        {
            var msg = string.Empty;
            var nvrId = 0;

            try
            {
                //校验是否登录状态？如果是，先执行登出操作
                if (HikvisionHelper.m_lUserID >= 0)
                {
                    NET_DVR_Logout(HikvisionHelper.m_lUserID);
                }

                //参数赋值
                HikvisionHelper.m_deviceIp = Model.DeviceIp;
                HikvisionHelper.m_devicePort = Model.DevicePort;
                HikvisionHelper.m_UserName = Model.UserName;
                HikvisionHelper.m_Password = Model.Password;

                //登录设备
                HikvisionHelper.m_lUserID = NET_DVR_Login_V30(HikvisionHelper.m_deviceIp, HikvisionHelper.m_devicePort, HikvisionHelper.m_UserName, HikvisionHelper.m_Password, ref HikvisionHelper.m_struDeviceInfo);

                if (HikvisionHelper.m_lUserID < 0)
                {
                    HikvisionHelper.iLastErr = NET_DVR_GetLastError();

                    msg = $"登录设备失败，错误号为{HikvisionHelper.iLastErr}";
                }
                else
                {
                    msg = AddNvr(out nvrId);
                }

                if (!string.IsNullOrEmpty(msg))
                {
                    LogHelper.Warn(msg);
                    HandyControl.Controls.Growl.Warning(msg);
                }
                else
                {
                    //初始化设备
                    HikvisionHelper.AddDvr(nvrId);

                    //刷新网格数据
                    this.QueryDevice();

                    HandyControl.Controls.Growl.Info("保存成功");
                }
            }
            catch (Exception ex)
            {
                msg = ex?.Message;
                LogHelper.Error(msg);
                HandyControl.Controls.Growl.Error(msg);
            }
        }

        /// <summary>
        /// 新增NVR
        /// </summary>
        /// <param name="nvrId"></param>
        /// <returns></returns>
        public string AddNvr(out int nvrId)
        {
            var msg = string.Empty;

            nvrId = 0;

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
                            var sql1 = $"UPDATE NVRPara SET Port={Model.DevicePort},NVRName='{Model.DeviceName}',UserId='{Model.UserName}',pwd='{Model.Password}' WHERE IP='{HikvisionHelper.m_deviceIp}'";
                            OleDbHelper.ExcuteSql(sql1);
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
                        var sql2 = $"INSERT INTO NVRPara(NVRName,IP,Port,UserId,Psw) VALUES('{Model.DeviceName}','{Model.DeviceIp}',{Model.DevicePort},'{Model.Password}')";
                        OleDbHelper.ExcuteSql(sql2);
                    }
                }

                //获取主键ID
                using (var dt = OleDbHelper.DataTable(sql))
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        nvrId = Convert.ToInt32(dt.Rows[0]["ID"]);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex?.Message;
            }
            return msg;
        }

        /// <summary>
        /// 查询设备（NVR+DVR）
        /// </summary>
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
                            ChannelNo = Convert.ToInt32(dr["Channel"]),
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
            try
            {
                //校验是否登录状态？如果是，先执行登出操作
                if (HikvisionHelper.m_lUserID >= 0)
                {
                    NET_DVR_Logout(HikvisionHelper.m_lUserID);
                }

                var sql = string.Empty;

                //删除录像机配置
                sql = "DELETE FROM NVRPara WHERE 1=1";
                OleDbHelper.ExcuteSql(sql);

                //删除摄像机配置
                sql = "DELETE FROM DVRPara WHERE 1=1";
                OleDbHelper.ExcuteSql(sql);

                HandyControl.Controls.Growl.Info("清空成功");
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.Error(ex?.Message);
            }
        }

        #endregion

    }
}
