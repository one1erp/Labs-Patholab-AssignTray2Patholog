using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using LSSERVICEPROVIDERLib;
using Patholab_Common;
using Patholab_DAL_V1;

namespace AssignTray2Patholog
{
    public class Manager
    {
        #region Ctor

        // אין סליידים ששוייכו לפתולוג. 
        //יש מקרה המשוייך לפתולוג, ויש סליידים המשויכים למקרה.
        //ייתכן שכול הסליידים / חלקם / אף אחד מהם – הועברו כבר לפתולוג


        public Manager ( bool debug, INautilusDBConnection ntlsCon )
        {
            // this.dal = dal;
            if ( ntlsCon != null )
                _sid = ( long ) ntlsCon.GetSessionId ( );
            dal = new DataLayer ( );

            if ( debug )
                dal.MockConnect ( );
            else
                dal.Connect ( ntlsCon );

            _EnterdSdgs = new List<SdgItem> ( );
            //            _EnterdSdgs = new List<SDG> ( );
            _clients = new List<CLIENT> ( );
            //     Slides = new List<ALIQUOT>();
            _slides2Displays = new List<Slide2Display> ( );
        }

        #endregion

        public void Close ( )
        {
            if ( dal != null ) dal.Close ( );
        }

        internal bool haveSlides ( )
        {
            if ( _slides2Displays.Count < 1 ) return false;
            return _slides2Displays.Count > 0;
        }

        #region Properties

        private const string mboxHeader = "שיוך מגש לפתולוג";
        private readonly string[] _validStatuses = { "V", "C", "P", "I","D" };
        private readonly DataLayer dal;
        public long PathologId { get; set; }
        //  List<SDG> _EnterdSdgs;// { get; }
        List<SdgItem> _EnterdSdgs;// { get; }
        private List<Slide2Display> _slides2Displays;// { get; }
        private readonly List<CLIENT> _clients;
        private readonly long _sid;

        #endregion


        #region Public Methods

        public string AddSdg ( string input )
        {
            var sdg = dal.FindBy<SDG> ( x => x.NAME == input )
                .Include ( s => s.SDG_USER ).SingleOrDefault ( );
            if ( sdg == null )
                return "דרישה לא קימת.";
            if ( !_validStatuses.Contains ( sdg.STATUS ) )
                return "דרישה אינה בסטטוס המתאים.";
            AddSdg ( sdg );

            return "";
        }

        public void RemoveSdg ( SDG sdg )
        {
            var sdgItem = _EnterdSdgs.FirstOrDefault ( x => x.Sdg.SDG_ID == sdg.SDG_ID );
            _EnterdSdgs.Remove ( sdgItem );
            var aliqn = _slides2Displays.Where ( al => al.SdgName == sdg.NAME ).Select ( x => x.AliquotName ).ToList ( );
            foreach ( var item in aliqn ) RemoveSlide ( item );
        }

        public string AddSlide ( string input )
        {
            //   InputSlide = null;
            var slide = ( from al in dal.GetAll<ALIQUOT> ( ).Include ( a => a.ALIQUOT_USER )
                          where al.NAME == input &&
                                al.ALIQUOT_USER.U_GLASS_TYPE == "S"
                          select al ).Include ( a => a.ALIQUOT_USER ).SingleOrDefault ( );
            if ( slide == null ) return "סלייד לא קיים.";
            if ( !_validStatuses.Contains ( slide.STATUS ) ) return "סלייד אינו בסטטוס המתאים.";

            if ( slide.ALIQUOT_USER.U_SEND_TO_PATHOLOG_ON.HasValue ) return "סלייד כבר שויך לפתולוג.";

            AddSlide ( slide );

            return "";
        }

        public void RemoveSlide ( string aliquotName )
        {
            //   System.Diagnostics.Debugger.Launch();
            var al2remove = _slides2Displays.FirstOrDefault ( x => x.AliquotName == aliquotName );
            if ( al2remove != null )
            {
                _slides2Displays.Remove ( al2remove );
                var baseSdg = al2remove.SlideAliq.SAMPLE.SDG;
                var sameOperator = baseSdg.SDG_USER.U_PATHOLOG.HasValue &&
                                   baseSdg.SDG_USER.U_PATHOLOG.Value == PathologId;
                var lessSlides4Sdg = MissSlides4Sdg ( baseSdg );
                var lessSlides4Patient = MissSlides4Patient ( baseSdg );
                //  var enterdSdg = _EnterdSdgs.Contains ( baseSdg );
                UpdateSdgHasSlides ( baseSdg.SDG_ID );

                foreach ( var slide in _slides2Displays )
                {
                    if ( slide.PatientId == baseSdg.SDG_USER.U_PATIENT.Value )
                        slide.LessSlide4Patient = lessSlides4Patient;
                    if ( slide.SdgId == baseSdg.SDG_ID )
                    {
                        slide.LessSlide4Sdg = lessSlides4Sdg;
                        slide.SameOrNotExistOperator = sameOperator;
                    }
                }
            }
        }



