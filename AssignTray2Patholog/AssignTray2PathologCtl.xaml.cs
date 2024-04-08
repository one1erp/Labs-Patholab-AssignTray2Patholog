using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LSExtensionWindowLib;
using LSSERVICEPROVIDERLib;
using Patholab_Common;
using Patholab_DAL_V1;


//using MessageBox = System.Windows.Controls.MessageBox;


namespace AssignTray2Patholog
{
    public partial class AssignTray2PathologCtl : UserControl
    {
        #region Ctor

        public AssignTray2PathologCtl(INautilusServiceProvider sp, INautilusProcessXML xmlProcessor,
            INautilusDBConnection _ntlsCon, IExtensionWindowSite2 _ntlsSite,
            INautilusUser _ntlsUser)
        {
            InitializeComponent();

            //    this.xmlProcessor = xmlProcessor;
            this._ntlsCon = _ntlsCon;
            this._ntlsSite = _ntlsSite;
            //   this._ntlsUser = _ntlsUser;
            this.sp = sp;
            DataContext = this;
            Doctors = new ObservableCollection<OPERATOR>();
            //   Aliquots = new ObservableCollection<string>();
            Slides2Display = new ObservableCollection<Slide2Display>();
        }

        #endregion

        #region Initilaize

        public void InitializeData()
        {
            try
            {
                _manager = new Manager(Debug, _ntlsCon);

                Doctors = _manager.GetOperatoes("P");
                cmbPatholog.ItemsSource = Doctors;
                txtSdg.Focus();

                if (SelectedOperator != null)
                    _manager.PathologId = SelectedOperator.OPERATOR_ID;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error in  InitializeData " + "/n" + e.Message, mboxHeader);
                Logger.WriteLogFile(e);
            }
        }

        #endregion

        #region Private methods

        private void CreateList4Show()
        {
            //  Slides2Display.Clear();
            Slides2Display = _manager.GetEnteredSlides().ToObservableCollection();
            lv_slides.ItemsSource = Slides2Display;

            var Sdgs = _manager.GetEnteredSdg();
       
            lv_Referrals.ItemsSource = Sdgs;
         
            //Count
            dlidesCount.Text = lv_slides.Items.Count.ToString();
            sdgsCount.Text = lv_Referrals.Items.Count.ToString();
        }

        #endregion

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var rdb = sender as RadioButton;
            if (rdb.IsChecked != null)
            {
                if (rdb.Tag != null)
                    Doctors = _manager.GetOperatoes(rdb.Tag.ToString());
                cmbPatholog.ItemsSource = Doctors;
            }
        }




        #region Private fields

        private readonly IExtensionWindowSite2 _ntlsSite;
        private INautilusServiceProvider sp;
        private INautilusDBConnection _ntlsCon;

        public bool Debug;

        //  private long _sid;
        private const string mboxHeader = "שיוך מגש לפתולוג";
        private Manager _manager;

        #endregion


        #region public fields

        public ObservableCollection<OPERATOR> Doctors { get; set; }

        public ObservableCollection<Slide2Display> Slides2Display { get; private set; }

        
        public OPERATOR SelectedOperator { get; set; }

        #endregion


        #region IExtensionWindow

        public bool CloseQuery()
        {
            _manager.Close();

            return true;
        }

        public void PreDisplay()
        {
            InitializeData();
        }

        public void SetServiceProvider(object serviceProvider)
        {
            sp = serviceProvider as NautilusServiceProvider;
            _ntlsCon = Utils.GetNtlsCon(sp);
        }

        #endregion


        #region Events

        private void TxtBrcdSdg_OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                //UI Validation
                if (e.Key != Key.Enter) return;

                var tb = sender as TextBox;
                if (tb == null || string.IsNullOrEmpty(tb.Text)) return;

