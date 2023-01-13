using GZKL.Client.UI.Models;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static GZKL.Client.UI.Common.CHCNetSDK;

namespace GZKL.Client.UI.Common
{
    public class HikvisionHelper
    {
        //登录相关参数
        public static bool m_bInitSDK = false;
        public static int m_lUserID = -1;//登录返回值
        public static int m_lChannel;//当前选择通道
        public static string m_deviceIp;//设备IP
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
        public static NET_DVR_SHOWSTRING_V30 m_struShowStrCfg;

        //预览相关参数
        public static long lRealHandle;//预览句柄
        public static NET_DVR_CLIENTINFO struPlayInfo;
        public static NET_DVR_PREVIEWINFO lpPreviewInfo;

        //回放开始时间点
        //public static NET_DVR_TIME playBackStartTime;
        //public static int[] iChannelNum=new int[64];
        public static REALDATACALLBACK RealData = null;

        //回放下载相关参数
        public static Int32 m_lFindHandle = -1;
        public static Int32 m_lPlayHandle = -1;
        public static Int32 m_lDownHandle = -1;
        public static Int32 m_lRealHandle = -1;

        public static bool m_bPause = false;
        public static bool m_bReverse = false;
        public static bool m_bSound = false;

        public static DateTime _startRecordTime = DateTime.MinValue;//开始录制时间
        public static DateTime _endRecordTime = DateTime.MinValue;//结束录制时间


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
        /// 保存IP摄像机
        /// </summary>
        /// <param name="nvrId"></param>
        /// <param name="sIp"></param>
        /// <param name="sChannel"></param>
        public static void SaveIpDVR(int nvrId, string sIp, string sChannel)
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

                    //Todo：触发实时预览 by garfield 20230110


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
        /// 新增DVR
        /// </summary>
        /// <param name="nvrId"></param>
        public static void AddDvr(int nvrId)
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

                        SaveIpDVR(nvrId, sIp, sChannel);
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

                                SaveIpDVR(nvrId, sIp, sChannel);
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

                                SaveIpDVR(nvrId, sIp, sChannel);
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

        /// <summary>
        /// 设置显示字符
        /// </summary>
        /// <param name="testNo"></param>
        public static void SetShowString(string testNo)
        {
            UInt32 dwReturn = 0;
            Int32 nSize = Marshal.SizeOf(m_struShowStrCfg);
            IntPtr ptrShowStrCfg = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(m_struShowStrCfg, ptrShowStrCfg, false);
            if (!NET_DVR_GetDVRConfig(m_lUserID, NET_DVR_GET_SHOWSTRING_V30, m_lChannel, ptrShowStrCfg, (UInt32)nSize, ref dwReturn))
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = "NET_DVR_GET_SHOWSTRING_V30 failed, error code= " + iLastErr;
                //获取字符叠加参数失败，输出错误号 Failed to get overlay parameters and output the error code

                LogHelper.Error(strErr);
                HandyControl.Controls.Growl.Error(strErr);
            }
            else
            {
                m_struShowStrCfg = (NET_DVR_SHOWSTRING_V30)Marshal.PtrToStructure(ptrShowStrCfg, typeof(NET_DVR_SHOWSTRING_V30));

                m_struShowStrCfg.struStringInfo[0].wShowString = 1;
                m_struShowStrCfg.struStringInfo[0].wStringSize = (ushort)testNo.Length;
                m_struShowStrCfg.struStringInfo[0].sString = testNo;
                m_struShowStrCfg.struStringInfo[0].wShowStringTopLeftX = 30;
                m_struShowStrCfg.struStringInfo[0].wShowStringTopLeftY = 10;

                m_struShowStrCfg.struStringInfo[1].wShowString = 0;
                m_struShowStrCfg.struStringInfo[2].wShowString = 0;
                m_struShowStrCfg.struStringInfo[3].wShowString = 0;
                m_struShowStrCfg.struStringInfo[4].wShowString = 0;
                m_struShowStrCfg.struStringInfo[5].wShowString = 0;
                m_struShowStrCfg.struStringInfo[6].wShowString = 0;
                m_struShowStrCfg.struStringInfo[7].wShowString = 0;
            }
            Marshal.FreeHGlobal(ptrShowStrCfg);
        }