        public List<SdgItem> GetEnteredSdg ( )
        {
            return _EnterdSdgs.ToList ( );
        }

        public List<Slide2Display> GetEnteredSlides ( )
        {
            return _slides2Displays.OrderBy ( x => x.SlideAliq.SAMPLE_ID ).OrderBy ( x => x.SlideAliq.CREATED_ON ).OrderBy ( x => x.SlideAliq.ALIQUOT_ID ).ToList ( );
        }

        public string GetSdgPathoName ( string name )
        {
            return _EnterdSdgs.FirstOrDefault ( x => x.Sdg.NAME == name ).Sdg.SDG_USER.U_PATHOLAB_NUMBER;
        }

        public string GetAliqPathoName ( string name )
        {

            return _slides2Displays.FirstOrDefault ( x => x.AliquotName == name ).PathoAliquotName;
        }

        public ObservableCollection<OPERATOR> GetOperatoes ( string tag )
        {
            ObservableCollection<OPERATOR> oper;

            if ( tag == "C" )
                oper = dal.FindBy<OPERATOR>(o => o.LIMS_ROLE.NAME == "Cytoscreener" && o.OPERATOR_USER.U_IS_ACTIVE == "T")
                    //&& o.OPERATOR_USER.U_ORDER > 0)
                    .Include ( op => op.OPERATOR_USER )
                    .Include ( a => a.LIMS_ROLE )
                    .OrderBy ( x => x.OPERATOR_USER.U_HEBREW_NAME )
                    .ToObservableCollection ( );
            else if ( tag == "P" )
                oper = dal.FindBy<OPERATOR>(o => o.LIMS_ROLE.NAME == "DOCTOR" && o.OPERATOR_USER.U_IS_ACTIVE == "T" && (o.OPERATOR_USER.U_IS_DIGITAL_PATHOLOG == "F" || o.OPERATOR_USER.U_IS_DIGITAL_PATHOLOG == null))
                    //&& o.OPERATOR_USER.U_ORDER > 0)
                    .Include ( op => op.OPERATOR_USER )
                    .Include ( a => a.LIMS_ROLE )
                    .OrderBy ( x => x.OPERATOR_USER.U_HEBREW_NAME )
                    .ToObservableCollection ( );
            else if (tag == "D")
                oper = dal.FindBy<OPERATOR>(o => o.LIMS_ROLE.NAME == "DOCTOR" && o.OPERATOR_USER.U_IS_DIGITAL_PATHOLOG == "T" && o.OPERATOR_USER.U_IS_ACTIVE == "T")
                    //&& o.OPERATOR_USER.U_ORDER > 0)
                    .Include(op => op.OPERATOR_USER)
                    .Include(a => a.LIMS_ROLE)
                    .OrderBy(x => x.OPERATOR_USER.U_HEBREW_NAME)
                    .ToObservableCollection();
            else
                oper = dal.FindBy<OPERATOR> ( o => o.LIMS_ROLE.NAME == "DOCTOR"
                                                 || o.LIMS_ROLE.NAME == "Cytoscreener"
                                                 && o.OPERATOR_USER.U_ORDER > 0 )
                    .Include ( op => op.OPERATOR_USER )
                    .Include ( a => a.LIMS_ROLE )
                    .OrderBy ( x => x.OPERATOR_USER.U_HEBREW_NAME )
                    .ToObservableCollection ( );

            return oper;
        }

        public void AddSdgLog ( long sdgId, string assignSlide, long sid, string msg )
        {
            dal.InsertToSdgLog ( sdgId, "Assign slide", sid, msg );
        }

        #endregion

        #region save

        internal void Save ( OPERATOR op )
        {
            //עדכון סליידים במקרה והוזנה הפנייה או שלא הוזנה הפנייה אבל פתולוג תואם  
            var grpa = from item in _slides2Displays where item.EnteredSdg || item.SameOrNotExistOperator select item;
            UpdateSlides ( grpa.ToList ( ), op.NAME );

            // דרישות במקרה והוזנה הפנייה   
            var grpb = from item in _slides2Displays where item.EnteredSdg select item.SdgId;
            UpdatePatholog ( grpb, op.OPERATOR_ID );

            //פתולוג לא תואם
            var grpc = from item in _slides2Displays
                       where item.EnteredSdg == false && item.SameOrNotExistOperator == false
                       select item;
            //Do Nothing
        }

