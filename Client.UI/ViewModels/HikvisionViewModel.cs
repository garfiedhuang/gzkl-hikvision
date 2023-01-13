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
        /// ���캯��
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

            //��ʼ������
            InitData(loginSuccessModel);

            //��ʼ��DVR
            InitDvr();

            //��¼NVR
            LoginDvr();
        }

        #region =====data

        /// <summary>
        /// ����ģ��
        /// </summary>
        private HikvisionModel model;
        public HikvisionModel Model
        {
            get { return model; }
            set
            {
                model = value;
                RaisePropertyChanged();
                HikvisionHelper.m_lChannel = Convert.ToInt32(value);//���õ�ǰѡ�е�ͨ��
            }
        }

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

        /// <summary>
        /// �������ģ��
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

        #region ��ʼ��

        /// <summary>
        /// ��ʼ������
        /// </summary>
        /// <param name="loginSuccessModel"></param>
        public void InitData(LoginSuccessModel loginSuccessModel)
        {
            try
            {
                SessionInfo.Instance.UserInfo = loginSuccessModel.User;

                //�첽��ȡӦ����Ϣ���������ú�ע����Ϣ
                this.GetAppInfo();

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
            try
            {
                HikvisionHelper.InitDvr();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"��ʼ��DVRʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ��¼DVR
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
                        HandyControl.Controls.Growl.Warning("��������¼����豸IP��");
                        return;
                    }
                    if (nvrConfig.DevicePort < 1)
                    {
                        HandyControl.Controls.Growl.Warning("��������¼����豸�˿ڣ�");
                        return;
                    }
                    if (string.IsNullOrEmpty(nvrConfig.UserName))
                    {
                        HandyControl.Controls.Growl.Warning("��������¼����û�����");
                        return;
                    }
                    if (string.IsNullOrEmpty(nvrConfig.Password))
                    {
                        HandyControl.Controls.Growl.Warning("��������¼������룡");
                        return;
                    }
                    HikvisionHelper.LoginDvr(nvrConfig);
                }
                else
                {
                    HandyControl.Controls.Growl.Warning("��������¼����豸��");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"��¼NVRʧ�ܣ�{ex?.Message}");
            }
        }

        #endregion

        #region ¼��

        /// <summary>
        /// �˳�
        /// </summary>
        public void Exit()
        {
            try
            {
                HikvisionHelper.LogoutDvr();

                HikvisionHelper.Dispose();

                Environment.Exit(0);//ǿ���˳�
            }
            catch (Exception ex)
            {
                LogHelper.Error($"�˳�ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ע��
        /// </summary>
        public void Register()
        {
            try
            {
                string msg = string.Empty;
                string fullName = $"{SessionInfo.Instance.ComputerInfo.HostName}-{SessionInfo.Instance.ComputerInfo.CPU}";

                if (string.IsNullOrEmpty(fullName))
                {
                    msg = "������Ϣ��ȡʧ�ܣ����Ժ����ԣ�";
                    LogHelper.Warn(msg);
                    HandyControl.Controls.Growl.Warning(msg);

                    return;
                }

                string sql = "";
                int rowCount = 0;

                //�ж��Ƿ����ע����Ϣ��
                sql = $"SELECT COUNT(1) FROM [sys_config] WHERE [category]='System-{fullName}' AND [value]='Register' AND [is_deleted]=0";

                rowCount = Convert.ToInt32(OleDbHelper.ExecuteScalar(sql) ?? "0");

                if (rowCount > 0)
                {
                    msg = $"��ǰ���ԡ�{fullName}���Ѵ���ע���¼�������ظ�ע��";
                    LogHelper.Warn(msg);
                    HandyControl.Controls.Growl.Warning(msg);

                    return;
                }

                var registerCode = SecurityHelper.DESEncrypt(fullName);
                var registerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var value = string.Empty;
                var text = string.Empty;
                var remark = string.Empty;

                //ע����Ϣд�����ݿ�
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
                            remark = "ϵͳ��Ϣ";
                            break;
                        case 2:
                            value = "CPU";
                            text = SessionInfo.Instance.ComputerInfo.CPU;
                            remark = "ϵͳ��Ϣ";
                            break;
                    }

                    sql = string.Format(sql, $"System-{fullName}", value, text, remark, SessionInfo.Instance.UserInfo.Id);

                    OleDbHelper.ExcuteSql(sql);
                }

                this.Model.IsRegister = false;

                msg = $"��ǰ����{SessionInfo.Instance.ComputerInfo.HostName}ע��ɹ�";

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
        /// Уʱ
        /// </summary>
        public void TimeSet()
        {
            try
            {
                HikvisionHelper.TimeSet();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Уʱʧ�ܣ�{ex?.Message}");
            }
        }

        #endregion

        #region �ط�

        /// <summary>
        /// ��ѯ
        /// </summary>
        public void Query()
        {
            try
            {
                if (string.IsNullOrEmpty(Model.TestNo))
                {
                    HandyControl.Controls.Growl.Warning("���������ţ�");
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
                LogHelper.Error($"��ѯʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ��ʼ�ط�
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
                LogHelper.Error($"��ʼ�ط�ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ֹͣ�ط�
        /// </summary>
        private void StopPlayBack()
        {
            try
            {
                HikvisionHelper.StopPlayBack();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"ֹͣ�ط�ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        private void Download()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"����ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ��ͣ�ط�
        /// </summary>
        private void PausePlayBack()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"��ͣ�ط�ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// �ָ��ط�
        /// </summary>
        private void RecoverPlayBack()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"�ָ��ط�ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ���ٻط�
        /// </summary>
        private void FastPlayBack()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"���ٻط�ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ���ٻط�
        /// </summary>
        private void SlowPlayBack()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Error($"���ٻط�ʧ�ܣ�{ex?.Message}");
            }
        }

        /// <summary>
        /// ����ѡ���¼�
        /// </summary>
        /// <param name="selectedItem"></param>
        private void DataGridSelectionChanged(object selectedItem)
        {
            if (selectedItem == null)
            {
                HandyControl.Controls.Growl.Warning("��ѡ�����¼��");
                return;
            }
            var item = selectedItem as TestData;

            Model.SelectedTestData = item;
        }

        #endregion

        #region ˽�з���

        /// <summary>
        /// ��ȡӦ����Ϣ
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

                    if (!string.IsNullOrEmpty(SessionInfo.Instance.RegisterInfo.RegCode))//��ע��
                    {
                        this.Model.IsRegister = false;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"��ȡӦ����Ϣʧ�ܣ�{ex?.Message}");
                }
            }));
        }

        #endregion

    }
}