﻿using ONE1_richTextCtrl;
using Patholab_Common;
using Patholab_DAL_V1;
using Patholab_DAL_V1.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ReSecretary
{

    public class RTF_Manger
    {

        public readonly Dictionary<string, RichSpellCtrl> _result2RichText;

        public DataLayer _dal { get; set; }
        public SDG sdg { get; set; }

        public event Action TemplatesClicked;

        public RTF_Manger ( LSSERVICEPROVIDERLib.INautilusServiceProvider sp, DataLayer dal, RichSpellCtrl richDiag, RichSpellCtrl richImpDiag )
        {
            this._dal = dal;

            this.sp = sp;

            _result2RichText = new Dictionary<string, RichSpellCtrl>
                {
                    {Constants.IMP_DIAGNOS, richDiag},
                    {Constants.DIAGNOSIS,richImpDiag},
                };
            foreach ( var rtCtrls in _result2RichText.Values.Distinct ( ) )
            {

                rtCtrls.ExtraStrip.Text = "Templates";
                rtCtrls.ExtraBtnClciked += Pb_ExtraBtnClciked;

                rtCtrls.RightToLeft = RightToLeft.No;
                rtCtrls.SetDefaultSpelling();
            }
        }


        internal void LoadResults ( SDG sdg, List<WrapperRtf> currentResults )
        {
            this.sdg = sdg;

            //Gets current results id
            var ids = currentResults.Select ( x => x.ResultId );

            //Gets results with RTF
            var resultsHaveRtf = from res in _dal.FindBy<RTF_RESULT> ( x => ids.Contains ( x.RTF_RESULT_ID ) ) select res;


            //LOADS RTF TO LIST
            foreach ( RTF_RESULT rtfResult in resultsHaveRtf )
            {
                _dal.ReloadEntity ( rtfResult );
                var rr = currentResults.FirstOrDefault ( x => x.ResultId == rtfResult.RTF_RESULT_ID );
                if ( rr != null )
                {
                    rr.RtfText = rtfResult.RTF_TEXT;
                }
            }

            //Sets data into rich text
            foreach ( KeyValuePair<string, RichSpellCtrl> result2RichTextHi in _result2RichText )
            {

                var res = currentResults.FirstOrDefault ( x => x.Name == result2RichTextHi.Key );
                if ( res != null && res.RtfText != null )
                {
                    result2RichTextHi.Value.SetRtf ( res.RtfText );

                    result2RichTextHi.Value.Focus ( );
                }
                //pop
                // result2RichTextHi.Value.SetConstZoom();
            }
            if ( sdg.SdgType == SdgType.Cytology )
            {
                //   Debugger.Launch();
                var cytoMicRes = currentResults.FirstOrDefault ( x => x.Name == "aaaa" );// Constants.CytMac );
                if ( cytoMicRes != null )
                {
                    //Has rtf ?

                    var rtf = resultsHaveRtf.FirstOrDefault ( x => x.RTF_RESULT_ID == cytoMicRes.ResultId );
                    if ( rtf == null )
                    {
                        string cytoMicTempl = GetCytoMacroTemplate ( sdg );
                        MessageBox.Show ( "Test" );
                    }
                }
            }
        }

        internal void SaveResults ( List<WrapperRtf> currentResults )
        {
            try
            {

                foreach ( KeyValuePair<string, RichSpellCtrl> rt_result in _result2RichText )
                {
                    //   Debugger.Launch();
                    var trf = currentResults.FirstOrDefault ( x => x.Name == rt_result.Key );
                    //    Logger.WriteLogFile("_current Result is " + result2RichTextHi.Key);

                    if ( trf != null )
                    {

                        var q = _dal.GetAll<RTF_RESULT> ( ).FirstOrDefault ( x => x.RTF_RESULT_ID == trf.ResultId );

                        string rtf = rt_result.Value.GetRtf ( );
                        if ( q != null )
                        {
                            q.RTF_TEXT = rtf; //update
                        }
                        else
                        {
                            // insert
                            var newRTF = new RTF_RESULT ( );
                            newRTF.RTF_RESULT_ID = trf.ResultId;
                            newRTF.RTF_TEXT = rtf;
                            _dal.Add ( newRTF );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                MessageBox.Show ( @"Error on save to RTF table " + ex.Message, "Constants.MboxCaption", MessageBoxButtons.OK, MessageBoxIcon.Error );
                Logger.WriteLogFile ( ex );
            }


        }

        internal bool SaveAsText ( List<WrapperRtf> _currentResults )
        {

            Patholab_XmlService.ResultEntryXmlHandler rex = new Patholab_XmlService.ResultEntryXmlHandler ( sp );

            bool frstkkd = true;
            foreach ( var res in _currentResults )
            {
                if ( !_result2RichText.ContainsKey ( res.Name ) ) continue;

                string val = _result2RichText [ res.Name ].GetOriginalText ( );

                //limit for 4000 
                var q = val.Take ( 3999 );

                var splTxt = new string ( q.ToArray ( ) );
                splTxt = Regex.Replace ( splTxt, @"\t|\n|\r", "" );
                RESULT res2Update = _currentResults.Select ( x => x.Result_ ).SingleOrDefault ( x => x.RESULT_ID == res.ResultId );

                if ( res2Update != null )
                {

                    if ( frstkkd )
                    {
                        rex.CreateResultEntryXml ( res2Update.TEST_ID.Value, res2Update.RESULT_ID, splTxt );
                        frstkkd = false;
                    }

                    else
                        rex.AddResultEntryElem ( res2Update.RESULT_ID.ToString ( ), splTxt );

                }

            }
            var succecss = rex.ProcssXml ( );
            return ( succecss );




        }

        private string _cytoTemplateSql = null;
        private LSSERVICEPROVIDERLib.INautilusServiceProvider sp;
        private bool frstkk;


        private string GetCytoMacroTemplate ( SDG sdg )
        {
            if ( string.IsNullOrEmpty ( _cytoTemplateSql ) )
            {
                _cytoTemplateSql = _dal.GetPhraseByName ( "CytologyTemplate" ).PhraseEntriesDictonary [ "Macro" ];
            }
            string val = "";
            var queries = _cytoTemplateSql.Split ( new string [ 1 ] { "~^" }, StringSplitOptions.RemoveEmptyEntries );
            foreach ( string query in queries )
            {
                var sql = query.Replace ( "#SDG_ID#", sdg.SDG_ID.ToString ( ) );
                var res = _dal.GetDynamicList ( sql );
                if ( res != null )
                    val = res.Aggregate ( val, ( current, s ) => current + s );
            }

            return val;

        }
        #region Init



        private void Pb_ExtraBtnClciked ( )
        {
            if ( TemplatesClicked != null ) TemplatesClicked ( );

        }

        internal void EnableControls ( bool p )
        {
            foreach ( KeyValuePair<string, RichSpellCtrl> rt in _result2RichText )
            {
                rt.Value.Enabled = p;
            }
        }

        internal void ClearScreen ( )
        {
            //foreach (var rt in pages)
            //{
            //    rt.Text = string.Empty;
            //}

            foreach ( KeyValuePair<string, RichSpellCtrl> rtCtrls in _result2RichText )
            {
                rtCtrls.Value.Text = string.Empty;
                rtCtrls.Value.ClearText ( );// = string.Empty;
            }
        }

        #endregion


    }

}
