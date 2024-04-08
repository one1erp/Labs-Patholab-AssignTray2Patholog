using System.Collections.Generic;
using Patholab_DAL_V1;

namespace AssignTray2Patholog
{
    public class Slide2Display
    {

        public Slide2Display(ALIQUOT aliquot, string pathoName)
        {
            this.SlideAliq = aliquot;

            this.SdgName = SlideAliq.SAMPLE.SDG.NAME;
            this.PathoSdgName = SlideAliq.SAMPLE.SDG.SDG_USER.U_PATHOLAB_NUMBER;
            this.PathoAliquotName = pathoName;       
            this.SdgId = SlideAliq.SAMPLE.SDG_ID.Value;
            this.PatientId = SlideAliq.SAMPLE.SDG.SDG_USER.U_PATIENT.Value;
            this.AliquotName = SlideAliq.NAME;
            this.Status = SlideAliq.STATUS;
            Remarks2 = new List<string>();

            this.ShowRemarks = false;
        }

    
        public ALIQUOT SlideAliq { get; set; }


        public bool ShowRemarks { get; set; }
        public string PathoAliquotName { get; set; }
        public string SdgName { get; set; }
        public string PathoSdgName { get; set; }
        public string Status { get; set; }
        public string AliquotName { get; set; }
        public string Remarks { get; private set; }
        public List<string> Remarks2 { get; private set; }
        public int LessSlide4Sdg { get; set; }
        public int LessSlide4Patient { get; set; }
        public bool SameOrNotExistOperator { get; set; }
        public bool EnteredSdg { get; set; }
        public long SdgId { get; set; }
        public long PatientId { get; set; }

        

        public bool IsValid
        {
            get { return LessSlide4Patient < 1 && LessSlide4Sdg < 1 && SameOrNotExistOperator; }

        }
        public void FillExceptional()
        {
            Remarks2.Clear();
            if (!SameOrNotExistOperator)
            {
                Remarks2.Add(operatorMsg);
            }
            if (this.LessSlide4Sdg > 0)
            {
                Remarks2.Add(less4sdgMsg);
            }
            if (this.LessSlide4Patient > 0)
            {
                Remarks2.Add(less4patientMsg);
            }

            if (!EnteredSdg)
            {
                Remarks2.Add(enteredSdgMsg);
            }
        }

        private const string operatorMsg = "פתולוג אינו תואם לדרישה" + "\n";
        private const string less4sdgMsg = "חסרים סליידים לדרישה" + "\n";
        private const string less4patientMsg = "חסרים סליידים לנבדק" + "\n";
        private const string enteredSdgMsg = "לא הוזנה דרישה" + "\n";
    }
}