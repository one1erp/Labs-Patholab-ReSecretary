using LSExtensionWindowLib;
using LSSERVICEPROVIDERLib;
using ONE1_richTextCtrl;
using Patholab_Common;
using Patholab_DAL_V1;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using UserControl = System.Windows.Forms.UserControl;


namespace ReSecretary
{


    [ComVisible(true)]
    [ProgId("ReSecretary.ReSecretaryCls")]
    public partial class ReSecretaryCls : UserControl, IExtensionWindow
    {

        #region Private fields

        private INautilusProcessXML xmlProcessor;
        private IExtensionWindowSite2 _ntlsSite;
        private INautilusServiceProvider sp;
        private INautilusDBConnection _ntlsCon;
        public List<U_CLINIC> Clinics { get; set; }

        private DataLayer _dal;
        public bool DEBUG;

        private string mboxHeader = "הזנת פרטים קליניים";
        private SDG _sdg;


        private RTF_Manger _rtfManager;

        bool _ft = true;
        private List<WrapperRtf> _currentResults;





        #endregion

        #region Ctor

        public ReSecretaryCls()
        {
            InitializeComponent();
            BackColor = Color.FromName("Control");
            this.Dock = DockStyle.Fill;
        }

        #endregion


        #region Implementation of IExtensionWindow

        public bool CloseQuery()
        {
            if (_dal != null) _dal.Close();
            this.Dispose();
            return true;
        }

        public void Internationalise()
        {
        }

        public void SetSite(object site)
        {
            _ntlsSite = (IExtensionWindowSite2)site;

            _ntlsSite.SetWindowInternalName("ReSecretary");
            _ntlsSite.SetWindowRegistryName("ReSecretary");
            _ntlsSite.SetWindowTitle(mboxHeader);
        }




        public void PreDisplay()
        {

            xmlProcessor = Utils.GetXmlProcessor(sp);

            Utils.GetNautilusUser(sp);

            InitializeData();
            InitilaizeRichSpellCtrl();
        }

        public WindowButtonsType GetButtons()
        {
            return LSExtensionWindowLib.WindowButtonsType.windowButtonsNone;
        }

        public bool SaveData()
        {
            return false;
        }

        public void SetServiceProvider(object serviceProvider)
        {
            sp = serviceProvider as NautilusServiceProvider;
            _ntlsCon = Utils.GetNtlsCon(sp);

        }

        public void SetParameters(string parameters)
        {

        }

        public void Setup()
        {

        }

        public WindowRefreshType DataChange()
        {
            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;
        }

        public WindowRefreshType ViewRefresh()
        {
            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;
        }

        public void refresh()
        {
        }

        public void SaveSettings(int hKey)
        {
        }

        public void RestoreSettings(int hKey)
        {
        }

        public void Close()
        {

        }

        #endregion


        #region Initilaize

        public void InitializeData()
        {


            //    Debugger.Launch();
            try
            {
                _dal = new DataLayer();

                if (DEBUG)
                    _dal.MockConnect();
                else
                    _dal.Connect(_ntlsCon);




            }
            catch (Exception e)
            {


                MessageBox.Show("Error in  InitializeData " + "/n" + e.Message, mboxHeader);
                Logger.WriteLogFile(e);
            }

        }

        #endregion

        private void setCursorInRichText()
        {
            try
            {

                foreach (ToolStrip toolStrip in richDiagn.Controls.OfType<ToolStrip>())
                {
                    foreach (ToolStripButton button in toolStrip.Items.OfType<ToolStripButton>())
                    {
                        if (button.Name.Equals("tsbtnLTR"))
                        {
                            button.PerformClick();
                            goto EndLTR;
                        }
                    }
                }

            EndLTR:

                foreach (ToolStrip toolStrip in richImpDiag.Controls.OfType<ToolStrip>())
                {
                    foreach (ToolStripButton button in toolStrip.Items.OfType<ToolStripButton>())
                    {
                        if (button.Name.Equals("tsbtnRTL"))
                        {
                            button.PerformClick();
                            goto EndRTL;
                        }
                    }
                }

            EndRTL:

                return;
            }
            catch
            {

                return;
            }
        }