                if (cmbPatholog.Text == "")
                {
                    MessageBox.Show("אנא בחר פתולוג.", mboxHeader, MessageBoxButton.OK,
                        MessageBoxImage.Hand, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                    return;
                }

                var input = tb.Text.ToUpper();
                var sdgMsg = _manager.AddSdg(input);


                if (!string.IsNullOrEmpty(sdgMsg))
                {
                    MessageBox.Show(sdgMsg, mboxHeader, MessageBoxButton.OK,
                        MessageBoxImage.Hand, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                    txtSlide.Clear();
                    return;
                }


                tbDet.Text = tb.Text + " - " + _manager.GetSdgPathoName(input);

                tb.Text = string.Empty;
                txtSlide.Text = string.Empty;


                cmbPatholog.IsEnabled = false;

                CreateList4Show();
            }
            catch
                (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת הדרישה." + ex.Message,
                    mboxHeader, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading);
            }
        }

        private void Remove_sdg_click(object sender, RoutedEventArgs e)
        {
            var sdgItem = ((FrameworkElement) sender).DataContext as SdgItem;
            if (sdgItem == null)
                return;
            var dg = MessageBox.Show("כל הסליידים שהוזנו לדרישה זו יימחקו.האם ברצונך להמשיך?",
                mboxHeader, MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, MessageBoxResult.Cancel,
                MessageBoxOptions.RtlReading);

            if (dg == MessageBoxResult.Yes)
            {
                _manager.RemoveSdg(sdgItem.Sdg);
                CreateList4Show();
            }
        }

        private void TxtBrcdSlide_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var tb = sender as TextBox;
                if (tb == null || string.IsNullOrEmpty(tb.Text)) return;
                if (cmbPatholog.Text == "")
                {
                    MessageBox.Show("אנא בחר פתולוג.", mboxHeader, MessageBoxButton.OK,
                        MessageBoxImage.Hand, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                    return;
                }


                var slideMsg = _manager.AddSlide(txtSlide.Text);


                if (!string.IsNullOrEmpty(slideMsg))
                {
                    MessageBox.Show(slideMsg, mboxHeader, MessageBoxButton.OK,
                        MessageBoxImage.Hand, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                    //Netanel asked not clear
                    //txtSlide.Clear();
                    return;
                }

                tbDet.Text = txtSlide.Text + " - " + _manager.GetAliqPathoName(txtSlide.Text);


                tb.Text = string.Empty;
                txtSlide.Text = string.Empty;
                txtSdg.Text = string.Empty;
                cmbPatholog.IsEnabled = false;


                CreateList4Show();
                txtSlide.Focus();
            }
        }

        private void btnOK_CLICK(object sender, RoutedEventArgs e)
        {
            if (cmbPatholog.Text == "")
            {
                MessageBox.Show("אנא בחר פתולוג.", mboxHeader, MessageBoxButton.OK,
                    MessageBoxImage.Hand, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                return;
            }

            if (!_manager.haveSlides())
            {
                MessageBox.Show("לא הוזנו סליידים!", mboxHeader, MessageBoxButton.OK,
                    MessageBoxImage.Hand, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                return;
            }

            var dg = MessageBoxResult.None;
            if (!_manager.ValidationSdgNoSlides())
                dg = MessageBox.Show("לא כל הדרישות עודכנו, האם ברצונך להמשיך?", mboxHeader,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
            if (dg != MessageBoxResult.None && dg != MessageBoxResult.Yes)
                return;
            if (!_manager.ValidationSlides())
                dg = MessageBox.Show("קיימים חריגים,האם ברצונך להמשיך?", mboxHeader, MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question, MessageBoxResult.OK, MessageBoxOptions.RtlReading);

            if (dg != MessageBoxResult.None && dg != MessageBoxResult.Yes)
                return;

            //else


            _manager.Save(SelectedOperator);

            MessageBox.Show("המגש שויך לפתולוג", mboxHeader, MessageBoxButton.OK,
                MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
            tbDet.Text = "";
            BtnClean_click(null, null);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            var dg = MessageBox.Show("האם אתה בטוח שברצונך לצאת?", mboxHeader, MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
            if (dg == MessageBoxResult.Yes)
                if (_ntlsSite != null)
                    _ntlsSite.CloseWindow();
        }

        private void cmbPatholog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_manager != null)
                if (SelectedOperator != null)
                    _manager.PathologId = SelectedOperator.OPERATOR_ID;
        }

        private void BtnClean_click(object sender, RoutedEventArgs e)
        {
            lv_Referrals.ItemsSource = null;
            lv_slides.ItemsSource = null;
            lv_slides.Items.Clear();
            lv_Referrals.Items.Clear();

            _manager.Clear();
   
            sdgsCount.Text = string.Empty;
            dlidesCount.Text = string.Empty;

            cmbPatholog.IsEnabled = true;
            //    cmbPatholog.SelectedIndex = -1;
        }

        private void TxtSdg_OnGotFocus(object sender, RoutedEventArgs e)
        {
            zLang.English();
        }

        private void txtSdg_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //    Slide2Display slide2 = this.lv_slides.SelectedItem as Slide2Display;
                var slide2 = ((FrameworkElement) sender).DataContext as Slide2Display;

                if (slide2 == null)
                    return;

                _manager.RemoveSlide(slide2.AliquotName);
                CreateList4Show();
            }
            catch
                (Exception ex)
            {
                MessageBox.Show("Error on Remove aliquot." + ex.Message,
                    mboxHeader, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading);
            }
        }

        #endregion
    }
}