        /// <summary>
        /// 实时预览
        /// </summary>
        /// <param name="handle"></param>
        internal static void Preview(IntPtr handle)
        {
            if (m_lUserID < 0)
            {
                strErr = "Please login the device firstly";
                LogHelper.Warn(strErr);
                HandyControl.Controls.Growl.Warning(strErr);
                return;
            }

            if (m_lRealHandle < 0)
            {
                NET_DVR_PREVIEWINFO lpPreviewInfo = new NET_DVR_PREVIEWINFO();

                lpPreviewInfo.hPlayWnd = handle;//预览窗口

                lpPreviewInfo.lChannel = m_lChannel;//预览的设备通道
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 15; //播放库播放缓冲区最大缓冲帧数
                lpPreviewInfo.byProtoType = 0;
                lpPreviewInfo.byPreviewMode = 0;

                //if (textBoxID.Text != "")
                //{
                //    lpPreviewInfo.lChannel = -1;
                //    byte[] byStreamID = System.Text.Encoding.Default.GetBytes(textBoxID.Text);
                //    lpPreviewInfo.byStreamID = new byte[32];
                //    byStreamID.CopyTo(lpPreviewInfo.byStreamID, 0);
                //}

                if (RealData == null)
                {
                    RealData = new REALDATACALLBACK(RealDataCallBack);//预览实时流回调函数
                }

                IntPtr pUser = new IntPtr();//用户数据

                //打开预览 Start live view 
                m_lRealHandle = NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, null/*RealData*/, pUser);
                if (m_lRealHandle < 0)
                {
                    iLastErr = NET_DVR_GetLastError();
                    strErr = "NET_DVR_RealPlay_V40 failed, error code= " + iLastErr; //预览失败，输出错误号

                    LogHelper.Error(strErr);
                    HandyControl.Controls.Growl.Error(strErr);
                    return;
                }
                else
                {
                    strErr = "预览成功";
                    LogHelper.Info(strErr);
                    HandyControl.Controls.Growl.Info(strErr);
                }
            }
            else
            {
                //停止预览 Stop live view 
                if (!NET_DVR_StopRealPlay(m_lRealHandle))
                {
                    iLastErr = NET_DVR_GetLastError();
                    strErr = "NET_DVR_StopRealPlay failed, error code= " + iLastErr;

                    LogHelper.Error(strErr);
                    HandyControl.Controls.Growl.Error(strErr);
                    return;
                }
                m_lRealHandle = -1;
                //btnPreview.Text = "Live View";

            }
        }

        internal static void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            if (dwBufSize > 0)
            {
                byte[] sData = new byte[dwBufSize];
                Marshal.Copy(pBuffer, sData, 0, (Int32)dwBufSize);

                string str = "实时流数据.ps";
                FileStream fs = new FileStream(str, FileMode.Create);
                int iLen = (int)dwBufSize;
                fs.Write(sData, 0, iLen);
                fs.Close();
            }
        }

        /// <summary>
        /// 开始远程录制视频
        /// </summary>
        internal static void StartDvrRecord()
        {
            if (m_lUserID < 0)
            {
                strErr = "Please login the device firstly";
                LogHelper.Warn(strErr);
                HandyControl.Controls.Growl.Warning(strErr);
                return;
            }

            if (!NET_DVR_StartDVRRecord(m_lUserID, m_lChannel, 0))
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = "NET_DVR_StartDVRRecord failed, error code= " + iLastErr;

                LogHelper.Error(strErr);
                HandyControl.Controls.Growl.Error(strErr);
                return;
            }
            else
            {
                uint dwSize = (uint)Marshal.SizeOf(m_struTimeCfg);//非托管对象大小，单位字节
                IntPtr ptrGetTimeCfg = Marshal.AllocHGlobal((Int32)dwSize);//从进程中的非托管内存分配内存
                Marshal.StructureToPtr(m_struTimeCfg, ptrGetTimeCfg, false);//将数据从托管对象封送到非托管内存块

                uint dwReturn = 0;

                //获取DVR时间设置
                if (!NET_DVR_GetDVRConfig(m_lUserID, NET_DVR_GET_TIMECFG, m_lChannel, ptrGetTimeCfg, dwSize, ref dwReturn))
                {
                    iLastErr = NET_DVR_GetLastError();
                    strErr = $"获取NET_DVR_GET_TIMECFG失败, 错误号为：{iLastErr}";
                    LogHelper.Error(strErr);

                    HandyControl.Controls.Growl.Error(strErr);
                }
                else
                {
                    //开始录制时间
                    var _startRecordTime = new DateTime(m_struTimeCfg.dwYear, m_struTimeCfg.dwMonth, m_struTimeCfg.dwDay, m_struTimeCfg.dwHour, m_struTimeCfg.dwMinute, m_struTimeCfg.dwSecond);

                    //回放开始时间
                    //m_struTimeCfg
                }
            }
        }

        /// <summary>
        /// 停止远程录制视频
        /// </summary>
        /// <param name="testNo"></param>
        /// <param name="dvrName"></param>
        /// <param name="intPtr"></param>
        internal static void StopDvrRecord(string testNo, string dvrName, IntPtr intPtr)
        {
            if (m_lUserID < 0)
            {
                strErr = "Please login the device firstly";
                LogHelper.Warn(strErr);
                HandyControl.Controls.Growl.Warning(strErr);
                return;
            }

            if (!NET_DVR_StopDVRRecord(m_lUserID, m_lChannel))
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = "NET_DVR_StopDVRRecord failed, error code= " + iLastErr;

                LogHelper.Error(strErr);
                HandyControl.Controls.Growl.Error(strErr);
                return;
            }
            else
            {
                uint dwSize = (uint)Marshal.SizeOf(m_struTimeCfg);//非托管对象大小，单位字节
                IntPtr ptrGetTimeCfg = Marshal.AllocHGlobal((Int32)dwSize);//从进程中的非托管内存分配内存
                Marshal.StructureToPtr(m_struTimeCfg, ptrGetTimeCfg, false);//将数据从托管对象封送到非托管内存块

                uint dwReturn = 0;

                //获取DVR时间设置
                if (!NET_DVR_GetDVRConfig(m_lUserID, NET_DVR_GET_TIMECFG, m_lChannel, ptrGetTimeCfg, dwSize, ref dwReturn))
                {
                    iLastErr = NET_DVR_GetLastError();
                    strErr = $"获取NET_DVR_GET_TIMECFG失败, 错误号为：{iLastErr}";
                    LogHelper.Error(strErr);

                    HandyControl.Controls.Growl.Error(strErr);
                }
                else
                {
                    //停止录制时间
                    _endRecordTime = new DateTime(m_struTimeCfg.dwYear, m_struTimeCfg.dwMonth, m_struTimeCfg.dwDay, m_struTimeCfg.dwHour, m_struTimeCfg.dwMinute, m_struTimeCfg.dwSecond);

                    //入库
                    AddJcRecord(testNo, dvrName);

                    //回放
                    _startRecordTime = _startRecordTime.AddSeconds(-0.00012);
                    _endRecordTime = _endRecordTime.AddSeconds(0.00012);

                    var result = StartPlayBack(_startRecordTime, _endRecordTime, intPtr);
                    if (result)
                    {
                        uint iOutValue = 0;
                        if (!NET_DVR_PlayBackControl_V40(m_lPlayHandle, NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
                        {
                            iLastErr = NET_DVR_GetLastError();
                            strErr = "NET_DVR_PLAYSTART failed, error code= " + iLastErr; //回放控制失败，输出错误号

                            LogHelper.Error(strErr);
                            HandyControl.Controls.Growl.Error(strErr);

                            return;
                        }
                        else
                        {
                            var lpLableIdentify = new NET_DVR_LABEL_IDENTIFY();
                            var lpRecordLabel = new NET_DVR_RECORD_LABEL();

                            lpRecordLabel.struTimeLabel = m_struTimeCfg;
                            lpRecordLabel.byQuickAdd = 0;
                            lpRecordLabel.sLabelName = System.Text.Encoding.UTF8.GetBytes(testNo);

                            if (NET_DVR_InsertRecordLabel(m_lPlayHandle, lpRecordLabel,ref lpLableIdentify))
                            {
                                strErr = "停止录像并且增加标签成功！";

                                LogHelper.Info(strErr);
                                HandyControl.Controls.Growl.Info(strErr);
                            }
                            else
                            {
                                strErr = "NET_DVR_InsertRecordLabel failed, error code= " + iLastErr; //回放控制失败，输出错误号

                                LogHelper.Error(strErr);
                                HandyControl.Controls.Growl.Error(strErr);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 新增检测记录
        /// </summary>
        /// <param name="testNo"></param>
        /// <param name="nvrName"></param>
        private static void AddJcRecord(string testNo, string nvrName)
        {
            var sql = $"INSERT INTO JCRecord(JcNo,DVRName,startTime,stopTime) VALUES('{testNo}','{nvrName}','{_startRecordTime}','{_endRecordTime}')";
            OleDbHelper.ExcuteSql(sql);
        }

        /// <summary>
        /// 开始回放
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="intPtr"></param>
        public static bool StartPlayBack(DateTime startTime, DateTime endTime, IntPtr intPtr)
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

                    return false;
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
            struVodPara.struIDInfo.dwChannel = (uint)m_lChannel; //通道号 Channel number  
            struVodPara.hWnd = intPtr;//回放窗口句柄

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

                return false;
            }

            uint iOutValue = 0;
            if (!NET_DVR_PlayBackControl_V40(m_lPlayHandle, NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
            {
                iLastErr = NET_DVR_GetLastError();
                strErr = "NET_DVR_PLAYSTART failed, error code= " + iLastErr; //回放控制失败，输出错误号

                LogHelper.Error(strErr);
                HandyControl.Controls.Growl.Error(strErr);

                return false;
            }
            //timerPlayback.Interval = 1000;
            //timerPlayback.Enabled = true;
            //btnStopPlayback.Enabled = true;

            return true;
        }

        /// <summary>
        /// 停止回放
        /// </summary>
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

        /// <summary>
        /// 暂停回放
        /// </summary>
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


        /// <summary>
        /// 初始化DVR
        /// </summary>
        public static void InitDvr()
        {
            m_bInitSDK = NET_DVR_Init();
            if (m_bInitSDK == false)
            {
                strErr = $"NET_DVR_Init error!";
                LogHelper.Error(strErr);
                HandyControl.Controls.Growl.Error(strErr);
                return;
            }
            else
            {
                var sdkLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SdkLog");

                //保存SDK日志
                NET_DVR_SetLogToFile(3, sdkLogPath, true);
            }
        }

        /// <summary>
        /// 登录DVR
        /// </summary>
        /// <param name="nvrConfig"></param>
        internal static void LoginDvr(NvrData nvrConfig)
        {
            if (m_lUserID < 0)
            {
                string DVRIPAddress = nvrConfig.DeviceIp; //设备IP地址或者域名
                Int16 DVRPortNumber = Convert.ToInt16(nvrConfig.DevicePort);//设备服务端口号
                string DVRUserName = nvrConfig.UserName;//设备登录用户名
                string DVRPassword = nvrConfig.Password;//设备登录密码

                //DeviceInfo = new NET_DVR_DEVICEINFO_V30();

                //登录设备 Login the device
                m_lUserID = NET_DVR_Login_V30(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword, ref m_struDeviceInfo);
                if (m_lUserID < 0)
                {
                    iLastErr = NET_DVR_GetLastError();
                    strErr = "NET_DVR_Login_V30 failed, error code= " + iLastErr; //登录失败，输出错误号
                    LogHelper.Error(strErr);
                    HandyControl.Controls.Growl.Error(strErr);
                    return;
                }
                else
                {
                    //登录成功
                    AddDvr(nvrConfig.ID);
                }
            }
        }

        /// <summary>
        /// 登出
        /// </summary>
        public static void LogoutDvr()
        {
            //停止回放 Stop playback
            if (m_lPlayHandle >= 0)
            {
                NET_DVR_StopPlayBack(m_lPlayHandle);
                m_lPlayHandle = -1;
            }

            //停止下载 Stop download
            if (m_lDownHandle >= 0)
            {
                NET_DVR_StopGetFile(m_lDownHandle);
                m_lDownHandle = -1;
            }

            //注销登录 Logout the device
            if (m_lUserID >= 0)
            {
                //NET_DVR_Logout(m_lUserID);
                NET_DVR_Logout_V30(m_lUserID);
                m_lUserID = -1;
            }
        }

        /// <summary>
        /// 释放SDK资源
        /// </summary>
        public static void Dispose()
        {
            NET_DVR_Cleanup();
        }
    }
}