        private void txtBarcodeName_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyData == Keys.Enter || e.KeyData == Keys.Tab)
            {

                //var d = (from item in richImpDiag.Controls.OfType<ToolStrip>().Where(btnStrip => btnStrip.Items.OfType<ToolStripButton>().Where(btn => btn.Name.Equals("tsbtnLTR")).FirstOrDefault() != null)
                //    select item.Items);
                //var c = b as ToolStripButton;
                try
                {
                    //richDiagn.Enabled = false;
                    _sdg = null;

                    var tb = txtBarcodeName;
                    if (tb == null || string.IsNullOrEmpty(tb.Text)) return;


                    string sn = tb.Text.Replace(".", "/").ToUpper();
                    //By SDG 
                    _sdg = _dal.FindBy<SDG>(x => (x.NAME == sn || x.SDG_USER.U_PATHOLAB_NUMBER == sn) && !x.NAME.Contains("V"))

                               .SingleOrDefault();

                    //     _dal.ReloadEntity ( _sdg );

                    if (_sdg == null)
                    {
                        //By aliquot
                        var aliq =
                        _dal.FindBy<ALIQUOT>(
                                x =>
                                x.NAME == tb.Text.ToUpper() ||
                                x.ALIQUOT_USER.U_PATHOLAB_ALIQUOT_NAME == tb.Text.ToUpper()
                                && !x.NAME.Contains("V"))
                                .FirstOrDefault();
                        if (aliq != null)
                            _sdg = aliq.SAMPLE.SDG;
                    }
                    if (_sdg.SdgType == Patholab_DAL_V1.Enums.SdgType.Pap)
                    {

                        MessageBox.Show(".לא ניתן להזין דרישות PAP", mboxHeader, MessageBoxButtons.OK,
                                  MessageBoxIcon.Hand);


                        ClearScreen();
                        txtBarcodeName.Focus();
                        return;
                    }
                    if (_sdg == null)
                    {
                        MessageBox.Show(".דרישה לא קיימת", mboxHeader, MessageBoxButtons.OK,
                                        MessageBoxIcon.Hand);
                        txtBarcodeName.Focus();
                        ClearScreen();

                        return;
                    }
                    else
                    {
                        ClearScreen();
                        SetHeader();
                        LoadResult();

                        if (_ft)
                        {

                            this.richImpDiag.SetRichDefenitions();
                            this.richDiagn.SetRichDefenitions();
                            _ft = false;
                        }
                        this.richDiagn.Enabled = true;

                        //fixing bug(the form was loading too early)
                        this.richDiagn.formLoadingControlflag = true;
                        this.richDiagn.Form1_Load(null, null);

                        this.btnSave.Enabled = true;
                        this.tabControl1.Enabled = true;
                        richDiagn.Focus();
                        this.tabControl1.SelectedIndex = 0;

                        _dal.InsertToSdgLog(_sdg.SDG_ID, "RESC.LOAD", (long)_ntlsCon.GetSessionId(), "מסך מזכירות-פתיחה");

                    }
                }
                catch
                    (Exception ex)
                {
                    MessageBox.Show(".שגיאה בטעינת הדרישה" + ex.Message,
                                   "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logger.WriteLogFile(ex);
                }
                finally
                {
                    txtBarcodeName.Text = "";
                }
            }
        }


