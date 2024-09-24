using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZZPacsPrintServer
{
  public  class AppSettingHelper
    {
        public static string GetPacsPort()
        {
            var dataset = DbHelperSQLite.Query(string.Format("select * from app_setting where key='aet_port'"));
            if (dataset.Tables[0].Rows.Count > 0)
            {
                return dataset.Tables[0].Rows[0]["value"].ToString();
            }
            else
            {
                return "119";
            }
        }

        public static string GetPacsAEName()
        {
            var dataset = DbHelperSQLite.Query(string.Format("select * from app_setting where key='aet_name'"));
            if (dataset.Tables[0].Rows.Count > 0)
            {
                return dataset.Tables[0].Rows[0]["value"].ToString();
            }
            else
            {
                return "ZZPrinter";
            }
        }

        public static string GetDCMFolder()
        {
            string configStr = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "../config/AppConfig.json", System.Text.Encoding.UTF8);
            var configObj = JsonConvert.DeserializeObject<AppConfigJson>(configStr, new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace });
            //读取配置文件中的
            string path = configObj.DCMFolder;
            if (string.IsNullOrEmpty(path))
            {
                path= AppDomain.CurrentDomain.BaseDirectory + "../DCMData";
            }
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }             
            return path;
        }

        public static string GetDCMJpgFolder()
        {
            string configStr = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "../config/AppConfig.json", System.Text.Encoding.UTF8);
            var configObj = JsonConvert.DeserializeObject<AppConfigJson>(configStr, new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace });
            //读取配置文件中的
            string path = configObj.DCMJpgFolder;
            if (string.IsNullOrEmpty(path))
            {
                path = AppDomain.CurrentDomain.BaseDirectory + "../DCMJpg";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

    }
}

public class AppConfigJson
{
    public string DCMFolder { set; get; }

    public string DCMJpgFolder { set; get; }
}
