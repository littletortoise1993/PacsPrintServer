using FellowOakDicom;
using FellowOakDicom.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZZPacsPrintServer
{
    /// <summary>
    /// 法医项目的图像保存
    /// </summary>
    class QTStudyService
    {
        private List<string> studyIdList=new List<string>();
        private List<string> seriesIdList = new List<string>();
        
        public async Task AddDicomImageAsync(DicomDataset dicom)
        {
            string studyUID = dicom.GetSingleValueOrDefault<string>(DicomTag.StudyInstanceUID, "");
            if(string.IsNullOrEmpty(studyUID))
            {
                studyUID = DicomUID.Generate().UID;
            }
            StudyRecord study = GetStudyRecordByStudyInstanceUID(studyUID);           
            if (study==null)
            {
                await CreateStudyFromDicom(dicom);                
            }
            else
            {               
                if(studyIdList.IndexOf(study.ID) <0)
                {
                    studyIdList.Add(study.ID);
                }                
                await AddImageItem(study, dicom);
            }
        }

        private StudyRecord GetStudyRecordByStudyInstanceUID(string studyUID)
        {
           var dataset= DbHelperSQLite.Query(string.Format("select * from study_record where study_uid='{0}'", studyUID));
           if(dataset.Tables[0].Rows.Count>0)
           {
                StudyRecord study = new StudyRecord();
                study.ID = dataset.Tables[0].Rows[0]["id"].ToString();
                study.StudyInstanceUID = dataset.Tables[0].Rows[0]["study_uid"].ToString();                
                study.StudyTime = dataset.Tables[0].Rows[0]["study_time"].ToString();
                study.StudyDescription = dataset.Tables[0].Rows[0]["study_description"].ToString();
                study.PatientID = dataset.Tables[0].Rows[0]["patient_id"].ToString();
                study.PatientName = dataset.Tables[0].Rows[0]["patient_name"].ToString();
                study.PatientSex = dataset.Tables[0].Rows[0]["patient_sex"].ToString();
                study.PatientBirthDate = dataset.Tables[0].Rows[0]["patient_birthdate"].ToString();
                study.PatientAge = dataset.Tables[0].Rows[0]["patient_age"].ToString();
                study.AccessionNumber = dataset.Tables[0].Rows[0]["accession_number"].ToString();               
                study.UpdateDateTime = dataset.Tables[0].Rows[0]["update_dateTime"].ToString();
                study.StudyStatus = dataset.Tables[0].Rows[0]["study_status"].ToString();
                return study;
            }
           else
            {
                return null;
            }
        }

        private StudyRecord GetStudyRecordByID(string id)
        {
            var dataset = DbHelperSQLite.Query(string.Format("select * from study_record where id='{0}'", id));
            if (dataset.Tables[0].Rows.Count > 0)
            {
                StudyRecord study = new StudyRecord();
                study.ID = dataset.Tables[0].Rows[0]["id"].ToString();
                study.StudyInstanceUID = dataset.Tables[0].Rows[0]["study_uid"].ToString();                
                study.StudyTime = dataset.Tables[0].Rows[0]["study_time"].ToString();
                study.StudyDescription = dataset.Tables[0].Rows[0]["study_description"].ToString();
                study.PatientID = dataset.Tables[0].Rows[0]["patient_id"].ToString();
                study.PatientName = dataset.Tables[0].Rows[0]["patient_name"].ToString();
                study.PatientSex = dataset.Tables[0].Rows[0]["patient_sex"].ToString();
                study.PatientBirthDate = dataset.Tables[0].Rows[0]["patient_birthdate"].ToString();
                study.PatientAge = dataset.Tables[0].Rows[0]["patient_age"].ToString();
                study.AccessionNumber = dataset.Tables[0].Rows[0]["accession_number"].ToString();                 
                study.UpdateDateTime = dataset.Tables[0].Rows[0]["update_dateTime"].ToString();
                study.StudyStatus = dataset.Tables[0].Rows[0]["study_status"].ToString();
                return study;
            }
            else
            {
                return null;
            }
        }

        private SeriesRecord GetSeriesRecordByStudyDbIdSeriesInstanceUID(string seriesInstanceUID, string studyDbId)
        {
            var dataset = DbHelperSQLite.Query(string.Format("select * from print_record where series_uid='{0}' and study_id='{1}'", seriesInstanceUID, studyDbId));
            if (dataset.Tables[0].Rows.Count > 0)
            {
                SeriesRecord series = new SeriesRecord();
                series.ID = dataset.Tables[0].Rows[0]["id"].ToString();                 
                series.SeriesDescription = dataset.Tables[0].Rows[0]["series_description"].ToString();
                series.PrintDate = dataset.Tables[0].Rows[0]["print_date"].ToString();
                series.ImageCount = dataset.Tables[0].Rows[0]["image_count"].ToString();
                series.Path = dataset.Tables[0].Rows[0]["path"].ToString();
                series.SeriesInstanceUID = dataset.Tables[0].Rows[0]["series_uid"].ToString();
                series.StudyId = dataset.Tables[0].Rows[0]["study_id"].ToString();                
                return series;
            }
            else
            {
                return null;
            }
        }

        private SeriesRecord GetSeriesRecordByID(string id)
        {
            var dataset = DbHelperSQLite.Query(string.Format("select * from print_record where id='{0}'", id));
            if (dataset.Tables[0].Rows.Count > 0)
            {
                SeriesRecord series = new SeriesRecord();
                series.ID = dataset.Tables[0].Rows[0]["id"].ToString();
                series.PrintDate = dataset.Tables[0].Rows[0]["print_date"].ToString();
                series.SeriesDescription = dataset.Tables[0].Rows[0]["series_description"].ToString();                 
                series.ImageCount = dataset.Tables[0].Rows[0]["image_count"].ToString();
                series.Path = dataset.Tables[0].Rows[0]["path"].ToString();
                series.StudyId = dataset.Tables[0].Rows[0]["study_id"].ToString();
                return series;
            }
            else
            {
                return null;
            }
        }

        private async Task<StudyRecord> CreateStudyFromDicom(DicomDataset dicom)
        {
            StudyRecord study = new StudyRecord();
            study.ID = "";
            string studyUID = dicom.GetSingleValueOrDefault<string>(DicomTag.StudyInstanceUID, "");
            if (string.IsNullOrEmpty(studyUID))
            {
                studyUID = DicomUID.Generate().UID;
            }
            study.StudyInstanceUID = studyUID;
            study.StudyTime = dicom.GetSingleValueOrDefault<string>(DicomTag.StudyTime, string.Empty).TrimEnd('\0');
            study.StudyDescription = dicom.GetSingleValueOrDefault<string>(DicomTag.StudyDescription, string.Empty).TrimEnd('\0');
            study.PatientID = dicom.GetSingleValueOrDefault<string>(DicomTag.PatientID, string.Empty).TrimEnd('\0');
            study.PatientName = dicom.GetSingleValueOrDefault<string>(DicomTag.PatientName, string.Empty).TrimEnd('\0');
            study.PatientSex = dicom.GetSingleValueOrDefault<string>(DicomTag.PatientSex, string.Empty).TrimEnd('\0');
            study.PatientBirthDate = dicom.GetSingleValueOrDefault<string>(DicomTag.PatientBirthDate, string.Empty).TrimEnd('\0');
            study.PatientAge = dicom.GetSingleValueOrDefault(DicomTag.PatientAge, string.Empty).TrimEnd('\0');
            study.AccessionNumber = dicom.GetSingleValueOrDefault<string>(DicomTag.AccessionNumber, string.Empty).TrimEnd('\0');
            study.UpdateDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            study.StudyStatus = "0";
            //保存study
            if(AddStudyRecordToDb(study))
            {
                study = GetStudyRecordByStudyInstanceUID(study.StudyInstanceUID);
                await AddImageItem(study, dicom);
            }
            return study;
        }


        private SeriesRecord CreateSeriesFromDicom(DicomDataset dicom, StudyRecord study)
        {
            SeriesRecord series = new SeriesRecord();
            series.ID = "";
            series.SeriesDescription = dicom.GetSingleValueOrDefault(DicomTag.SeriesDescription, string.Empty).TrimEnd('\0');
            series.ImageCount = "1";            
            series.StudyId = study.ID;
            string SeriesInstanceUID = dicom.GetSingleValueOrDefault<string>(DicomTag.SeriesInstanceUID, "");
            if (string.IsNullOrEmpty(SeriesInstanceUID))
            {
                SeriesInstanceUID = DicomUID.Generate().UID;
            }
            series.SeriesInstanceUID = SeriesInstanceUID;
            //
            string seriesPath = GetSeriesFolder(study.StudyInstanceUID, series.SeriesInstanceUID);
            Directory.CreateDirectory(seriesPath);
            series.Path = seriesPath;
            if (AddSeriesRecordToDb(series))
            {
                series = GetSeriesRecordByStudyDbIdSeriesInstanceUID(series.SeriesInstanceUID, study.ID);
                //将文件写入路径
            }
            return series;
        }
        private bool AddStudyRecordToDb(StudyRecord study)
        {
            string insertSql = string.Format(@"insert into study_record(
            study_uid, study_time,study_description,
            accession_number,patient_name,patient_id,patient_birthdate,
            patient_sex,patient_age, study_status,update_dateTime )
             values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}')",
             study.StudyInstanceUID, study.StudyTime,study.StudyDescription,
             study.AccessionNumber,study.PatientName,study.PatientID,study.PatientBirthDate,
             study.PatientSex,study.PatientAge, 
             study.StudyStatus,study.UpdateDateTime) ;
            if (DbHelperSQLite.ExecuteSql(insertSql)>0)
            {
                return true;
            }
            return false;
        }


        private bool UpdateStudyRecordToDb(StudyRecord study)
        {
            string insertSql = string.Format(@"update study_record set update_dateTime='{0}' where  id='{1}'", study.UpdateDateTime,study.ID);
            if (DbHelperSQLite.ExecuteSql(insertSql) > 0)
            {
                return true;
            }
            return false;
        }

        private bool UpdateSeriesRecordToDb(SeriesRecord series)
        {
            string insertSql = string.Format(@"update print_record set image_count='{0}',
             path='{1}' where  id='{2}'", series.ImageCount, series.Path, series.ID);
            if (DbHelperSQLite.ExecuteSql(insertSql) > 0)
            {
                return true;
            }
            return false;
        }


        private bool AddSeriesRecordToDb(SeriesRecord series)
        {
            string insertSql = string.Format(@"insert into print_record(
            series_uid,series_description,image_count,path,study_id)
             values('{0}','{1}','{2}','{3}','{4}')",
             series.SeriesInstanceUID,series.SeriesDescription, series.ImageCount, series.Path, series.StudyId);
            if (DbHelperSQLite.ExecuteSql(insertSql) > 0)
            {
                return true;
            }
            return false;
        }

        private bool AddMakeJPGRecordToDb(List<string> parms)
        {
            string insertSql = string.Format(@"insert into make_jpg_record(
            status,study_uid,series_uid,sop_uid,image_path,print_times,create_dateTime)
             values(0,'{0}','{1}','{2}','{3}',0,'{4}')",
            parms[0], parms[1], parms[2], parms[3], DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            if (DbHelperSQLite.ExecuteSql(insertSql) > 0)
            {
                return true;
            }
            return false;
        }


        private string GetSeriesFolder(string studyUID, string seriesUID)
        {
            return Path.Combine(AppSettingHelper.GetDCMFolder(), studyUID, seriesUID);
        }

        private string GetImageFile(string seriesPath, string sopUID)
        {
            return Path.Combine(seriesPath, sopUID + ".dcm");
        }
        private async Task AddImageItem(StudyRecord study, DicomDataset dicom)
        {
            string SeriesInstanceUID = dicom.GetSingleValueOrDefault<string>(DicomTag.SeriesInstanceUID, "");
            if (string.IsNullOrEmpty(SeriesInstanceUID))
            {
                SeriesInstanceUID = DicomUID.Generate().UID;
            }
            string seriesUID = SeriesInstanceUID;
            //判断序列信息是否已存在
            SeriesRecord series = GetSeriesRecordByStudyDbIdSeriesInstanceUID(seriesUID, study.ID);          
            if (series == null)
            {
                //新建
                series= CreateSeriesFromDicom(dicom, study);
            }
            //保存文件
            string imageUID = dicom.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            var  filename = GetImageFile(series.Path, imageUID);
            //做必要tag校验是否存在
            string sopClassId = dicom.GetSingleValueOrDefault<string>(DicomTag.SOPClassUID, "");
            if(string.IsNullOrEmpty(sopClassId))
            {
                dicom.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.Generate().UID);
            }

            DicomFile file = new DicomFile(dicom);
            await file.SaveAsync(filename);
            //将数据插入待生成图片记录
            List<string> parms=new List<string>();
            parms.Add(study.StudyInstanceUID);
            parms.Add(seriesUID);
            parms.Add(imageUID);
            //将dcm生成图片
            try
            {
                var colorImg = new DicomImage(filename);
                using (var bitmap = colorImg.RenderImage().As<Bitmap>())
                {
                    string thumbnail_path = AppSettingHelper.GetDCMJpgFolder() + string.Format("/{0}.jpeg", imageUID);
                    bitmap.Save(thumbnail_path, System.Drawing.Imaging.ImageFormat.Jpeg);
                    parms.Add(thumbnail_path);
                }
            }
            catch (Exception ex)
            {
                TestLogger.Instance.WriteLog($"生成jpeg发生错误。。。。{ex.ToString()}");
            }
            AddMakeJPGRecordToDb(parms);
            if (seriesIdList.IndexOf(series.ID) < 0)
            {
                seriesIdList.Add(series.ID);
            }
        }


        private List<StudyRecord> GetStudyRecordBySqlWhere(string sqlWhere)
        {
            List<StudyRecord> studyRecords = new List<StudyRecord>();
            var dataset = DbHelperSQLite.Query(string.Format("select * from study_record where {0}", sqlWhere));
            for(int i=0;i< dataset.Tables[0].Rows.Count;i++)
            {
                StudyRecord study = new StudyRecord();
                study.ID = dataset.Tables[0].Rows[i]["id"].ToString();
                study.StudyInstanceUID = dataset.Tables[0].Rows[i]["study_uid"].ToString();
                
                study.StudyTime = dataset.Tables[0].Rows[i]["study_time"].ToString();
                study.StudyDescription = dataset.Tables[0].Rows[i]["study_description"].ToString();
                study.PatientID = dataset.Tables[0].Rows[i]["patient_id"].ToString();
                study.PatientName = dataset.Tables[0].Rows[i]["patient_name"].ToString();
                study.PatientSex = dataset.Tables[0].Rows[i]["patient_sex"].ToString();
                study.PatientBirthDate = dataset.Tables[0].Rows[i]["patient_birthdate"].ToString();
                study.PatientAge = dataset.Tables[0].Rows[i]["patient_age"].ToString();
                study.AccessionNumber = dataset.Tables[0].Rows[i]["accession_number"].ToString();                
                study.UpdateDateTime = dataset.Tables[0].Rows[i]["update_dateTime"].ToString();
                study.StudyStatus = dataset.Tables[0].Rows[i]["study_status"].ToString();
                studyRecords.Add(study);
            }
            return studyRecords;


        }


        private List<SeriesRecord> GetSeriesRecordBySqlWhere(string sqlWhere)
        {
            List<SeriesRecord> studyRecords = new List<SeriesRecord>();
            var dataset = DbHelperSQLite.Query(string.Format("select * from print_record where {0}", sqlWhere));
            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
            {
                SeriesRecord series = new SeriesRecord();
                series.ID = dataset.Tables[0].Rows[i]["id"].ToString();
                series.SeriesInstanceUID = dataset.Tables[0].Rows[i]["series_uid"].ToString();                 
                series.SeriesDescription = dataset.Tables[0].Rows[i]["series_description"].ToString();
                series.ImageCount = dataset.Tables[0].Rows[i]["image_count"].ToString();
                series.Path = dataset.Tables[0].Rows[i]["path"].ToString();
                series.StudyId = dataset.Tables[0].Rows[i]["study_id"].ToString();
                studyRecords.Add(series);
            }
            return studyRecords;


        }

        //===========================查询=======================
        public IEnumerable<DicomDataset> FindPatient(string patientName, string patientId)
        {
            // Find the matching DicomStudy.
            var query = GetStudyRecordBySqlWhere(string.Format(@" patient_name='{0}' and patient_id='{1}'",patientName,patientId));
            // Turn to DicomDataset.
            foreach (var item in query)
            {

                //查询序列个数

                DicomDataset dataset = new DicomDataset();
                dataset.AddOrUpdate(DicomTag.StudyInstanceUID, item.StudyInstanceUID)                    
                    .AddOrUpdate(DicomTag.PatientName, item.PatientName)
                    .AddOrUpdate(DicomTag.PatientID, item.PatientID)                   
                    .AddOrUpdate(DicomTag.StudyTime, item.StudyTime)
                    .AddOrUpdate(DicomTag.StudyDescription, item.StudyDescription)
                    .AddOrUpdate(DicomTag.NumberOfStudyRelatedSeries, 0);                
                dataset.AddOrUpdate(DicomTag.PatientID, item.PatientID);
                dataset.AddOrUpdate(DicomTag.PatientName, item.PatientName);
                dataset.AddOrUpdate(DicomTag.StudyDescription, item.StudyDescription);
                
                yield return dataset;
            }
        }

        public IEnumerable<DicomDataset> FindStudy(string patientName, string patientId, string accessionNbr, string studyUID)
        {
            var query = GetStudyRecordBySqlWhere(string.Format(@" patient_name like '%{0}%' and patient_id  like '%{1}%'
          and accession_number like '%{2}%' and study_uid like '%{3}%'", patientName, patientId, accessionNbr, studyUID));
            // Turn to DicomDataset.
            foreach (var item in query)
            {

                //查询序列个数

                DicomDataset dataset = new DicomDataset();
                dataset.AddOrUpdate(DicomTag.StudyInstanceUID, item.StudyInstanceUID)
                    
                    .AddOrUpdate(DicomTag.PatientName, item.PatientName)
                    .AddOrUpdate(DicomTag.PatientID, item.PatientID)                   
                    .AddOrUpdate(DicomTag.StudyDescription, item.StudyDescription)
                    .AddOrUpdate(DicomTag.NumberOfStudyRelatedSeries, 0);
               
                dataset.AddOrUpdate(DicomTag.PatientID, item.PatientID);
                dataset.AddOrUpdate(DicomTag.PatientName, item.PatientName);
                dataset.AddOrUpdate(DicomTag.PatientAge, item.PatientAge);
                dataset.AddOrUpdate(DicomTag.PatientSex, item.PatientSex);
                dataset.AddOrUpdate(DicomTag.StudyDescription, item.StudyDescription);
                
                dataset.AddOrUpdate(DicomTag.AccessionNumber, item.AccessionNumber);
              
              
                
               
                yield return dataset;
            }
        }

        public IEnumerable<DicomDataset> FindSeries(string patientName, string patientId, string accessionNbr, string studyUID, string seriesUID, string modality)
        {
            // Find the matching DicomStudy.
            var query = GetStudyRecordBySqlWhere(string.Format(@" study_uid='{0}'",  studyUID));

            foreach (var study in query)
            {
                var series = GetSeriesRecordBySqlWhere(string.Format(@"study_id='{2}' and series_uid like '%{0}%' and modality like '%{1}%'", seriesUID, modality, study.ID));
                foreach (var item in series)
                {
                    //读取dicom
                    var file = DicomFile.Open(Directory.GetFiles(item.Path).FirstOrDefault());
                    DicomDataset dataset = file.Dataset;
                    dataset.AddOrUpdate(DicomTag.StudyInstanceUID, study.StudyInstanceUID)

                      .AddOrUpdate(DicomTag.PatientName, study.PatientName)
                      .AddOrUpdate(DicomTag.PatientID, study.PatientID)
                      .AddOrUpdate(DicomTag.SeriesInstanceUID, item.SeriesInstanceUID)

                      .AddOrUpdate(DicomTag.SeriesDescription, item.SeriesDescription)
                      .AddOrUpdate(DicomTag.NumberOfSeriesRelatedInstances, item.ImageCount.ToString());
                   

                    yield return dataset;
                }
            }
        }


        public List<string> FindFilesByStudy(string studyUID)
        {
            List<string> imageList= new List<string>();
            var study = GetStudyRecordByStudyInstanceUID(studyUID);
            var query = GetSeriesRecordBySqlWhere(string.Format(@" study_id='{0}'  ", study.ID));
            foreach(var item in query)
            {
                string[] files = Directory.GetFiles(item.Path, "*.*", SearchOption.AllDirectories);
                imageList.AddRange(files);
            }           
            return imageList;
        }

        public  List<string> FindFilesBySeries(string studyUID, string seriesUID)
        {
            List<string> imageList = new List<string>();
            var query = GetSeriesRecordBySqlWhere(string.Format(@" series_uid='{0}'  ", seriesUID));
            foreach (var item in query)
            {
                string[] files = Directory.GetFiles(item.Path, "*.*", SearchOption.AllDirectories);
                imageList.AddRange(files);
            }
            return imageList;
        }


        public void UpdateDbInfo()
        {
            var studyIdListTemp = new List<string>(studyIdList);
            var seriesIdListTemp = new List<string>(seriesIdList);
            studyIdList.Clear();
            seriesIdList.Clear();     
            for (int i=0;i< studyIdListTemp.Count;i++)
            {
                try
                {
                    var study = GetStudyRecordByID(studyIdListTemp[i]);
                    study.UpdateDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    UpdateStudyRecordToDb(study);
                }
                catch(Exception ex)
                {
                    TestLogger.Instance.WriteLog($"UpdateStudyRecordToDb发生错误。。。。{ex.ToString()}");
                }
               
            }

            for (int i = 0; i < seriesIdListTemp.Count; i++)
            {
                try
                {
                    var series = GetSeriesRecordByID(seriesIdListTemp[i]);
                    //更新SeriesRecord  文件数量
                    int fileCount = Directory.GetFiles(series.Path).Length;
                    series.ImageCount = fileCount.ToString();
                    UpdateSeriesRecordToDb(series);
                }
                catch (Exception ex)
                {
                    TestLogger.Instance.WriteLog($"UpdateSeriesRecordToDb发生错误。。。。{ex.ToString()}");
                }

            }
        }
    }
}
