using GalaSoft.MvvmLight;

namespace GZKL.Client.UI.Models
{
    /// <summary>
    /// Hikvision模型
    /// </summary>
    public class HikvisionModel : ObservableObject
    {
        /// <summary>
        /// 是否注册？
        /// </summary>
        private bool isRegister = true;
        public bool IsRegister
        {
            get { return isRegister; }
            set { isRegister = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 通道号
        /// </summary>
        private string channelNo;
        public string ChannelNo
        {
            get { return channelNo; }
            set { channelNo = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 检测编号1
        /// </summary>
        private string shootingTestNo = "";
        public string ShootingTestNo
        {
            get { return shootingTestNo; }
            set { shootingTestNo = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 检测编号2
        /// </summary>
        private string testNo = "";
        public string TestNo
        {
            get { return testNo; }
            set { testNo = value; RaisePropertyChanged(); }
        }

    }
}
