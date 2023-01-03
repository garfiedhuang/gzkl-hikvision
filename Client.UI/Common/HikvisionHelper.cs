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

        public static int m_lUserID = -1;//登录返回值，具有唯一性，后续对设备的操作都需要通过此ID实现

        public static string m_deviceIp = "";//设备IP地址
        public static int m_devicePort;//设备端口号

        public static string m_UserName;//登录用户名
        public static string m_Password;//登录密码

        public static int dwAChanTotalNum;
        public static int dwDChanTotalNum;

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

        //错误号
        public static Int64 iLastErr;
        public static uint dwRet;

        //回放开始时间点
        //public static NET_DVR_TIME playBackStartTime;
        //public static int[] iChannelNum=new int[64];

        //回放下载相关参数
        public static int iPlaybackHandle;
        public static int iDownloadHandle;
        public static bool isRecording;


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

        public static Int64 startPlayBack(DateTime startTime,DateTime endTime,string dvrName)
        {
            throw new NotImplementedException();
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
