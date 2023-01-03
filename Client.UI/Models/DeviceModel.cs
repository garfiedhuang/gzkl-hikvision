using GalaSoft.MvvmLight;

namespace GZKL.Client.UI.Models
{
    /// <summary>
    /// 设备模型
    /// </summary>
    public class DeviceModel : ObservableObject
    {
        /// <summary>
        /// 设备类型 VR-录像机，CR-摄像机
        /// </summary>
        private string deviceType = "VR";
        public string DeviceType { get { return deviceType; } set { deviceType = value; RaisePropertyChanged(); } }

        /// <summary>
        /// 设备名称
        /// </summary>
        private string deviceName = "";
        public string DeviceName { get { return deviceName; } set { deviceName = value; RaisePropertyChanged(); } }


        /// <summary>
        /// 设备IP
        /// </summary>
        private string deviceIp = "";
        public string DeviceIp { get { return deviceIp; } set { deviceIp = value; RaisePropertyChanged(); } }


        /// <summary>
        /// 设备端口或设备通道号
        /// </summary>
        private int devicePortOrChannelNo;
        public int DevicePortOrChannelNo { get { return devicePortOrChannelNo; } set { devicePortOrChannelNo = value; RaisePropertyChanged(); } }

        /// <summary>
        /// 用户名
        /// </summary>
        private string userName = "";
        public string UserName { get { return userName; } set { userName = value; RaisePropertyChanged(); } }

        /// <summary>
        /// 密码
        /// </summary>
        private string password = "";
        public string Password { get { return password; } set { password = value; RaisePropertyChanged(); } }

    }

    /// <summary>
    /// NVR
    /// </summary>
    public class NvrData
    {
        public int ID { get; set; }
        public string DeviceName { get; set; }
        public string DeviceIp { get; set; }
        public int DevicePort { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// DVR
    /// </summary>
    public class DvrData
    {
        public int ID { get; set; }
        public string DeviceName { get; set; }
        public string DeviceIp { get; set; }
        public int DeviceChannelNo { get; set; }
        public int NVRID { get; set; }
    }
}
