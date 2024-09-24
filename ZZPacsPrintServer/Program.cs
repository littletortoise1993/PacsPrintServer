using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ZZPacsPrintServer;

namespace ZZPacsPrintServer
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            new DicomSetupBuilder()
              .RegisterServices(s => s
                  .AddFellowOakDicom()
                  .AddImageManager<WinFormsImageManager>())
              .Build();
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new Service1()
                };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                TestLogger.Instance.WriteLog(ex.ToString(), LogType.Error);
            }
            //Test();

        }

        static void Test()
        {
           var _server = (DicomServer<PrintService>)DicomServerFactory.Create<PrintService>(120);
            Console.Read();

            Console.WriteLine("Stopping print service");

        }
    }
}