        private void tab_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {

                var tt = this.tabControl1.SelectedTab;
                if (tt == null) return;

                if (tt.Controls.Count < 1)
                {
                    return;
                }
                RichSpellCtrl t = tt.Controls[0] as RichSpellCtrl;
                if (t != null)
                {


                    if (t.Name == "richImpDiag")
                    {
                        zLang.Hebrew();
                    }
                    else
                    {
                        zLang.English();
                    }
                    t.Focus();
                }

            }
            catch (Exception exception)
            {
                Logger.WriteLogFile(exception);
                MessageBox.Show("Error on tab_SelectedIndexChanged \n " + exception.Message);
            }
        }

        private void SetHeader()
        {
            var client = (from c in _dal.FindBy<CLIENT_USER>(x => x.CLIENT_ID == _sdg.SDG_USER.U_PATIENT)
                          select new { c.U_FIRST_NAME, c.U_LAST_NAME, c.CLIENT.NAME, c.U_GENDER, c.U_DATE_OF_BIRTH }
       ).SingleOrDefault();
            if (client == null)
            {
                MessageBox.Show("לא הוגדר פציינט לדרישה.");
                return;
            }
            string cln = client.U_FIRST_NAME + " " + client.U_LAST_NAME + " - " + client.NAME + " ";

            lblPathoName.Text = string.Format("{0} - {1}", cln, _sdg.SDG_USER.U_PATHOLAB_NUMBER);
            pictureBox1.Image = new Bitmap(Path.Combine(Patholab_Common.Utils.GetResourcePath(), "sdg" + _sdg.STATUS + ".ico"));
        }


        private void ClearScreen()
        {
            _rtfManager.ClearScreen();
            lblPathoName.Text = string.Empty;
        }
        private void EnableControls(bool p)
        {
            //snomed tab
            _rtfManager.EnableControls(p);

        }
        private void LoadResult()
        {
            EnableControls("ARXU".Contains(_sdg.STATUS) == false);

            _rtfManager._dal = _dal;
            //Get results by sdg ID
            //שליפה של התוצאות מהDB
            var res2 = (from diag in _dal.FindBy<RESULT>(x => x.TEST.ALIQUOT.SAMPLE.SDG_ID == _sdg.SDG_ID)
                        where diag.TEST.STATUS != "X"
                        && (diag.NAME == Constants.DIAGNOSIS || diag.NAME == Constants.IMP_DIAGNOS)
                        select diag);

            foreach (var item in res2)
            {
                _dal.ReloadEntity(item);
            }

            //יצירה של אובייקט ברשימה עבור כל תוצאה שנשלפה
            _currentResults = (from rl in res2
                               select new WrapperRtf()
                               {
                                   Result_ = rl,
                                   Name = rl.NAME,
                                   ResultId = rl.RESULT_ID,
                                   TestName = rl.TEST.NAME
                               }).ToList();

            _rtfManager.LoadResults(_sdg, _currentResults);

        }

        private void InitilaizeRichSpellCtrl()
        {
            _rtfManager = new RTF_Manger(sp, _dal, richImpDiag, richDiagn);//, rich_impDiag, rich_mic, rich_mac, rich_FreeTxt );
            _rtfManager.TemplatesClicked += RtfManager_tmcclicked;

        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            richDiagn.Enabled = false;
            this.tabControl1.Enabled = false;
            this.tabControl1.SelectedIndex = 0;
            if (_sdg == null) return;

            if ("ARXU".Contains(_sdg.STATUS))
            {
                MessageBox.Show("לא ניתן לערוך דרישה זו.", mboxHeader, MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            //Saving rtf is for every type of sdg 
            _rtfManager.SaveResults(_currentResults);
            bool b = _rtfManager.SaveAsText(_currentResults);
            _dal.SaveChanges();
            if (b)
            {


                _dal.ReloadEntity(_sdg);
                pictureBox1.Image = new Bitmap(Path.Combine(Patholab_Common.Utils.GetResourcePath(), "sdg" + _sdg.STATUS + ".ico"));
                _dal.InsertToSdgLog(_sdg.SDG_ID, "RESC.SAVE", (long)_ntlsCon.GetSessionId(), "מסך מזכירות-סגירה");

            }


            #region temporary, bug fix

            var diagnosisFromRichTxt = richDiagn.GetOriginalText();

            _dal = new DataLayer();
            _dal.Connect(_ntlsCon);

            var diagRes = _dal.FindBy<RESULT>(x => x.TEST.ALIQUOT.SAMPLE.SDG.SDG_ID == _sdg.SDG_ID && x.NAME == "Diagnosis").FirstOrDefault();
            bool resultSaved = diagRes == null || (diagRes.FORMATTED_RESULT != null);

            
            if (diagnosisFromRichTxt != null)
            {
                var cleanedStr = Regex.Replace(diagnosisFromRichTxt, @"\t|\n|\r", "");
                if (cleanedStr.Length > 0 && !resultSaved)
                {
                    MessageBox.Show("                   !                   \n פרטים קליניים למקרה זה לא נשמרו \n יש לבצע כניסה ושמירה נוספת במסך");
                    Logger.WriteExceptionToLog($"sdg id: {_sdg.SDG_ID},_currentResults - Diagnosis RtfText: {_currentResults.Where(x => x.Name == "Diagnosis").FirstOrDefault().RtfText} diagRes formatted res {diagRes.FORMATTED_RESULT} diagnosisFromRichTxt {diagnosisFromRichTxt}");
                }
            }





            #endregion

            ClearScreen();
            btnSave.Enabled = false;

            radButton1.Enabled = true;


            this.tabControl1.Enabled = false;
            this.tabControl1.SelectedIndex = 0;
            txtBarcodeName.Focus();

        }


        #region Templates
        private void RtfManager_tmcclicked()
        {


            string All_organs = "ALL";
            string l = _sdg.NAME[0].ToString();

            //If has organ get it, else get "All
            var organs4Sdg = _sdg.SAMPLEs.Select(SM => SM.SAMPLE_USER.U_ORGAN ?? All_organs).Distinct().ToList();
            if (!organs4Sdg.Contains(All_organs))
                organs4Sdg.Add(All_organs);

            var organsList = (from item in _dal.GetAll<U_NLIST_USER>()
                              where item.U_SDG_TYPE == l
                              select item).ToList();

            List<U_NLIST_USER> list4Show = new List<U_NLIST_USER>();
            foreach (var item in organsList)
            {
                foreach (var org in organs4Sdg)
                {
                    if (item.U_ORGANS != null && item.U_ORGANS.Contains(org))
                    {
                        list4Show.Add(item);
                    }
                }
            }
            var organs4Show = list4Show.Select(SM => SM.U_TEXT).ToList().Distinct();
            if (organs4Show.Count() < 1)
            {

                MessageBox.Show(".לא קיימים טמפלייטים", mboxHeader, MessageBoxButtons.OK,
                          MessageBoxIcon.Hand);
                return;
            }
            templateCtrl = new TextTemplateCtrl(organs4Show);
            templateCtrl.ShowDialog();
            if (this.tabControl1.SelectedTab != null && !string.IsNullOrEmpty(templateCtrl.SelectedText))
            {
                RichSpellCtrl r = this.tabControl1.SelectedTab.Controls.OfType<RichSpellCtrl>().ToList()[0];
                if (r != null)
                    r.AppendText(templateCtrl.SelectedText);
            }
        }

        private TextTemplateCtrl templateCtrl;
        #endregion

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (_ntlsSite != null)
                _ntlsSite.CloseWindow();
        }

        private void txtBarcodeName_TextChanged(object sender, EventArgs e)
        {

        }

    }







}







