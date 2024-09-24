using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using FellowOakDicom.Printing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;
namespace ZZPacsPrintServer
{
    public class PrintService : DicomService, IDicomServiceProvider, IDicomNServiceProvider, IDicomCEchoProvider
    {

        private readonly object _synchRoot = new object();
        private string CallingAE;
        private string CalledAE;
        private string CallingHost;
        private int CallingHostPort;
        private static QTStudyService qTStudyService = new QTStudyService();
        private static Timer sendMsgTimer = new Timer();
        private static int count = 0;
        private string storeScpAE;

        //
        private FilmSession _filmSession;


        /// <summary>
        /// 初始化自定义变量对象
        /// </summary>
        private void Init()
        {
            sendMsgTimer.Interval = 5000;
            sendMsgTimer.Elapsed += OnSendMessageToAtlas;
            storeScpAE = AppSettingHelper.GetPacsAEName();
        }

        private void OnSendMessageToAtlas(object sender, ElapsedEventArgs e)
        {
            sendMsgTimer.Stop();
            count = 0;
            qTStudyService.UpdateDbInfo();
            SendMessageHelper.SendMessageToAtlasRefreshStudy();
        }
        //定义一下通知工作站更新界面  下面方法调用完后10秒后发生 发生windows消息
        private static ActionBlock<DicomDataset> _actionBlock = new ActionBlock<DicomDataset>(async dataset =>
        {
            sendMsgTimer.Stop();
            count++;
            //TestLogger.Instance.WriteLog($"接收到图像。。。。");
            try
            {
                //检验dataset完整性               
                await qTStudyService.AddDicomImageAsync(dataset);
                sendMsgTimer.Start();
            }
            catch (Exception ex)
            {
                TestLogger.Instance.WriteLog($"图像存储发生异常。。。。{ex.ToString()}");
            }
        });





        public PrintService(INetworkStream stream, Encoding fallbackEncoding, ILogger log, DicomServiceDependencies dependencies)
            : base(stream, fallbackEncoding, log, dependencies)
        {
            Init();
        }

