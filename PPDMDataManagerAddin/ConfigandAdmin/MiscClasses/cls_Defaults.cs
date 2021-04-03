using System.ComponentModel.DataAnnotations;

namespace SimpleODM.systemconfigandutil
{
    public class cls_Defaults
    {
        public string DbName { get; set; }
        public string WELL_XREFMyUWI { get; set; }
        public string EntityName { get; set; }
        public string EntityType { get; set; }
        public int WELL_NUMERIC_ID { get; set; }
        public string WELL_LOG_ID { get; set; }
        public string WELL_LOG_JOB_ID { get; set; }
        public string WELL_LOG_DICT_ID { get; set; }
        public string WELL_LOG_DICT_PARAM_ID { get; set; }
        public string WELL_LOG_DICT_PARAM_CLASS_ID { get; set; }
        public string WELL_LOG_DICT_CURVE_ID { get; set; }
        public string WELL_LOG_TRIP_OBS_NO { get; set; }
        public int WELL_LOG_PARAMETER_SEQ_NO { get; set; }
        public string DICTIONARY_ID { get; set; }
        public string WELL_LOG_TOOL_PASS_NO { get; set; } // WELL_LOG_PASS OBS NO
        public string RESENT_ID { get; set; }
        public string RESERVE_CLASS_ID { get; set; }
        public string ECONOMICS_RUN_ID { get; set; }
        public string SUMMARY_ID { get; set; }
        public string SUMMARY_OBS_NO { get; set; }
        public string FORMULA_ID { get; set; }
        public string COMPONENT_NAME { get; set; }
        public string LITH_SAMPLE_ID { get; set; }
        public string PREPARATION_ID { get; set; }
        public string PDEN_ID { get; set; }
        public string PDEN_TYPE { get; set; }
        public string PDEN_SOURCE { get; set; }
        public int PDEN_PRS_XREF_SEQ_NO { get; set; }
        public string VOLUME_METHOD { get; set; }
        public string PRODUCT_TYPE { get; set; }
        public string ACTIVTY_TYPE { get; set; }
        public string VOLUME_DATE { get; set; }
        public string CASE_ID { get; set; }
        public string SEGMENT_ID { get; set; }
        public string AREA { get; set; }
        public string AREA_TYPE { get; set; }
        public string WELLBOREID { get; set; }
        public string AREA_XREF_AREAID { get; set; }
        public int COMPLETION_OBS_NO { get; set; }
        public string COMPLETION_SOURCE { get; set; }
        public int PERIOD_OBS_NO { get; set; }
        public int RECOVERY_OBS_NO { get; set; }
        public string PERIOD_TYPE { get; set; }
        public int PR_STR_FORM_OBS_NO { get; set; }
        public string PROD_STRING_SOURCE { get; set; }
        public string STRING_ID { get; set; }
        public int PERFORATION_OBS_NO { get; set; }
        public string PERFORATION_SOURCE { get; set; }
        public string TESTNUM { get; set; }
        public string RUNNUM { get; set; }
        public string TESTTYPE { get; set; } = "";
        public string TESTSUBTYPE { get; set; } = "";
        public int PRESSURE_OBS_NO { get; set; }
        public string PRESSURE_SOURCE { get; set; } = "";
        public string PRESSURE_AOF_SOURCE { get; set; } = "";
        public int ACTIVITY_OBS_NO { get; set; }
        public string STRAT_WELL_ACQTN_ID { get; set; } = "";
        public string STRAT_NAME_SET { get; set; } = "";
        public string STRAT_UNIT_ID { get; set; } = "";
        public string STRAT_COLUMN_ID { get; set; } = "";
        public string STRAT_COLUMN_SOURCE { get; set; } = "";
        public string MEAS_SECTION_ID { get; set; }
        public string MEAS_SECTION_SOURCE { get; set; }
        public string LITH_LOG_SOURCE { get; set; }
        public string LITHOLOGY_LOG_ID { get; set; }
        public int DEPTH_OBS_NO { get; set; }
        public string ROCK_TYPE { get; set; }
        public int ROCK_TYPE_OBS_NO { get; set; }
        public string CORE_ID { get; set; } = "";
        public int CORE_ANALYSIS_OBS_NO { get; set; }
        public string CORE_SAMPLE_NUM { get; set; } = "";
        public int CORE_SAMPLE_ANALYSIS_OBS_NO { get; set; }
        public int CORE_DESCRIPTION_OBS_NO { get; set; }
        public string NODE_ID { get; set; } = "";
        public string INTERP_ID { get; set; } = "";
        public string POROUS_INTERVAL_ID { get; set; } = "";
        public string ZONE_ID { get; set; } = "";
        public string ZONE_SOURCE { get; set; } = "";
        public string PAYZONE_TYPE { get; set; } = "";
        public string FLUID_TYPE { get; set; } = "";
        public string INTERVAL_ID { get; set; } = "";
        public string INTERVAL_SOURCE { get; set; } = "";
        public int TUBING_OBS_NO { get; set; }
        public string TUBING_TYPE { get; set; } = "";
        public string TUBING_SOURCE { get; set; } = "";
        public int PLUGBACK_OBS_NO { get; set; }
        public int AOF_OBS_NO { get; set; }
        public string LICENSE_ID { get; set; } = "";
        public string LICENSE_TYPE { get; set; } = "";
        public string CONDITION_ID { get; set; } = "";
        public string CONDITION_TYPE { get; set; } = "";
        public string LICENSE_SOURCE { get; set; } = "";
        public string SURVEYID { get; set; } = "";
        public string SURVEYSOURCE { get; set; } = "";
        public string EQUIPMENTID { get; set; } = "";
        public string ROW_QUALITY { get; set; } = "";
        public string ROW_SOURCE { get; set; } = "";
        public string SOURCE { get; set; } = "";
        public string BUSINESS_ASSOCIATE { get; set; } = "";
        public string CREW_ID { get; set; } = "";
        public string APPLICATIONNAME { get; set; } = "Simple ODM";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLHEADSTREAMIDENTIFIER { get; set; } = "WS";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLREPORTINGHEADSTREAMIDENTIFIER { get; set; } = "WRS";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLSETIDENTIFIER { get; set; } = "WELLSET";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLORIGINIDENTIFIER { get; set; } = "WO";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLIDENTIFIER { get; set; } = "WELL";
        // <Required(AllowEmptyStrings:=False, ErrorMessage:="Value Needed")>
        // Public Property WELLPRODSTRINGIDENTIFIER As String = "PROD STRING"
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLBOREIDENTIFIER { get; set; } = "WB";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLBORESEGMENTIDENTIFIER { get; set; } = "WS";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLBORECOMPLETIONIDENTIFIER { get; set; } = "C";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELLCONTACTINTERVALIDENTIFIER { get; set; } = "CI";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLREPORTINGHEADSTREAMIDENTIFIER { get; set; } = "WRS";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLHEADSTREAMIDENTIFIER { get; set; } = "WHS";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLSETIDENTIFIER { get; set; } = "WELLSET";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLORIGINIDENTIFIER { get; set; } = "WO";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLIDENTIFIER { get; set; } = "WELL";
        // <Required(AllowEmptyStrings:=False, ErrorMessage:="Value Needed")>
        // Public Property XREFWELLPRODSTRINGIDENTIFIER As String = "PROD STRING"
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLBOREIDENTIFIER { get; set; } = "WB";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLBORESEGMENTIDENTIFIER { get; set; } = "WELL SEGMENT";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLBORECOMPLETIONIDENTIFIER { get; set; } = "C";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string XREFWELLCONTACTINTERVALIDENTIFIER { get; set; } = "CI";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string BAPERSONIDENTIFIER { get; set; } = "PERSON";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string BACOMPANYIDENTIFIER { get; set; } = "COMPANY";
        public bool GUIDREQUIRED { get; set; } = true;
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string ACTIVITY_SET_ID_FOR_LOG { get; set; } = "";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string ACTIVITY_SET_TYPE_FOR_LOG { get; set; } = "";
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value Needed")]
        public string WELL_ACTIVITY_COMPONENT_TYPE { get; set; } = "";
        public string UOM_SYSTEM_ID { get; set; } = "SI";
        public string OIL_VOLUME_UOM { get; set; } = "BBL";
        public string WATER_VOLUME_UOM { get; set; } = "BBL";
        public string GAS_VOLUME_UOM { get; set; } = "SCF";
        public string POOLNAME { get; set; } = "";
        public string POOLID { get; set; } = "";
        public string Mud_Sample_ID { get; set; } = "";
        public int KICKOFF_POINT_OBSNO { get; set; }
        public int SPOKE_POINT_OBSNO { get; set; }
        public int AIR_DRILL_OBS_NO { get; set; }
        public int AIR_DRILL_INTERVAL_DEPTH_OBS_NO { get; set; }
        public string AIR_DRILL_INTERVAL_SOURCE { get; set; }
        public int SHOW_OBS_NO { get; set; }
        public string SHOW_SOURCE { get; set; } = "";
        public string SHOW_TYPE { get; set; } = "";
        public int FIELD_STATION_ID { get; set; }
        public string SERVICE_TYPE { get; set; } = "";
        public int SERVICE_SEQ_NO { get; set; }
        public string SECURITY_GROUP_ID { get; set; }
        public string ENTITLEMENT_ID { get; set; } = "";
        public string SPEC_ID { get; set; } = "";
        public string SPEC_SET_ID { get; set; } = "";
        public string EQUIP_MAIN_ID { get; set; } = "";
        public string MAINTAIN_ID { get; set; } = "";
        public string FACILITY_ID { get; set; } = "";
        public string FACILITY_TYPE { get; set; } = "";
        public string CATALOGUE_ADDITIVE_ID { get; set; } = "";
        public string CATALOGUE_EQUIP_ID { get; set; } = "";
        public string APPLICATION_ID { get; set; } = "";
        public string LAND_SALE_NUMBER { get; set; } = "";
        public string JURISDICTION { get; set; } = "";
        public string LAND_SALE_OFFERING_ID { get; set; } = "";
        public string RESTRICTION_ID { get; set; } = "";
        public int RESTRICTION_VERSION { get; set; }
        public string LAND_OFFERING_BID_ID { get; set; } = "";
        public string LAND_RIGHT_ID { get; set; } = "";
        public string LAND_RIGHT_TYPE { get; set; } = "";
        public string INTEREST_SET_ID { get; set; } = "";
        public int INTEREST_SET_SEQ_NO { get; set; }
        public string PARTNER_BA_ID { get; set; } = "";
        public string INSTRUMENT_ID { get; set; } = "";
        public string OBLIGATION_ID { get; set; } = "";
        public int OBLIGATION_SEQ_NO { get; set; }
        public string DEDUCTION_ID { get; set; } = "";
        public string CONTRACT_ID { get; set; } = "";
        public string PROVISION_ID { get; set; } = "";
        public string OPERATING_PROCEDURE_ID { get; set; }
        public string CONSENT_ID { get; set; }
        public string CLASSIFICATION_SYSTEM_ID { get; set; } = "";
        public string CLASS_LEVEL_ID { get; set; } = "";
        public bool AGREEMENT { get; set; } = false;
        public bool PPDMAGREEMENT { get; set; } = false;
        public string INFORMATION_ITEM_ID { get; set; } = "";
        public string INFO_ITEM_TYPE { get; set; } = "";
        public string STORE_ID { get; set; } = "";
        public string FILE_SYSTEM_STORE_TYPE { get; set; } = "";
        public string PHYSICAL_STORE_TYPE { get; set; } = "";
        public string PHYSICAL_ITEM_ID { get; set; } = "";
        public string DATA_STORE_HIER_ID { get; set; } = "";
        public string CURRENT_TABLE { get; set; } = "";
        public string CURRENT_TABLE_R_TABLE { get; set; } = "";
        public string CURRENT_FIELD { get; set; } = "";
        public string CURRENT_FIELD_R_FIELD { get; set; } = "";
        public string SYSTEM_ID { get; set; } = "";
        public int mHelp { get; set; } = (int)System.Windows.Forms.Keys.F1;
        public int mFindall { get; set; } = (int)System.Windows.Forms.Keys.Home;
        public int mFind { get; set; } = (int)System.Windows.Forms.Keys.F2;
        public int mPrevious { get; set; } = (int)System.Windows.Forms.Keys.F3;
        public int mNext { get; set; } = (int)System.Windows.Forms.Keys.F4;
        public int mNew { get; set; } = (int)System.Windows.Forms.Keys.F5;
        public int mDelete { get; set; } = (int)System.Windows.Forms.Keys.F6;
        public int mCancelEdit { get; set; } = (int)System.Windows.Forms.Keys.F7;
        // Public Property mFav As Integer = Windows.Forms.Keys.F8
        public int mSave { get; set; } = (int)System.Windows.Forms.Keys.F9;
        public int mLibrary { get; set; } = (int)System.Windows.Forms.Keys.F10;
        public int mFileUpLoad { get; set; } = (int)System.Windows.Forms.Keys.F11;
        //Public Property mDesignEdit As Integer = Windows.Forms.Keys.F12;
        public int mPrint { get; set; } = (int)System.Windows.Forms.Keys.PrintScreen;
        public int mExportExcel { get; set; } = (int)System.Windows.Forms.Keys.F12;
        public int mShowLov { get; set; } = (int)System.Windows.Forms.Keys.Escape;
        public int mRefershLOV { get; set; } = (int)System.Windows.Forms.Keys.F8;
        // Public Property mPreviewChanges As Integer = System.Windows.Forms.Keys.
        public cls_Defaults()
        {
        }
    }
}