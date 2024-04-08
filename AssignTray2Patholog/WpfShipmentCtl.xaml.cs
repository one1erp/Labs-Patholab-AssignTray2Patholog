using System;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LSExtensionWindowLib;
using LSSERVICEPROVIDERLib;
using Patholab_Common;

using Patholab_DAL_V1;
using Patholab_XmlService;
using Binding = System.Windows.Data.Binding;
using DataGrid = System.Windows.Controls.DataGrid;
using DataGridCell = System.Windows.Controls.DataGridCell;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

//using MessageBox = System.Windows.Controls.MessageBox;


namespace ReciveShipmentWpf
{
    /// <summary>
    /// Interaction logic for WpfShipmentCtl.xaml
    /// </summary>
    public partial class WpfShipmentCtl : System.Windows.Controls.UserControl
    {


        public WpfShipmentCtl(INautilusServiceProvider sp, INautilusProcessXML xmlProcessor, INautilusDBConnection _ntlsCon, IExtensionWindowSite2 _ntlsSite, INautilusUser _ntlsUser)
        {
            InitializeComponent();
            // TODO: Complete member initialization
            //    this.xmlProcessor = xmlProcessor;
            this._ntlsCon = _ntlsCon;
            this._ntlsSite = _ntlsSite;
            //   this._ntlsUser = _ntlsUser;
            this.sp = sp;
            this.dal = dal;
            this.DataContext = this;
        }




        #region Private fields
        private IExtensionWindowSite2 _ntlsSite;
        private INautilusServiceProvider sp;
        private INautilusDBConnection _ntlsCon;
        private DataLayer dal;
        public bool DEBUG;
        private const string mboxHeader = "קבלת משלוחים";
        public ObservableCollection<U_CONTAINER> shipmentslist { get; set; }
        #endregion






        public bool CloseQuery()
        {
            if (dal != null) dal.Close();

            return true;
        }







        public void PreDisplay()
        {

            //    xmlProcessor = Utils.GetXmlProcessor(sp);

            //      _ntlsUser = Utils.GetNautilusUser(sp);

            InitializeData();
        }




        public void SetServiceProvider(object serviceProvider)
        {
            sp = serviceProvider as NautilusServiceProvider;
            _ntlsCon = Utils.GetNtlsCon(sp);

        }


        #region Initilaize

        public void InitializeData()
        {

            try
            {
                dal = new DataLayer();

                if (DEBUG)
                    dal.MockConnect();
                else
                    dal.Connect(_ntlsCon);

              
                
                this.Clinics = dal.GetAll<U_CLINIC>().OrderBy(x => x.NAME).ToList();



                InitilaizeGrid();


            }
            catch (Exception e)
            {

                MessageBox.Show("Error in  InitializeData " + "/n" + e.Message, mboxHeader);
                Logger.WriteLogFile(e);
            }

        }

        private bool ft = true;

        private void InitilaizeGrid()
        {

            var today = DateTime.Now.Date.AddDays(-1);
            // Debugger.Launch();
            if (DEBUG)
            {
                today = DateTime.Now.Date.AddDays(-12);

                shipmentslist = //new List<U_CONTAINER>().ToObservableCollection();
                dal.GetAll<U_CONTAINER>().Where(x => x.U_CONTAINER_USER.U_RECEIVED_ON > today).Take(5).OrderBy(x => x.U_CONTAINER_ID).ToObservableCollection();
                ft = false;
            }
            else
            {
                shipmentslist =
                               dal.GetAll<U_CONTAINER>()
                                    .Where(x => x.U_CONTAINER_USER.U_RECEIVED_ON > today)
                                  .OrderBy(x => x.U_CONTAINER_ID).ToObservableCollection();
            }



            string s = "סה\"כ הפניות";
            string s1 = "סה\"כ צנצנות";
            lblOrders.Text = s + " : " + shipmentslist.Sum(x => x.U_CONTAINER_USER.U_NUMBER_OF_ORDERS).ToString();
            lblSamples.Text = s1 + " : " + shipmentslist.Sum(x => x.U_CONTAINER_USER.U_NUMBER_OF_SAMPLES).ToString();

        }

        public List<U_CLINIC> Clinics { get; set; }

        #endregion



        #region Data

        private void SaveRow(U_CONTAINER row)
        {
            // Debugger.Launch();

            var id = row.U_CONTAINER_ID;//.Cells["ID"].Value;


            if (id == 0)
            {
                //   Debugger.Launch();
                AddShipment(row);
            }
            else
            {
                dal.SaveChanges();

            }

        }

