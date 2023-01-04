﻿using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using static GZKL.Client.UI.Common.CHCNetSDK;

namespace GZKL.Client.UI.Common
{
    public class HikvisionHelper
    {
        //登录相关参数
        public static bool m_bInitSDK = false;
        public static int m_lUserID = -1;//登录返回值
        public static string m_deviceIp = "";//设备IP
        public static int m_devicePort;//设备端口号

        public static string m_UserName;//登录用户名
        public static string m_Password;//登录密码

        //错误号
        public static uint iLastErr = 0;
        public static string strErr;

        public static NET_DVR_DEVICECFG_V40 m_struDeviceCfg;
        public static NET_DVR_NETCFG_V30 m_struNetCfg;
        public static NET_DVR_TIME m_struTimeCfg;
        public static NET_DVR_DEVICEINFO_V30 m_struDeviceInfo;
        public static NET_DVR_IPPARACFG_V40 m_struIpParaCfgV40;

        //预览相关参数
        public static long lRealHandle;//预览句柄
        public static NET_DVR_CLIENTINFO struPlayInfo;//预览参数
        public static NET_DVR_PREVIEWINFO lpPreviewInfo;


        //回放开始时间点
        //public static NET_DVR_TIME playBackStartTime;
        //public static int[] iChannelNum=new int[64];

        //回放下载相关参数
        public static Int32 m_lFindHandle = -1;
        public static Int32 m_lPlayHandle = -1;
        public static Int32 m_lDownHandle = -1;

        public static bool m_bPause = false;
        public static bool m_bReverse = false;
        public static bool m_bSound = false;


