using FellowOakDicom.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ZZPacsPrintServer
{
    public partial class Service1 : ServiceBase
    {
        int StoreScpPort = 119;
        public Service1()
        {
            InitializeComponent();
            StoreScpPort = Convert.ToInt32(AppSettingHelper.GetPacsPort());           
        }       

        private static DicomServer<PrintService> _server;

        protected override void OnStart(string[] args)
        {
           
            TestLogger.Instance.WriteLog($"启动胶片打印接收服务器中。。。。");             
            _server = (DicomServer<PrintService>)DicomServerFactory.Create<PrintService>(StoreScpPort);
            string storeScpAE = AppSettingHelper.GetPacsAEName();
            TestLogger.Instance.WriteLog($"启动胶片打印接收服务器Success,端口：{StoreScpPort} AE:{storeScpAE}");
        }

        protected override void OnStop()
        {
            _server.Dispose();
            TestLogger.Instance.WriteLog($"关闭胶片打印接收服务器。。。。");
        }
    }
}
