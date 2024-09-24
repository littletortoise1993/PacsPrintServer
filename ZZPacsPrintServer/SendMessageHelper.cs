using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZZPacsPrintServer
{
   public class SendMessageHelper
    {
        const int WM_USER = 0x0400; // 自定义消息的起始值
        const int WM_MYMESSAGE = WM_USER + 1; // 自定义消息的值
        [DllImportAttribute("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll",SetLastError =true)]
        public static extern bool  PostThreadMessage(uint idThread, uint msg,UIntPtr wParam,IntPtr lParm);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        public const int WM_COPYDATA = 0x004A; // 固定数值，不可更改
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData; // 任意值
            public int cbData;    // 指定lpData内存区域的字节数
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData; // 发送给目标窗口所在进程的数据
        }
        public static void SendMessageToAtlasRefreshStudy()
        {
            //IntPtr hWnd = FindWindow(null, "IntelliAtlas");
            //if (hWnd != IntPtr.Zero)
            //{
            //    // 封装消息
            //    string s = "RefreshStudyEvent";
            //    byte[] sarr = System.Text.Encoding.Default.GetBytes(s);
            //    int len = sarr.Length;
            //    COPYDATASTRUCT cds2;
            //    cds2.dwData = (IntPtr)0;
            //    cds2.cbData = len + 1;
            //    cds2.lpData = s;
            //    // 发送消息
            //    SendMessage(hWnd, WM_COPYDATA, IntPtr.Zero, ref cds2);
            //}
            //else
            //{
            //    TestLogger.Instance.WriteLog($"未找到工作站窗体", LogType.Warning);
            //}
            //System.Diagnostics.Process[] myProcess = System.Diagnostics.Process.GetProcessesByName("IntelliAtlas");

            //foreach (Process process in myProcess)
            //{
            //    uint threadId = GetWindowThreadProcessId(process.MainWindowHandle, IntPtr.Zero);
            //    PostThreadMessage(threadId, WM_MYMESSAGE, UIntPtr.Zero, IntPtr.Zero);
            //}


            ////启动进程来发送消息
            //var FileName = AppDomain.CurrentDomain.BaseDirectory+string.Format(@"SendMessageConsoleApp.exe");
            //string cmdstr2 = "";
            //ExeCommand(FileName, cmdstr2, true);


            //创建文件标识
            var FilePath = AppDomain.CurrentDomain.BaseDirectory + string.Format(@"receive.data");
            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }
        }


        public static void ExeCommand(string exePath, string commandtext, bool waitForExit = true)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = exePath;//要执行的程序名称(属性，获取或设置要启动的应用程序或文档。FileName 属性不需要表示可执行文件。它可以是其扩展名已经与系统上安装的应用程序关联的任何文件类型。)
                    process.StartInfo.Arguments = " " + commandtext;//启动该进程时传递的命令行参数
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = false;//可能接受来自调用程序的输入信息  
                    process.StartInfo.RedirectStandardOutput = false;//由调用程序获取输出信息   
                    process.StartInfo.RedirectStandardError = false;//重定向标准错误输出
                    process.StartInfo.CreateNoWindow = true;//不显示程序窗口
                    process.Start();//启动程序
                    //if (waitForExit)
                    //    process.WaitForExit();//等待程序执行完退出进程(避免进程占用文件或者是合成文件还未生成)*
                }
            }
            catch (Exception ex)
            {
                TestLogger.Instance.WriteLog(ex.ToString());
            }
        }
    }
}