        public void OnConnectionClosed(Exception exception)
        {
            if (exception != null)
                TestLogger.Instance.WriteLog("Connection Closed!\n" + exception.ToString());
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            TestLogger.Instance.WriteLog($"AbortSource: {source}, AbortReason: {reason}");
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return SendAssociationReleaseResponseAsync();
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            CallingAE = association.CallingAE;
            CalledAE = association.CalledAE;
            CallingHost = association.RemoteHost;
            CallingHostPort = association.RemotePort;

            TestLogger.Instance.WriteLog($"接收到新请求：CallingHost： {CallingHost} CallingHostPort:{CallingHostPort} CallingAE:{CallingAE} CalledAE:{CalledAE} ");
            if (storeScpAE != CalledAE)
            {
                TestLogger.Instance.WriteLog($"Association with {CallingAE} rejected since called aet {CalledAE} is unknown");
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }
            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification ||
                    pc.AbstractSyntax == DicomUID.BasicGrayscalePrintManagementMeta ||
                    pc.AbstractSyntax == DicomUID.BasicColorPrintManagementMeta ||
                    pc.AbstractSyntax == DicomUID.Printer ||
                    pc.AbstractSyntax == DicomUID.BasicFilmSession ||
                    pc.AbstractSyntax == DicomUID.BasicFilmBox ||
                    pc.AbstractSyntax == DicomUID.BasicGrayscaleImageBox ||
                    pc.AbstractSyntax == DicomUID.BasicColorImageBox)
                {
                    pc.AcceptTransferSyntaxes(SupportedTransferSyntaxes.AcceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax == DicomUID.PrintJob)
                {
                    pc.AcceptTransferSyntaxes(SupportedTransferSyntaxes.AcceptedTransferSyntaxes);         
                }
                else
                {
                   
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            return SendAssociationAcceptAsync(association);
        }

        public async Task<DicomNActionResponse> OnNActionRequestAsync(DicomNActionRequest request)
        {
            if (_filmSession == null)
            {
                TestLogger.Instance.WriteLog("A basic film session does not exist for this association {0}");
                return new DicomNActionResponse(request, DicomStatus.InvalidObjectInstance);
            }

            lock (_synchRoot)
            {
                try
                {

                    var filmBoxList = new List<FilmBox>();
                    if (request.SOPClassUID == DicomUID.BasicFilmSession && request.ActionTypeID == 0x0001)
                    {
                        TestLogger.Instance.WriteLog($"Creating new print job for film session {_filmSession.SOPInstanceUID.UID}");
                        filmBoxList.AddRange(_filmSession.BasicFilmBoxes);
                    }
                    else if (request.SOPClassUID == DicomUID.BasicFilmBox && request.ActionTypeID == 0x0001)
                    {
                        TestLogger.Instance.WriteLog($"Creating new print job for film box {request.SOPInstanceUID.UID}");

                        var filmBox = _filmSession.FindFilmBox(request.SOPInstanceUID);
                        if (filmBox != null)
                        {
                            filmBoxList.Add(filmBox);
                        }
                        else
                        {                            
                            return new DicomNActionResponse(request, DicomStatus.NoSuchObjectInstance);
                        }
                    }
                    else
                    {
                        if (request.ActionTypeID != 0x0001)
                        {                            
                            return new DicomNActionResponse(request, DicomStatus.NoSuchActionType);
                        }
                        else
                        {                             
                            return new DicomNActionResponse(request, DicomStatus.NoSuchSOPClass);
                        }
                    }
                    try
                    {
                        var SOPInstanceUID = DicomUID.Generate();
                        for (int i = 0; i < filmBoxList.Count; i++)
                        {
                            var filmBox = filmBoxList[i];
                            var dataset = filmBox.BasicImageBoxes[0].ImageSequence;
                            dataset.AddOrUpdate<string>(DicomTag.SOPInstanceUID, SOPInstanceUID.UID);
                            _actionBlock.SendAsync(dataset);
                        }
                        var result = new DicomDataset();
                        result.Add(new DicomSequence(new DicomTag(0x2100, 0x0500),
                            new DicomDataset(new DicomUniqueIdentifier(DicomTag.ReferencedSOPClassUID, DicomUID.PrintJob)),
                            new DicomDataset(new DicomUniqueIdentifier(DicomTag.ReferencedSOPInstanceUID, SOPInstanceUID))));

                        var response = new DicomNActionResponse(request, DicomStatus.Success);
                        response.Command.AddOrUpdate(DicomTag.AffectedSOPInstanceUID, SOPInstanceUID);
                        response.Dataset = result;

                        return response;
                    }
                    catch(Exception ex2)
                    {
                        TestLogger.Instance.WriteLog(ex2.Message);
                        return new DicomNActionResponse(request, DicomStatus.ProcessingFailure);
                    }
                    
                     
                }
                catch (Exception ex)
                {                     
                    TestLogger.Instance.WriteLog(ex.Message);
                    return new DicomNActionResponse(request, DicomStatus.ProcessingFailure);
                }
            }
        }
        
        public async Task<DicomNCreateResponse> OnNCreateRequestAsync(DicomNCreateRequest request)
        {

            lock(_synchRoot)
            {
                if (request.SOPClassUID == DicomUID.BasicFilmSession)
                {
                    return CreateFilmSession(request);
                }
                else if (request.SOPClassUID == DicomUID.BasicFilmBox)
                {
                    return CreateFilmBox(request);
                }
                else
                {
                    return new DicomNCreateResponse(request, DicomStatus.SOPClassNotSupported);
                }
            }
        }

        private DicomNCreateResponse CreateFilmSession(DicomNCreateRequest request)
        {
            if (_filmSession != null)
            {               
                return new DicomNCreateResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            var pc = request.PresentationContext;

            bool isColor = pc != null && pc.AbstractSyntax == DicomUID.BasicColorPrintManagementMeta;
            _filmSession = new FilmSession(request.SOPClassUID, request.SOPInstanceUID, request.Dataset, isColor);
            var response = new DicomNCreateResponse(request, DicomStatus.Success);
            response.Command.AddOrUpdate(DicomTag.AffectedSOPInstanceUID, _filmSession.SOPInstanceUID);
            return response;
        }

        private DicomNCreateResponse CreateFilmBox(DicomNCreateRequest request)
        {
            if (_filmSession == null)
            {              
                return new DicomNCreateResponse(request, DicomStatus.NoSuchObjectInstance);

            }
            var filmBox = _filmSession.CreateFilmBox(request.SOPInstanceUID, request.Dataset);
            if (!filmBox.Initialize())
            {              
                return new DicomNCreateResponse(request, DicomStatus.ProcessingFailure);
            }
           
            var response = new DicomNCreateResponse(request, DicomStatus.Success);
            response.Command.AddOrUpdate(DicomTag.AffectedSOPInstanceUID, filmBox.SOPInstanceUID);
            response.Dataset = filmBox;
            return response;
        }


        public async Task<DicomNDeleteResponse> OnNDeleteRequestAsync(DicomNDeleteRequest request)
        {
            return new DicomNDeleteResponse(request, DicomStatus.Success);
        }

        public async Task<DicomNEventReportResponse> OnNEventReportRequestAsync(DicomNEventReportRequest request)
        {
            return new DicomNEventReportResponse(request, DicomStatus.Success);
        }

        public async Task<DicomNGetResponse> OnNGetRequestAsync(DicomNGetRequest request)
        {
            return new DicomNGetResponse(request, DicomStatus.Success);
        }

        public async Task<DicomNSetResponse> OnNSetRequestAsync(DicomNSetRequest request)
        {
            lock (_synchRoot)
            {
                if (request.SOPClassUID == DicomUID.BasicFilmSession)
                {
                    return SetFilmSession(request);
                }
                else if (request.SOPClassUID == DicomUID.BasicFilmBox)
                {
                    return SetFilmBox(request);
                }
                else if (request.SOPClassUID == DicomUID.BasicColorImageBox ||
                    request.SOPClassUID == DicomUID.BasicGrayscaleImageBox)
                {
                    return SetImageBox(request);
                }
                else
                {
                    return new DicomNSetResponse(request, DicomStatus.SOPClassNotSupported);
                }
            }
        }

        private DicomNSetResponse SetImageBox(DicomNSetRequest request)
        {
            if (_filmSession == null)
            {
               
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

           

            var imageBox = _filmSession.FindImageBox(request.SOPInstanceUID);
            if (imageBox == null)
            {
                 
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            request.Dataset.CopyTo(imageBox);

            return new DicomNSetResponse(request, DicomStatus.Success);
        }

        private DicomNSetResponse SetFilmBox(DicomNSetRequest request)
        {
            if (_filmSession == null)
            {
                 
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

             
            var filmBox = _filmSession.FindFilmBox(request.SOPInstanceUID);

            if (filmBox == null)
            {
                
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            request.Dataset.CopyTo(filmBox);

            filmBox.Initialize();

            var response = new DicomNSetResponse(request, DicomStatus.Success);
            response.Command.Add(DicomTag.AffectedSOPInstanceUID, filmBox.SOPInstanceUID);
            response.Command.Add(DicomTag.CommandDataSetType, (ushort)0x0202);
            response.Dataset = filmBox;
            return response;
        }

        private DicomNSetResponse SetFilmSession(DicomNSetRequest request)
        {
            if (_filmSession == null || _filmSession.SOPInstanceUID.UID != request.SOPInstanceUID.UID)
            {
                
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

             
            request.Dataset.CopyTo(_filmSession);

            return new DicomNSetResponse(request, DicomStatus.Success);
        }

        public async Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }
    }
}
