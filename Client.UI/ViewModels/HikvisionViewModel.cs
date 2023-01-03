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
        /// 构造函数
        /// </summary>
        /// <param name="loginSuccessModel"></param>
        public HikvisionViewModel(LoginSuccessModel loginSuccessModel)
        {

            //初始化数据
            InitData(loginSuccessModel);
        }

        #region =====data


        public string UserName { get; set; }

        #endregion

        #region ====cmd


        #endregion


        /// <summary>
        /// 获取电脑和注册数据
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
        /// 初始化数据
        /// </summary>
        /// <param name="loginSuccessModel"></param>
        private void InitData(LoginSuccessModel loginSuccessModel)
        {
            try
            {
                SessionInfo.Instance.UserInfo = loginSuccessModel.User;
                this.UserName = loginSuccessModel?.User?.Name;

                GetPCAndRegisterData();//异步
            }
            catch (Exception ex)
            {
                LogHelper.Error($"初始化数据失败，{ex?.Message}");
            }
        }
    }
}