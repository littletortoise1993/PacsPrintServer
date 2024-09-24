
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Printing;
using ZZPacsPrintServer;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;
 

namespace ZZPacsPrintServer
{
    public class StatusUpdateEventArgs : EventArgs
    {
        public ushort EventTypeId { get; private set; }
        public string ExecutionStatusInfo { get; private set; }
        public string FilmSessionLabel { get; private set; }
        public string PrinterName { get; private set; }

        public StatusUpdateEventArgs(ushort eventTypeId, string executionStatusInfo, string filmSessionLabel, string printerName)
        {
            EventTypeId = eventTypeId;
            ExecutionStatusInfo = executionStatusInfo;
            FilmSessionLabel = filmSessionLabel;
            PrinterName = printerName;
        }
    }
    public enum PrintJobStatus : ushort
    {
        Pending = 1,
        Printing = 2,
        Done = 3,
        Failure = 4
    }

    public class PrintJob : DicomDataset
    {
        #region Properties and Attributes

        public bool SendNEventReport { get; set; }

        private readonly object _synchRoot = new object();

        public Guid PrintJobGuid { get; private set; }

        public IList<string> FilmBoxFolderList { get; private set; }

       

        public PrintJobStatus Status { get; private set; }

        public string PrintJobFolder { get; private set; }

        public string FullPrintJobFolder { get; private set; }

        public Exception Error { get; private set; }

        public string FilmSessionLabel { get; private set; }

        private int _currentPage;
        private FilmBox _currentFilmBox;
        /// <summary>
        /// Print job SOP class UID
        /// </summary>
        public readonly DicomUID SOPClassUID = DicomUID.PrintJob;

        /// <summary>
        /// Print job SOP instance UID
        /// </summary>
        public DicomUID SOPInstanceUID { get; private set; }

 
 

        /// <summary>
        /// Date/Time of print job creation.
        /// </summary>
        public DateTime CreationDateTime
        {
            get { return this.GetDateTime(DicomTag.CreationDate, DicomTag.CreationTime); }
            set
            {
                Add(DicomTag.CreationDate, value);
                Add(DicomTag.CreationTime, value);
            }
        }

   

 


        public event EventHandler<StatusUpdateEventArgs> StatusUpdate;
        #endregion

        #region Constructors

        /// <summary>
        /// Construct new print job using specified SOP instance UID. If passed SOP instance UID is missing, new UID will
        /// be generated
        /// </summary>
        /// <param name="sopInstance">New print job SOP instance uID</param>
        public PrintJob(DicomUID sopInstance, string originator)
            : base()
        {
           

            

            if (sopInstance == null || sopInstance.UID == string.Empty)
            {
                SOPInstanceUID = DicomUID.Generate();
            }
            else
            {
                SOPInstanceUID = sopInstance;
            }

            this.Add(DicomTag.SOPClassUID, SOPClassUID);
            this.Add(DicomTag.SOPInstanceUID, SOPInstanceUID);

           

            Status = PrintJobStatus.Pending;

            if (CreationDateTime == DateTime.MinValue)
            {
                CreationDateTime = DateTime.Now;
            }

            PrintJobFolder = SOPInstanceUID.UID;

            var receivingFolder = Environment.CurrentDirectory + @"\PrintJobs";

            FullPrintJobFolder = string.Format(@"{0}\{1}", receivingFolder.TrimEnd('\\'), PrintJobFolder);

            FilmBoxFolderList = new List<string>();
        }

        #endregion

        #region Printing Methods

        public void Print(IList<FilmBox> filmBoxList)
        {
            try
            {
                Status = PrintJobStatus.Pending;             
                var printJobDir = new System.IO.DirectoryInfo(FullPrintJobFolder);
                if (!printJobDir.Exists)
                {
                    printJobDir.Create();
                }
                DicomFile file;
                int filmsCount = FilmBoxFolderList.Count;
                for (int i = 0; i < filmBoxList.Count; i++)
                {
                    var filmBox = filmBoxList[i];
                    var filmBoxDir = printJobDir.CreateSubdirectory(string.Format("F{0:000000}", i + 1 + filmsCount));
                    var dataset = filmBox.BasicImageBoxes[0].ImageSequence;
                    file = new DicomFile(dataset);//假如这里出错则使用默认dicm作为模板保存dicom
                    var dicomFile = string.Format(@"{0}\test.dcm", filmBoxDir.FullName);
                    file.Save(dicomFile);
                    //dicom 转 jpg
                    var imagePath = string.Format(@"{0}\test.jpg", filmBoxDir.FullName);
                    MakeImage(dicomFile, imagePath);//生成图片逻辑放于QT界面端

                }
                FilmSessionLabel = filmBoxList.First().FilmSession.FilmSessionLabel;

                
            }
            catch (Exception ex)
            {
                Error = ex;
                Status = PrintJobStatus.Failure;
               
            }
        }


        private void MakeImage(string dicomFile,string imagePath)
        {
            // 指定要调用的exe文件的路径
            string exePath = AppDomain.CurrentDomain.BaseDirectory+ @"\ConverImageTool\DICOMCoverter.exe";
            // 创建一个Process对象，指定要调用的exe文件的路径
            Process process = new Process();
            // 设置要执行的命令行参数
            string arguments = string.Format(" -dicomfile={0}\"\" -outfile=\"{1}\"", dicomFile, imagePath);
            // 设置进程的启动方式，例如"启动"或"交互式"
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false; // 不使用shell执行，而是直接启动进程
            process.Start();
            // 等待进程完成执行
            process.WaitForExit();
        }







        #endregion

    }
}
