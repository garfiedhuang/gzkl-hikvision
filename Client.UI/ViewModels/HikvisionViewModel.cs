using GalaSoft.MvvmLight;
using GZKL.Client.UI.Models;
using GZKL.Client.UI.Common;
using System;
using System.Threading.Tasks;

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

            //��ʼ������
            InitData(loginSuccessModel);
        }

        #region =====data


        public string UserName { get; set; }

        #endregion

        #region ====cmd


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
                this.UserName = loginSuccessModel?.User?.Name;

                GetPCAndRegisterData();//�첽
            }
            catch (Exception ex)
            {
                LogHelper.Error($"��ʼ������ʧ�ܣ�{ex?.Message}");
            }
        }
    }
}