        // עדכון הסליידים שהוזנו, במקרה והוזנה הפנייה
        private void UpdateSlides ( List<Slide2Display> slides, string opName )
        {

            foreach ( var slide in slides )
            {


                slide.SlideAliq.ALIQUOT_USER.U_SEND_TO_PATHOLOG_ON = DateTime.Now;
                slide.SlideAliq.ALIQUOT_USER.U_ALIQUOT_STATION = null;
                var msg = string.Format ( "{0} Assigned to {1} ", slide.PathoAliquotName, opName );
                AddSdgLog ( slide.SdgId, "Assign slide", _sid, msg );

            }

            dal.SaveChanges ( );
        }

        private void UpdatePatholog ( IEnumerable<long> listIds, long opid )
        {
            var list = listIds.Distinct ( );
            foreach ( var id in list )
            {
                var sdg=   _EnterdSdgs.FirstOrDefault ( x => x.Sdg.SDG_ID == id ).Sdg;
                if ( sdg != null )
                    sdg.SDG_USER.U_PATHOLOG = opid;
                else
                {
                    System.Windows.Forms.MessageBox.Show ( "טעות בקוד 346" );

                }
            }
            //Update patholog for sdg

            dal.SaveChanges ( );
        }



        #endregion

        #region Validation

        public bool ValidationSdgNoSlides ( )
        {
            foreach ( var item in _EnterdSdgs )
            {
                var cnt = _slides2Displays.Where ( al => al.SdgName == item.Sdg.NAME ).Select ( x => x.AliquotName ).Count ( );
                if ( cnt < 1 ) return false;
            }

            return true;
        }

        public bool ValidationSlides ( )
        {
            if ( _slides2Displays.All ( x => x.IsValid ) )
                return true;
            return false;
        }

        public void ShowInvalid ( )
        {
            foreach ( var slides2Display in _slides2Displays )
            {
                slides2Display.FillExceptional ( );
                slides2Display.ShowRemarks = true;
            }
        }

        #endregion


        #region Private Methods

        //Sdg scanned
        private void AddSdg ( SDG sdg )
        {
            if ( _EnterdSdgs.FirstOrDefault ( x => x.Sdg.SDG_ID == sdg.SDG_ID ) == null )// !contains(( sdg ) )
            {
                var hs = _slides2Displays.FirstOrDefault ( x => x.SdgId == sdg.SDG_ID ) != null;

                _EnterdSdgs.Add ( new SdgItem ( ) { Sdg = sdg, HasSlides = hs } );
                _clients.Add ( sdg.SDG_USER.CLIENT );

                //SDG - אם הוזנו כבר סליידם לשייך אותם ל 
                foreach ( var slide in _slides2Displays )
                    if ( slide.SdgId == sdg.SDG_ID )
                        slide.EnteredSdg = true;
            }

            else
            {
                MessageBox.Show ( "ההפניה כבר נקראה.", mboxHeader, MessageBoxButton.OK,
                    MessageBoxImage.Hand );
            }
        }
        private void UpdateSdgHasSlides ( long p )
        {
            var sdgitem = _EnterdSdgs.FirstOrDefault ( a => a.Sdg.SDG_ID == p );
            if ( sdgitem != null )
            {
                sdgitem.HasSlides = _slides2Displays.Where ( x => x.SdgId == p ).Count ( ) > 0;
            }
        }


        //Slide scanned
        private void AddSlide ( ALIQUOT aliquot )
        {
            var exists = _slides2Displays.Exists ( x => x.AliquotName == aliquot.NAME );
            if ( !exists )
            {
                var pathoName = dal.ExecuteScalar
                    ( "SELECT LIMS.PATHOLAB_aliquot_NBR(" + aliquot.ALIQUOT_ID + ") FROM dual" );
                if ( string.IsNullOrEmpty ( pathoName ) )
                    pathoName = aliquot.NAME;
                var s2d = new Slide2Display ( aliquot, pathoName );
                _slides2Displays.Add ( s2d );

                //Slides.Add(aliquot);
                var baseSdg = aliquot.SAMPLE.SDG;
                s2d.SameOrNotExistOperator = baseSdg.SDG_USER.U_PATHOLOG.HasValue == false ||
                                   baseSdg.SDG_USER.U_PATHOLOG.Value == PathologId;
                s2d.LessSlide4Sdg = MissSlides4Sdg ( baseSdg );
                s2d.LessSlide4Patient = MissSlides4Patient ( baseSdg );
                s2d.EnteredSdg = _EnterdSdgs.FirstOrDefault ( x => x.Sdg.SDG_ID == baseSdg.SDG_ID ) != null;
                UpdateSdgHasSlides ( baseSdg.SDG_ID );

                foreach ( var slide in _slides2Displays )
                {
                    if ( slide.PatientId == baseSdg.SDG_USER.U_PATIENT.Value )
                        slide.LessSlide4Patient = s2d.LessSlide4Patient;
                    if ( slide.SdgId == baseSdg.SDG_ID )
                    {
                        slide.LessSlide4Sdg = s2d.LessSlide4Sdg;
                        slide.SameOrNotExistOperator = s2d.SameOrNotExistOperator;
                    }
                }
            }
            else
            {
                MessageBox.Show ( "סלייד כבר נקרא.", mboxHeader, MessageBoxButton.OK,
                    MessageBoxImage.Hand, MessageBoxResult.OK, MessageBoxOptions.RtlReading );
            }
        }

