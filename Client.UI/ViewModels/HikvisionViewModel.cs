using GalaSoft.MvvmLight;
using GZKL.Client.UI.Models;
using GZKL.Client.UI.Common;
using System;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using CommonServiceLocator;
using System.IO;

namespace GZKL.Client.UI.ViewsModels
{
    public class HikvisionViewModel : ViewModelBase
    {
        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="loginSuccessModel"></param>
        public HikvisionViewModel(LoginSuccessModel loginSuccessModel)
        {
            this.TimeSetCmd = new RelayCommand(this.TimeSet);
            this.RegisterCmd = new RelayCommand(this.Register);
            this.ExitCmd = new RelayCommand(this.Exit);

            this.StartShootingCmd = new RelayCommand(this.StartShooting);
            this.StopShootingCmd = new RelayCommand(this.StopShooting);

            //��ʼ������
            InitData(loginSuccessModel);

            //��ʼ��DVR
            InitDvr();
        }

        #region =====data

        /// <summary>
        /// NVRģ��
        /// </summary>
        private ObservableCollection<NvrData> nvrData;
        public ObservableCollection<NvrData> NvrData
        {
            get { return nvrData; }
            set { nvrData = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// DVRģ��
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
        /// ��ȡ���Ժ�ע������
        /// </summary>
        private void GetPCAndRegisterData()
        {
            Task.Run(new Action(() =>
            {
                SessionInfo.Instance.ComputerInfo = ComputerInfo.GetInstance().ReadComputerInfo();

                var fullName = $"{SessionInfo.Instance.ComputerInfo.HostName}-{SessionInfo.Instance.ComputerInfo.CPU}";

                SessionInfo.Instance.RegisterInfo = RegisterInfo.GetInstance().GetRegisterInfo(fullName);
            }));
        }

        /// <summary>
        /// ��ʼ������
        /// </summary>
        /// <param name="loginSuccessModel"></param>
        private void InitData(LoginSuccessModel loginSuccessModel)
        {
            try
            {
                SessionInfo.Instance.UserInfo = loginSuccessModel.User;

                //�첽��ȡ�������ú�ע����Ϣ
                GetPCAndRegisterData();

                //����¼��������������
                var deviceViewModel = ServiceLocator.Current.GetInstance<DeviceViewModel>();

                deviceViewModel.QueryDevice();

                this.DvrData = deviceViewModel.DvrData;
                this.NvrData = deviceViewModel.NvrData;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"��ʼ������ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ��ʼ��DVR
        /// </summary>
        public void InitDvr()
        {
            HikvisionHelper.m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            if (HikvisionHelper.m_bInitSDK == false)
            {
                LogHelper.Error($"NET_DVR_Init error!");
                return;
            }
            else
            {
                var sdkLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SdkLog");

                //����SDK��־
                CHCNetSDK.NET_DVR_SetLogToFile(3, sdkLogPath, true);
            }
        }

        /// <summary>
        /// �˳�
        /// </summary>
        private void Exit()
        {
            //ֹͣ�ط� Stop playback
            if (HikvisionHelper.m_lPlayHandle >= 0)
            {
                CHCNetSDK.NET_DVR_StopPlayBack(HikvisionHelper.m_lPlayHandle);
                HikvisionHelper.m_lPlayHandle = -1;
            }

            //ֹͣ���� Stop download
            if (HikvisionHelper.m_lDownHandle >= 0)
            {
                CHCNetSDK.NET_DVR_StopGetFile(HikvisionHelper.m_lDownHandle);
                HikvisionHelper.m_lDownHandle = -1;
            }

            //ע����¼ Logout the device
            if (HikvisionHelper.m_lUserID >= 0)
            {
                CHCNetSDK.NET_DVR_Logout(HikvisionHelper.m_lUserID);
                HikvisionHelper.m_lUserID = -1;
            }

            //�˳���������
            Environment.Exit(0);
        }

        private void Register()
        {
            throw new NotImplementedException();
        }

        private void TimeSet()
        {
            throw new NotImplementedException();
        }


        private void StopShooting()
        {
            throw new NotImplementedException();
        }

        private void StartShooting()
        {
            throw new NotImplementedException();
        }
    }
}