        private void AddShipment(U_CONTAINER container)
        {
            var cu = container.U_CONTAINER_USER;

            U_CLINIC clinic = Clinics.SingleOrDefault(c => c.U_CLINIC_ID == cu.U_CLINIC);
            var login = new LoginXmlHandler(sp);
            login = new LoginXmlHandler(sp);
            login.CreateLoginXml("U_CONTAINER", "Shipment");
            login.AddProperties("U_NUMBER_OF_ORDERS", cu.U_NUMBER_OF_ORDERS.ToString());
            login.AddProperties("U_NUMBER_OF_SAMPLES", cu.U_NUMBER_OF_SAMPLES.ToString());
            login.AddProperties("DESCRIPTION", container.DESCRIPTION);
            login.AddProperties("U_SEND_ON", container.U_CONTAINER_USER.U_SEND_ON.ToString());
            login.AddProperties("U_RECEIVE_NUMBER", cu.U_RECEIVE_NUMBER);

            if (clinic != null)
                login.AddProperties("U_CLINIC", clinic.NAME);

            else
            {
                System.Windows.MessageBox.Show("חןבה לבחור מרפאה!", mboxHeader);
                return;
            }
            var s = login.ProcssXml();
            if (!s)
            {
                System.Windows.MessageBox.Show("error" + login.ErrorResponse, mboxHeader);
            }



        }

        #endregion

        #region button events

        private void Print_Click(object sender, RoutedEventArgs e)
        {

            if (GridShipments.Items.Count > 0)
            {
                var ship = GridShipments.SelectedItem as U_CONTAINER;
                if (ship != null)
                {
                    var id = ship.U_CONTAINER_ID;



                    MessageBox.Show(id.ToString());
                    if (id == 0)
                    {
                        MessageBox.Show("להדפיס מדבקה " + id.ToString(), mboxHeader);
                    }
                    else
                    {
                        MessageBox.Show("לא ניתן להדפיס מדבקה לישות לציידנית שטרם נשמרה. ", mboxHeader);

                    }
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            var dg = System.Windows.MessageBox.Show("?האם אתה בטוח שברצונך לצאת", mboxHeader, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dg == MessageBoxResult.Yes)
                _ntlsSite.CloseWindow();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in shipmentslist)
            {
                SaveRow(row);
            }

            InitilaizeGrid();

        }



        #endregion









        private void GridShipments_OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            //    e.Row.Header = (e.Row.GetIndex() + 1).ToString();
            if (e.Row.IsNewItem)
            {
                U_CONTAINER a = e.Row.Item as U_CONTAINER;
                if (a != null && a.U_CONTAINER_USER == null)
                {
                    a.U_CONTAINER_USER = new U_CONTAINER_USER();
                }
            }

        }





        private void GridShipments_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {

            U_CONTAINER a = e.NewItem as U_CONTAINER;
            if (a != null && a.U_CONTAINER_USER == null)
            {
                a.U_CONTAINER_USER = new U_CONTAINER_USER();
                a.U_CONTAINER_USER.U_SEND_ON = DateTime.Now;
            }

        }



        // Be warned that the `Loaded` event runs anytime the window loads into view,
        // so you will probably want to include an Unloaded event that detaches the
        // collection




        private void GridShipments_OnLoaded(object sender, RoutedEventArgs e)
        {

            var dg = (DataGrid)sender;
            if (dg == null || dg.ItemsSource == null) return;

            var sourceCollection = dg.ItemsSource as ObservableCollection<U_CONTAINER>;
            if (sourceCollection == null) return;

            sourceCollection.CollectionChanged +=
                new NotifyCollectionChangedEventHandler(DataGrid_CollectionChanged);
        

            dg.CurrentCell = new DataGridCellInfo(
    dg.Items[0], dg.Columns[0]);
            dg.BeginEdit();

        }




        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        private void GridShipments_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {

            UIElement inputElement;

            // Texbox is the first control in my template column
            inputElement = FindVisualChildren<TextBox>(e.EditingElement).FirstOrDefault();
            if (inputElement != null)
            {
                Keyboard.Focus(inputElement);
            }

        }


        private void GridShipments_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            U_CONTAINER a = GridShipments.CurrentItem as U_CONTAINER;
            if (a != null && a.U_CONTAINER_USER == null)
            {
                a.U_CONTAINER_USER = new U_CONTAINER_USER();
            }
        }


        void DataGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {


            var a = e.NewItems[0] as U_CONTAINER;
            //  U_CONTAINER a = e.NewItem as U_CONTAINER;
            if (a != null && a.U_CONTAINER_USER == null)
            {
                a.U_CONTAINER_USER = new U_CONTAINER_USER();
                a.U_CONTAINER_USER.U_SEND_ON = DateTime.Now;
            }
        }

        private void GridShipments_OnLoadingRowDetails(object sender, DataGridRowDetailsEventArgs e)
        {

        }

        private void UIElement_OnPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (GridShipments.Items.Count>shipmentslist.Count)
            {
                
            }
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {

            U_CONTAINER a = GridShipments.CurrentItem as U_CONTAINER;
            if (a != null && a.U_CONTAINER_USER == null)
            {
                a.U_CONTAINER_USER = new U_CONTAINER_USER();
            }
        }
    }


}