        /// <summary>
        ///     Calculate how much slides are missing for sdg
        /// </summary>
        /// <param name="baseSdg"></param>
        /// <returns></returns>
        private int MissSlides4Sdg ( SDG baseSdg )
        {
            //מס סליידם במערכת עבור הפנייה 
            var slides4Sdg =
                baseSdg.SAMPLEs.SelectMany ( al => al.ALIQUOTs
                    .Where ( a => a.ALIQUOT_USER.U_GLASS_TYPE == "S"
                                && a.ALIQUOT_USER.U_SEND_TO_PATHOLOG_ON == null
                                && _validStatuses.Contains ( a.STATUS ) ) ).Count ( );


            //סליידים שהוזנו במסך עבור הפנייה
            var enteredSlides4Sdg = _slides2Displays.Count ( x => x.SdgId == baseSdg.SDG_ID );

            var lessSlides4Sdg = slides4Sdg - enteredSlides4Sdg;

            return lessSlides4Sdg;
        }

        private int MissSlides4Patient ( SDG baseSdg )
        {
            var patient = _clients.FirstOrDefault ( x => x.CLIENT_ID == baseSdg.SDG_USER.U_PATIENT.Value );
            if ( patient == null )
            {
                patient = dal.FindBy<CLIENT> ( c => c.CLIENT_ID == baseSdg.SDG_USER.U_PATIENT.Value ).SingleOrDefault ( );
                _clients.Add ( patient );
            }


            // מ 4 הימים האחרונים סליידם עבור נבדק
            var sql = string.Format ( "SELECT COUNT(1) FROM LIMS_SYS.SDG D, LIMS_SYS.SDG_USER DU" +
                                    ",LIMS_SYS.SAMPLE S ,LIMS_SYS.ALIQUOT A, LIMS_SYS.ALIQUOT_USER  AU " +
                                    "WHERE D.SDG_ID=DU.SDG_ID " +
                                    "AND S.SDG_ID=DU.SDG_ID " +
                                    "AND A.ALIQUOT_ID=AU.ALIQUOT_ID " +
                                    "AND A.SAMPLE_ID=S.SAMPLE_ID " +
                                    "AND D.CREATED_ON > (TO_DATE('{1}','DD/MM/YYYY') - 3) " +
                                    "AND D.CREATED_ON < (TO_DATE('{1}','DD/MM/YYYY') + 3) " +
                                    "AND DU.U_PATIENT={0} " +
                                    "AND SUBSTR(D.NAME,1,1)='{2}' " +
                                    "AND A.STATUS IN ('V','C','I','P') " +
                                    "AND AU.U_GLASS_TYPE = 'S' AND AU.U_SEND_TO_PATHOLOG_ON IS  NULL",
                patient.CLIENT_ID, baseSdg.CREATED_ON.Value.ToShortDateString ( ), baseSdg.NAME [ 0 ].ToString ( ) );

            var originalSlides4Patient = dal.GetDynamicDecimal ( sql );

            //סליידים שהוזנו עבור נבדק
            var enteredSlides4Patient =
                _slides2Displays.Count ( x =>
                    x.SlideAliq.SAMPLE.SDG.SDG_USER.U_PATIENT.Value == baseSdg.SDG_USER.U_PATIENT.Value );

            var lessSlides4Patient = ( int ) originalSlides4Patient.Value - enteredSlides4Patient;

            return lessSlides4Patient;
        }

        #endregion

        public void Clear ( )
        {
            _EnterdSdgs.Clear ( );
            _slides2Displays.Clear ( );
            _clients.Clear ( );
        }
    }
}