        public static CHAN_INFO m_struChanNoInfo = new CHAN_INFO();

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct CHAN_INFO
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 256, ArraySubType = UnmanagedType.U4)]
            public Int32[] lChannelNo;
            public void Init()
            {
                lChannelNo = new Int32[256];
                for (int i = 0; i < 256; i++)
                    lChannelNo[i] = -1;
            }
        }

        /// <summary>
        /// 设置IP摄像机
        /// </summary>
        /// <param name="nvrId"></param>
        /// <param name="sIp"></param>
        /// <param name="sChannel"></param>
        public static void SetIPDVR(int nvrId,string sIp,string sChannel)
        {
            try
            {

                var sql = $"SELECT * FROM DVRPara WHERE 1=1 WHERE NVRID={nvrId} AND IP='{sIp}'";
                using (var dt = OleDbHelper.DataTable(sql))
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        //更新
                        sql = $"UPDATE DVRPara SET DVRName='通道{sChannel}摄像机',Channel='{sChannel}' WHERE NVRID={nvrId} AND IP='{sIp}'";
                        OleDbHelper.ExcuteSql(sql);
                    }
                    else
                    {
                        //新增
                        sql = $"INSERT INTO DVRPara(DVRName,IP,Channel,NVRID) VALUES('通道{sChannel}摄像机','{sIp}',{sChannel},'{nvrId}')";
                        OleDbHelper.ExcuteSql(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                strErr = $"保存摄像机失败，{ex?.Message}";
                LogHelper.Error(strErr);

                HandyControl.Controls.Growl.Error(strErr);
            }
        }

        /// <summary>
        /// 初始化摄像机设备
        /// </summary>
        /// <param name="nvrId"></param>
        public static void InitDvrDevice(int nvrId)
        {
            if (m_lUserID < 0)
            {
                throw new Exception($"未登录设备，请先登录设备后再试！");
            }

            int i = 0, j = 0;
            string strErr, sChannel, sIp;
            m_struChanNoInfo.Init();

            uint dwDChanTotalNum = (uint)m_struDeviceInfo.byIPChanNum + 256 * (uint)m_struDeviceInfo.byHighDChanNum;

            if (dwDChanTotalNum <= 0) return;

            uint dwSize = (uint)Marshal.SizeOf(m_struIpParaCfgV40);

            IntPtr ptrIpParaCfgV40 = Marshal.AllocHGlobal((Int32)dwSize);
            Marshal.StructureToPtr(m_struIpParaCfgV40, ptrIpParaCfgV40, false);

            uint dwReturn = 0;
            int iGroupNo = 0;  //该Demo仅获取第一组64个通道，如果设备IP通道大于64路，需要按组号0~i多次调用NET_DVR_GET_IPPARACFG_V40获取

            if (!NET_DVR_GetDVRConfig(m_lUserID, NET_DVR_GET_IPPARACFG_V40, iGroupNo, ptrIpParaCfgV40, dwSize, ref dwReturn))
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = $"获取NET_DVR_GET_IPPARACFG_V40失败, 错误号为：{iLastErr}";
                LogHelper.Error(strErr);

                HandyControl.Controls.Growl.Error(strErr);
            }
            else
            {
                m_struIpParaCfgV40 = (NET_DVR_IPPARACFG_V40)Marshal.PtrToStructure(ptrIpParaCfgV40, typeof(NET_DVR_IPPARACFG_V40));

                //获取可用的模拟通道
                for (i = 0; i < m_struIpParaCfgV40.dwAChanNum; i++)
                {
                    if (m_struIpParaCfgV40.byAnalogChanEnable[i] == 1)
                    {
                        sChannel = (i + (int)m_struIpParaCfgV40.dwStartDChan).ToString();
                        sIp = System.Text.Encoding.UTF8.GetString(m_struIpParaCfgV40.struIPDevInfo[i].struIP.sIpV4);

                        //str = String.Format("通道{0}", i + 1);
                        //comboBoxChan.Items.Add(str);
                        m_struChanNoInfo.lChannelNo[j] = i + m_struDeviceInfo.byStartChan;
                        j++;

                        SetIPDVR(nvrId, sIp, sChannel);
                    }
                }

                //获取前64个IP通道中的在线通道
                uint iDChanNum = 64;

                if (dwDChanTotalNum < 64)
                {
                    iDChanNum = dwDChanTotalNum; //如果设备IP通道小于64路，按实际路数获取
                }

                byte byStreamType;
                for (i = 0; i < iDChanNum; i++)
                {
                    byStreamType = m_struIpParaCfgV40.struStreamMode[i].byGetStreamType;
                    NET_DVR_STREAM_MODE m_struStreamMode = new NET_DVR_STREAM_MODE();
                    dwSize = (uint)Marshal.SizeOf(m_struStreamMode);

                    sChannel = (i + (int)m_struIpParaCfgV40.dwStartDChan).ToString();
                    sIp = System.Text.Encoding.UTF8.GetString(m_struIpParaCfgV40.struIPDevInfo[i].struIP.sIpV4);

                    switch (byStreamType)
                    {
                        //0- 直接从设备取流 0- get stream from device directly
                        case 0:
                            IntPtr ptrChanInfo = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfo, false);
                            NET_DVR_IPCHANINFO m_struChanInfo = new NET_DVR_IPCHANINFO();
                            m_struChanInfo = (NET_DVR_IPCHANINFO)Marshal.PtrToStructure(ptrChanInfo, typeof(NET_DVR_IPCHANINFO));

                            //列出IP通道 List the IP channel
                            if (m_struChanInfo.byEnable == 1 && m_struChanInfo.byIPID != 0)
                            {
                                //str = String.Format("IP通道{0}", i + 1);
                                //comboBoxChan.Items.Add(str);
                                m_struChanNoInfo.lChannelNo[j] = i + (int)m_struIpParaCfgV40.dwStartDChan;
                                j++;

                                SetIPDVR(nvrId, sIp, sChannel);
                            }
                            Marshal.FreeHGlobal(ptrChanInfo);
                            break;
                        //6- 直接从设备取流扩展 6- get stream from device directly(extended)
                        case 6:
                            IntPtr ptrChanInfoV40 = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfoV40, false);
                            NET_DVR_IPCHANINFO_V40 m_struChanInfoV40 = new NET_DVR_IPCHANINFO_V40();
                            m_struChanInfoV40 = (NET_DVR_IPCHANINFO_V40)Marshal.PtrToStructure(ptrChanInfoV40, typeof(NET_DVR_IPCHANINFO_V40));

                            //列出IP通道 List the IP channel
                            if (m_struChanInfoV40.byEnable == 1 && m_struChanInfoV40.wIPID != 0)
                            {
                                //str = String.Format("IP通道{0}", i + 1);
                                //comboBoxChan.Items.Add(str);
                                m_struChanNoInfo.lChannelNo[j] = i + (int)m_struIpParaCfgV40.dwStartDChan;
                                j++;

                                SetIPDVR(nvrId, sIp, sChannel);
                            }
                            Marshal.FreeHGlobal(ptrChanInfoV40);
                            break;
                        default:
                            break;
                    }
                }
                Marshal.FreeHGlobal(ptrIpParaCfgV40);
            }
        }

        /// <summary>
        /// 校时
        /// </summary>
        public static void TimeSet()
        {
            if (m_lUserID < 0)
            {
                throw new Exception($"未登录设备，请先登录设备后再试！");
            }

            var dtNow = DateTime.Now;

            m_struTimeCfg.dwYear = dtNow.Year;
            m_struTimeCfg.dwMonth = dtNow.Month;
            m_struTimeCfg.dwDay = dtNow.Day;
            m_struTimeCfg.dwHour = dtNow.Hour;
            m_struTimeCfg.dwMinute = dtNow.Minute;
            m_struTimeCfg.dwSecond = dtNow.Second;

            int nSize = Marshal.SizeOf(m_struTimeCfg);
            IntPtr ptrTimeCfg = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(m_struTimeCfg, ptrTimeCfg, false);

            if (!NET_DVR_SetDVRConfig(m_lUserID, NET_DVR_SET_TIMECFG, -1, ptrTimeCfg, (uint)nSize))
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = "NET_DVR_SET_TIMECFG failed, error code= " + iLastErr;
                //设置时间失败，输出错误号 Failed to set the time of device and output the error code

                LogHelper.Error(strErr);

                HandyControl.Controls.Growl.Error(strErr);
            }
            else
            {
                HandyControl.Controls.Growl.Info("校时成功！");
            }

            Marshal.FreeHGlobal(ptrTimeCfg);
        }

        //public static void 

        public static void PlayBackTime(DateTime startTime, DateTime endTime, string dvrName)
        {
            if (m_lPlayHandle >= 0)
            {
                //如果已经正在回放，先停止回放
                if (!NET_DVR_StopPlayBack(m_lPlayHandle))
                {
                    iLastErr = NET_DVR_GetLastError();
                    strErr = "NET_DVR_StopPlayBack failed, error code= " + iLastErr;

                    LogHelper.Error(strErr);
                    HandyControl.Controls.Growl.Error(strErr);

                    return;
                }

                //m_bReverse = false;
                //btnReverse.Text = "Reverse";
                //labelReverse.Text = "切换为倒放";

                //m_bPause = false;
                //btnPause.Text = "||";
                //labelPause.Text = "暂停";

                m_lPlayHandle = -1;

                //PlaybackprogressBar.Value = 0;
            }

            NET_DVR_VOD_PARA struVodPara = new NET_DVR_VOD_PARA();
            struVodPara.dwSize = (uint)Marshal.SizeOf(struVodPara);
            //struVodPara.struIDInfo.dwChannel = (uint)iChannelNum[(int)iSelIndex]; //通道号 Channel number  
            //struVodPara.hWnd = VideoPlayWnd.Handle;//回放窗口句柄

            //设置回放的开始时间 Set the starting time to search video files
            struVodPara.struBeginTime.dwYear = startTime.Year;
            struVodPara.struBeginTime.dwMonth = startTime.Month;
            struVodPara.struBeginTime.dwDay = startTime.Day;
            struVodPara.struBeginTime.dwHour = startTime.Hour;
            struVodPara.struBeginTime.dwMinute = startTime.Minute;
            struVodPara.struBeginTime.dwSecond = startTime.Second;

            //设置回放的结束时间 Set the stopping time to search video files
            struVodPara.struEndTime.dwYear = endTime.Year;
            struVodPara.struEndTime.dwMonth = endTime.Month;
            struVodPara.struEndTime.dwDay = endTime.Day;
            struVodPara.struEndTime.dwHour = endTime.Hour;
            struVodPara.struEndTime.dwMinute = endTime.Minute;
            struVodPara.struEndTime.dwSecond = endTime.Second;

            //按时间回放 Playback by time
            m_lPlayHandle = NET_DVR_PlayBackByTime_V40(m_lUserID, ref struVodPara);
            if (m_lPlayHandle < 0)
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = "NET_DVR_PlayBackByTime_V40 failed, error code= " + iLastErr;

                LogHelper.Error(strErr);
                HandyControl.Controls.Growl.Error(strErr);

                return;
            }

            uint iOutValue = 0;
            if (!NET_DVR_PlayBackControl_V40(m_lPlayHandle, NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = "NET_DVR_PLAYSTART failed, error code= " + iLastErr; //回放控制失败，输出错误号

                LogHelper.Error(strErr);
                HandyControl.Controls.Growl.Error(strErr);

                return;
            }
            //timerPlayback.Interval = 1000;
            //timerPlayback.Enabled = true;
            //btnStopPlayback.Enabled = true;
        }

        public static void StopPlayBack()
        {
            if (m_lPlayHandle < 0)
            {
                return;
            }

            //停止回放
            if (!NET_DVR_StopPlayBack(m_lPlayHandle))
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = "NET_DVR_StopPlayBack failed, error code= " + iLastErr;
                LogHelper.Error(strErr);

                HandyControl.Controls.Growl.Error(strErr);
                return;
            }

            //PlaybackprogressBar.Value = 0;
            //timerPlayback.Stop();

            //m_bReverse = false;
            //btnReverse.Text = "Reverse";
            //labelReverse.Text = "切换为倒放";

            //m_bPause = false;
            //btnPause.Text = "||";
            //labelPause.Text = "暂停";

            m_lPlayHandle = -1;
            //VideoPlayWnd.Invalidate();//刷新窗口    
            //btnStopPlayback.Enabled = false;
        }

        public static void PausePalyBack()
        {
            uint iOutValue = 0;

            if (!m_bPause)
            {
                if (!NET_DVR_PlayBackControl_V40(m_lPlayHandle, NET_DVR_PLAYPAUSE, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
                {
                    iLastErr = NET_DVR_GetLastError();
                    strErr = "NET_DVR_PLAYPAUSE failed, error code= " + iLastErr; //回放控制失败，输出错误号
                    LogHelper.Error(strErr);

                    HandyControl.Controls.Growl.Error(strErr);
                    return;
                }
                m_bPause = true;
                //btnPause.Text = ">";
                //labelPause.Text = "播放";
            }
            else
            {
                if (!NET_DVR_PlayBackControl_V40(m_lPlayHandle, NET_DVR_PLAYRESTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
                {
                    iLastErr = NET_DVR_GetLastError();
                    strErr = "NET_DVR_PLAYRESTART failed, error code= " + iLastErr; //回放控制失败，输出错误号
                    LogHelper.Error(strErr);

                    HandyControl.Controls.Growl.Error(strErr);
                    return;
                }
                m_bPause = false;
                //btnPause.Text = "||";
                //labelPause.Text = "暂停";
            }
            return;
        }


    }
}
