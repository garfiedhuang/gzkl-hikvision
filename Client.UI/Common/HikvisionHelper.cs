using GZKL.Client.UI.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Interop;
using static GZKL.Client.UI.Common.CHCNetSDK;

namespace GZKL.Client.UI.Common
{
    public class HikvisionHelper
    {
        //登录相关参数
        public static Int32 m_lUserID = -1;//登录返回值，具有唯一性，后续对设备的操作都需要通过此ID实现
        public static string m_deviceIp = "";//设备IP地址
        public static int m_devicePort;//设备端口号

        public static string m_UserName;//登录用户名
        public static string m_Password;//登录密码

        //错误号
        public static bool m_bInitSDK = false;
        public static uint iLastErr = 0;
        public static string strErr;

        //public static int dwAChanTotalNum;
        //public static int dwDChanTotalNum;

        //public static NET_DVR_DEVICEINFO_V30 struDeviceInfo;//设备参数信息结构
        //public static NET_DVR_IPPARACFG_V40 m_struIpParaCfgV40;

        //public static NET_DVR_IPCHANINFO m_struChanInfo;
        //public static NET_DVR_IPCHANINFO_V40 m_struChanInfoV40;
        //public static NET_DVR_PU_STREAM_URL m_struStreamURL;

        public static CHCNetSDK.NET_DVR_DEVICECFG_V40 m_struDeviceCfg;
        public static CHCNetSDK.NET_DVR_NETCFG_V30 m_struNetCfg;
        public static CHCNetSDK.NET_DVR_TIME m_struTimeCfg;
        public static CHCNetSDK.NET_DVR_DEVICEINFO_V30 m_struDeviceInfo;
        public static CHCNetSDK.NET_DVR_IPPARACFG_V40 m_struIpParaCfgV40;


        //预览相关参数
        public static Int64 lRealHandle;//预览句柄
        public static NET_DVR_CLIENTINFO struPlayInfo;//预览参数
        public static NET_DVR_PREVIEWINFO lpPreviewInfo;
        //public static Pointer pUser;//用户数据

        public static DateTime beginRTime;
        public static DateTime endRTime;


        //回放开始时间点
        //public static NET_DVR_TIME playBackStartTime;
        //public static int[] iChannelNum=new int[64];

        //回放下载相关参数
        public static int m_lPlayHandle;
        public static int iDownloadHandle;
        public static bool isRecording;


        private static bool m_bPause = false;
        private static bool m_bReverse = false;
        private static bool m_bSound = false;


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

        public static void myDVR_INT(int nvrId)
        {
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

            if (!CHCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, CHCNetSDK.NET_DVR_GET_IPPARACFG_V40, iGroupNo, ptrIpParaCfgV40, dwSize, ref dwReturn))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                strErr = $"获取NET_DVR_GET_IPPARACFG_V40失败, 错误号为：{iLastErr}";
                LogHelper.Error(strErr);

                HandyControl.Controls.Growl.Error(strErr);
            }
            else
            {
                m_struIpParaCfgV40 = (CHCNetSDK.NET_DVR_IPPARACFG_V40)Marshal.PtrToStructure(ptrIpParaCfgV40, typeof(CHCNetSDK.NET_DVR_IPPARACFG_V40));

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
                    CHCNetSDK.NET_DVR_STREAM_MODE m_struStreamMode = new CHCNetSDK.NET_DVR_STREAM_MODE();
                    dwSize = (uint)Marshal.SizeOf(m_struStreamMode);

                    sChannel = (i + (int)m_struIpParaCfgV40.dwStartDChan).ToString();
                    sIp = System.Text.Encoding.UTF8.GetString(m_struIpParaCfgV40.struIPDevInfo[i].struIP.sIpV4);

                    switch (byStreamType)
                    {
                        //0- 直接从设备取流 0- get stream from device directly
                        case 0:
                            IntPtr ptrChanInfo = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfo, false);
                            CHCNetSDK.NET_DVR_IPCHANINFO m_struChanInfo = new CHCNetSDK.NET_DVR_IPCHANINFO();
                            m_struChanInfo = (CHCNetSDK.NET_DVR_IPCHANINFO)Marshal.PtrToStructure(ptrChanInfo, typeof(CHCNetSDK.NET_DVR_IPCHANINFO));

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
                            CHCNetSDK.NET_DVR_IPCHANINFO_V40 m_struChanInfoV40 = new CHCNetSDK.NET_DVR_IPCHANINFO_V40();
                            m_struChanInfoV40 = (CHCNetSDK.NET_DVR_IPCHANINFO_V40)Marshal.PtrToStructure(ptrChanInfoV40, typeof(CHCNetSDK.NET_DVR_IPCHANINFO_V40));

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

        public static void PlayBackTime(DateTime startTime, DateTime endTime, string dvrName)
        {
            if (m_lPlayHandle >= 0)
            {
                //如果已经正在回放，先停止回放
                if (!CHCNetSDK.NET_DVR_StopPlayBack(m_lPlayHandle))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
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

            CHCNetSDK.NET_DVR_VOD_PARA struVodPara = new CHCNetSDK.NET_DVR_VOD_PARA();
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
            m_lPlayHandle = CHCNetSDK.NET_DVR_PlayBackByTime_V40(m_lUserID, ref struVodPara);
            if (m_lPlayHandle < 0)
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                strErr = "NET_DVR_PlayBackByTime_V40 failed, error code= " + iLastErr;

                LogHelper.Error(strErr);
                HandyControl.Controls.Growl.Error(strErr);

                return;
            }

            uint iOutValue = 0;
            if (!CHCNetSDK.NET_DVR_PlayBackControl_V40(m_lPlayHandle, CHCNetSDK.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
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
            if (!CHCNetSDK.NET_DVR_StopPlayBack(m_lPlayHandle))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
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
                if (!CHCNetSDK.NET_DVR_PlayBackControl_V40(m_lPlayHandle, CHCNetSDK.NET_DVR_PLAYPAUSE, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
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
                if (!CHCNetSDK.NET_DVR_PlayBackControl_V40(m_lPlayHandle, CHCNetSDK.NET_DVR_PLAYRESTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
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

        public static NET_DVR_TIME DateTimeToDVRTime(DateTime localTime)
        {
            throw new NotImplementedException();
        }

        public static DateTime DVRTimeToDateTime(NET_DVR_TIME dvrTime)
        {
            throw new NotImplementedException();
        }

        public static Int64 SetShowString(string txt,Int64 lChannel,string showTxt)
        {
            throw new NotImplementedException();
        }
    }
}
