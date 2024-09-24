using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZZPacsPrintServer
{
   public class StudyRecord
    {
		public string ID;
		public string StudyInstanceUID;		 
		public string StudyTime;
		public string StudyDescription;
		public string PatientID;
		public string PatientName;
		public string PatientSex;
		public string PatientBirthDate;
		public string PatientAge;
		public string AccessionNumber;		
		public string UpdateDateTime;
		public string StudyStatus;
	}

	public class SeriesRecord
	{
		public string ID;		
		public string PrintDate;
		public string SeriesDescription;	
		public string Path;
		public string ImageCount;
		public string StudyId;
		public string SeriesInstanceUID;
	}
}
