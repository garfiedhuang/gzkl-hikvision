using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GZKL.Client.UI.Common;
using GZKL.Client.UI.Models;
using GZKL.Client.UI.ViewsModels;
using MessageBox = HandyControl.Controls.MessageBox;

namespace GZKL.Client.UI.Views.SystemMgt.Device
{
    /// <summary>
    /// Device.xaml 的交互逻辑
    /// </summary>
    public partial class Device
    {
        public Device()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 数据查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var viewModel = this.DataContext as AutoCollectViewModel;
                //var collectDataEnum = (CollectDataEnum)viewModel.Model.InterfaceId;

                ////采集引擎工厂
                //var collectEngine = CreateCollectEngine.Create(collectDataEnum);

                ////查询本地数据库
                //viewModel.QueryData();

                ////查询设备数据库
                //collectEngine.QueryDeviceData(viewModel);

                ////启用【数据入库】按钮
                //this.btnSave.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"操作提示");
            }
        }

        /// <summary>
        /// 数据入库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                

                //var viewModel = this.DataContext as AutoCollectViewModel;

                //var result = viewModel.CheckValue();

                //if (result == -1)
                //{
                //    return;
                //}

                ////采集引擎工厂
                //var collectDataEnum = (CollectDataEnum)viewModel.Model.InterfaceId;
                //var collectEngine = CreateCollectEngine.Create(collectDataEnum);

                //var interfaceImportDetailInfo = new InterfaceImportDetailInfo()
                //{
                //    InterfaceId = viewModel.Model.InterfaceId,
                //    InterfaceTestItemId = viewModel.Model.InterfaceTestItemId,
                //    SystemTestItemNo = viewModel.Model.SystemTestItemNo,
                //    SampleNo = viewModel.Model.QuerySampleNo,
                //    TestNo = viewModel.Model.QueryTestNo,
                //    Remark = viewModel.Model.InterfaceName
                //};

                //collectEngine.AddImportDetail(interfaceImportDetailInfo);

                ////写入数据库
                //collectEngine.ImportData(viewModel);

                //this.tiProcessed.IsSelected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "操作提示");
            }
        }
    }
}
