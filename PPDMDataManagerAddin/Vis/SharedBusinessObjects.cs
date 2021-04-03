using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;

using SimpleODM.Logmodule;
using SimpleODM.SharedLib;
using SimpleODM.systemconfigandutil;

public  class MyForms
{
    private SimpleODM.systemconfigandutil.PPDMConfig simpleODMConfig;

    public SimpleODM.systemconfigandutil.PPDMConfig GetSimpleODMConfig()
    {
        return simpleODMConfig;
    }

    public void SetSimpleODMConfig(SimpleODM.systemconfigandutil.PPDMConfig value)
    {
        simpleODMConfig = value;
    }

    public SimpleODM.wellmodule.uc_wellDirSurvey WellDirSRVYManager { get; set; }
    public SimpleODM.welltestandpressuremodule.uc_WellTest WellTestManager { get; set; }
    public SimpleODM.classhierandreportmdoule.uc_NewEditClassification ClassificationModule { get; set; }
    public SimpleODM.supportmodule.uc_area AreaModule { get; set; }
    public SimpleODM.supportmodule.uc_equipment EquipmentModule { get; set; }
    public SimpleODM.facilitymodule.uc_facility FacilityModule { get; set; }
    private SimpleODM.wellmodule.uc_NewEditWell _WellModule { get; set; }

    public SimpleODM.wellmodule.uc_NewEditWell WellModule { get; set; }
   

    public SimpleODM.wellmodule.uc_wellversion Wellversion { get; set; }
    public SimpleODM.wellmodule.uc_well_alias Wellalias { get; set; }
    public SimpleODM.wellmodule.uc_well_area Wellarea;
    public SimpleODM.wellmodule.uc_wellorigin wellorigin{ get; set; }
    public SimpleODM.wellmodule.uc_well_remark Wellremark{ get; set; }
    public SimpleODM.wellmodule.uc_well_misc_data Wellmisc{ get; set; }
    public SimpleODM.wellmodule.uc_well_facility WellFacility { get; set; }
    public SimpleODM.wellmodule.uc_well_activity Well_Activity { get; set; }
    public SimpleODM.wellmodule.uc_well_activities_cause Well_Activity_Conditions_and_Events { get; set; }
    public SimpleODM.wellmodule.uc_well_activities_duration Well_Activity_Duration { get; set; }
    public SimpleODM.wellmodule.uc_well_node Well_Node { get; set; }
    public SimpleODM.wellmodule.uc_well_node_area Well_Node_Area { get; set; }
    public SimpleODM.wellmodule.uc_well_node_geometry Well_Node_Geometry { get; set; }
    public SimpleODM.wellmodule.uc_well_node_metesandbound Well_Node_Metes_and_Bound { get; set; }
    public SimpleODM.wellmodule.uc_well_node_stratunit Well_Node_Stratigraphy { get; set; }
    public SimpleODM.wellmodule.uc_well_core Well_Core { get; set; }
    public SimpleODM.wellmodule.uc_well_core_analysis Well_Core_Aanlysis { get; set; }
    public SimpleODM.wellmodule.uc_well_core_analysis_sample Well_Core_Aanlysis_Sample { get; set; }
    public SimpleODM.wellmodule.uc_well_core_analysis_sample_description Well_Core_Aanlysis_Sample_description { get; set; }
    public SimpleODM.wellmodule.uc_well_core_analysis_sample_remark Well_Core_Aanlysis_Sample_remark { get; set; }
    public SimpleODM.wellmodule.uc_well_core_analysis_method Well_Core_Aanlysis_Method { get; set; }
    public SimpleODM.wellmodule.uc_well_core_analysis_remark Well_Core_Aanlysis_Remark { get; set; }
    public SimpleODM.wellmodule.uc_well_core_description Well_Core_Description { get; set; }
    public SimpleODM.wellmodule.uc_well_core_description_strat_unit Well_Core_Description_Stratigraphy { get; set; }
    public SimpleODM.wellmodule.uc_well_core_shift Well_Core_Shift { get; set; }
    public SimpleODM.wellmodule.uc_well_core_remark Well_Core_Remark { get; set; }
    public SimpleODM.wellmodule.uc_well_facility Well_Facility { get; set; }
    public SimpleODM.wellmodule.uc_well_ba_services Well_BA_Services { get; set; }
    public SimpleODM.wellmodule.uc_well_geometry Well_Geometry { get; set; }
    public SimpleODM.wellmodule.uc_well_Licenses Well_Interpretation { get; set; }
    public SimpleODM.wellmodule.uc_well_Licenses Well_Dictionaries { get; set; }
    public SimpleODM.wellmodule.uc_well_support_facility Well_Support_Facility { get; set; }
    public SimpleODM.wellmodule.uc_well_License_permit_Types Well_Permit { get; set; }
    public SimpleODM.wellmodule.uc_well_Licenses welllicense { get; set; }
    public SimpleODM.wellmodule.uc_well_license_violation welllicense_violation { get; set; }
    public SimpleODM.wellmodule.uc_well_license_status welllicense_status { get; set; }
    public SimpleODM.wellmodule.uc_well_license_remark welllicense_remark { get; set; }
    public SimpleODM.wellmodule.uc_well_license_cond welllicense_cond { get; set; }
    public SimpleODM.wellmodule.uc_well_license_area welllicense_area { get; set; }
    public SimpleODM.wellmodule.uc_well_Landrights wellLandrights { get; set; }
    public SimpleODM.wellmodule.uc_well_mud_sample well_mud_sample { get; set; }
    public SimpleODM.wellmodule.uc_well_mud_sample_property well_mud_sample_proerty { get; set; }
    public SimpleODM.wellmodule.uc_well_mud_sample_resistivity well_mud_sample_resistivity { get; set; }
    public SimpleODM.wellmodule.uc_NewEditwellbore WellBoreModule { get; set; }
    // ----------- Well Bore Controls -------------------------------------------
    public SimpleODM.wellmodule.uc_WellDesigner wellboreDesigner;
    public SimpleODM.wellmodule.uc_wellboretubular wellboreTubular;
    public SimpleODM.wellmodule.uc_NewEditwellbore wellbore;
    public SimpleODM.wellmodule.uc_prodstring Productionstring;
    public SimpleODM.wellmodule.uc_prodstringformation prodstringformation;
    public SimpleODM.wellmodule.uc_completion well_completion;
    public SimpleODM.wellmodule.uc_completion_xref well_completion_xref;
    public SimpleODM.wellmodule.uc_completion_String2Formation_link well_completion_String2Formation_link;
    public SimpleODM.wellmodule.uc_preforation wellperf;
    public SimpleODM.wellmodule.uc_well_equipment well_equipments;
    public SimpleODM.wellmodule.uc_well_equipment_search well_equipments_search;
    public SimpleODM.wellmodule.uc_wellborepayzone wellborepayzone;
    public SimpleODM.wellmodule.uc_wellborePlugback wellborePlugback;
    public SimpleODM.wellmodule.uc_wellborePorousinterval wellborePorousinterval;
    public SimpleODM.wellmodule.uc_wellborezoneinterval wellborezoneinterval;
    public SimpleODM.wellmodule.uc_wellborezoneintervalvalue wellborezoneintervalvalue;
    public SimpleODM.wellmodule.uc_wellboretubularCement wellboretubularcement;
    // -------------------------- Drilling Controls -----------------------------------
    public SimpleODM.wellmodule.uc_well_show well_show;
    public SimpleODM.wellmodule.uc_well_show_remark well_show_remark;
    public SimpleODM.wellmodule.uc_well_air_drill well_air_drill;
    public SimpleODM.wellmodule.uc_well_air_drill_Interval well_air_drill_Interval;
    public SimpleODM.wellmodule.uc_well_air_drill_interval_period well_air_drill_interval_period;
    public SimpleODM.wellmodule.uc_well_Horiz_Drill well_Horiz_Drill;
    public SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_kop well_Horiz_Drill_drill_kop;
    public SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_poe well_Horiz_Drill_drill_poe;
    public SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_KOP_spoke well_Horiz_Drill_drill_spoke;
    // ------------------------ Well test Controls ----------------------------------
    public SimpleODM.welltestandpressuremodule.uc_WellTest well_test;
    public SimpleODM.welltestandpressuremodule.uc_welltestAnalysis well_test_analysis;
    public SimpleODM.welltestandpressuremodule.uc_WellTestCushion well_test_cushion;
    public SimpleODM.welltestandpressuremodule.uc_welltest_period well_test_Period;
    public SimpleODM.welltestandpressuremodule.uc_welltestContaminant well_test_Contaminant;
    public SimpleODM.welltestandpressuremodule.uc_welltestEquipment well_test_Equipment;
    public SimpleODM.welltestandpressuremodule.uc_welltestFlow well_test_Flow;
    public SimpleODM.welltestandpressuremodule.uc_welltestFlowMeas well_test_FlowMeas;
    public SimpleODM.welltestandpressuremodule.uc_welltestMud well_test_Mud;
    public SimpleODM.welltestandpressuremodule.uc_welltestPress well_test_Press;
    public SimpleODM.welltestandpressuremodule.uc_welltestPressMeas well_test_PressMeas;
    public SimpleODM.welltestandpressuremodule.uc_welltestRecorder well_test_Recorder;
    public SimpleODM.welltestandpressuremodule.uc_welltestRecovery well_test_Recovery;
    public SimpleODM.welltestandpressuremodule.uc_welltestRemarks well_test_Remarks;
    public SimpleODM.welltestandpressuremodule.uc_welltestShutoff well_test_Shutoff;
    public SimpleODM.welltestandpressuremodule.uc_welltestStratunit well_test_StratUnit;
    public SimpleODM.welltestandpressuremodule.uc_well_pressure well_Pressure;
    public SimpleODM.welltestandpressuremodule.uc_well_pressure_aof well_Pressureaof;
    public SimpleODM.welltestandpressuremodule.uc_well_pressure_aof_4pt well_Pressureaod4pt;
    public SimpleODM.welltestandpressuremodule.uc_well_pressure_bh well_PressureBH;
    // --------------------------------------------------------------------------
    // Public WithEvents CatalogueEquip As SimpleODM.supportmodule.uc_NewEditCatEquipment
    // Public WithEvents ConstraintsModule As SimpleODM.etlmodule.uc_constraint_manager
    // Public WithEvents WellScoreCard As SimpleODM.wellmodule.uc_wellscorecard
    // Public WithEvents ExportData As SimpleODM.etlmodule.uc_exportTableData
    // ----------------- ETL Usercontrols -------------------------
    // Public WithEvents LoadFilesinDirectory As SimpleODM.etlmodule.uc_LoadFileIndirectory
    // Public WithEvents spreadsheet_mapping As SimpleODM.etlmodule.uc_ExcelMapping
    // Public WithEvents MappingData As SimpleODM.etlmodule.uc_MAppingFromSource2Target

    // Public WithEvents WorkFlowManager As SimpleODM.etlmodule.uc_WorkFlow
    // Public WithEvents WebService_Mapping As SimpleODM.etlmodule.uc_WebServiceMapping
    // Public WithEvents Table_Mapping As SimpleODM.etlmodule.uc_TableMapping
    // Public WithEvents WorkFlowRun As SimpleODM.wellmodule.uc_completion
    // ------------------------------------------------------------------
    public SimpleODM.stratigraphyandlithologyModule.uc_NewEditLithology LithModule;
    // --------------------------- Stratigraphy Modules ------------------------------------------------------
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_name_set StratigraphyModule;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_unit strat_unit;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_name_set_xref strat_xref;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_alias strat_alias;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_topo_relation strat_topo_relation;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_unit_age strat_unit_age;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_unit_description strat_unit_description;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_acqtn_method strat_acqtn_method;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_interp_corr strat_interp_corr;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_equivalance strat_equivalance;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_hierarchy strat_hierarchy;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_hierarchy_desc strat_hierarchy_desc;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_column StratColumnsModule;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_col_acqtn strat_col_acqtn;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_col_unit strat_col_unit;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_col_unit_age strat_col_unit_age;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_col_xref strat_col_xref;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_field_station strat_field_station;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_field_section strat_field_section;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_field_node strat_field_node;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_field_geometry strat_field_geometry;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_field_acqtn strat_field_acqtn;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_fld_interp_age strat_fld_interp_age;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_node_version strat_field_node_version;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_well_section strat_well_section;
    public SimpleODM.stratigraphyandlithologyModule.uc_strat_well_acqtn strat_well_acqtn;
    public SimpleODM.stratigraphyandlithologyModule.uc_well_strat_interp_age strat_well_interp_age;
    // --------------------------------------------------------------------------------------------------------------
    // -------------------------------------------Business Associate ------------------------------------------------
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate businessAssociate;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_address businessAssociate_address;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_alias businessAssociate_alias;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_authority businessAssociate_authority;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_consortuimservice businessAssociate_consortuimservice;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_contactinfo businessAssociate_contactinfo;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_crew businessAssociate_crew;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_crew_memeber businessAssociate_crew_memeber;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_crosspreference businessAssociate_crosspreference;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_description businessAssociate_description;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_employee businessAssociate_employee;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license businessAssociate_license;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_alias businessAssociate_license_alias;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_area businessAssociate_license_area;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond businessAssociate_license_cond;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond_code businessAssociate_license_cond_code;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond_type businessAssociate_license_cond_type;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_remark businessAssociate_license_remark;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_status businessAssociate_license_status;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_type businessAssociate_license_type;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_violation businessAssociate_license_violation;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_organization businessAssociate_organization;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_permit businessAssociate_permit;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_preference businessAssociate_preference;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_services businessAssociate_services;
    public SimpleODM.BusinessAssociateModule.uc_businessAssociate_services_address businessAssociate_services_address;
    public SimpleODM.BusinessAssociateModule.uc_entitlement entitlement;
    public SimpleODM.BusinessAssociateModule.uc_ent_components ent_components;
    public SimpleODM.BusinessAssociateModule.uc_entitlement_group entitlement_group;
    public SimpleODM.BusinessAssociateModule.uc_entitlement_security_ba entitlement_security_ba;
    public SimpleODM.BusinessAssociateModule.uc_entitlement_security_group entitlement_security_group;
    public SimpleODM.BusinessAssociateModule.uc_entitlement_security_group_xref entitlement_security_group_xref;
    // -------------------------------------------End Business Associate --------------------------------------------
    public SimpleODM.supportmodule.uc_equipment equipment;
    public SimpleODM.supportmodule.uc_equipment_alias equipment_alias;
    public SimpleODM.supportmodule.uc_equipment_ba equipment_ba;
    public SimpleODM.supportmodule.uc_equipment_spec equipment_spec;
    public SimpleODM.supportmodule.uc_equipment_spec_set equipment_spec_set;
    public SimpleODM.supportmodule.uc_equipment_spec_set_spec equipment_spec_set_spec;
    public SimpleODM.supportmodule.uc_equipment_crossreference equipment_crossreference;
    public SimpleODM.supportmodule.uc_equipment_maintain equipment_maintain;
    public SimpleODM.supportmodule.uc_equipment_maintain_status equipment_maintain_status;
    public SimpleODM.supportmodule.uc_equipment_maintain_type equipment_maintain_type;
    public SimpleODM.supportmodule.uc_equipment_status equipment_status;
    public SimpleODM.supportmodule.uc_equipment_use_stat equipment_use_stat;
    // ------------------------------------------ Facilities ----------------------------------------------------
    public SimpleODM.facilitymodule.uc_facility_alias facility_alias;
    public SimpleODM.facilitymodule.uc_facility_area facility_area;
    public SimpleODM.facilitymodule.uc_facility_ba_service facility_ba_service;
    public SimpleODM.facilitymodule.uc_facility_class facility_class;
    public SimpleODM.facilitymodule.uc_facility_description facility_description;
    public SimpleODM.facilitymodule.uc_facility_equipment facility_equipment;
    public SimpleODM.facilitymodule.uc_facility_field facility_field;
    public SimpleODM.facilitymodule.uc_facility_license facility_license;
    public SimpleODM.facilitymodule.uc_facility_license_alias facility_license_alias;
    public SimpleODM.facilitymodule.uc_facility_license_area facility_license_area;
    public SimpleODM.facilitymodule.uc_facility_license_cond facility_license_cond;
    public SimpleODM.facilitymodule.uc_facility_license_remark facility_license_remark;
    public SimpleODM.facilitymodule.uc_facility_license_status facility_license_status;
    public SimpleODM.facilitymodule.uc_facility_license_type facility_license_type;
    public SimpleODM.facilitymodule.uc_facility_license_violation facility_license_violation;
    public SimpleODM.facilitymodule.uc_facility_maintain facility_maintain;
    public SimpleODM.facilitymodule.uc_facility_maintain_status facility_maintain_status;
    public SimpleODM.facilitymodule.uc_facility_rate facility_rate;
    public SimpleODM.facilitymodule.uc_facility_restriction facility_restriction;
    public SimpleODM.facilitymodule.uc_facility_status facility_status;
    public SimpleODM.facilitymodule.uc_facility_substance facility_substance;
    public SimpleODM.facilitymodule.uc_facility_version facility_version;
    public SimpleODM.facilitymodule.uc_facility_xref facility_xref;
    // ------------------------------------ Catalog -------------------------------
    public SimpleODM.supportmodule.uc_Cat_equipment cat_equipment;
    public SimpleODM.supportmodule.uc_Cat_equipment_alias Cat_equipment_alias;
    public SimpleODM.supportmodule.uc_Cat_equipment_spec Cat_equipment_spec;
    public SimpleODM.supportmodule.uc_cat_additive cat_additive;
    public SimpleODM.supportmodule.uc_cat_additive_alias cat_additive_alias;
    public SimpleODM.supportmodule.uc_cat_additive_spec cat_additive_spec;
    public SimpleODM.supportmodule.uc_cat_additive_type cat_additive_type;
    public SimpleODM.supportmodule.uc_cat_additive_xref cat_additive_xref;
    // ------------------------- --- Applications ------------------------------

    public SimpleODM.supportmodule.uc_application application4auth;
    public SimpleODM.supportmodule.uc_application_alias application_alias;
    public SimpleODM.supportmodule.uc_application_area application_area;
    public SimpleODM.supportmodule.uc_application_attach application_attach;
    public SimpleODM.supportmodule.uc_application_ba application_ba;
    public SimpleODM.supportmodule.uc_application_desc application_desc;
    public SimpleODM.supportmodule.uc_application_remark application_remark;
    // ----------------------------- Area ---------------------------------------------
    public SimpleODM.supportmodule.uc_area Area;
    public SimpleODM.supportmodule.uc_area_alias area_alias;
    public SimpleODM.supportmodule.uc_area_contain area_contain;
    public SimpleODM.supportmodule.uc_area_description area_description;
    // ------------------------------ Pool --------------------------------------------

    public SimpleODM.reservepoolproductionmodule.uc_pool Pool;
    public SimpleODM.reservepoolproductionmodule.uc_pool_alias pool_alias;
    public SimpleODM.reservepoolproductionmodule.uc_pool_area pool_area;
    public SimpleODM.reservepoolproductionmodule.uc_pool_instrument pool_instrument;
    public SimpleODM.reservepoolproductionmodule.uc_pool_version pool_version;


    // ------------------------------ Production Entities --------------------------------------------
    public SimpleODM.reservepoolproductionmodule.uc_pden pden;
    public SimpleODM.reservepoolproductionmodule.uc_pden_alloc_factor pden_alloc_factor;
    public SimpleODM.reservepoolproductionmodule.uc_pden_area pden_area;
    public SimpleODM.reservepoolproductionmodule.uc_pden_business_assoc pden_business_assoc;
    public SimpleODM.reservepoolproductionmodule.uc_pden_decline_case pden_decline_case;
    public SimpleODM.reservepoolproductionmodule.uc_pden_decline_condition pden_decline_condition;
    public SimpleODM.reservepoolproductionmodule.uc_pden_decline_segment pden_decline_segment;
    public SimpleODM.reservepoolproductionmodule.uc_pden_facility pden_facility;
    public SimpleODM.reservepoolproductionmodule.uc_pden_field pden_field;
    public SimpleODM.reservepoolproductionmodule.uc_pden_flow_measurement pden_flow_measurement;
    public SimpleODM.reservepoolproductionmodule.uc_pden_in_area pden_in_area;
    public SimpleODM.reservepoolproductionmodule.uc_pden_land_right pden_land_right;
    public SimpleODM.reservepoolproductionmodule.uc_pden_lease_unit pden_lease_unit;
    public SimpleODM.reservepoolproductionmodule.uc_pden_material_bal pden_material_bal;
    public SimpleODM.reservepoolproductionmodule.uc_pden_oper_hist pden_oper_hist;
    public SimpleODM.reservepoolproductionmodule.uc_pden_other pden_other;
    public SimpleODM.reservepoolproductionmodule.uc_pden_pool pden_pool;
    public SimpleODM.reservepoolproductionmodule.uc_pden_pr_str_allowable pden_pr_str_allowable;
    public SimpleODM.reservepoolproductionmodule.uc_pden_pr_str_form pden_pr_str_form;
    public SimpleODM.reservepoolproductionmodule.uc_pden_prod_string pden_prod_string;
    public SimpleODM.reservepoolproductionmodule.uc_pden_prod_string_xref pden_prod_string_xref;
    public SimpleODM.reservepoolproductionmodule.uc_pden_resent pden_resent;
    public SimpleODM.reservepoolproductionmodule.uc_pden_resent_class pden_resent_class;
    public SimpleODM.reservepoolproductionmodule.uc_pden_status_hist pden_status_hist;
    public SimpleODM.reservepoolproductionmodule.uc_pden_summary pden_summary;
    public SimpleODM.reservepoolproductionmodule.uc_pden_vol_disposition pden_vol_disposition;
    public SimpleODM.reservepoolproductionmodule.uc_pden_vol_regime pden_vol_regime;
    public SimpleODM.reservepoolproductionmodule.uc_pden_vol_summ_other pden_vol_summ_other;
    public SimpleODM.reservepoolproductionmodule.uc_pden_volume_analysis pden_volume_analysis;
    public SimpleODM.reservepoolproductionmodule.uc_pden_well pden_well;
    public SimpleODM.reservepoolproductionmodule.uc_pden_xref pden_xref;
    // ------------------------------ Reserve and Classification --------------------------------------------
    public SimpleODM.reservepoolproductionmodule.uc_resent_class resent_class;
    public SimpleODM.reservepoolproductionmodule.uc_resent_eco_run resent_eco_run;
    public SimpleODM.reservepoolproductionmodule.uc_resent_eco_schedule resent_eco_schedule;
    public SimpleODM.reservepoolproductionmodule.uc_resent_eco_volume resent_eco_volume;
    public SimpleODM.reservepoolproductionmodule.uc_resent_prod_property resent_prod_property;
    public SimpleODM.reservepoolproductionmodule.uc_resent_product resent_product;
    public SimpleODM.reservepoolproductionmodule.uc_resent_revision_cat resent_revision_cat;
    public SimpleODM.reservepoolproductionmodule.uc_resent_vol_regime resent_vol_regime;
    public SimpleODM.reservepoolproductionmodule.uc_resent_vol_revision resent_vol_revision;
    public SimpleODM.reservepoolproductionmodule.uc_resent_vol_summary resent_vol_summary;
    public SimpleODM.reservepoolproductionmodule.uc_resent_xref resent_xref;
    public SimpleODM.reservepoolproductionmodule.uc_reserve_class reserve_class;
    public SimpleODM.reservepoolproductionmodule.uc_reserve_class_calc reserve_class_calc;
    public SimpleODM.reservepoolproductionmodule.uc_reserve_class_formula reserve_class_formula;
    public SimpleODM.reservepoolproductionmodule.uc_reserve_entity reserve_entity;
    // ------------------------------------------------------------------------------------------------------
    // ------------------------------------ Lithology -------------------------------------------------------
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_comp_grain_size lith_comp_grain_size;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_component lith_component;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_component_color lith_component_color;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_dep_ent_int lith_dep_ent_int;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_desc_other lith_desc_other;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_diagenesis lith_diagenesis;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_grain_size lith_grain_size;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_interval lith_interval;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_log lith_log;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_log_ba_service lith_log_ba_service;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_log_remark lith_log_remark;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_measured_sec lith_measured_sec;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_porosity lith_porosity;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_color lith_rock_color;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_structure lith_rock_structure;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_type lith_rock_type;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_sample lith_sample;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_collection lith_sample_collection;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_desc lith_sample_desc;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_prep lith_sample_prep;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_prep_math lith_sample_prep_math;
    public SimpleODM.stratigraphyandlithologyModule.uc_lith_structure lith_structure;
    // ------------------------------------------------------------------------------------------------
    public SimpleODM.Logmodule.uc_log Well_Logs;
    public SimpleODM.Logmodule.uc_log_link_to_activity Well_Logs_Activities;
    public SimpleODM.Logmodule.uc_log_job Well_Log_job;
    public SimpleODM.Logmodule.uc_log_curve Well_Log_curve;
    public SimpleODM.Logmodule.uc_log_Loader Well_Log_loader;
    public SimpleODM.Logmodule.uc_log_dictionary Well_Log_dictionary;
    public SimpleODM.Logmodule.uc_log_parameter well_log_parameter;
    public SimpleODM.Logmodule.uc_log_parameter_array well_log_parameter_array;
    public SimpleODM.Logmodule.uc_log_job_trip_remark well_log_job_trip_remark;
    public SimpleODM.Logmodule.uc_log_job_trip_pass well_log_job_trip_pass;
    public SimpleODM.Logmodule.uc_log_job_trip well_log_job_trip;
    public SimpleODM.Logmodule.uc_log_dictionary_proc well_log_dictionary_proc;
    public SimpleODM.Logmodule.uc_log_dictionary_param_value well_log_dictionary_param_value;
    public SimpleODM.Logmodule.uc_log_dictionary_param_cls_Types well_log_dictionary_param_cls_Types;
    public SimpleODM.Logmodule.uc_log_dictionary_param_cls well_log_dictionary_param_cls;
    public SimpleODM.Logmodule.uc_log_dictionary_param well_log_dictionary_param;
    public SimpleODM.Logmodule.uc_log_dictionary_curve_cls well_log_dictionary_curve_cls;
    public SimpleODM.Logmodule.uc_log_dictionary_curve well_log_dictionary_curve;
    public SimpleODM.Logmodule.uc_log_dictionary_ba well_log_dictionary_ba;
    public SimpleODM.Logmodule.uc_log_dictionary_alias well_log_dictionary_alias;
    public SimpleODM.Logmodule.uc_log_curve_class well_log_curve_class;
    public SimpleODM.Logmodule.uc_log_cls_crv_cls well_log_cls_crv_cls;
    public SimpleODM.Logmodule.uc_log_class well_log_class;
    public SimpleODM.Logmodule.uc_log_remark well_log_remark;
    public SimpleODM.Logmodule.uc_log_manager well_log_manager;
    public SimpleODM.Logmodule.uc_log_manager well_log_files_manager;

    // -------------------------------- Record Managment RM_INFORMATION_ITEM -------------------------------

    // Public WithEvents rm_info_item As SimpleODM.RecordManagementModule.uc_rm_info_item
    // Public WithEvents rm_info_rm_Manager As SimpleODM.RecordManagementModule.uc_rm_Manager
    // Public WithEvents rm_info_item_physical_item As SimpleODM.RecordManagementModule.uc_rm_info_item_physical_item

    public SimpleODM.SharedLib.uc_simpleReporting_lite reportbuilder;
    public SimpleODM.SharedLib.uc_spreadsheet Excelreportbuilder;
    // ------------------------------------------------------------------------------------------------------
    private uc_login _uc_login1;

    public uc_login uc_login1
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _uc_login1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_uc_login1 != null)
            {
                _uc_login1.Logout -= Uc_login1_Logout;
                _uc_login1.ShowDatabase -= Uc_login1_ShowDatabase;
                _uc_login1.LoginCancel -= _uc_login_LoginCancel;
                _uc_login1.LoginSucccess -= _uc_login_LoginSucccess;
            }

            _uc_login1 = value;
            if (_uc_login1 != null)
            {
                _uc_login1.Logout += Uc_login1_Logout;
                _uc_login1.ShowDatabase += Uc_login1_ShowDatabase;
                _uc_login1.LoginCancel += _uc_login_LoginCancel;
                _uc_login1.LoginSucccess += _uc_login_LoginSucccess;
            }
        }
    }

    public SimpleODM.SharedLib.uc_dbdefine db_def;
    public uc_NoPrivControl no_priv;
    public rvmwithrtableslist_ctl rvmwithrtableslistModule;
    public uc_defaults_settings defaultsSetup;
    public uc_db_admin Administrator;
    // --------------------------------------------------------------------------------------
    // Public WithEvents BulkDataLoading As uc_SimpleTableLoading
    public uc_tableloader BulkDataLoading;

    public event LoginSucccessEventHandler LoginSucccess;

    public delegate void LoginSucccessEventHandler();

    public event LogoutEventHandler Logout;

    public delegate void LogoutEventHandler();

    public event LoginCancelEventHandler LoginCancel;

    public delegate void LoginCancelEventHandler();

    public event ShowDatabaseEventHandler ShowDatabase;

    public delegate void ShowDatabaseEventHandler();

    public MyForms()
    {
    }

    private void Uc_login1_Logout()
    {
        Logout?.Invoke();
    }

    private void Uc_login1_ShowDatabase()
    {
        // WindowsUIView1.Controller.Activate(DatabaseContainer)
        ShowDatabase?.Invoke();
    }

    private void _uc_login_LoginCancel()
    {
        LoginCancel?.Invoke();
    }

    private void _uc_login_LoginSucccess()
    {
        LoginSucccess?.Invoke();
    }

    ~MyForms()
    {
    }

    public event WellSelectedEventHandler WellSelected;

    public delegate void WellSelectedEventHandler(string UWI);

    private void _WellModule_WellSelected(string UWI)
    {
        WellSelected?.Invoke(UWI);
    }
}

public  class SharedBusinessObjects
{
    public SharedBusinessObjects()
    {
        bkwork = new BackgroundWorker();
        MyModules = new MyForms();

        // MyModules.uc_login1 = New SimpleODM.SharedLib.uc_login()
        // MyModules.db_def = New SimpleODM.SharedLib.uc_dbdefine()
        // MyModules.no_priv = New SimpleODM.SharedLib.uc_NoPrivControl
        SimpleUtil.SimpleODMConfig = SimpleODMConfig;
        SimpleUtil.f_HeightRatio = 1000;
        SimpleUtil.f_WidthRatio = 1800;
    }

    private DevExpress.XtraBars.Docking2010.Views.WindowsUI.WindowsUIView _WindowsUIView1;

    public DevExpress.XtraBars.Docking2010.Views.WindowsUI.WindowsUIView WindowsUIView1
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _WindowsUIView1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_WindowsUIView1 != null)
            {
                _WindowsUIView1.ControlReleased -= WindowsUIView1_ControlReleased;
                _WindowsUIView1.ControlReleasing -= WindowsUIView1_ControlReleasing;
                _WindowsUIView1.QueryControl -= WindowsUIView1_QueryControl;
            }

            _WindowsUIView1 = value;
            if (_WindowsUIView1 != null)
            {
                _WindowsUIView1.ControlReleased += WindowsUIView1_ControlReleased;
                _WindowsUIView1.ControlReleasing += WindowsUIView1_ControlReleasing;
                _WindowsUIView1.QueryControl += WindowsUIView1_QueryControl;
            }
        }
    }

    private BackgroundWorker bkwork;
    private SimpleODM.systemconfigandutil.PPDMConfig _SimpleODMConfig;

    public SimpleODM.systemconfigandutil.PPDMConfig SimpleODMConfig
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _SimpleODMConfig;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_SimpleODMConfig != null)
            {
                _SimpleODMConfig.ShowCtlInView -= SimpleODMConfig_ShowCtlInView;
                _SimpleODMConfig.ShowCtlInTile -= SimpleODMConfig_ShowControlOnTile;
            }

            _SimpleODMConfig = value;
            if (_SimpleODMConfig != null)
            {
                _SimpleODMConfig.ShowCtlInView += SimpleODMConfig_ShowCtlInView;
                _SimpleODMConfig.ShowCtlInTile += SimpleODMConfig_ShowControlOnTile;
            }
        }
    }

    public SimpleODM.SharedLib.util SimpleUtil { get; set; } = new SimpleODM.SharedLib.util();

    private DevExpress.XtraBars.Docking2010.Views.WindowsUI.Document ParentDoc;
    // Public Protection As ProtectionModule
    // Public WithEvents ETLConfig As SimpleODM.etlmodule.cls_ETL
    public string Title;

    public bool DidIInitForms { get; set; } = false;
    public bool DidIloginStatus { get; set; } = false;

    private MyForms _MyModules;

    public MyForms MyModules
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _MyModules;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_MyModules != null)
            {
                _MyModules.WellSelected -= MyModules_WellSelected;
                _MyModules.LoginCancel -= uc_login_LoginCancel;
                _MyModules.LoginSucccess -= uc_login_LoginSucccess;
                _MyModules.Logout -= uc_login_Logout;
                _MyModules.ShowDatabase -= uc_login_ShowDatabase;
            }

            _MyModules = value;
            if (_MyModules != null)
            {
                _MyModules.WellSelected += MyModules_WellSelected;
                _MyModules.LoginCancel += uc_login_LoginCancel;
                _MyModules.LoginSucccess += uc_login_LoginSucccess;
                _MyModules.Logout += uc_login_Logout;
                _MyModules.ShowDatabase += uc_login_ShowDatabase;
            }
        }
    }

    private int workinitprec = 0;

    public event FormsInitProgressEventHandler FormsInitProgress;

    public delegate void FormsInitProgressEventHandler(int Prec);

    public event FormsInitFinishEventHandler FormsInitFinish;

    public delegate void FormsInitFinishEventHandler();

    private bool InitForms = false;
    private string WellBoreItem;
    private string TransType;
    private string ucType;

    public void IniTheForms()
    {
        // InitMyForms()
        SimpleODMConfig.Defaults.WELLBORECOMPLETIONIDENTIFIER = "WELLCOMPLETION";
        SimpleODMConfig.Defaults.WELLBOREIDENTIFIER = "WELLBORE";
        SimpleODMConfig.Defaults.WELLIDENTIFIER = "WELL";
        SimpleODMConfig.Defaults.SOURCE = "PPDM";
        SimpleODMConfig.Defaults.ROW_SOURCE = "PPDM";
        WindowsUIView1.Caption = SimpleODMConfig.Defaults.APPLICATIONNAME;
    }

    ~SharedBusinessObjects()
    {
    }

    public void InitDBAAdminForms()
    {
        int Result = 0;
    }

    public void ShowControlInForm(ref Control Ctl, [Optional, DefaultParameterValue(null)] ref Control parentcontrol, string Title = "")
    {
        var frm = new Form();
        frm.Width = Ctl.Width;
        frm.Height = Ctl.Height;
        frm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Ctl.Dock = DockStyle.Fill;
        frm.Controls.Add(Ctl);
        if (!string.IsNullOrEmpty(Title))
        {
            frm.Text = Title;
        }

        if (parentcontrol is null)
        {
            frm.Show();
        }
        else
        {
            frm.Show(parentcontrol);
        }
    }
    /* TODO ERROR: Skipped RegionDirectiveTrivia *//* TODO ERROR: Skipped RegionDirectiveTrivia */
    public MenuItem CreateScreenHandler(string Name, ref EventHandler Func)
    {
        var item = new MenuItem();
        item.Name = Name;
        // item.  Other options

        item.Click += Func;
        return item;
    }

    public event WellSelectedEventHandler WellSelected;

    public delegate void WellSelectedEventHandler(string UWI);

    private void MyModules_WellSelected(string UWI)
    {
        WellSelected?.Invoke(UWI);
    }

    public event ShowControlOnTileEventHandler ShowControlOnTile;

    public delegate void ShowControlOnTileEventHandler(string Title, string pType, string trans);

    private XtraForm SimplePRintFrm;

    private void SimpleODMConfig_ShowCtlInView(string Title, string pType, string trans)
    {
        if (MyModules.BulkDataLoading==null)
        {
            MyModules.BulkDataLoading = new uc_tableloader( ref _SimpleODMConfig,SimpleUtil);
        }

        if (SimplePRintFrm==null)
        {
            SimplePRintFrm = new XtraForm();
            SimplePRintFrm.Width = MyModules.BulkDataLoading.Width + 50;
            SimplePRintFrm.Height = MyModules.BulkDataLoading.Height + 50;
            SimplePRintFrm.Controls.Add(MyModules.BulkDataLoading);
        }

        MyModules.BulkDataLoading.Dock = DockStyle.Fill;
        if (MyModules.BulkDataLoading.TableName != SimpleODMConfig.Defaults.CURRENT_TABLE)
        {
            MyModules.BulkDataLoading.LoadTableSchemas(SimpleODMConfig.Defaults.CURRENT_TABLE);
        }

        SimplePRintFrm.StartPosition = FormStartPosition.CenterScreen;
        SimplePRintFrm.Text = "Data Loading Module";
        SimplePRintFrm.ShowDialog();
    }

    private void SimpleODMConfig_ShowControlOnTile(string Title, string pType, string trans)
    {
        WellBoreItem = Title;
        ucType = pType;
        TransType = trans;
        try
        {
            switch (ucType ?? "")
            {
                case "PPDMSYSTEMTABLES":
                    {
                        break;
                    }
                // Simpleconfig.ShowControlInTile(Me.TitleLabel.Text, "PPDMSYSTEMTABLES", "EDIT")
                case "PPDMSYSTEMAUDIT":
                    {
                        break;
                    }
                // Simpleconfig.ShowControlInTile(Me.TitleLabel.Text, "PPDMSYSTEMAUDIT", "EDIT")
                case "PPDMSYSTEMRULES":
                    {
                        break;
                    }
                // Simpleconfig.ShowControlInTile(Me.TitleLabel.Text, "PPDMSYSTEMRULES", "EDIT")
                case "PPDMSYSTEMQC":
                    {
                        break;
                    }
                // Simpleconfig.ShowControlInTile(Me.TitleLabel.Text, "PPDMSYSTEMQC", "EDIT")
                // -------------------------------------- Log Data Management -----------------------------------------
                case "WELLLOG":
                    {
                        if (MyModules.Well_Logs is null)
                        {
                            MyModules.Well_Logs = new SimpleODM.Logmodule.uc_log( ref _SimpleODMConfig,SimpleUtil);
                        }

                        MyModules.Well_Logs.WellON = true;
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Logs.MyUWI)
                        {
                            MyModules.Well_Logs.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "LOGLOADER":
                    {
                        if (MyModules.well_log_manager is null)
                        {
                            MyModules.well_log_manager = new SimpleODM.Logmodule.uc_log_manager( ref _SimpleODMConfig, SimpleUtil);
                        }

                        break;
                    }

                case "WELLLOGMANAGER":
                    {
                        if (MyModules.well_log_manager is null)
                        {
                            MyModules.well_log_manager = new SimpleODM.Logmodule.uc_log_manager( ref _SimpleODMConfig, SimpleUtil);
                        }

                        MyModules.Well_Logs.LoadManager();
                        break;
                    }

                case "LOGDICT":
                case "WELLLOGDICTIONARY":
                case "WELLLOGDICTIONARYAPP":
                    {
                        if (MyModules.Well_Log_dictionary is null)
                        {
                            MyModules.Well_Log_dictionary = new SimpleODM.Logmodule.uc_log_dictionary( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.Well_Log_dictionary.DictID)
                        {
                            MyModules.Well_Log_dictionary.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID);
                        }

                        break;
                    }

                case "LOGACTIVITY":
                    {
                        if (MyModules.Well_Logs_Activities is null)
                        {
                            MyModules.Well_Logs_Activities = new SimpleODM.Logmodule.uc_log_link_to_activity( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Logs_Activities.MyUWI | MyModules.Well_Logs_Activities.MyWELL_LOG_ID != SimpleODMConfig.Defaults.WELL_LOG_ID | MyModules.Well_Logs_Activities.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE)
                        {
                            MyModules.Well_Logs_Activities.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLLOGJOB":
                case "WELLLOGJOBAPP":
                    {
                        if (MyModules.Well_Log_job is null)
                        {
                            MyModules.Well_Log_job = new SimpleODM.Logmodule.uc_log_job( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Log_job.MyUWI)
                        {
                            MyModules.Well_Log_job.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLLOGCURVE":
                case "WELLLOGCURVEAPP":
                    {
                        if (MyModules.Well_Log_curve is null)
                        {
                            MyModules.Well_Log_curve = new SimpleODM.Logmodule.uc_log_curve( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Log_curve.MyUWI)
                        {
                            MyModules.Well_Log_curve.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE);
                        }

                        break;
                    }

                case "LOGJOB":
                    {
                        if (MyModules.Well_Log_job is null)
                        {
                            MyModules.Well_Log_job = new SimpleODM.Logmodule.uc_log_job( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Log_job.MyUWI)
                        {
                            MyModules.Well_Log_job.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLLOGJOBTRIP":                   // Well Log Job Trip
                    {
                        if (MyModules.well_log_job_trip is null)
                        {
                            MyModules.well_log_job_trip = new SimpleODM.Logmodule.uc_log_job_trip( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_log_job_trip.MyUWI | MyModules.well_log_job_trip.MyWELL_LOG_JOB_ID != SimpleODMConfig.Defaults.WELL_LOG_JOB_ID | MyModules.well_log_job_trip.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE)
                        {
                            MyModules.well_log_job_trip.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_JOB_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLLOGJOBTRIPPASS":                // Well Log Job Trip Pass
                    {
                        if (MyModules.well_log_job_trip_pass is null)
                        {
                            MyModules.well_log_job_trip_pass = new SimpleODM.Logmodule.uc_log_job_trip_pass( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_log_job_trip_pass.MyUWI | MyModules.well_log_job_trip_pass.MyWELL_LOG_JOB_ID != SimpleODMConfig.Defaults.WELL_LOG_JOB_ID | MyModules.well_log_job_trip_pass.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE | MyModules.well_log_job_trip_pass.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE)
                        {
                            MyModules.well_log_job_trip_pass.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_JOB_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_LOG_TRIP_OBS_NO);
                        }

                        break;
                    }

                case "WELLLOGJOBTRIPREMARK":                  // Well Log Job Trip Remark
                    {
                        if (MyModules.well_log_job_trip_remark is null)
                        {
                            MyModules.well_log_job_trip_remark = new SimpleODM.Logmodule.uc_log_job_trip_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_log_job_trip_remark.MyUWI | MyModules.well_log_job_trip_remark.MyWELL_LOG_JOB_ID != SimpleODMConfig.Defaults.WELL_LOG_JOB_ID | MyModules.well_log_job_trip_remark.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE | MyModules.well_log_job_trip_remark.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE)
                        {
                            MyModules.well_log_job_trip_remark.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_JOB_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_LOG_TRIP_OBS_NO);
                        }

                        break;
                    }

                case "LOGDICTALIAS":                     // Well Log Dictionary Alias
                    {
                        if (MyModules.well_log_dictionary_alias is null)
                        {
                            MyModules.well_log_dictionary_alias = new SimpleODM.Logmodule.uc_log_dictionary_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.well_log_dictionary_alias.DictID)
                        {
                            MyModules.well_log_dictionary_alias.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID);
                        }

                        break;
                    }

                case "LOGDICTCURVE":                         // Well Log Dictionary Curve
                    {
                        if (MyModules.well_log_dictionary_curve is null)
                        {
                            MyModules.well_log_dictionary_curve = new SimpleODM.Logmodule.uc_log_dictionary_curve( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.well_log_dictionary_curve.DictID)
                        {
                            MyModules.well_log_dictionary_curve.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID);
                        }

                        break;
                    }

                case "LOGDICTCURVECLASS":                // Well Log Dictionary Curve Classification
                    {
                        if (MyModules.well_log_dictionary_curve_cls is null)
                        {
                            MyModules.well_log_dictionary_curve_cls = new SimpleODM.Logmodule.uc_log_dictionary_curve_cls( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.well_log_dictionary_curve_cls.DictID | SimpleODMConfig.Defaults.WELL_LOG_DICT_CURVE_ID != MyModules.well_log_dictionary_curve_cls.myDict_Curve_id)
                        {
                            MyModules.well_log_dictionary_curve_cls.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID, SimpleODMConfig.Defaults.WELL_LOG_DICT_CURVE_ID);
                        }

                        break;
                    }

                case "LOGDICTPARAMETER":                          // Well Log Dictionary Parameter
                    {
                        if (MyModules.well_log_dictionary_param is null)
                        {
                            MyModules.well_log_dictionary_param = new SimpleODM.Logmodule.uc_log_dictionary_param( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.well_log_dictionary_param.DictID)
                        {
                            MyModules.well_log_dictionary_param.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID);
                        }

                        break;
                    }

                case "LOGDICTPARAMETERCLASS":                              // Well Log Dictionary Parameter Classification
                    {
                        if (MyModules.well_log_dictionary_param_cls is null)
                        {
                            MyModules.well_log_dictionary_param_cls = new SimpleODM.Logmodule.uc_log_dictionary_param_cls( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.well_log_dictionary_param_cls.DictID | SimpleODMConfig.Defaults.WELL_LOG_DICT_PARAM_ID != MyModules.well_log_dictionary_param_cls.Dict_Parameter_id)
                        {
                            MyModules.well_log_dictionary_param_cls.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID, SimpleODMConfig.Defaults.WELL_LOG_DICT_PARAM_ID);
                        }

                        break;
                    }

                case "LOGDICTPARAMETERCLASSTYPES":                    // Well Log Dictionary Parameter Classification Types
                    {
                        if (MyModules.well_log_dictionary_param_cls_Types is null)
                        {
                            MyModules.well_log_dictionary_param_cls_Types = new SimpleODM.Logmodule.uc_log_dictionary_param_cls_Types( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_param_cls_Types.m) Then
                        MyModules.well_log_dictionary_param_cls_Types.Loaddata();
                        break;
                    }
                // End If
                case "LOGDICTPARAMETERVALUE":             // Well Log Dictionary Parameter Values
                    {
                        if (MyModules.well_log_dictionary_param_value is null)
                        {
                            MyModules.well_log_dictionary_param_value = new SimpleODM.Logmodule.uc_log_dictionary_param_value( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.well_log_dictionary_param_value.DictID | SimpleODMConfig.Defaults.WELL_LOG_DICT_PARAM_ID != MyModules.well_log_dictionary_param_value.Dict_Parameter_id)
                        {
                            MyModules.well_log_dictionary_param_value.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID, SimpleODMConfig.Defaults.WELL_LOG_DICT_PARAM_ID);
                        }

                        break;
                    }

                case "LOGDICTBA":                                 // Well Log Dictionary Business Associate
                    {
                        if (MyModules.well_log_dictionary_ba is null)
                        {
                            MyModules.well_log_dictionary_ba = new SimpleODM.Logmodule.uc_log_dictionary_ba( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.well_log_dictionary_ba.DictID)
                        {
                            MyModules.well_log_dictionary_ba.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID);
                        }

                        break;
                    }

                case "LOGDICTPROCEDURE":                // Well Log Dictionary Procedure
                    {
                        if (MyModules.well_log_dictionary_proc is null)
                        {
                            MyModules.well_log_dictionary_proc = new SimpleODM.Logmodule.uc_log_dictionary_proc( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID != MyModules.well_log_dictionary_proc.DictID)
                        {
                            MyModules.well_log_dictionary_proc.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID);
                        }

                        break;
                    }

                case "WELLLOGCLASSES":
                case "WELLLOGCLASSESAPP":                         // Well Log Classification,
                    {
                        if (MyModules.well_log_class is null)
                        {
                            MyModules.well_log_class = new uc_log_class( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (MyModules.well_log_class.MyUWI != SimpleODMConfig.Defaults.WELL_XREFMyUWI | MyModules.well_log_class.MyWELL_LOG_ID != SimpleODMConfig.Defaults.WELL_LOG_ID | MyModules.well_log_class.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE)
                        {
                            MyModules.well_log_class.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLLOGREMARK":
                case "WELLLOGREMARKAPP":                        // Well Log Remark
                    {
                        if (MyModules.well_log_remark is null)
                        {
                            MyModules.well_log_remark = new uc_log_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (MyModules.well_log_remark.MyUWI != SimpleODMConfig.Defaults.WELL_XREFMyUWI | MyModules.well_log_remark.MyWELL_LOG_ID != SimpleODMConfig.Defaults.WELL_LOG_ID | MyModules.well_log_remark.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE)
                        {
                            MyModules.well_log_remark.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }
                // Case "WELLLOGCURVEORFRAME"
                // If (MyModules.BulkDataLoading Is Nothing) Then
                // MyModules.BulkDataLoading = New uc_SimpleTableLoading( ref _SimpleODMConfig, SimpleUtil)

                // End If
                // If MyModules.BulkDataLoading.TableName <> SimpleODMConfig.Defaults.CURRENT_TABLE Then
                // MyModules.BulkDataLoading.TableName = SimpleODMConfig.Defaults.CURRENT_TABLE
                // MyModules.BulkDataLoading.LoadTableSchemas(SimpleODMConfig.Defaults.CURRENT_TABLE)
                // End If


                case "WELLLOGPARAMETER":
                case "WELLLOGPARAMETERAPP":                // Well Log Parameters
                    {
                        if (MyModules.well_log_parameter is null)
                        {
                            MyModules.well_log_parameter = new uc_log_parameter( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (MyModules.well_log_parameter.MyUWI != SimpleODMConfig.Defaults.WELL_XREFMyUWI | MyModules.well_log_parameter.MyWELL_LOG_ID != SimpleODMConfig.Defaults.WELL_LOG_ID | MyModules.well_log_parameter.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE)
                        {
                            MyModules.well_log_parameter.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLLOGPARAMETERARRAY":                   // Well Log Parameters
                    {
                        if (MyModules.well_log_parameter_array is null)
                        {
                            MyModules.well_log_parameter_array = new uc_log_parameter_array( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (MyModules.well_log_parameter_array.MyUWI != SimpleODMConfig.Defaults.WELL_XREFMyUWI | MyModules.well_log_parameter_array.MyWELL_LOG_ID != SimpleODMConfig.Defaults.WELL_LOG_ID | MyModules.well_log_parameter_array.MyWELL_LOG_SOURCE != SimpleODMConfig.Defaults.SOURCE | MyModules.well_log_parameter_array.MyWELL_LOG_PARAMETER_SEQ_NO != SimpleODMConfig.Defaults.WELL_LOG_PARAMETER_SEQ_NO)
                        {
                            MyModules.well_log_parameter_array.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI, MyModules.well_log_parameter_array.MyWELL_LOG_PARAMETER_SEQ_NO);
                        }

                        break;
                    }


                // ----------------------------------------------------------------------------------------------------
                // --------------------------------------Bulk Data Loader-----------------------------------------------
                case "BULKDATA":
                    {
                        if (MyModules.BulkDataLoading is null)
                        {
                            MyModules.BulkDataLoading = new uc_tableloader( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (MyModules.BulkDataLoading.TableName != SimpleODMConfig.Defaults.CURRENT_TABLE)
                        {
                            MyModules.BulkDataLoading.TableName = SimpleODMConfig.Defaults.CURRENT_TABLE;
                            MyModules.BulkDataLoading.LoadTableSchemas(SimpleODMConfig.Defaults.CURRENT_TABLE);
                        }

                        break;
                    }
                // ------------------------------------ Lithology -------------------------------------------------------
                case "LITHLOG":
                    {
                        if (MyModules.lith_log is null)
                        {
                            MyModules.lith_log = new SimpleODM.stratigraphyandlithologyModule.uc_lith_log( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.MEAS_SECTION_ID != MyModules.lith_log.MEASSECTIONID | SimpleODMConfig.Defaults.MEAS_SECTION_SOURCE != MyModules.lith_log.MEASSECTIONSOURCE)
                        {
                            MyModules.lith_log.Loaddata(SimpleODMConfig.Defaults.MEAS_SECTION_ID, SimpleODMConfig.Defaults.MEAS_SECTION_SOURCE);
                        }

                        break;
                    }

                case "LITHLOGREMARK":
                    {
                        if (MyModules.lith_log_remark is null)
                        {
                            MyModules.lith_log_remark = new SimpleODM.stratigraphyandlithologyModule.uc_lith_log_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_log_remark.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_log_remark.LITHLOGSOURCE)
                        {
                            MyModules.lith_log_remark.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE);
                        }

                        break;
                    }

                case "LITHLOGBASERVICE":
                    {
                        if (MyModules.lith_log_ba_service is null)
                        {
                            MyModules.lith_log_ba_service = new SimpleODM.stratigraphyandlithologyModule.uc_lith_log_ba_service( ref _SimpleODMConfig,SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_log_ba_service.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_log_ba_service.LITHLOGSOURCE)
                        {
                            MyModules.lith_log_ba_service.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE);
                        }

                        break;
                    }

                case "LITHLOGENVINT":
                    {
                        if (MyModules.lith_dep_ent_int is null)
                        {
                            MyModules.lith_dep_ent_int = new SimpleODM.stratigraphyandlithologyModule.uc_lith_dep_ent_int( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_dep_ent_int.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_dep_ent_int.LITHLOGSOURCE)
                        {
                            MyModules.lith_dep_ent_int.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE);
                        }

                        break;
                    }

                case "LITHLOGDEPTHINT":
                    {
                        if (MyModules.lith_interval is null)
                        {
                            MyModules.lith_interval = new SimpleODM.stratigraphyandlithologyModule.uc_lith_interval( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_interval.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_interval.LITHLOGSOURCE)
                        {
                            MyModules.lith_interval.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE);
                        }

                        break;
                    }

                case "LITHINTERVALROCKTYPE":
                    {
                        if (MyModules.lith_rock_type is null)
                        {
                            MyModules.lith_rock_type = new SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_type( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_rock_type.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_rock_type.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_rock_type.DEPTHOBSNO)
                        {
                            MyModules.lith_rock_type.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO);
                        }

                        break;
                    }

                case "LITHINTERVALROCKTYPECOMP":
                    {
                        if (MyModules.lith_component is null)
                        {
                            MyModules.lith_component = new SimpleODM.stratigraphyandlithologyModule.uc_lith_component( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_component.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_component.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_component.DEPTHOBSNO | SimpleODMConfig.Defaults.ROCK_TYPE != MyModules.lith_component.ROCKTYPE | SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO != MyModules.lith_component.ROCKTYPEOBSNO)
                        {
                            MyModules.lith_component.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO);
                        }

                        break;
                    }

                case "LITHCOMPCOLOR":
                    {
                        if (MyModules.lith_component_color is null)
                        {
                            MyModules.lith_component_color = new SimpleODM.stratigraphyandlithologyModule.uc_lith_component_color( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_component_color.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_component_color.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_component_color.DEPTHOBSNO | SimpleODMConfig.Defaults.ROCK_TYPE != MyModules.lith_component_color.ROCKTYPE | SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO != MyModules.lith_component_color.ROCKTYPEOBSNO | SimpleODMConfig.Defaults.COMPONENT_NAME != MyModules.lith_component_color.COMPONENTNAME)
                        {
                            MyModules.lith_component_color.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO, SimpleODMConfig.Defaults.COMPONENT_NAME);
                        }

                        break;
                    }

                case "LITHCOMPGRAINSIZE":
                    {
                        if (MyModules.lith_comp_grain_size is null)
                        {
                            MyModules.lith_comp_grain_size = new SimpleODM.stratigraphyandlithologyModule.uc_lith_comp_grain_size( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_comp_grain_size.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_comp_grain_size.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_comp_grain_size.DEPTHOBSNO | SimpleODMConfig.Defaults.ROCK_TYPE != MyModules.lith_comp_grain_size.ROCKTYPE | SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO != MyModules.lith_comp_grain_size.ROCKTYPEOBSNO | SimpleODMConfig.Defaults.COMPONENT_NAME != MyModules.lith_comp_grain_size.COMPONENTNAME)
                        {
                            MyModules.lith_comp_grain_size.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO, SimpleODMConfig.Defaults.COMPONENT_NAME);
                        }

                        break;
                    }

                case "LITHINTERVALROCKTYPEDIAG":
                    {
                        if (MyModules.lith_diagenesis is null)
                        {
                            MyModules.lith_diagenesis = new SimpleODM.stratigraphyandlithologyModule.uc_lith_diagenesis( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_diagenesis.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_diagenesis.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_diagenesis.DEPTHOBSNO | SimpleODMConfig.Defaults.ROCK_TYPE != MyModules.lith_diagenesis.ROCKTYPE | SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO != MyModules.lith_diagenesis.ROCKTYPEOBSNO)
                        {
                            MyModules.lith_diagenesis.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO);
                        }

                        break;
                    }

                case "LITHINTERVALROCKTYPEGRAINSIZE":
                    {
                        if (MyModules.lith_grain_size is null)
                        {
                            MyModules.lith_grain_size = new SimpleODM.stratigraphyandlithologyModule.uc_lith_grain_size( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_grain_size.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_grain_size.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_grain_size.DEPTHOBSNO | SimpleODMConfig.Defaults.ROCK_TYPE != MyModules.lith_grain_size.ROCKTYPE | SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO != MyModules.lith_grain_size.ROCKTYPEOBSNO)
                        {
                            MyModules.lith_grain_size.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO);
                        }

                        break;
                    }

                case "LITHINTERVALROCKTYPEPOROSITY":
                    {
                        if (MyModules.lith_porosity is null)
                        {
                            MyModules.lith_porosity = new SimpleODM.stratigraphyandlithologyModule.uc_lith_porosity( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_porosity.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_porosity.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_porosity.DEPTHOBSNO | SimpleODMConfig.Defaults.ROCK_TYPE != MyModules.lith_porosity.ROCKTYPE | SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO != MyModules.lith_porosity.ROCKTYPEOBSNO)
                        {
                            MyModules.lith_porosity.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO);
                        }

                        break;
                    }

                case "LITHINTERVALROCKTYPECOLOR":
                    {
                        if (MyModules.lith_rock_color is null)
                        {
                            MyModules.lith_rock_color = new SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_color( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_rock_color.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_rock_color.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_rock_color.DEPTHOBSNO | SimpleODMConfig.Defaults.ROCK_TYPE != MyModules.lith_rock_color.ROCKTYPE | SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO != MyModules.lith_rock_color.ROCKTYPEOBSNO)
                        {
                            MyModules.lith_rock_color.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO);
                        }

                        break;
                    }

                case "LITHINTERVALROCKTYPESTRUCTURE":
                    {
                        if (MyModules.lith_rock_structure is null)
                        {
                            MyModules.lith_rock_structure = new SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_structure( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_rock_structure.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_rock_structure.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_rock_structure.DEPTHOBSNO | SimpleODMConfig.Defaults.ROCK_TYPE != MyModules.lith_rock_structure.ROCKTYPE | SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO != MyModules.lith_rock_structure.ROCKTYPEOBSNO)
                        {
                            MyModules.lith_rock_structure.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO);
                        }

                        break;
                    }

                case "LITHINTERVALSTRUCTURE":
                    {
                        if (MyModules.lith_structure is null)
                        {
                            MyModules.lith_structure = new SimpleODM.stratigraphyandlithologyModule.uc_lith_structure( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID != MyModules.lith_structure.LITHOLOGYLOGID | SimpleODMConfig.Defaults.LITH_LOG_SOURCE != MyModules.lith_structure.LITHLOGSOURCE | SimpleODMConfig.Defaults.DEPTH_OBS_NO != MyModules.lith_structure.DEPTHOBSNO)
                        {
                            MyModules.lith_structure.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO);
                        }

                        break;
                    }

                case "LITHSAMPLECOLLECTION":
                    {
                        if (MyModules.lith_sample_collection is null)
                        {
                            MyModules.lith_sample_collection = new SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_collection( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITH_SAMPLE_ID != MyModules.lith_sample_collection.LITHSAMPLEID)
                        {
                            MyModules.lith_sample_collection.Loaddata(SimpleODMConfig.Defaults.LITH_SAMPLE_ID);
                        }

                        break;
                    }

                case "LITHSAMPLEDESC":
                    {
                        if (MyModules.lith_sample_desc is null)
                        {
                            MyModules.lith_sample_desc = new SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_desc( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITH_SAMPLE_ID != MyModules.lith_sample_desc.LITHSAMPLEID)
                        {
                            MyModules.lith_sample_desc.Loaddata(SimpleODMConfig.Defaults.LITH_SAMPLE_ID);
                        }

                        break;
                    }

                case "LITHSAMPLEPREP":
                    {
                        if (MyModules.lith_sample_prep is null)
                        {
                            MyModules.lith_sample_prep = new SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_prep( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITH_SAMPLE_ID != MyModules.lith_sample_prep.LITHSAMPLEID)
                        {
                            MyModules.lith_sample_prep.Loaddata(SimpleODMConfig.Defaults.LITH_SAMPLE_ID);
                        }

                        break;
                    }

                case "LITHSAMPLEPREPMETH":
                    {
                        if (MyModules.lith_sample_prep_math is null)
                        {
                            MyModules.lith_sample_prep_math = new SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_prep_math( ref _SimpleODMConfig, SimpleUtil);
                        }

                        MyModules.lith_sample_prep_math.Loaddata();
                        break;
                    }

                case "LITHDESCOTHER":
                    {
                        if (MyModules.lith_desc_other is null)
                        {
                            MyModules.lith_desc_other = new SimpleODM.stratigraphyandlithologyModule.uc_lith_desc_other( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.LITH_SAMPLE_ID != MyModules.lith_desc_other.LITHSAMPLEID)
                        {
                            MyModules.lith_desc_other.Loaddata(SimpleODMConfig.Defaults.LITH_SAMPLE_ID);
                        }

                        break;
                    }
                // ------------------------------------ Reserve Entities and Classisifactions --------------------
                case "RESERVECLASSECO":
                    {
                        if (MyModules.resent_eco_run is null)
                        {
                            MyModules.resent_eco_run = new SimpleODM.reservepoolproductionmodule.uc_resent_eco_run( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_eco_run.RESERVECLASSid | SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_eco_run.RESENTid)
                        {
                            MyModules.resent_eco_run.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID);
                        }

                        break;
                    }

                case "RESERVECLASSPRODUCT":
                    {
                        if (MyModules.resent_product is null)
                        {
                            MyModules.resent_product = new SimpleODM.reservepoolproductionmodule.uc_resent_product( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_product.RESERVECLASSid | SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_product.RESENTid)
                        {
                            MyModules.resent_product.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID);
                        }

                        break;
                    }

                case "RESERVECLASSECOPARAM":
                    {
                        if (MyModules.resent_eco_schedule is null)
                        {
                            MyModules.resent_eco_schedule = new SimpleODM.reservepoolproductionmodule.uc_resent_eco_schedule( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_eco_schedule.RESERVECLASSid | SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_eco_schedule.RESENTid | SimpleODMConfig.Defaults.ECONOMICS_RUN_ID != MyModules.resent_eco_schedule.ECONOMICSRUNID)
                        {
                            MyModules.resent_eco_schedule.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.ECONOMICS_RUN_ID);
                        }

                        break;
                    }

                case "RESERVECLASSECOVOLUME":
                    {
                        if (MyModules.resent_eco_volume is null)
                        {
                            MyModules.resent_eco_volume = new SimpleODM.reservepoolproductionmodule.uc_resent_eco_volume( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_eco_volume.RESERVECLASSid | SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_eco_volume.RESENTid | SimpleODMConfig.Defaults.ECONOMICS_RUN_ID != MyModules.resent_eco_volume.ECONOMICSRUNID)
                        {
                            MyModules.resent_eco_volume.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.ECONOMICS_RUN_ID);
                        }

                        break;
                    }

                case "RESERVEPRODPROP":
                    {
                        if (MyModules.resent_prod_property is null)
                        {
                            MyModules.resent_prod_property = new SimpleODM.reservepoolproductionmodule.uc_resent_prod_property( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_prod_property.RESERVECLASSid | SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_prod_property.RESENTid | SimpleODMConfig.Defaults.PRODUCT_TYPE != MyModules.resent_prod_property.Producttype)
                        {
                            MyModules.resent_prod_property.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE);
                        }

                        break;
                    }

                case "RESERVEPRODVOLSUMMARY":
                    {
                        if (MyModules.resent_vol_summary is null)
                        {
                            MyModules.resent_vol_summary = new SimpleODM.reservepoolproductionmodule.uc_resent_vol_summary( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_vol_summary.RESERVECLASSid | SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_vol_summary.RESENTid | SimpleODMConfig.Defaults.PRODUCT_TYPE != MyModules.resent_vol_summary.Producttype)
                        {
                            MyModules.resent_vol_summary.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE);
                        }

                        break;
                    }

                case "RESERVEVOLREVISIONS":
                    {
                        if (MyModules.resent_vol_revision is null)
                        {
                            MyModules.resent_vol_revision = new SimpleODM.reservepoolproductionmodule.uc_resent_vol_revision( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_vol_revision.RESERVECLASSid | SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_vol_revision.RESENTid | SimpleODMConfig.Defaults.PRODUCT_TYPE != MyModules.resent_vol_revision.Producttype | SimpleODMConfig.Defaults.SUMMARY_ID != MyModules.resent_vol_revision.SUMMARYID | SimpleODMConfig.Defaults.SUMMARY_OBS_NO != MyModules.resent_vol_revision.SUMMARYOBSNO)
                        {
                            MyModules.resent_vol_revision.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE, SimpleODMConfig.Defaults.SUMMARY_ID, SimpleODMConfig.Defaults.SUMMARY_OBS_NO);
                        }

                        break;
                    }

                case "RESERVECLASSIFICATIONSFORMULA":
                    {
                        if (MyModules.reserve_class_formula is null)
                        {
                            MyModules.reserve_class_formula = new SimpleODM.reservepoolproductionmodule.uc_reserve_class_formula( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.reserve_class_formula.RESERVECLASSID)
                        {
                            MyModules.reserve_class_formula.Loaddata(SimpleODMConfig.Defaults.RESERVE_CLASS_ID);
                        }

                        break;
                    }

                case "RESERVECLASSIFICATIONSFORMULACALC":
                    {
                        if (MyModules.reserve_class_calc is null)
                        {
                            MyModules.reserve_class_calc = new SimpleODM.reservepoolproductionmodule.uc_reserve_class_calc( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.reserve_class_calc.RESERVECLASSID | SimpleODMConfig.Defaults.FORMULA_ID != MyModules.reserve_class_calc.FORMULAID)
                        {
                            MyModules.reserve_class_calc.Loaddata(SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.FORMULA_ID);
                        }

                        break;
                    }

                case "RESERVECLASSIFICATIONS":
                    {
                        if (MyModules.reserve_class is null)
                        {
                            MyModules.reserve_class = new SimpleODM.reservepoolproductionmodule.uc_reserve_class( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // If (SimpleODMconfig.Defaults.RESENT_ID <> MyModules.resent_revision_cat.RESENTid) Then
                        MyModules.reserve_class.Loaddata();
                        break;
                    }

                case "RESERVEENTITYCLASS":
                    {
                        if (MyModules.resent_class is null)
                        {
                            MyModules.resent_class = new SimpleODM.reservepoolproductionmodule.uc_resent_class( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_class.RESENTid)
                        {
                            MyModules.resent_class.Loaddata(SimpleODMConfig.Defaults.RESENT_ID);
                        }

                        break;
                    }

                case "RESERVECROSSREF":
                    {
                        if (MyModules.resent_xref is null)
                        {
                            MyModules.resent_xref = new SimpleODM.reservepoolproductionmodule.uc_resent_xref( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_xref.RESENTid)
                        {
                            MyModules.resent_xref.Loaddata(SimpleODMConfig.Defaults.RESENT_ID);
                        }

                        break;
                    }

                case "RESERVEUNITREGIME":
                    {
                        if (MyModules.resent_vol_regime is null)
                        {
                            MyModules.resent_vol_regime = new SimpleODM.reservepoolproductionmodule.uc_resent_vol_regime( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.RESENT_ID != MyModules.resent_vol_regime.RESENTid)
                        {
                            MyModules.resent_vol_regime.Loaddata(SimpleODMConfig.Defaults.RESENT_ID);
                        }

                        break;
                    }

                case "RESERVEREVISION":
                    {
                        if (MyModules.resent_revision_cat is null)
                        {
                            MyModules.resent_revision_cat = new SimpleODM.reservepoolproductionmodule.uc_resent_revision_cat( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // If (SimpleODMconfig.Defaults.RESENT_ID <> MyModules.resent_revision_cat.RESENTid) Then
                        MyModules.resent_revision_cat.Loaddata();
                        break;
                    }
                // End If
                // ------------------------------------ PDEN Production Entities -------

                case "PDENBA":
                    {
                        if (MyModules.pden_business_assoc is null)
                        {
                            MyModules.pden_business_assoc = new SimpleODM.reservepoolproductionmodule.uc_pden_business_assoc( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_business_assoc.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_business_assoc.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_business_assoc.PDENSOURCE)
                        {
                            MyModules.pden_business_assoc.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENAREA":
                    {
                        if (MyModules.pden_area is null)
                        {
                            MyModules.pden_area = new SimpleODM.reservepoolproductionmodule.uc_pden_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_area.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_area.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_area.PDENSOURCE)
                        {
                            MyModules.pden_area.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENOTHER":
                    {
                        if (MyModules.pden_other is null)
                        {
                            MyModules.pden_other = new SimpleODM.reservepoolproductionmodule.uc_pden_other( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_other.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_other.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_other.PDENSOURCE)
                        {
                            MyModules.pden_other.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENWELL":
                    {
                        if (MyModules.pden_well is null)
                        {
                            MyModules.pden_well = new SimpleODM.reservepoolproductionmodule.uc_pden_well( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_well.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_well.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_well.PDENSOURCE)
                        {
                            MyModules.pden_well.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENPRODSTRING":
                    {
                        if (MyModules.pden_prod_string is null)
                        {
                            MyModules.pden_prod_string = new SimpleODM.reservepoolproductionmodule.uc_pden_prod_string( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_prod_string.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_prod_string.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_prod_string.PDENSOURCE)
                        {
                            MyModules.pden_prod_string.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENRESERVE":
                    {
                        if (MyModules.pden_resent is null)
                        {
                            MyModules.pden_resent = new SimpleODM.reservepoolproductionmodule.uc_pden_resent( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_resent.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_resent.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_resent.PDENSOURCE)
                        {
                            MyModules.pden_resent.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENFORMATION":
                    {
                        if (MyModules.pden_pr_str_form is null)
                        {
                            MyModules.pden_pr_str_form = new SimpleODM.reservepoolproductionmodule.uc_pden_pr_str_form( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_pr_str_form.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_pr_str_form.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_pr_str_form.PDENSOURCE)
                        {
                            MyModules.pden_pr_str_form.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENPOOL":
                    {
                        if (MyModules.pden_pool is null)
                        {
                            MyModules.pden_pool = new SimpleODM.reservepoolproductionmodule.uc_pden_pool( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_pool.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_pool.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_pool.PDENSOURCE)
                        {
                            MyModules.pden_pool.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENLEASE":
                    {
                        if (MyModules.pden_lease_unit is null)
                        {
                            MyModules.pden_lease_unit = new SimpleODM.reservepoolproductionmodule.uc_pden_lease_unit( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_lease_unit.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_lease_unit.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_lease_unit.PDENSOURCE)
                        {
                            MyModules.pden_lease_unit.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENRESERVECLASS":
                    {
                        if (MyModules.pden_resent_class is null)
                        {
                            MyModules.pden_resent_class = new SimpleODM.reservepoolproductionmodule.uc_pden_resent_class( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_resent_class.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_resent_class.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_resent_class.PDENSOURCE)
                        {
                            MyModules.pden_resent_class.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENFACILITY":
                    {
                        if (MyModules.pden_facility is null)
                        {
                            MyModules.pden_facility = new SimpleODM.reservepoolproductionmodule.uc_pden_facility( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_facility.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_facility.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_facility.PDENSOURCE)
                        {
                            MyModules.pden_facility.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENFIELD":
                    {
                        if (MyModules.pden_field is null)
                        {
                            MyModules.pden_field = new SimpleODM.reservepoolproductionmodule.uc_pden_field( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_field.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_field.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_field.PDENSOURCE)
                        {
                            MyModules.pden_field.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENALLOCFACTOR":
                    {
                        if (MyModules.pden_alloc_factor is null)
                        {
                            MyModules.pden_alloc_factor = new SimpleODM.reservepoolproductionmodule.uc_pden_alloc_factor( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_alloc_factor.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_alloc_factor.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_alloc_factor.PDENSOURCE)
                        {
                            MyModules.pden_alloc_factor.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENDECLINECASE":
                    {
                        if (MyModules.pden_decline_case is null)
                        {
                            MyModules.pden_decline_case = new SimpleODM.reservepoolproductionmodule.uc_pden_decline_case( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_decline_case.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_decline_case.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_decline_case.PDENSOURCE)
                        {
                            MyModules.pden_decline_case.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENDECLINECASECOND":
                    {
                        if (MyModules.pden_decline_condition is null)
                        {
                            MyModules.pden_decline_condition = new SimpleODM.reservepoolproductionmodule.uc_pden_decline_condition( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_decline_condition.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_decline_condition.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_decline_condition.PDENSOURCE | SimpleODMConfig.Defaults.CASE_ID != MyModules.pden_decline_condition.CASEID | SimpleODMConfig.Defaults.PRODUCT_TYPE != MyModules.pden_decline_condition.PRODUCTTYPE)
                        {
                            MyModules.pden_decline_condition.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE, SimpleODMConfig.Defaults.CASE_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE);
                        }

                        break;
                    }

                case "PDENDECLINECASESEG":
                    {
                        if (MyModules.pden_decline_segment is null)
                        {
                            MyModules.pden_decline_segment = new SimpleODM.reservepoolproductionmodule.uc_pden_decline_segment( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_decline_segment.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_decline_segment.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_decline_segment.PDENSOURCE | SimpleODMConfig.Defaults.CASE_ID != MyModules.pden_decline_segment.CASEID | SimpleODMConfig.Defaults.PRODUCT_TYPE != MyModules.pden_decline_segment.PRODUCTTYPE)
                        {
                            MyModules.pden_decline_segment.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE, SimpleODMConfig.Defaults.CASE_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE);
                        }

                        break;
                    }

                case "PDENFLOWMEASURE":
                    {
                        if (MyModules.pden_flow_measurement is null)
                        {
                            MyModules.pden_flow_measurement = new SimpleODM.reservepoolproductionmodule.uc_pden_flow_measurement( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_flow_measurement.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_flow_measurement.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_flow_measurement.PDENSOURCE)
                        {
                            MyModules.pden_flow_measurement.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENINAREA":
                    {
                        if (MyModules.pden_in_area is null)
                        {
                            MyModules.pden_in_area = new SimpleODM.reservepoolproductionmodule.uc_pden_in_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_in_area.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_in_area.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_in_area.PDENSOURCE)
                        {
                            MyModules.pden_in_area.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENPRODSTRTOPDENCROSSREF":
                    {
                        if (MyModules.pden_prod_string_xref is null)
                        {
                            MyModules.pden_prod_string_xref = new SimpleODM.reservepoolproductionmodule.uc_pden_prod_string_xref( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_prod_string_xref.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_prod_string_xref.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_prod_string_xref.PDENSOURCE)
                        {
                            MyModules.pden_prod_string_xref.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENALLOWABLE":
                    {
                        if (MyModules.pden_pr_str_allowable is null)
                        {
                            MyModules.pden_pr_str_allowable = new SimpleODM.reservepoolproductionmodule.uc_pden_pr_str_allowable( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_pr_str_allowable.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_pr_str_allowable.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_pr_str_allowable.PDENSOURCE | SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.pden_pr_str_allowable.UWI | SimpleODMConfig.Defaults.PROD_STRING_SOURCE != MyModules.pden_pr_str_allowable.STRINGSOURCE | SimpleODMConfig.Defaults.PDEN_PRS_XREF_SEQ_NO != MyModules.pden_pr_str_allowable.PDEN_PRS_XREF_SEQ_NO)
                        {
                            MyModules.pden_pr_str_allowable.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PROD_STRING_SOURCE, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PDEN_PRS_XREF_SEQ_NO);
                        }

                        break;
                    }

                case "PDENMATERIALBAL":
                    {
                        if (MyModules.pden_material_bal is null)
                        {
                            MyModules.pden_material_bal = new SimpleODM.reservepoolproductionmodule.uc_pden_material_bal( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_material_bal.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_material_bal.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_material_bal.PDENSOURCE)
                        {
                            MyModules.pden_material_bal.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENOPERHIST":
                    {
                        if (MyModules.pden_oper_hist is null)
                        {
                            MyModules.pden_oper_hist = new SimpleODM.reservepoolproductionmodule.uc_pden_oper_hist( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_oper_hist.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_oper_hist.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_oper_hist.PDENSOURCE)
                        {
                            MyModules.pden_oper_hist.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENSTATUSHIST":
                    {
                        if (MyModules.pden_status_hist is null)
                        {
                            MyModules.pden_status_hist = new SimpleODM.reservepoolproductionmodule.uc_pden_status_hist( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_status_hist.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_status_hist.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_status_hist.PDENSOURCE)
                        {
                            MyModules.pden_status_hist.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENVOLDISP":
                    {
                        if (MyModules.pden_vol_disposition is null)
                        {
                            MyModules.pden_vol_disposition = new SimpleODM.reservepoolproductionmodule.uc_pden_vol_disposition( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_vol_disposition.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_vol_disposition.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_vol_disposition.PDENSOURCE)
                        {
                            MyModules.pden_vol_disposition.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENVOLREGIME":
                    {
                        if (MyModules.pden_vol_regime is null)
                        {
                            MyModules.pden_vol_regime = new SimpleODM.reservepoolproductionmodule.uc_pden_vol_regime( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_vol_regime.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_vol_regime.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_vol_regime.PDENSOURCE)
                        {
                            MyModules.pden_vol_regime.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENVOLSUMMARY":
                    {
                        if (MyModules.pden_summary is null)
                        {
                            MyModules.pden_summary = new SimpleODM.reservepoolproductionmodule.uc_pden_summary( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_summary.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_summary.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_summary.PDENSOURCE)
                        {
                            MyModules.pden_summary.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENVOLSUMMARYOTHER":
                    {
                        if (MyModules.pden_vol_summ_other is null)
                        {
                            MyModules.pden_vol_summ_other = new SimpleODM.reservepoolproductionmodule.uc_pden_vol_summ_other( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_vol_summ_other.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_vol_summ_other.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_vol_summ_other.PDENSOURCE)
                        {
                            MyModules.pden_vol_summ_other.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENVOLANALYSIS":
                    {
                        if (MyModules.pden_volume_analysis is null)
                        {
                            MyModules.pden_volume_analysis = new SimpleODM.reservepoolproductionmodule.uc_pden_volume_analysis( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_volume_analysis.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_volume_analysis.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_volume_analysis.PDENSOURCE)
                        {
                            MyModules.pden_volume_analysis.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }

                case "PDENCROSSREF":
                    {
                        if (MyModules.pden_xref is null)
                        {
                            MyModules.pden_xref = new SimpleODM.reservepoolproductionmodule.uc_pden_xref( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.PDEN_ID != MyModules.pden_xref.PDENID | SimpleODMConfig.Defaults.PDEN_TYPE != MyModules.pden_xref.PDENTYPE | SimpleODMConfig.Defaults.PDEN_SOURCE != MyModules.pden_xref.PDENSOURCE)
                        {
                            MyModules.pden_xref.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE);
                        }

                        break;
                    }
                // ------------------------------------ POOL ---------------------------
                case "POOLALIAS":
                    {
                        if (MyModules.pool_alias is null)
                        {
                            MyModules.pool_alias = new SimpleODM.reservepoolproductionmodule.uc_pool_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.POOLID != MyModules.pool_alias.PoolID)
                        {
                            MyModules.pool_alias.Loaddata(SimpleODMConfig.Defaults.POOLID);
                        }

                        break;
                    }

                case "POOLAREA":
                    {
                        if (MyModules.pool_area is null)
                        {
                            MyModules.pool_area = new SimpleODM.reservepoolproductionmodule.uc_pool_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.POOLID != MyModules.pool_area.PoolID)
                        {
                            MyModules.pool_area.Loaddata(SimpleODMConfig.Defaults.POOLID);
                        }

                        break;
                    }

                case "POOLINSTRUMENT":
                    {
                        if (MyModules.pool_instrument is null)
                        {
                            MyModules.pool_instrument = new SimpleODM.reservepoolproductionmodule.uc_pool_instrument( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.POOLID != MyModules.pool_instrument.PoolID)
                        {
                            MyModules.pool_instrument.Loaddata(SimpleODMConfig.Defaults.POOLID);
                        }

                        break;
                    }

                case "POOLVERSION":
                    {
                        if (MyModules.pool_version is null)
                        {
                            MyModules.pool_version = new SimpleODM.reservepoolproductionmodule.uc_pool_version( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.POOLID != MyModules.pool_version.PoolID)
                        {
                            MyModules.pool_version.Loaddata(SimpleODMConfig.Defaults.POOLID);
                        }

                        break;
                    }
                // ------------------------------------- Area --------------------------
                case "AREAALIAS":
                    {
                        if (MyModules.area_alias is null)
                        {
                            MyModules.area_alias = new SimpleODM.supportmodule.uc_area_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.AREA != MyModules.area_alias.Areaid | SimpleODMConfig.Defaults.AREA_TYPE != MyModules.area_alias.AreaType)
                        {
                            MyModules.area_alias.Loaddata(SimpleODMConfig.Defaults.AREA, SimpleODMConfig.Defaults.AREA_TYPE);
                        }

                        break;
                    }

                case "AREACONTAIN":
                    {
                        if (MyModules.area_contain is null)
                        {
                            MyModules.area_contain = new SimpleODM.supportmodule.uc_area_contain( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.AREA != MyModules.area_contain.Areaid | SimpleODMConfig.Defaults.AREA_TYPE != MyModules.area_contain.AreaType)
                        {
                            MyModules.area_contain.Loaddata(SimpleODMConfig.Defaults.AREA, SimpleODMConfig.Defaults.AREA_TYPE);
                        }

                        break;
                    }

                case "AREADESCRIPTION":
                    {
                        if (MyModules.area_description is null)
                        {
                            MyModules.area_description = new SimpleODM.supportmodule.uc_area_description( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.AREA != MyModules.area_description.Areaid | SimpleODMConfig.Defaults.AREA_TYPE != MyModules.area_description.AreaType)
                        {
                            MyModules.area_description.Loaddata(SimpleODMConfig.Defaults.AREA, SimpleODMConfig.Defaults.AREA_TYPE);
                        }

                        break;
                    }

                // ------------------------------------- Applications -------------------
                case "APPLICATIONALIAS":
                    {
                        if (MyModules.application_alias is null)
                        {
                            MyModules.application_alias = new SimpleODM.supportmodule.uc_application_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.APPLICATION_ID != MyModules.application_alias.AppiD)
                        {
                            MyModules.application_alias.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID);
                        }

                        break;
                    }

                case "APPLICATIONAREA":
                    {
                        if (MyModules.application_area is null)
                        {
                            MyModules.application_area = new SimpleODM.supportmodule.uc_application_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.APPLICATION_ID != MyModules.application_area.AppiD)
                        {
                            MyModules.application_area.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID);
                        }

                        break;
                    }

                case "APPLICATIONATTACH":
                    {
                        if (MyModules.application_attach is null)
                        {
                            MyModules.application_attach = new SimpleODM.supportmodule.uc_application_attach( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.APPLICATION_ID != MyModules.application_attach.AppiD)
                        {
                            MyModules.application_attach.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID);
                        }

                        break;
                    }

                case "APPLICATIONBA":
                    {
                        if (MyModules.application_ba is null)
                        {
                            MyModules.application_ba = new SimpleODM.supportmodule.uc_application_ba( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.APPLICATION_ID != MyModules.application_ba.AppiD)
                        {
                            MyModules.application_ba.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID);
                        }

                        break;
                    }

                case "APPLICATIONDESC":
                    {
                        if (MyModules.application_desc is null)
                        {
                            MyModules.application_desc = new SimpleODM.supportmodule.uc_application_desc( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.APPLICATION_ID != MyModules.application_desc.AppiD)
                        {
                            MyModules.application_desc.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID);
                        }

                        break;
                    }

                case "APPLICATIONREMARK":
                    {
                        if (MyModules.application_remark is null)
                        {
                            MyModules.application_remark = new SimpleODM.supportmodule.uc_application_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.APPLICATION_ID != MyModules.application_remark.AppiD)
                        {
                            MyModules.application_remark.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID);
                        }

                        break;
                    }
                // ------------------------------------- Catalog ------------------------
                case "CAT_ADDITIVEALIAS":
                    {
                        if (MyModules.cat_additive_alias is null)
                        {
                            MyModules.cat_additive_alias = new SimpleODM.supportmodule.uc_cat_additive_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID != MyModules.cat_additive_alias.CatAdditiveID)
                        {
                            MyModules.cat_additive_alias.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID);
                        }

                        break;
                    }

                case "CAT_ADDITIVESPEC":
                    {
                        if (MyModules.cat_additive_spec is null)
                        {
                            MyModules.cat_additive_spec = new SimpleODM.supportmodule.uc_cat_additive_spec( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID != MyModules.cat_additive_spec.CatAdditiveID)
                        {
                            MyModules.cat_additive_spec.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID);
                        }

                        break;
                    }

                case "CAT_ADDITIVETYPE":
                    {
                        if (MyModules.cat_additive_type is null)
                        {
                            MyModules.cat_additive_type = new SimpleODM.supportmodule.uc_cat_additive_type( ref _SimpleODMConfig, SimpleUtil);
                        }

                        MyModules.cat_additive_type.Loaddata();
                        break;
                    }

                case "CAT_ADDITIVEXREF":
                    {
                        if (MyModules.cat_additive_xref is null)
                        {
                            MyModules.cat_additive_xref = new SimpleODM.supportmodule.uc_cat_additive_xref( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID != MyModules.cat_additive_xref.CatAdditiveID)
                        {
                            MyModules.cat_additive_xref.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID);
                        }

                        break;
                    }

                case "CAT_EQUIPALIAS":
                    {
                        if (MyModules.Cat_equipment_alias is null)
                        {
                            MyModules.Cat_equipment_alias = new SimpleODM.supportmodule.uc_Cat_equipment_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.CATALOGUE_EQUIP_ID != MyModules.Cat_equipment_alias.CatEquipID)
                        {
                            MyModules.Cat_equipment_alias.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_EQUIP_ID);
                        }

                        break;
                    }

                case "CAT_EQUIPSPEC":
                    {
                        if (MyModules.Cat_equipment_spec is null)
                        {
                            MyModules.Cat_equipment_spec = new SimpleODM.supportmodule.uc_Cat_equipment_spec( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.CATALOGUE_EQUIP_ID != MyModules.Cat_equipment_spec.CatEquipID)
                        {
                            MyModules.Cat_equipment_spec.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_EQUIP_ID);
                        }

                        break;
                    }
                // ------------------------------------- Facility ------------------------

                case "FACILITYALIAS":
                    {
                        if (MyModules.facility_alias is null)
                        {
                            MyModules.facility_alias = new SimpleODM.facilitymodule.uc_facility_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_alias.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_alias.FacilityType)
                        {
                            MyModules.facility_alias.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYAREA":
                    {
                        if (MyModules.facility_area is null)
                        {
                            MyModules.facility_area = new SimpleODM.facilitymodule.uc_facility_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_area.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_area.FacilityType)
                        {
                            MyModules.facility_area.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYBASERVICE":
                    {
                        if (MyModules.facility_ba_service is null)
                        {
                            MyModules.facility_ba_service = new SimpleODM.facilitymodule.uc_facility_ba_service( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_ba_service.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_ba_service.FacilityType)
                        {
                            MyModules.facility_ba_service.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYCLASS":
                    {
                        if (MyModules.facility_class is null)
                        {
                            MyModules.facility_class = new SimpleODM.facilitymodule.uc_facility_class( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_class.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_class.FacilityType)
                        {
                            MyModules.facility_class.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYDESCRIPTION":
                    {
                        if (MyModules.facility_description is null)
                        {
                            MyModules.facility_description = new SimpleODM.facilitymodule.uc_facility_description( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_description.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_description.FacilityType)
                        {
                            MyModules.facility_description.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYEQUIPMENT":
                    {
                        if (MyModules.facility_equipment is null)
                        {
                            MyModules.facility_equipment = new SimpleODM.facilitymodule.uc_facility_equipment( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_equipment.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_equipment.FacilityType)
                        {
                            MyModules.facility_equipment.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYFIELD":
                    {
                        if (MyModules.facility_field is null)
                        {
                            MyModules.facility_field = new SimpleODM.facilitymodule.uc_facility_field( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_field.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_field.FacilityType)
                        {
                            MyModules.facility_field.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYLICENSE":
                    {
                        if (MyModules.facility_license is null)
                        {
                            MyModules.facility_license = new SimpleODM.facilitymodule.uc_facility_license( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_license.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_license.FacilityType)
                        {
                            MyModules.facility_license.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYLICENSEALIAS":
                    {
                        if (MyModules.facility_license_alias is null)
                        {
                            MyModules.facility_license_alias = new SimpleODM.facilitymodule.uc_facility_license_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_license.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_license_alias.FacilityType | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.facility_license_alias.LicenseID)
                        {
                            MyModules.facility_license_alias.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "FACILITYLICENSEAREA":
                    {
                        if (MyModules.facility_license_area is null)
                        {
                            MyModules.facility_license_area = new SimpleODM.facilitymodule.uc_facility_license_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_license.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_license_area.FacilityType | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.facility_license_area.LicenseID)
                        {
                            MyModules.facility_license_area.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "FACILITYLICENSECOND":
                    {
                        if (MyModules.facility_license_cond is null)
                        {
                            MyModules.facility_license_cond = new SimpleODM.facilitymodule.uc_facility_license_cond( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_license.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_license_cond.FacilityType | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.facility_license_cond.LicenseID)
                        {
                            MyModules.facility_license_cond.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "FACILITYLICENSEREMARK":
                    {
                        if (MyModules.facility_license_remark is null)
                        {
                            MyModules.facility_license_remark = new SimpleODM.facilitymodule.uc_facility_license_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_license.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_license_remark.FacilityType | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.facility_license_remark.LicenseID)
                        {
                            MyModules.facility_license_remark.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "FACILITYLICENSESTATUS":
                    {
                        if (MyModules.facility_license_status is null)
                        {
                            MyModules.facility_license_status = new SimpleODM.facilitymodule.uc_facility_license_status( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_license.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_license_status.FacilityType | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.facility_license_status.LicenseID)
                        {
                            MyModules.facility_license_status.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "FACILITYLICENSETYPE":
                    {
                        if (MyModules.facility_license_type is null)
                        {
                            MyModules.facility_license_type = new SimpleODM.facilitymodule.uc_facility_license_type( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // If (SimpleODMconfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMconfig.Defaults.FACILITY_TYPE <> MyModules.facility_license.FacilityType) Then
                        MyModules.facility_license_type.Loaddata();
                        break;
                    }
                // End If
                case "FACILITYLICENSEVIOLATION":
                    {
                        if (MyModules.facility_license_violation is null)
                        {
                            MyModules.facility_license_violation = new SimpleODM.facilitymodule.uc_facility_license_violation( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_license.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_license_violation.FacilityType | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.facility_license_violation.LicenseID)
                        {
                            MyModules.facility_license_violation.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "FACILITYMAINTAIN":
                    {
                        if (MyModules.facility_maintain is null)
                        {
                            MyModules.facility_maintain = new SimpleODM.facilitymodule.uc_facility_maintain( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_maintain.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_maintain.FacilityType)
                        {
                            MyModules.facility_maintain.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYMAINTAINSTATUS":
                    {
                        if (MyModules.facility_maintain_status is null)
                        {
                            MyModules.facility_maintain_status = new SimpleODM.facilitymodule.uc_facility_maintain_status( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_maintain_status.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_maintain_status.FacilityType | SimpleODMConfig.Defaults.MAINTAIN_ID != MyModules.facility_maintain_status.MAINTAINID)
                        {
                            MyModules.facility_maintain_status.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.MAINTAIN_ID);
                        }

                        break;
                    }

                case "FACILITYRATE":
                    {
                        if (MyModules.facility_rate is null)
                        {
                            MyModules.facility_rate = new SimpleODM.facilitymodule.uc_facility_rate( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_rate.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_rate.FacilityType)
                        {
                            MyModules.facility_rate.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYRESTRICTION":
                    {
                        if (MyModules.facility_restriction is null)
                        {
                            MyModules.facility_restriction = new SimpleODM.facilitymodule.uc_facility_restriction( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_restriction.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_restriction.FacilityType)
                        {
                            MyModules.facility_restriction.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYSTATUS":
                    {
                        if (MyModules.facility_status is null)
                        {
                            MyModules.facility_status = new SimpleODM.facilitymodule.uc_facility_status( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_status.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_status.FacilityType)
                        {
                            MyModules.facility_status.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYSUBSTANCE":
                    {
                        if (MyModules.facility_substance is null)
                        {
                            MyModules.facility_substance = new SimpleODM.facilitymodule.uc_facility_substance( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_substance.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_substance.FacilityType)
                        {
                            MyModules.facility_substance.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYVERSION":
                    {
                        if (MyModules.facility_version is null)
                        {
                            MyModules.facility_version = new SimpleODM.facilitymodule.uc_facility_version( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_version.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_version.FacilityType)
                        {
                            MyModules.facility_version.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }

                case "FACILITYXREF":
                    {
                        if (MyModules.facility_xref is null)
                        {
                            MyModules.facility_xref = new SimpleODM.facilitymodule.uc_facility_xref( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FACILITY_ID != MyModules.facility_xref.FacilityID | SimpleODMConfig.Defaults.FACILITY_TYPE != MyModules.facility_xref.FacilityType)
                        {
                            MyModules.facility_xref.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE);
                        }

                        break;
                    }
                // --------------------------------------Equipment ------------------------


                case "EQUIPMENTALIAS":
                    {
                        if (MyModules.equipment_alias is null)
                        {
                            MyModules.equipment_alias = new SimpleODM.supportmodule.uc_equipment_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.EQUIPMENTID != MyModules.equipment_alias.EquipmentID)
                        {
                            MyModules.equipment_alias.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID);
                        }

                        break;
                    }

                case "EQUIPMENTBA":
                    {
                        if (MyModules.equipment_ba is null)
                        {
                            MyModules.equipment_ba = new SimpleODM.supportmodule.uc_equipment_ba( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.EQUIPMENTID != MyModules.equipment_ba.EquipmentID)
                        {
                            MyModules.equipment_ba.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID);
                        }

                        break;
                    }

                case "EQUIPMENTMAINTAIN":
                    {
                        if (MyModules.equipment_maintain is null)
                        {
                            MyModules.equipment_maintain = new SimpleODM.supportmodule.uc_equipment_maintain( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.EQUIPMENTID != MyModules.equipment_maintain.EquipmentID)
                        {
                            MyModules.equipment_maintain.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID);
                        }

                        break;
                    }

                case "EQUIPMENTMAINTAINSTATUS":
                    {
                        if (MyModules.equipment_maintain_status is null)
                        {
                            MyModules.equipment_maintain_status = new SimpleODM.supportmodule.uc_equipment_maintain_status( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.EQUIPMENTID != MyModules.equipment_maintain_status.EquipmentID | SimpleODMConfig.Defaults.EQUIP_MAIN_ID != MyModules.equipment_maintain_status.EQUIPMAINID)
                        {
                            MyModules.equipment_maintain_status.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID, SimpleODMConfig.Defaults.EQUIP_MAIN_ID);
                        }

                        break;
                    }

                case "EQUIPMENTMAINTAINTYPE":
                    {
                        if (MyModules.equipment_maintain_type is null)
                        {
                            MyModules.equipment_maintain_type = new SimpleODM.supportmodule.uc_equipment_maintain_type( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // If (SimpleODMconfig.Defaults.EQUIPMENTID <> MyModules.equipment_maintain.EquipmentID) Then
                        MyModules.equipment_maintain_type.Loaddata();
                        break;
                    }
                // End If
                case "EQUIPMENTSPEC":
                    {
                        if (MyModules.equipment_spec is null)
                        {
                            MyModules.equipment_spec = new SimpleODM.supportmodule.uc_equipment_spec( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.EQUIPMENTID != MyModules.equipment_spec.EquipmentID)
                        {
                            MyModules.equipment_spec.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID);
                        }

                        break;
                    }

                case "EQUIPMENTSPECSET":
                    {
                        if (MyModules.equipment_spec_set is null)
                        {
                            MyModules.equipment_spec_set = new SimpleODM.supportmodule.uc_equipment_spec_set( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // If (SimpleODMconfig.Defaults.EQUIPMENTID <> MyModules.equipment_spec_set.EquipmentID) Then
                        MyModules.equipment_spec_set.Loaddata();
                        break;
                    }
                // End If
                case "EQUIPMENTSPECSETSPEC":
                    {
                        if (MyModules.equipment_spec_set_spec is null)
                        {
                            MyModules.equipment_spec_set_spec = new SimpleODM.supportmodule.uc_equipment_spec_set_spec( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.SPEC_SET_ID != MyModules.equipment_spec_set_spec.SPECSETID)
                        {
                            MyModules.equipment_spec_set_spec.Loaddata(SimpleODMConfig.Defaults.SPEC_SET_ID);
                        }

                        break;
                    }

                case "EQUIPMENTSTATUS":
                    {
                        if (MyModules.equipment_status is null)
                        {
                            MyModules.equipment_status = new SimpleODM.supportmodule.uc_equipment_status( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.EQUIPMENTID != MyModules.equipment_status.EquipmentID)
                        {
                            MyModules.equipment_status.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID);
                        }

                        break;
                    }

                case "EQUIPMENTUSAGE":
                    {
                        if (MyModules.equipment_use_stat is null)
                        {
                            MyModules.equipment_use_stat = new SimpleODM.supportmodule.uc_equipment_use_stat( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.EQUIPMENTID != MyModules.equipment_use_stat.EquipmentID)
                        {
                            MyModules.equipment_use_stat.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID);
                        }

                        break;
                    }

                case "EQUIPMENTCROSSREFERNCE":
                    {
                        if (MyModules.equipment_crossreference is null)
                        {
                            MyModules.equipment_crossreference = new SimpleODM.supportmodule.uc_equipment_crossreference( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.EQUIPMENTID != MyModules.equipment_crossreference.EquipmentID)
                        {
                            MyModules.equipment_crossreference.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID);
                        }

                        break;
                    }

                // -------------------------------------- BA ------------------------------
                case "ENTITLEMENTSCOMPONENTS":
                    {
                        if (MyModules.ent_components is null)
                        {
                            MyModules.ent_components = new SimpleODM.BusinessAssociateModule.uc_ent_components( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.ENTITLEMENT_ID != MyModules.ent_components.EntID)
                        {
                            MyModules.ent_components.Loaddata(SimpleODMConfig.Defaults.ENTITLEMENT_ID);
                        }

                        break;
                    }

                case "ENTITLEMENTSFORGROUP":
                    {
                        if (MyModules.entitlement_group is null)
                        {
                            MyModules.entitlement_group = new SimpleODM.BusinessAssociateModule.uc_entitlement_group( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.SECURITY_GROUP_ID != MyModules.entitlement_group.SECURITY_GROUPID)
                        {
                            MyModules.entitlement_group.Loaddata(SimpleODMConfig.Defaults.SECURITY_GROUP_ID);
                        }

                        break;
                    }

                case "ENTITLEMENTGROUPS":
                    {
                        if (MyModules.entitlement_security_group is null)
                        {
                            MyModules.entitlement_security_group = new SimpleODM.BusinessAssociateModule.uc_entitlement_security_group( ref _SimpleODMConfig, SimpleUtil);
                        }

                        MyModules.entitlement_security_group.Loaddata();
                        break;
                    }

                case "ENTITLEMENTS":
                    {
                        if (MyModules.entitlement is null)
                        {
                            MyModules.entitlement = new SimpleODM.BusinessAssociateModule.uc_entitlement( ref _SimpleODMConfig, SimpleUtil);
                        }

                        MyModules.entitlement.Loaddata();
                        break;
                    }

                case "BUSINESSASSOCIATEENTITLEMENT":
                    {
                        if (MyModules.entitlement_security_ba is null)
                        {
                            MyModules.entitlement_security_ba = new SimpleODM.BusinessAssociateModule.uc_entitlement_security_ba( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.entitlement_security_ba.BA)
                        {
                            MyModules.entitlement_security_ba.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATEADDRESS":
                    {
                        if (MyModules.businessAssociate_address is null)
                        {
                            MyModules.businessAssociate_address = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_address( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_address.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_address.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATEALIAS":
                    {
                        if (MyModules.businessAssociate_alias is null)
                        {
                            MyModules.businessAssociate_alias = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_alias.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_alias.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATEAUTHORITY":
                    {
                        if (MyModules.businessAssociate_authority is null)
                        {
                            MyModules.businessAssociate_authority = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_authority( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_authority.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_authority.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATECONSURTUIMSERVICE":
                    {
                        if (MyModules.businessAssociate_consortuimservice is null)
                        {
                            MyModules.businessAssociate_consortuimservice = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_consortuimservice( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_consortuimservice.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_consortuimservice.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATECONTACTINFO":
                    {
                        if (MyModules.businessAssociate_contactinfo is null)
                        {
                            MyModules.businessAssociate_contactinfo = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_contactinfo( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_contactinfo.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_contactinfo.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATECREW":
                    {
                        if (MyModules.businessAssociate_crew is null)
                        {
                            MyModules.businessAssociate_crew = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_crew( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_crew.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_crew.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATECREWMEMEBERS":
                    {
                        if (MyModules.businessAssociate_crew_memeber is null)
                        {
                            MyModules.businessAssociate_crew_memeber = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_crew_memeber( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_crew_memeber.MyBusinessAssociateID | SimpleODMConfig.Defaults.CREW_ID != MyModules.businessAssociate_crew_memeber.MyCrewID)
                        {
                            MyModules.businessAssociate_crew_memeber.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.CREW_ID);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATEEMPLOYEE":
                    {
                        if (MyModules.businessAssociate_employee is null)
                        {
                            MyModules.businessAssociate_employee = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_employee( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_employee.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_employee.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSE":
                    {
                        if (MyModules.businessAssociate_license is null)
                        {
                            MyModules.businessAssociate_license = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_license.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSEALIAS":
                    {
                        if (MyModules.businessAssociate_license_alias is null)
                        {
                            MyModules.businessAssociate_license_alias = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_alias.MyBusinessAssociateID | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.businessAssociate_license_alias.MyLicenseID)
                        {
                            MyModules.businessAssociate_license_alias.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSEAREA":
                    {
                        if (MyModules.businessAssociate_license_area is null)
                        {
                            MyModules.businessAssociate_license_area = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_area.MyBusinessAssociateID | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.businessAssociate_license_area.MyLicenseID)
                        {
                            MyModules.businessAssociate_license_area.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSECONDITION":
                    {
                        if (MyModules.businessAssociate_license_cond is null)
                        {
                            MyModules.businessAssociate_license_cond = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_cond.MyBusinessAssociateID | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.businessAssociate_license_cond.MyLicenseID)
                        {
                            MyModules.businessAssociate_license_cond.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSETYPECONDVIOLATION":
                    {
                        if (MyModules.businessAssociate_license_violation is null)
                        {
                            MyModules.businessAssociate_license_violation = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_violation( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_violation.MyBusinessAssociateID | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.businessAssociate_license_violation.MyLicenseID | SimpleODMConfig.Defaults.CONDITION_ID != MyModules.businessAssociate_license_violation.MyConditionID)
                        {
                            MyModules.businessAssociate_license_violation.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID, SimpleODMConfig.Defaults.CONDITION_ID);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSEREMARK":
                    {
                        if (MyModules.businessAssociate_license_remark is null)
                        {
                            MyModules.businessAssociate_license_remark = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_remark.MyBusinessAssociateID | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.businessAssociate_license_remark.MyLicenseID)
                        {
                            MyModules.businessAssociate_license_remark.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSESTATUS":
                    {
                        if (MyModules.businessAssociate_license_status is null)
                        {
                            MyModules.businessAssociate_license_status = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_status( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_status.MyBusinessAssociateID | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.businessAssociate_license_status.MyLicenseID)
                        {
                            MyModules.businessAssociate_license_status.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSETYPE":
                    {
                        if (MyModules.businessAssociate_license_type is null)
                        {
                            MyModules.businessAssociate_license_type = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_type( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_type.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_license_type.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSETYPECONDTYPE":
                    {
                        if (MyModules.businessAssociate_license_cond_type is null)
                        {
                            MyModules.businessAssociate_license_cond_type = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond_type( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_cond_type.MyBusinessAssociateID | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.businessAssociate_license_status.MyLicenseID)
                        {
                            MyModules.businessAssociate_license_cond_type.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATELICENSETYPECONDTYPECODE":
                    {
                        if (MyModules.businessAssociate_license_cond_code is null)
                        {
                            MyModules.businessAssociate_license_cond_code = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond_code( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_license_cond_code.MyBusinessAssociateID | SimpleODMConfig.Defaults.LICENSE_TYPE != MyModules.businessAssociate_license_cond_code.MyLicenseTYPEID | SimpleODMConfig.Defaults.CONDITION_TYPE != MyModules.businessAssociate_license_cond_code.MyLicenseCONDTYPEID)
                        {
                            MyModules.businessAssociate_license_cond_code.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_TYPE, SimpleODMConfig.Defaults.CONDITION_TYPE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATEORGANIZATION":
                    {
                        if (MyModules.businessAssociate_organization is null)
                        {
                            MyModules.businessAssociate_organization = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_organization( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_organization.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_organization.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATEPERMIT":
                    {
                        if (MyModules.businessAssociate_permit is null)
                        {
                            MyModules.businessAssociate_permit = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_permit( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_permit.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_permit.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATESERVICE":
                    {
                        if (MyModules.businessAssociate_services is null)
                        {
                            MyModules.businessAssociate_services = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_services( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_services.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_services.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATESERVICEADDRESS":
                    {
                        if (MyModules.businessAssociate_services_address is null)
                        {
                            MyModules.businessAssociate_services_address = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_services_address( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_services_address.MyBusinessAssociateID | SimpleODMConfig.Defaults.SERVICE_TYPE != MyModules.businessAssociate_services_address.MyServiceType | SimpleODMConfig.Defaults.SERVICE_SEQ_NO != MyModules.businessAssociate_services_address.MyServiceSeqNo)
                        {
                            MyModules.businessAssociate_services_address.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.SERVICE_TYPE, SimpleODMConfig.Defaults.SERVICE_SEQ_NO);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATEPREFERENCE":
                    {
                        if (MyModules.businessAssociate_preference is null)
                        {
                            MyModules.businessAssociate_preference = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_preference( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_preference.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_preference.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATECROSSREFERENCE":
                    {
                        if (MyModules.businessAssociate_crosspreference is null)
                        {
                            MyModules.businessAssociate_crosspreference = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_crosspreference( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_crosspreference.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_crosspreference.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }

                case "BUSINESSASSOCIATEDESCRIPTION":
                    {
                        if (MyModules.businessAssociate_description is null)
                        {
                            MyModules.businessAssociate_description = new SimpleODM.BusinessAssociateModule.uc_businessAssociate_description( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE != MyModules.businessAssociate_description.MyBusinessAssociateID)
                        {
                            MyModules.businessAssociate_description.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE);
                        }

                        break;
                    }
                // -------------------------------------- End BA ------------------------------
                // -------------------------------------Strat------------------------------------------


                case "STRATFIELDINTREPAGE":
                    {
                        if (MyModules.strat_fld_interp_age is null)
                        {
                            MyModules.strat_fld_interp_age = new SimpleODM.stratigraphyandlithologyModule.uc_strat_fld_interp_age( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FIELD_STATION_ID != MyModules.strat_fld_interp_age.STRATFIELDid)
                        {
                            MyModules.strat_fld_interp_age.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID, SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID);
                        }

                        break;
                    }

                case "STRATFIELDNODEVERSION":
                    {
                        if (MyModules.strat_field_node_version is null)
                        {
                            MyModules.strat_field_node_version = new SimpleODM.stratigraphyandlithologyModule.uc_strat_node_version( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FIELD_STATION_ID != MyModules.strat_field_node_version.STRATFIELDid)
                        {
                            MyModules.strat_field_node_version.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID, SimpleODMConfig.Defaults.NODE_ID, SimpleODMConfig.Defaults.SOURCE);
                        }

                        break;
                    }

                case "STRATFIELDNODE":
                    {
                        if (MyModules.strat_field_node is null)
                        {
                            MyModules.strat_field_node = new SimpleODM.stratigraphyandlithologyModule.uc_strat_field_node( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FIELD_STATION_ID != MyModules.strat_field_node.STRATFIELDid)
                        {
                            MyModules.strat_field_node.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID);
                        }

                        break;
                    }

                case "STRATFIELDSECTION":
                    {
                        if (MyModules.strat_field_section is null)
                        {
                            MyModules.strat_field_section = new SimpleODM.stratigraphyandlithologyModule.uc_strat_field_section( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FIELD_STATION_ID != MyModules.strat_field_section.STRATFIELDid)
                        {
                            MyModules.strat_field_section.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID);
                        }

                        break;
                    }

                case "STRATFIELDFEOMETRY":
                    {
                        if (MyModules.strat_field_geometry is null)
                        {
                            MyModules.strat_field_geometry = new SimpleODM.stratigraphyandlithologyModule.uc_strat_field_geometry( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FIELD_STATION_ID != MyModules.strat_field_geometry.STRATFIELDid)
                        {
                            MyModules.strat_field_geometry.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID);
                        }

                        break;
                    }

                case "STRATFIELDACQUISITION":
                    {
                        if (MyModules.strat_field_acqtn is null)
                        {
                            MyModules.strat_field_acqtn = new SimpleODM.stratigraphyandlithologyModule.uc_strat_field_acqtn( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.FIELD_STATION_ID != MyModules.strat_field_geometry.STRATFIELDid)
                        {
                            MyModules.strat_field_acqtn.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID, SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID);
                        }

                        break;
                    }

                case "STRATNAMESETXREF":
                    {
                        if (MyModules.strat_xref is null)
                        {
                            MyModules.strat_xref = new SimpleODM.stratigraphyandlithologyModule.uc_strat_name_set_xref( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_xref.StratNameSetID)
                        {
                            MyModules.strat_xref.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET);
                        }

                        break;
                    }

                case "STRATUNIT":
                    {
                        if (MyModules.strat_unit is null)
                        {
                            MyModules.strat_unit = new SimpleODM.stratigraphyandlithologyModule.uc_strat_unit( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_unit.StratNameSetID)
                        {
                            MyModules.strat_unit.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET);
                        }

                        break;
                    }

                case "STRATUNITALIAS":
                    {
                        if (MyModules.strat_alias is null)
                        {
                            MyModules.strat_alias = new SimpleODM.stratigraphyandlithologyModule.uc_strat_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_alias.StratNameSetID | SimpleODMConfig.Defaults.STRAT_UNIT_ID != MyModules.strat_alias.StratUnitID)
                        {
                            MyModules.strat_alias.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID);
                        }

                        break;
                    }

                case "STRATUNITEQUIVALANCE":
                    {
                        if (MyModules.strat_equivalance is null)
                        {
                            MyModules.strat_equivalance = new SimpleODM.stratigraphyandlithologyModule.uc_strat_equivalance( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_equivalance.StratNameSetID | SimpleODMConfig.Defaults.STRAT_UNIT_ID != MyModules.strat_equivalance.StratUnitID)
                        {
                            MyModules.strat_equivalance.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID);
                        }

                        break;
                    }

                case "STRATUNITHIERARCHY":
                    {
                        if (MyModules.strat_hierarchy is null)
                        {
                            MyModules.strat_hierarchy = new SimpleODM.stratigraphyandlithologyModule.uc_strat_hierarchy( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_hierarchy.StratNameSetID | SimpleODMConfig.Defaults.STRAT_UNIT_ID != MyModules.strat_hierarchy.StratUnitID)
                        {
                            MyModules.strat_hierarchy.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID);
                        }

                        break;
                    }

                case "STRATHIERARCYDESCR":
                    {
                        if (MyModules.strat_hierarchy_desc is null)
                        {
                            MyModules.strat_hierarchy_desc = new SimpleODM.stratigraphyandlithologyModule.uc_strat_hierarchy_desc( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // If (SimpleODMconfig.Defaults.STRAT_NAME_SET <> MyModules.strat_hierarchy_desc.StratNameSetID) Or (SimpleODMconfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_hierarchy_desc.StratUnitID) Then
                        MyModules.strat_hierarchy_desc.Loaddata();
                        break;
                    }
                // End If
                case "STRATUNITAGE":
                    {
                        if (MyModules.strat_unit_age is null)
                        {
                            MyModules.strat_unit_age = new SimpleODM.stratigraphyandlithologyModule.uc_strat_unit_age( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_unit_age.StratNameSetID | SimpleODMConfig.Defaults.STRAT_UNIT_ID != MyModules.strat_unit_age.StratUnitID)
                        {
                            MyModules.strat_unit_age.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID);
                        }

                        break;
                    }

                case "STRATUNITDESCRIPTION":
                    {
                        if (MyModules.strat_unit_description is null)
                        {
                            MyModules.strat_unit_description = new SimpleODM.stratigraphyandlithologyModule.uc_strat_unit_description( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_unit_description.StratNameSetID | SimpleODMConfig.Defaults.STRAT_UNIT_ID != MyModules.strat_unit_description.StratUnitID)
                        {
                            MyModules.strat_unit_description.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID);
                        }

                        break;
                    }

                case "STRATUNITTOPOLOGY":
                    {
                        if (MyModules.strat_topo_relation is null)
                        {
                            MyModules.strat_topo_relation = new SimpleODM.stratigraphyandlithologyModule.uc_strat_topo_relation( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_topo_relation.StratNameSetID | SimpleODMConfig.Defaults.STRAT_UNIT_ID != MyModules.strat_topo_relation.StratUnitID)
                        {
                            MyModules.strat_topo_relation.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID);
                        }

                        break;
                    }

                case "STRATCOLUMNAGE":
                    {
                        if (MyModules.strat_col_unit_age is null)
                        {
                            MyModules.strat_col_unit_age = new SimpleODM.stratigraphyandlithologyModule.uc_strat_col_unit_age( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_col_unit_age.StratNameSetID | SimpleODMConfig.Defaults.STRAT_UNIT_ID != MyModules.strat_col_unit_age.StratUnitID | SimpleODMConfig.Defaults.STRAT_COLUMN_ID != MyModules.strat_col_unit_age.StratColumnID | SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE != MyModules.strat_col_unit_age.StratColumnSource | SimpleODMConfig.Defaults.INTERP_ID != MyModules.strat_col_unit_age.IntrepID)
                        {
                            MyModules.strat_col_unit_age.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_COLUMN_ID, SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID);
                        }

                        break;
                    }

                case "STRATCOLUMNACQTN":
                    {
                        if (MyModules.strat_col_acqtn is null)
                        {
                            MyModules.strat_col_acqtn = new SimpleODM.stratigraphyandlithologyModule.uc_strat_col_acqtn( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_NAME_SET != MyModules.strat_col_acqtn.StratNameSetID | SimpleODMConfig.Defaults.STRAT_UNIT_ID != MyModules.strat_col_acqtn.StratUnitID | SimpleODMConfig.Defaults.STRAT_COLUMN_ID != MyModules.strat_col_acqtn.StratColumnID | SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE != MyModules.strat_col_acqtn.StratColumnSource | SimpleODMConfig.Defaults.INTERP_ID != MyModules.strat_col_acqtn.IntrepID)
                        {
                            MyModules.strat_col_acqtn.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_COLUMN_ID, SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID);
                        }

                        break;
                    }

                case "STRATCOLUMNUNIT":
                    {
                        if (MyModules.strat_col_unit is null)
                        {
                            MyModules.strat_col_unit = new SimpleODM.stratigraphyandlithologyModule.uc_strat_col_unit( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_COLUMN_ID != MyModules.strat_col_unit.StratColumnID | SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE != MyModules.strat_col_unit.StratColumnSource)
                        {
                            MyModules.strat_col_unit.Loaddata(SimpleODMConfig.Defaults.STRAT_COLUMN_ID, SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE);
                        }

                        break;
                    }

                case "STRATCOLUMNCROSSREF":
                    {
                        if (MyModules.strat_col_xref is null)
                        {
                            MyModules.strat_col_xref = new SimpleODM.stratigraphyandlithologyModule.uc_strat_col_xref( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.STRAT_COLUMN_ID != MyModules.strat_col_xref.StratColumnID | SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE != MyModules.strat_col_xref.StratColumnSource)
                        {
                            MyModules.strat_col_xref.Loaddata(SimpleODMConfig.Defaults.STRAT_COLUMN_ID, SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE);
                        }

                        break;
                    }

                case "WELLFORMATION":
                    {
                        if (MyModules.strat_well_section is null)
                        {
                            MyModules.strat_well_section = new SimpleODM.stratigraphyandlithologyModule.uc_strat_well_section( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.strat_well_section.UWI)
                        {
                            MyModules.strat_well_section.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLSECTIONINTREPAGE":
                    {
                        if (MyModules.strat_well_interp_age is null)
                        {
                            MyModules.strat_well_interp_age = new SimpleODM.stratigraphyandlithologyModule.uc_well_strat_interp_age( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.strat_well_interp_age.UWI)
                        {
                            MyModules.strat_well_interp_age.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID);
                        }

                        break;
                    }

                case "WELLSECTIONACQUISTION":
                    {
                        if (MyModules.strat_well_acqtn is null)
                        {
                            MyModules.strat_well_acqtn = new SimpleODM.stratigraphyandlithologyModule.uc_strat_well_acqtn( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.strat_well_acqtn.UWI)
                        {
                            MyModules.strat_well_acqtn.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID);
                        }

                        break;
                    }

                case "WELLBORETUBULARCEMENT":
                    {
                        if (MyModules.wellboretubularcement is null)
                        {
                            MyModules.wellboretubularcement = new SimpleODM.wellmodule.uc_wellboretubularCement( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellboretubularcement.MyUWI)
                        {
                            MyModules.wellboretubularcement.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.TUBING_OBS_NO, SimpleODMConfig.Defaults.TUBING_TYPE, SimpleODMConfig.Defaults.TUBING_SOURCE);
                        }

                        break;
                    }

                case "WELLBORETUBULAR":
                    {
                        if (MyModules.wellboreTubular is null)
                        {
                            MyModules.wellboreTubular = new SimpleODM.wellmodule.uc_wellboretubular( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellboreTubular.MyUWI | SimpleODMConfig.Defaults.TUBING_OBS_NO != MyModules.wellboreTubular.TubingObsNo)
                        {
                            MyModules.wellboreTubular.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.TUBING_OBS_NO);
                        }

                        break;
                    }

                case "WELLBOREPAYZONE":
                    {
                        if (MyModules.wellborepayzone is null)
                        {
                            MyModules.wellborepayzone = new SimpleODM.wellmodule.uc_wellborepayzone( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellborepayzone.MyUWI | SimpleODMConfig.Defaults.ZONE_ID != MyModules.wellborepayzone.ZoneID)
                        {
                            MyModules.wellborepayzone.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ZONE_ID, SimpleODMConfig.Defaults.ZONE_SOURCE);
                        }

                        break;
                    }

                case "WELLBOREZONEINTERVAL":
                    {
                        if (MyModules.wellborezoneinterval is null)
                        {
                            MyModules.wellborezoneinterval = new SimpleODM.wellmodule.uc_wellborezoneinterval( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellborezoneinterval.MyUWI | SimpleODMConfig.Defaults.ZONE_ID != MyModules.wellborezoneinterval.ZoneID)
                        {
                            MyModules.wellborezoneinterval.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ZONE_ID, SimpleODMConfig.Defaults.INTERVAL_ID);
                        }

                        break;
                    }

                case "WELLBOREPOROUSINTERVAL":
                    {
                        if (MyModules.wellborePorousinterval is null)
                        {
                            MyModules.wellborePorousinterval = new SimpleODM.wellmodule.uc_wellborePorousinterval( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellborePorousinterval.MyUWI | SimpleODMConfig.Defaults.POROUS_INTERVAL_ID != MyModules.wellborePorousinterval.PorousIntervalID)
                        {
                            MyModules.wellborePorousinterval.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.POROUS_INTERVAL_ID);
                        }

                        break;
                    }

                case "WELLZONEINTERVALVALUE":
                    {
                        if (MyModules.wellborezoneintervalvalue is null)
                        {
                            MyModules.wellborezoneintervalvalue = new SimpleODM.wellmodule.uc_wellborezoneintervalvalue( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellborezoneintervalvalue.MyUWI | SimpleODMConfig.Defaults.ZONE_ID != MyModules.wellborezoneintervalvalue.ZoneID | SimpleODMConfig.Defaults.INTERVAL_ID != MyModules.wellborezoneintervalvalue.IntervalID)
                        {
                            MyModules.wellborezoneintervalvalue.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ZONE_ID, SimpleODMConfig.Defaults.INTERVAL_ID, SimpleODMConfig.Defaults.ZONE_SOURCE, SimpleODMConfig.Defaults.INTERVAL_SOURCE);
                        }

                        break;
                    }

                case "WELLBOREPLUGBACK":
                    {
                        if (MyModules.wellborePlugback is null)
                        {
                            MyModules.wellborePlugback = new SimpleODM.wellmodule.uc_wellborePlugback( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellborePlugback.MyUWI | SimpleODMConfig.Defaults.PLUGBACK_OBS_NO != MyModules.wellborePlugback.PlugbackgObsNo)
                        {
                            MyModules.wellborePlugback.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PLUGBACK_OBS_NO);
                        }

                        break;
                    }

                case "PRESSUREAOF4PT":
                    {
                        if (MyModules.well_Pressureaod4pt is null)
                        {
                            MyModules.well_Pressureaod4pt = new SimpleODM.welltestandpressuremodule.uc_well_pressure_aof_4pt( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_Pressureaod4pt.UWI | SimpleODMConfig.Defaults.PRESSURE_OBS_NO != MyModules.well_Pressureaod4pt.PRESSURE_OBS_NO | SimpleODMConfig.Defaults.AOF_OBS_NO != MyModules.well_Pressureaod4pt.AOF_OBS_NO)
                        {
                            MyModules.well_Pressureaod4pt.loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PRESSURE_OBS_NO, SimpleODMConfig.Defaults.AOF_OBS_NO, SimpleODMConfig.Defaults.PRESSURE_AOF_SOURCE);
                        }

                        break;
                    }

                case "PRESSUREAOF":
                    {
                        if (MyModules.well_Pressureaof is null)
                        {
                            MyModules.well_Pressureaof = new SimpleODM.welltestandpressuremodule.uc_well_pressure_aof( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_Pressureaof.myuwi | SimpleODMConfig.Defaults.PRESSURE_OBS_NO != MyModules.well_Pressureaof.PRESSURE_OBS_NO)
                        {
                            MyModules.well_Pressureaof.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PRESSURE_OBS_NO, SimpleODMConfig.Defaults.PRESSURE_SOURCE);
                        }

                        break;
                    }

                case "PRESSUREBH":
                    {
                        if (MyModules.well_PressureBH is null)
                        {
                            MyModules.well_PressureBH = new SimpleODM.welltestandpressuremodule.uc_well_pressure_bh( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_PressureBH.UWI | SimpleODMConfig.Defaults.PRESSURE_OBS_NO != MyModules.well_PressureBH.PRESSURE_OBS_NO)
                        {
                            MyModules.well_PressureBH.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PRESSURE_OBS_NO, SimpleODMConfig.Defaults.PRESSURE_SOURCE);
                        }

                        break;
                    }

                case "WELLTESTPRESSURE":
                    {
                        if (MyModules.well_Pressure is null)
                        {
                            MyModules.well_Pressure = new SimpleODM.welltestandpressuremodule.uc_well_pressure( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_Pressure.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_Pressure.RUN_NUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_Pressure.TEST_NUM)
                        {
                            MyModules.well_Pressure.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.SOURCE);
                        }

                        break;
                    }

                case "WELLLICVIOLATIONS":
                    {
                        if (MyModules.welllicense_violation is null)
                        {
                            MyModules.welllicense_violation = new SimpleODM.wellmodule.uc_well_license_violation( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.welllicense_violation.MyUWI | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.welllicense_violation.LicenceID)
                        {
                            MyModules.welllicense_violation.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID, SimpleODMConfig.Defaults.CONDITION_ID);
                        }

                        break;
                    }

                case "WELLLICSTATUS":
                    {
                        if (MyModules.welllicense_status is null)
                        {
                            MyModules.welllicense_status = new SimpleODM.wellmodule.uc_well_license_status( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.welllicense_status.MyUWI | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.welllicense_status.LicenceID)
                        {
                            MyModules.welllicense_status.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "WELLLICCONDTION":
                    {
                        if (MyModules.welllicense_cond is null)
                        {
                            MyModules.welllicense_cond = new SimpleODM.wellmodule.uc_well_license_cond( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.welllicense_cond.MyUWI | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.welllicense_cond.LicenceID)
                        {
                            MyModules.welllicense_cond.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "WELLLICAREA":
                    {
                        if (MyModules.welllicense_area is null)
                        {
                            MyModules.welllicense_area = new SimpleODM.wellmodule.uc_well_license_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.welllicense_area.MyUWI | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.welllicense_area.LicenceID)
                        {
                            MyModules.welllicense_area.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "WELLLICREMARK":
                    {
                        if (MyModules.welllicense_remark is null)
                        {
                            MyModules.welllicense_remark = new SimpleODM.wellmodule.uc_well_license_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.welllicense_remark.MyUWI | SimpleODMConfig.Defaults.LICENSE_ID != MyModules.welllicense_remark.LicenceID)
                        {
                            MyModules.welllicense_remark.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID);
                        }

                        break;
                    }

                case "WELLTESTREMARKS":
                    {
                        if (MyModules.well_test_Remarks is null)
                        {
                            MyModules.well_test_Remarks = new SimpleODM.welltestandpressuremodule.uc_welltestRemarks( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Remarks.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Remarks._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Remarks._TESTNUM)
                        {
                            MyModules.well_test_Remarks.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLTESTMUD":
                    {
                        if (MyModules.well_test_Mud is null)
                        {
                            MyModules.well_test_Mud = new SimpleODM.welltestandpressuremodule.uc_welltestMud( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Mud.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Mud._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Mud._TESTNUM)
                        {
                            MyModules.well_test_Mud.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLTESTEQUIP":
                    {
                        if (MyModules.well_test_Equipment is null)
                        {
                            MyModules.well_test_Equipment = new SimpleODM.welltestandpressuremodule.uc_welltestEquipment( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Equipment.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Equipment._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Equipment._TESTNUM)
                        {
                            MyModules.well_test_Equipment.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLTESTSHUTOFF":
                    {
                        if (MyModules.well_test_Shutoff is null)
                        {
                            MyModules.well_test_Shutoff = new SimpleODM.welltestandpressuremodule.uc_welltestShutoff( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Shutoff.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Shutoff._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Shutoff._TESTNUM)
                        {
                            MyModules.well_test_Shutoff.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLTESTSTRAT":
                    {
                        if (MyModules.well_test_StratUnit is null)
                        {
                            MyModules.well_test_StratUnit = new SimpleODM.welltestandpressuremodule.uc_welltestStratunit( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_StratUnit.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_StratUnit._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_StratUnit._TESTNUM)
                        {
                            MyModules.well_test_StratUnit.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLTESTRECORDER":
                    {
                        if (MyModules.well_test_Recorder is null)
                        {
                            MyModules.well_test_Recorder = new SimpleODM.welltestandpressuremodule.uc_welltestRecorder( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Recorder.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Recorder._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Recorder._TESTNUM)
                        {
                            MyModules.well_test_Recorder.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLTESTPRESS":
                    {
                        if (MyModules.well_test_Press is null)
                        {
                            MyModules.well_test_Press = new SimpleODM.welltestandpressuremodule.uc_welltestPress( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Press.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Press._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Press._TESTNUM | SimpleODMConfig.Defaults.PERIOD_OBS_NO != MyModules.well_test_Press.period_obs_no)
                        {
                            MyModules.well_test_Press.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.PERIOD_OBS_NO, SimpleODMConfig.Defaults.PERIOD_TYPE);
                        }

                        break;
                    }

                case "WELLTESTPRESSMEAS":
                    {
                        if (MyModules.well_test_PressMeas is null)
                        {
                            MyModules.well_test_PressMeas = new SimpleODM.welltestandpressuremodule.uc_welltestPressMeas( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_PressMeas.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_PressMeas._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_PressMeas._TESTNUM)
                        {
                            MyModules.well_test_PressMeas.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLTESTFLOW":
                    {
                        if (MyModules.well_test_Flow is null)
                        {
                            MyModules.well_test_Flow = new SimpleODM.welltestandpressuremodule.uc_welltestFlow( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Flow.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Flow._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Flow._TESTNUM | SimpleODMConfig.Defaults.PERIOD_OBS_NO != MyModules.well_test_Flow.period_obs_no)
                        {
                            MyModules.well_test_Flow.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.PERIOD_OBS_NO, SimpleODMConfig.Defaults.PERIOD_TYPE);
                        }

                        break;
                    }

                case "WELLTESTFLOWMEAS":
                    {
                        if (MyModules.well_test_FlowMeas is null)
                        {
                            MyModules.well_test_FlowMeas = new SimpleODM.welltestandpressuremodule.uc_welltestFlowMeas( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_FlowMeas.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_FlowMeas._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_FlowMeas._TESTNUM)
                        {
                            MyModules.well_test_FlowMeas.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLTESTRECOVERY":
                    {
                        if (MyModules.well_test_Recovery is null)
                        {
                            MyModules.well_test_Recovery = new SimpleODM.welltestandpressuremodule.uc_welltestRecovery( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Recovery.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Recovery._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Recovery._TESTNUM | SimpleODMConfig.Defaults.PERIOD_OBS_NO != MyModules.well_test_Recovery.period_obs_no)
                        {
                            MyModules.well_test_Recovery.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.PERIOD_OBS_NO, SimpleODMConfig.Defaults.PERIOD_TYPE);
                        }

                        break;
                    }

                case "WELLTESTCONTAMINANT":
                    {
                        if (MyModules.well_test_Contaminant is null)
                        {
                            MyModules.well_test_Contaminant = new SimpleODM.welltestandpressuremodule.uc_welltestContaminant( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Contaminant.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Contaminant._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Contaminant._TESTNUM | SimpleODMConfig.Defaults.RECOVERY_OBS_NO != MyModules.well_test_Contaminant.Recovery_Obs_no)
                        {
                            MyModules.well_test_Contaminant.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.RECOVERY_OBS_NO);
                        }

                        break;
                    }

                case "WELLTESTPERIOD":
                    {
                        if (MyModules.well_test_Period is null)
                        {
                            MyModules.well_test_Period = new SimpleODM.welltestandpressuremodule.uc_welltest_period( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_Period.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_Period._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_Period._TESTNUM)
                        {
                            MyModules.well_test_Period.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.TESTTYPE);
                        }

                        break;
                    }

                // WindowsUIView1.Controller.Activate(WellTestAnalysisPage)
                case "WELLTESTANALYSIS":
                    {
                        if (MyModules.well_test_analysis is null)
                        {
                            MyModules.well_test_analysis = new SimpleODM.welltestandpressuremodule.uc_welltestAnalysis( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_analysis.MyUWI | SimpleODMConfig.Defaults.RUNNUM != MyModules.well_test_analysis._RUNNUM | SimpleODMConfig.Defaults.TESTNUM != MyModules.well_test_analysis._TESTNUM | SimpleODMConfig.Defaults.PERIOD_OBS_NO != MyModules.well_test_analysis.period_obs_no)
                        {
                            MyModules.well_test_analysis.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.POOLNAME, SimpleODMConfig.Defaults.PERIOD_OBS_NO, SimpleODMConfig.Defaults.PERIOD_TYPE);
                        }

                        break;
                    }

                // WindowsUIView1.Controller.Activate(WellTestAnalysisPage)
                case "WELLTESTCUSHION":
                    {
                        if (MyModules.well_test_cushion is null)
                        {
                            MyModules.well_test_cushion = new SimpleODM.welltestandpressuremodule.uc_WellTestCushion( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test_cushion.MyUWI)
                        {
                            MyModules.well_test_cushion.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                            // WindowsUIView1.Controller.Activate(WellTestCushionPage)
                        }

                        break;
                    }

                case "WELLORIGIN":
                    {
                        if (MyModules.wellorigin is null)
                        {
                            MyModules.wellorigin = new SimpleODM.wellmodule.uc_wellorigin( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellorigin.WellSetUWI)
                        {
                            MyModules.wellorigin.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLBORE":
                    {
                        if (MyModules.wellbore is null)
                        {
                            MyModules.wellbore = new SimpleODM.wellmodule.uc_NewEditwellbore( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellbore.WellSetUWI | SimpleODMConfig.Defaults.WELLBOREID != MyModules.wellbore.WellboreUWI)
                        {
                            MyModules.wellbore.LoadWellBoreForWellSet(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELLBOREID);
                        }

                        break;
                    }

                // WindowsUIView1.Controller.Activate(WellBoreInformationManagementPage)
                case "PRODSTRING":
                    {
                        if (MyModules.Productionstring is null)
                        {
                            MyModules.Productionstring = new SimpleODM.wellmodule.uc_prodstring( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELLBOREID != MyModules.Productionstring.MyUWI | SimpleODMConfig.Defaults.STRING_ID != MyModules.Productionstring.STRING_ID)
                        {
                            MyModules.Productionstring.Loaddata(SimpleODMConfig.Defaults.WELLBOREID, TransType, SimpleODMConfig.Defaults.STRING_ID);
                        }

                        break;
                    }

                // WindowsUIView1.Controller.Activate(WellProdstringPage)
                case "FORMATION":
                    {
                        if (MyModules.prodstringformation is null)
                        {
                            MyModules.prodstringformation = new SimpleODM.wellmodule.uc_prodstringformation( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.prodstringformation.MyUWI | SimpleODMConfig.Defaults.STRING_ID != MyModules.prodstringformation.STRING_ID)
                        {
                            MyModules.prodstringformation.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.STRING_ID, TransType, SimpleODMConfig.Defaults.PROD_STRING_SOURCE, SimpleODMConfig.Defaults.PR_STR_FORM_OBS_NO);
                        }

                        break;
                    }

                case "COMPLETION":
                    {
                        if (MyModules.well_completion is null)
                        {
                            MyModules.well_completion = new SimpleODM.wellmodule.uc_completion( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_completion.MyUWI | SimpleODMConfig.Defaults.STRING_ID != MyModules.well_completion.string_ID | SimpleODMConfig.Defaults.PR_STR_FORM_OBS_NO != MyModules.well_completion.PR_STR_FORM_OBS_NO | SimpleODMConfig.Defaults.COMPLETION_OBS_NO != MyModules.well_completion.COMPLETION_OB_NO)
                        {
                            MyModules.well_completion.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PROD_STRING_SOURCE, SimpleODMConfig.Defaults.PR_STR_FORM_OBS_NO, TransType, SimpleODMConfig.Defaults.COMPLETION_OBS_NO, SimpleODMConfig.Defaults.COMPLETION_SOURCE);
                        }

                        MyModules.well_completion.SetLabel();
                        break;
                    }

                case "WELLCOMPXREF":
                    {
                        if (MyModules.well_completion_xref is null)
                        {
                            MyModules.well_completion_xref = new SimpleODM.wellmodule.uc_completion_xref( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_completion_xref.MyUWI | SimpleODMConfig.Defaults.COMPLETION_OBS_NO != MyModules.well_completion_xref.COMPLETION_OBS)
                        {
                            MyModules.well_completion_xref.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.COMPLETION_OBS_NO);
                        }

                        break;
                    }

                case "WELLCOMPSTRING2FORM":
                    {
                        if (MyModules.well_completion_String2Formation_link is null)
                        {
                            MyModules.well_completion_String2Formation_link = new SimpleODM.wellmodule.uc_completion_String2Formation_link( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_completion_String2Formation_link.WellSetUWI | SimpleODMConfig.Defaults.COMPLETION_OBS_NO != MyModules.well_completion_String2Formation_link.COMPLETIONOBSNO | SimpleODMConfig.Defaults.COMPLETION_SOURCE != MyModules.well_completion_String2Formation_link.COMPLETIONSOURCE)
                        {
                            MyModules.well_completion_String2Formation_link.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.COMPLETION_OBS_NO, SimpleODMConfig.Defaults.COMPLETION_SOURCE);
                        }

                        break;
                    }

                case "PERFORATION":
                    {
                        if (MyModules.wellperf is null)
                        {
                            MyModules.wellperf = new SimpleODM.wellmodule.uc_preforation( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellperf.MyUWI | SimpleODMConfig.Defaults.PERFORATION_OBS_NO != MyModules.wellperf.PERFORATION_OBS_NO | SimpleODMConfig.Defaults.COMPLETION_OBS_NO != MyModules.wellperf.Completion_OB_NO)
                        {
                            MyModules.wellperf.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.COMPLETION_SOURCE, SimpleODMConfig.Defaults.COMPLETION_OBS_NO, TransType, SimpleODMConfig.Defaults.PERFORATION_OBS_NO);
                        }

                        break;
                    }

                case "WELLCOMPCI":
                    {
                        if (MyModules.wellperf is null)
                        {
                            MyModules.wellperf = new SimpleODM.wellmodule.uc_preforation( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellperf.MyUWI | SimpleODMConfig.Defaults.PERFORATION_OBS_NO != MyModules.wellperf.PERFORATION_OBS_NO | SimpleODMConfig.Defaults.COMPLETION_OBS_NO != MyModules.wellperf.Completion_OB_NO)
                        {
                            MyModules.wellperf.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.COMPLETION_SOURCE, SimpleODMConfig.Defaults.COMPLETION_OBS_NO, TransType, SimpleODMConfig.Defaults.PERFORATION_OBS_NO);
                        }

                        break;
                    }


                // WindowsUIView1.Controller.Activate(WellPerforationPage)
                case "PRODSTRINGEQUIPMENT":
                    {
                        if (MyModules.well_equipments is null)
                        {
                            MyModules.well_equipments = new SimpleODM.wellmodule.uc_well_equipment( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_equipments.MyUWI | SimpleODMConfig.Defaults.STRING_ID != MyModules.well_equipments._STRING_ID)
                        {
                            MyModules.well_equipments.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PROD_STRING_SOURCE);
                        }

                        MyModules.well_equipments.SetTitleLabel();
                        break;
                    }
                // WindowsUIView1.Controller.Activate(ProdStringEquipmentPage)
                case "WELLBOREEQUIPMENT":
                    {
                        if (MyModules.well_equipments is null)
                        {
                            MyModules.well_equipments = new SimpleODM.wellmodule.uc_well_equipment( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_equipments.MyUWI | string.IsNullOrEmpty(MyModules.well_equipments._STRING_ID) == true)
                        {
                            MyModules.well_equipments.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, "", "", SimpleODMConfig.Defaults.EQUIPMENTID);
                        }

                        MyModules.well_equipments._STRING_ID = "";
                        MyModules.well_equipments._STRING_SOURCE = "";
                        MyModules.well_equipments.SetTitleLabel();
                        break;
                    }

                case "EQUIPMENTSEARCH":
                    {
                        if (MyModules.well_equipments_search is null)
                        {
                            MyModules.well_equipments_search = new SimpleODM.wellmodule.uc_well_equipment_search( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_equipments_search.MyUWI) Then
                        MyModules.well_equipments_search.Loaddata();
                        break;
                    }
                // End If
                // WindowsUIView1.Controller.Activate(WellboreEquipmentPage)
                case "PRODSTRINGFACILITIES":
                    {
                        if (MyModules.Well_Facility is null)
                        {
                            MyModules.Well_Facility = new SimpleODM.wellmodule.uc_well_facility( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Facility.MyUWI | SimpleODMConfig.Defaults.STRING_ID != MyModules.Well_Facility.STRINGID)
                        {
                            MyModules.Well_Facility.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PROD_STRING_SOURCE);
                        }

                        MyModules.Well_Facility.SetTitle();
                        break;
                    }

                case "WBFACILITIES":
                    {
                        if (MyModules.Well_Facility is null)
                        {
                            MyModules.Well_Facility = new SimpleODM.wellmodule.uc_well_facility( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Facility.MyUWI | SimpleODMConfig.Defaults.STRING_ID == "" & MyModules.Well_Facility.STRINGID.Length > 0)
                        {
                            MyModules.Well_Facility.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType);
                        }

                        MyModules.Well_Facility.STRINGID = "";
                        MyModules.Well_Facility.STRINGSOURCE = "";
                        MyModules.Well_Facility.SetTitle();
                        break;
                    }
                // WindowsUIView1.Controller.Activate(ProdStringConnectedFacilitiesPage)
                case "PRODSTRINGTEST":
                    {
                        if (MyModules.well_test is null)
                        {
                            MyModules.well_test = new SimpleODM.welltestandpressuremodule.uc_WellTest( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test.MyUWI)
                        {
                            MyModules.well_test.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PROD_STRING_SOURCE, SimpleODMConfig.Defaults.PR_STR_FORM_OBS_NO, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM);
                        }

                        break;
                    }

                case "WELLDESIGN":
                    {
                        if (MyModules.wellboreDesigner is null)
                        {
                            MyModules.wellboreDesigner = new SimpleODM.wellmodule.uc_WellDesigner( ref _SimpleODMConfig, SimpleUtil);
                        }
                        // ParentDoc = WindowsUIView1.Controller.Manager.View.ActiveDocument
                        MyModules.wellboreDesigner.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        break;
                    }

                // WindowsUIView1.Controller.Activate(WellTestdocumentPage)
                // -------------------------------  End Well Desginer -------------------------------------------------
                // -------------------------------- Start of Well Details Modules -------------------------------------
                case "WELLREMARK":
                    {
                        if (MyModules.Wellremark is null)
                        {
                            MyModules.Wellremark = new SimpleODM.wellmodule.uc_well_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Wellremark.MyUWI)
                        {
                            MyModules.Wellremark.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLMISC":
                    {
                        if (MyModules.Wellmisc is null)
                        {
                            MyModules.Wellmisc = new SimpleODM.wellmodule.uc_well_misc_data( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Wellmisc.MyUWI)
                        {
                            MyModules.Wellmisc.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLAREA":
                    {
                        if (MyModules.Wellarea is null)
                        {
                            MyModules.Wellarea = new SimpleODM.wellmodule.uc_well_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Wellarea.MyUWI)
                        {
                            MyModules.Wellarea.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLTEST":
                    {
                        if (MyModules.well_test is null)
                        {
                            MyModules.well_test = new SimpleODM.welltestandpressuremodule.uc_WellTest( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_test.MyUWI)
                        {
                            MyModules.well_test.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, "EDIT");
                        }

                        break;
                    }

                case "WELLPRESS":
                    {
                        if (MyModules.well_Pressure is null)
                        {
                            MyModules.well_Pressure = new SimpleODM.welltestandpressuremodule.uc_well_pressure( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_Pressure.MyUWI)
                        {
                            MyModules.well_Pressure.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLFACILITY":
                    {
                        if (MyModules.Well_Facility is null)
                        {
                            MyModules.Well_Facility = new SimpleODM.wellmodule.uc_well_facility( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Facility.MyUWI)
                        {
                            MyModules.Well_Facility.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType);
                        }

                        MyModules.Well_Facility.SetTitle();
                        break;
                    }

                case "WELLVERSION":
                    {
                        if (MyModules.Wellversion is null)
                        {
                            MyModules.Wellversion = new SimpleODM.wellmodule.uc_wellversion( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Wellversion.MyUWI)
                        {
                            MyModules.Wellversion.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLALIAS":
                    {
                        if (MyModules.Wellalias is null)
                        {
                            MyModules.Wellalias = new SimpleODM.wellmodule.uc_well_alias( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Wellalias.MyUWI)
                        {
                            MyModules.Wellalias.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLLAND": // Land Rights
                    {
                        if (MyModules.wellLandrights is null)
                        {
                            MyModules.wellLandrights = new SimpleODM.wellmodule.uc_well_Landrights( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellLandrights.MyUWI)
                        {
                            MyModules.wellLandrights.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLLIC":
                    {
                        if (MyModules.welllicense is null)
                        {
                            MyModules.welllicense = new SimpleODM.wellmodule.uc_well_Licenses( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.welllicense.MyUWI)
                        {
                            MyModules.welllicense.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLBASERVICES":
                    {
                        if (MyModules.Well_BA_Services is null)
                        {
                            MyModules.Well_BA_Services = new SimpleODM.wellmodule.uc_well_ba_services( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_BA_Services.MyUWI)
                        {
                            MyModules.Well_BA_Services.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLGEO": // Geometry"
                    {
                        if (MyModules.Well_Geometry is null)
                        {
                            MyModules.Well_Geometry = new SimpleODM.wellmodule.uc_well_geometry( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Geometry.MyUWI)
                        {
                            MyModules.Well_Geometry.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_NUMERIC_ID);
                        }

                        break;
                    }

                case "WELLSRV": // Survey"
                    {
                        if (MyModules.WellDirSRVYManager is null)
                        {
                            MyModules.WellDirSRVYManager = new SimpleODM.wellmodule.uc_wellDirSurvey( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.WellDirSRVYManager.UWI)
                        {
                            MyModules.WellDirSRVYManager.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLSUPFAC": // Support Facility"
                    {
                        if (MyModules.Well_Support_Facility is null)
                        {
                            MyModules.Well_Support_Facility = new SimpleODM.wellmodule.uc_well_support_facility( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Support_Facility.MyUWI)
                        {
                            MyModules.Well_Support_Facility.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLPERMIT": // Permit"
                    {
                        if (MyModules.Well_Permit is null)
                        {
                            MyModules.Well_Permit = new SimpleODM.wellmodule.uc_well_License_permit_Types( ref _SimpleODMConfig, SimpleUtil);
                        }

                        MyModules.Well_Permit.LoadData();
                        break;
                    }

                case "WELLNODE":
                    {
                        if (MyModules.Well_Node is null)
                        {
                            MyModules.Well_Node = new SimpleODM.wellmodule.uc_well_node( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Node.MyUWI)
                        {
                            MyModules.Well_Node.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLNODEAREA":
                    {
                        if (MyModules.Well_Node_Area is null)
                        {
                            MyModules.Well_Node_Area = new SimpleODM.wellmodule.uc_well_node_area( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Node_Area.MyUWI)
                        {
                            MyModules.Well_Node_Area.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.NODE_ID);
                        }

                        break;
                    }

                case "WELLNODEGEO":
                    {
                        if (MyModules.Well_Node_Geometry is null)
                        {
                            MyModules.Well_Node_Geometry = new SimpleODM.wellmodule.uc_well_node_geometry( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Node_Geometry.MyUWI)
                        {
                            MyModules.Well_Node_Geometry.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.NODE_ID);
                        }

                        break;
                    }

                case "WELLNODEMETEANDBOUND":
                    {
                        if (MyModules.Well_Node_Metes_and_Bound is null)
                        {
                            MyModules.Well_Node_Metes_and_Bound = new SimpleODM.wellmodule.uc_well_node_metesandbound( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Node_Metes_and_Bound.MyUWI)
                        {
                            MyModules.Well_Node_Metes_and_Bound.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.NODE_ID);
                        }

                        break;
                    }

                case "WELLNODESTRAT":
                    {
                        if (MyModules.Well_Node_Stratigraphy is null)
                        {
                            MyModules.Well_Node_Stratigraphy = new SimpleODM.wellmodule.uc_well_node_stratunit( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Node_Stratigraphy.MyUWI)
                        {
                            MyModules.Well_Node_Stratigraphy.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.NODE_ID);
                        }

                        break;
                    }

                case "WELLACTIVITY":
                    {
                        if (MyModules.Well_Activity is null)
                        {
                            MyModules.Well_Activity = new SimpleODM.wellmodule.uc_well_activity( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Activity.MyUWI)
                        {
                            MyModules.Well_Activity.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLACTIVITYCAUSE":
                    {
                        if (MyModules.Well_Activity_Conditions_and_Events is null)
                        {
                            MyModules.Well_Activity_Conditions_and_Events = new SimpleODM.wellmodule.uc_well_activities_cause( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Activity_Conditions_and_Events.MyUWI)
                        {
                            MyModules.Well_Activity_Conditions_and_Events.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ACTIVITY_OBS_NO);
                        }

                        break;
                    }

                case "WELLACTIVITYDURATION":
                    {
                        if (MyModules.Well_Activity_Duration is null)
                        {
                            MyModules.Well_Activity_Duration = new SimpleODM.wellmodule.uc_well_activities_duration( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Activity_Duration.MyUWI)
                        {
                            MyModules.Well_Activity_Duration.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ACTIVITY_OBS_NO);
                        }

                        break;
                    }

                case "WELLCORE":
                    {
                        if (MyModules.Well_Core is null)
                        {
                            MyModules.Well_Core = new SimpleODM.wellmodule.uc_well_core( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core.MyUWI)
                        {
                            MyModules.Well_Core.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLCOREANALYSIS":
                    {
                        if (MyModules.Well_Core_Aanlysis is null)
                        {
                            MyModules.Well_Core_Aanlysis = new SimpleODM.wellmodule.uc_well_core_analysis( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Aanlysis.MyUWI)
                        {
                            MyModules.Well_Core_Aanlysis.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID);
                        }

                        break;
                    }

                case "WELLCOREANALYSISSAMPLE":
                    {
                        if (MyModules.Well_Core_Aanlysis_Sample is null)
                        {
                            MyModules.Well_Core_Aanlysis_Sample = new SimpleODM.wellmodule.uc_well_core_analysis_sample( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Aanlysis_Sample.MyUWI)
                        {
                            MyModules.Well_Core_Aanlysis_Sample.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO);
                        }

                        break;
                    }

                case "WELLCOREANALYSISSAMPLEREMARK":
                    {
                        if (MyModules.Well_Core_Aanlysis_Sample_description is null)
                        {
                            MyModules.Well_Core_Aanlysis_Sample_description = new SimpleODM.wellmodule.uc_well_core_analysis_sample_description( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Aanlysis_Sample_description.MyUWI)
                        {
                            MyModules.Well_Core_Aanlysis_Sample_description.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO, SimpleODMConfig.Defaults.CORE_SAMPLE_ANALYSIS_OBS_NO, SimpleODMConfig.Defaults.CORE_SAMPLE_NUM);
                        }

                        break;
                    }

                case "WELLCOREANALYSISSAMPLEDESCRIPTION":
                    {
                        if (MyModules.Well_Core_Aanlysis_Sample_remark is null)
                        {
                            MyModules.Well_Core_Aanlysis_Sample_remark = new SimpleODM.wellmodule.uc_well_core_analysis_sample_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Aanlysis_Sample_remark.MyUWI)
                        {
                            MyModules.Well_Core_Aanlysis_Sample_remark.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO, SimpleODMConfig.Defaults.CORE_SAMPLE_ANALYSIS_OBS_NO, SimpleODMConfig.Defaults.CORE_SAMPLE_NUM);
                        }

                        break;
                    }

                case "WELLCOREANALYSISMETHOD":
                    {
                        if (MyModules.Well_Core_Aanlysis_Method is null)
                        {
                            MyModules.Well_Core_Aanlysis_Method = new SimpleODM.wellmodule.uc_well_core_analysis_method( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Aanlysis_Method.MyUWI)
                        {
                            MyModules.Well_Core_Aanlysis_Method.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO);
                        }

                        break;
                    }

                case "WELLCOREANALYSISREMARK":
                    {
                        if (MyModules.Well_Core_Aanlysis_Remark is null)
                        {
                            MyModules.Well_Core_Aanlysis_Remark = new SimpleODM.wellmodule.uc_well_core_analysis_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Aanlysis_Remark.MyUWI)
                        {
                            MyModules.Well_Core_Aanlysis_Remark.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO);
                        }

                        break;
                    }

                case "WELLCOREDESCRIPTION":
                    {
                        if (MyModules.Well_Core_Description is null)
                        {
                            MyModules.Well_Core_Description = new SimpleODM.wellmodule.uc_well_core_description( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Description.MyUWI)
                        {
                            MyModules.Well_Core_Description.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID);
                        }

                        break;
                    }

                case "WELLCOREDESCRIPTIONSTRAT":
                    {
                        if (MyModules.Well_Core_Description_Stratigraphy is null)
                        {
                            MyModules.Well_Core_Description_Stratigraphy = new SimpleODM.wellmodule.uc_well_core_description_strat_unit( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Description.MyUWI)
                        {
                            MyModules.Well_Core_Description_Stratigraphy.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_DESCRIPTION_OBS_NO);
                        }

                        break;
                    }

                case "WELLCORESHIFT":
                    {
                        if (MyModules.Well_Core_Shift is null)
                        {
                            MyModules.Well_Core_Shift = new SimpleODM.wellmodule.uc_well_core_shift( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Shift.MyUWI)
                        {
                            MyModules.Well_Core_Shift.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID);
                        }

                        break;
                    }

                case "WELLCOREREMARK":
                    {
                        if (MyModules.Well_Core_Remark is null)
                        {
                            MyModules.Well_Core_Remark = new SimpleODM.wellmodule.uc_well_core_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Core_Remark.MyUWI)
                        {
                            MyModules.Well_Core_Remark.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID);
                        }

                        break;
                    }

                case "WELLMUDSAMPLE":
                    {
                        if (MyModules.well_mud_sample is null)
                        {
                            MyModules.well_mud_sample = new SimpleODM.wellmodule.uc_well_mud_sample( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_mud_sample.MyUWI)
                        {
                            MyModules.well_mud_sample.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLMUDRESISTIVITY":
                    {
                        if (MyModules.well_mud_sample_resistivity is null)
                        {
                            MyModules.well_mud_sample_resistivity = new SimpleODM.wellmodule.uc_well_mud_sample_resistivity( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_mud_sample_resistivity.MyUWI)
                        {
                            MyModules.well_mud_sample_resistivity.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.Mud_Sample_ID);
                        }

                        break;
                    }

                case "WELLMUDPROPERTY":
                    {
                        if (MyModules.well_mud_sample_proerty is null)
                        {
                            MyModules.well_mud_sample_proerty = new SimpleODM.wellmodule.uc_well_mud_sample_property( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_mud_sample_proerty.MyUWI)
                        {
                            MyModules.well_mud_sample_proerty.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.Mud_Sample_ID);
                        }

                        break;
                    }

                case "WELLAIRDRILL":
                    {
                        if (MyModules.well_air_drill is null)
                        {
                            MyModules.well_air_drill = new SimpleODM.wellmodule.uc_well_air_drill( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_air_drill.MyUWI)
                        {
                            MyModules.well_air_drill.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLAIRDRILLINTERVAL":
                    {
                        if (MyModules.well_air_drill_Interval is null)
                        {
                            MyModules.well_air_drill_Interval = new SimpleODM.wellmodule.uc_well_air_drill_Interval( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_air_drill_Interval.MyUWI)
                        {
                            MyModules.well_air_drill_Interval.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.AIR_DRILL_OBS_NO, SimpleODMConfig.Defaults.AIR_DRILL_INTERVAL_SOURCE);
                        }

                        break;
                    }

                case "WELLAIRDRILLINTERVALPERIOD":
                    {
                        if (MyModules.well_air_drill_interval_period is null)
                        {
                            MyModules.well_air_drill_interval_period = new SimpleODM.wellmodule.uc_well_air_drill_interval_period( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_air_drill_interval_period.MyUWI)
                        {
                            MyModules.well_air_drill_interval_period.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.AIR_DRILL_OBS_NO, SimpleODMConfig.Defaults.AIR_DRILL_INTERVAL_SOURCE, SimpleODMConfig.Defaults.AIR_DRILL_INTERVAL_DEPTH_OBS_NO);
                        }

                        break;
                    }

                case "WELLHORIZDRILL":
                    {
                        if (MyModules.well_Horiz_Drill is null)
                        {
                            MyModules.well_Horiz_Drill = new SimpleODM.wellmodule.uc_well_Horiz_Drill( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_Horiz_Drill.MyUWI)
                        {
                            MyModules.well_Horiz_Drill.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLHORIZDRILLKOP":
                    {
                        if (MyModules.well_Horiz_Drill_drill_kop is null)
                        {
                            MyModules.well_Horiz_Drill_drill_kop = new SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_kop( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_Horiz_Drill_drill_kop.MyUWI)
                        {
                            MyModules.well_Horiz_Drill_drill_kop.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLHORIZDRILLPOE":
                    {
                        if (MyModules.well_Horiz_Drill_drill_poe is null)
                        {
                            MyModules.well_Horiz_Drill_drill_poe = new SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_poe( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_Horiz_Drill_drill_poe.MyUWI)
                        {
                            MyModules.well_Horiz_Drill_drill_poe.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLHORIZDRILLSPOKE":
                    {
                        if (MyModules.well_Horiz_Drill_drill_spoke is null)
                        {
                            MyModules.well_Horiz_Drill_drill_spoke = new SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_KOP_spoke( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_Horiz_Drill_drill_spoke.MyUWI)
                        {
                            MyModules.well_Horiz_Drill_drill_spoke.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.KICKOFF_POINT_OBSNO);
                        }

                        break;
                    }

                case "WELLSHOW":
                    {
                        if (MyModules.well_show is null)
                        {
                            MyModules.well_show = new SimpleODM.wellmodule.uc_well_show( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_show.MyUWI)
                        {
                            MyModules.well_show.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                        }

                        break;
                    }

                case "WELLSHOWREMARK":
                    {
                        if (MyModules.well_show_remark is null)
                        {
                            MyModules.well_show_remark = new SimpleODM.wellmodule.uc_well_show_remark( ref _SimpleODMConfig, SimpleUtil);
                        }

                        if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.well_show_remark.MyUWI)
                        {
                            MyModules.well_show_remark.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.SHOW_TYPE, SimpleODMConfig.Defaults.SHOW_OBS_NO, SimpleODMConfig.Defaults.SHOW_SOURCE);
                        }

                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show("Error Init Module : " + ex.Message, "Simple ODM", MessageBoxButtons.OK);
        }
        // -------------------------------  End Well Desginer -------------------------------------------------
        ShowControlOnTile?.Invoke(Title, pType, trans);
    }

    /* TODO ERROR: Skipped EndRegionDirectiveTrivia *//* TODO ERROR: Skipped RegionDirectiveTrivia */
    public Page ActiveContainer { get; set; }

    private void WindowsUIView1_ControlReleased(object sender, DevExpress.XtraBars.Docking2010.Views.DeferredControlLoadEventArgs e)
    {
    }

    private void WindowsUIView1_ControlReleasing(object sender, DevExpress.XtraBars.Docking2010.Views.ControlReleasingEventArgs e)
    {
    }

    public void WindowsUIView1_QueryControl(object sender, DevExpress.XtraBars.Docking2010.Views.QueryControlEventArgs e)
    {
        DidIInitForms = true;
        // If SimpleODMconfig.ppdmcontext.ConnectionType = ConnectionTypeEnum.User Then
        // Dim x As New FlyoutProperties
        // x.Orientation = Orientation.Vertical
        // x.Alignment = ContentAlignment.MiddleCenter

        // WindowsUIView1.ShowFlyoutDialog(New DevExpress.XtraBars.Docking2010.Views.WindowsUI.Flyout(x))
        // WindowsUIView1.ShowSearchPanel()
        // ActiveContainer = WindowsUIView1.ActiveContentContainer
        switch (e.Document.Caption)
        {
            case "SpreadSheet Reporting":
                {
                    if (MyModules.Excelreportbuilder is null)
                    {
                        MyModules.Excelreportbuilder = new SimpleODM.SharedLib.uc_spreadsheet( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.Excelreportbuilder;
                    break;
                }

            case "Simple Report Builder":
                {
                    if (MyModules.reportbuilder is null)
                    {
                        MyModules.reportbuilder = new SimpleODM.SharedLib.uc_simpleReporting_lite( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.reportbuilder;
                    break;
                }
            // -----------------------------------------Log Data Management---------------------------------------------
            case "Well Log Loader":
                {
                    if (MyModules.well_log_manager is null)
                    {
                        MyModules.well_log_manager = new SimpleODM.Logmodule.uc_log_manager( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.well_log_manager;
                    break;
                }

            case "Log File Loader":
                {
                    e.Control = MyModules.well_log_files_manager;
                    break;
                }

            case "Log Data Manager":
                {
                    if (MyModules.Well_Logs is null)
                    {
                        MyModules.Well_Logs = new SimpleODM.Logmodule.uc_log( ref _SimpleODMConfig, SimpleUtil);
                    }

                    // If MyModules.Well_Logs.WellON = True Then
                    // If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Logs.MyUWI) Then
                    // MyModules.Well_Logs.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    // End If
                    // Else
                    MyModules.Well_Logs.LoadManager();
                    // End If

                    e.Control = MyModules.Well_Logs;
                    break;
                }

            // -------------------------------------------------
            case "Well Log Curve":
                {
                    if (MyModules.Well_Log_curve is null)
                    {
                        MyModules.Well_Log_curve = new SimpleODM.Logmodule.uc_log_curve( ref _SimpleODMConfig, SimpleUtil);
                    }

                    if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.Well_Log_curve.MyUWI)
                    {
                        MyModules.Well_Log_curve.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE);
                    }

                    e.Control = MyModules.Well_Log_curve;
                    break;
                }

            case "Log Data Dictionary Manager":
                {
                    if (MyModules.Well_Log_dictionary is null)
                    {
                        MyModules.Well_Log_dictionary = new SimpleODM.Logmodule.uc_log_dictionary( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.Well_Log_dictionary;
                    break;
                }

            case "Log Job Data Management":
                {
                    e.Control = MyModules.Well_Log_job;
                    break;
                }

            case "Well Log Job":
                {
                    e.Control = MyModules.Well_Log_job;
                    break;
                }

            case var @case when @case == "Log Data Dictionary Manager":
                {
                    e.Control = MyModules.Well_Log_dictionary;
                    break;
                }

            case "Well Logs":
                {
                    e.Control = MyModules.Well_Logs;
                    break;
                }

            case "Log Activity":
                {
                    e.Control = MyModules.Well_Logs_Activities;
                    break;
                }

            case var case1 when case1 == "Well Log Loader":
                {
                    e.Control = MyModules.Well_Log_loader;
                    break;
                }

            case "Well Log Job Trip":                    // Well Log Job Trip
                {
                    e.Control = MyModules.well_log_job_trip;
                    break;
                }

            case "Well Log Job Trip Pass":                  // Well Log Job Trip Pass
                {
                    e.Control = MyModules.well_log_job_trip_pass;
                    break;
                }

            case "Well Log Job Trip Remark":                   // Well Log Job Trip Remark
                {
                    e.Control = MyModules.well_log_job_trip_remark;
                    break;
                }

            case "Well Log Dictionary Alias":                        // Well Log Dictionary Alias
                {
                    e.Control = MyModules.well_log_dictionary_alias;
                    break;
                }

            case "Well Log Dictionary Curve":                           // Well Log Dictionary Curve
                {
                    e.Control = MyModules.well_log_dictionary_curve;
                    break;
                }

            case "Well Log Dictionary Curve Classification":                 // Well Log Dictionary Curve Classification
                {
                    e.Control = MyModules.well_log_dictionary_curve_cls;
                    break;
                }

            case "Well Log Dictionary Parameter":                             // Well Log Dictionary Parameter
                {
                    e.Control = MyModules.well_log_dictionary_param;
                    break;
                }

            case "Well Log Dictionary Parameter Classification":                                // Well Log Dictionary Parameter Classification
                {
                    e.Control = MyModules.well_log_dictionary_param_cls;
                    break;
                }

            case "Well Log Dictionary Parameter Classification Types":                    // Well Log Dictionary Parameter Classification Types
                {
                    e.Control = MyModules.well_log_dictionary_param_cls_Types;
                    break;
                }

            case "Well Log Dictionary Parameter Values":             // Well Log Dictionary Parameter Values
                {
                    e.Control = MyModules.well_log_dictionary_param_value;
                    break;
                }

            case "Well Log Dictionary Business Associate":                                 // Well Log Dictionary Business Associate
                {
                    e.Control = MyModules.well_log_dictionary_ba;
                    break;
                }

            case "Well Log Dictionary Procedure":                  // Well Log Dictionary Procedure
                {
                    e.Control = MyModules.well_log_dictionary_proc;
                    break;
                }

            case "Well Log Classification":                          // Well Log Classification
                {
                    e.Control = MyModules.well_log_class;
                    break;
                }

            case "Well Log Remark":                            // Well Log Remark
                {
                    e.Control = MyModules.well_log_remark;
                    break;
                }

            case "Well Log Parameters":                   // Well Log Parameters
                {
                    e.Control = MyModules.well_log_parameter;
                    break;
                }

            case "Well Log Parameters Array":
                {
                    e.Control = MyModules.well_log_parameter_array;
                    break;
                }
            // ------------------------------------ Lithology -------------------------------------------------------
            case "Lithlogy Measured Section":
                {
                    if (MyModules.lith_measured_sec is null)
                    {
                        MyModules.lith_measured_sec = new SimpleODM.stratigraphyandlithologyModule.uc_lith_measured_sec( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.lith_measured_sec;
                    break;
                }

            case "Descriptive Record of Litholgy":
                {
                    e.Control = MyModules.lith_log;
                    break;
                }

            case "Descriptive Record of Litholgy Remarks":
                {
                    e.Control = MyModules.lith_log_remark;
                    break;
                }

            case "Descriptive Record of Litholgy BA Services":
                {
                    e.Control = MyModules.lith_log_ba_service;
                    break;
                }

            case "An Interpreted depositional Env. over Specified Interval of a Descriptive Record of Litholgy":
                {
                    e.Control = MyModules.lith_dep_ent_int;
                    break;
                }

            case "A Depth Interval Descriptive Record of Litholgy":
                {
                    e.Control = MyModules.lith_interval;
                    break;
                }

            case "Description of Rock Type Comprising an Interval":
                {
                    e.Control = MyModules.lith_rock_type;
                    break;
                }

            case "Description of Major or minor Rock Components":
                {
                    e.Control = MyModules.lith_component;
                    break;
                }

            case "Color Description of Major or minor Rock Components":
                {
                    e.Control = MyModules.lith_component_color;
                    break;
                }

            case "Measured Sizes in Rock Components":
                {
                    e.Control = MyModules.lith_comp_grain_size;
                    break;
                }

            case "Description of the Post Depositional Alterations":
                {
                    e.Control = MyModules.lith_diagenesis;
                    break;
                }

            case "Description of Grain or Crystal sizes of Rock Components":
                {
                    e.Control = MyModules.lith_grain_size;
                    break;
                }

            case "The Observed Porosity of Rock Components":
                {
                    e.Control = MyModules.lith_porosity;
                    break;
                }

            case "Description of Color of the Rock Type":
                {
                    e.Control = MyModules.lith_rock_color;
                    break;
                }

            case "Description of Physical Structure Rock Type":
                {
                    e.Control = MyModules.lith_rock_structure;
                    break;
                }

            case "Physical Structure within a major rocktype or Sub Interval":
                {
                    e.Control = MyModules.lith_structure;
                    break;
                }

            case "Lithology Sample":
                {
                    if (MyModules.lith_sample is null)
                    {
                        MyModules.lith_sample = new SimpleODM.stratigraphyandlithologyModule.uc_lith_sample( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.lith_sample;
                    break;
                }

            case "Lithology Sample Collection":
                {
                    e.Control = MyModules.lith_sample_collection;
                    break;
                }

            case "Lithology Sample Description":
                {
                    e.Control = MyModules.lith_sample_desc;
                    break;
                }

            case "Describe the Physical or Chemical Process used to perpare the Sample":
                {
                    e.Control = MyModules.lith_sample_prep;
                    break;
                }

            case "Describe the Methods used to perpare the Sample":
                {
                    e.Control = MyModules.lith_sample_prep_math;
                    break;
                }

            case "Other Descriptions to the Lithogloy Sample":
                {
                    e.Control = MyModules.lith_desc_other;
                    break;
                }
            // ------------------------------------ Reserve Entities and Classisifactions --------------------
            case "Reserve Entities":
                {
                    if (MyModules.reserve_entity is null)
                    {
                        MyModules.reserve_entity = new SimpleODM.reservepoolproductionmodule.uc_reserve_entity( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.reserve_entity;
                    break;
                }

            case "Reserve Entity Economic Run":
                {
                    e.Control = MyModules.resent_eco_run;
                    break;
                }

            case "Reserve Entity Product":
                {
                    e.Control = MyModules.resent_product;
                    break;
                }

            case "Reserve Entity Economic Run Parameters":
                {
                    e.Control = MyModules.resent_eco_schedule;
                    break;
                }

            case "Reserve Entity Economic Run Volume":
                {
                    e.Control = MyModules.resent_eco_volume;
                    break;
                }

            case "Reserve Entity Product Properties":
                {
                    e.Control = MyModules.resent_prod_property;
                    break;
                }

            case "Reserve Entity Product Volume Summary":
                {
                    e.Control = MyModules.resent_vol_summary;
                    break;
                }

            case "Reserve Entity Product Volume Revision":
                {
                    e.Control = MyModules.resent_vol_revision;
                    break;
                }

            case "Reserve Class Formula":
                {
                    e.Control = MyModules.reserve_class_formula;
                    break;
                }

            case "Reserve Class Formula Calculation":
                {
                    e.Control = MyModules.reserve_class_calc;
                    break;
                }

            case "Reserve Classifications":
                {
                    e.Control = MyModules.reserve_class;
                    break;
                }

            case "Reserve Entities Class":
                {
                    e.Control = MyModules.resent_class;
                    break;
                }

            case "Reserve Entities Cross Reference":
                {
                    e.Control = MyModules.resent_xref;
                    break;
                }

            case "Reserve Revision Category":
                {
                    e.Control = MyModules.resent_revision_cat;
                    break;
                }

            case "Volume Unit Regime":
                {
                    e.Control = MyModules.resent_vol_regime;
                    break;
                }
            // ------------------------------------ PDEN Production Entities -------
            case "Production Entites":
                {
                    if (MyModules.pden is null)
                    {
                        MyModules.pden = new SimpleODM.reservepoolproductionmodule.uc_pden( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.pden;
                    break;
                }

            case "Production Entity as Business Associate":
                {
                    e.Control = MyModules.pden_business_assoc;
                    break;
                }

            case "Production Entity as Area":
                {
                    e.Control = MyModules.pden_area;
                    break;
                }

            case "Production Entity as Other":
                {
                    e.Control = MyModules.pden_other;
                    break;
                }

            case "Production Entity as Well":
                {
                    e.Control = MyModules.pden_well;
                    break;
                }

            case "Production Entity as Production String":
                {
                    e.Control = MyModules.pden_prod_string;
                    break;
                }

            case "Production Entity as Reserve":
                {
                    e.Control = MyModules.pden_resent;
                    break;
                }

            case "Production Entity as Formation":
                {
                    e.Control = MyModules.pden_pr_str_form;
                    break;
                }

            case "Production Entity as Pool (Reservoir)":
                {
                    e.Control = MyModules.pden_pool;
                    break;
                }

            case "Production Entity as Lease":
                {
                    e.Control = MyModules.pden_lease_unit;
                    break;
                }

            case "Production Entity as Reserve Class":
                {
                    e.Control = MyModules.pden_resent_class;
                    break;
                }

            case "Production Entity as Facility":
                {
                    e.Control = MyModules.pden_facility;
                    break;
                }

            case "Production Entity as Field":
                {
                    e.Control = MyModules.pden_field;
                    break;
                }

            case "Production Entity Allocation Factor":
                {
                    e.Control = MyModules.pden_alloc_factor;
                    break;
                }

            case "Production Entity Decline Forcast Case":
                {
                    e.Control = MyModules.pden_decline_case;
                    break;
                }

            case "Production Entity Decline Forcast Case Conditions":
                {
                    e.Control = MyModules.pden_decline_condition;
                    break;
                }

            case "Production Entity Decline Forcast Case Segments":
                {
                    e.Control = MyModules.pden_decline_segment;
                    break;
                }

            case "Production Entity Flow Measurement":
                {
                    e.Control = MyModules.pden_flow_measurement;
                    break;
                }

            case "Production Entity In Area":
                {
                    e.Control = MyModules.pden_in_area;
                    break;
                }

            case "Production Entity Production String to PDEN Cross Reference":
                {
                    e.Control = MyModules.pden_prod_string_xref;
                    break;
                }

            case "Production Entity Production String Contribution Allowable":
                {
                    e.Control = MyModules.pden_pr_str_allowable;
                    break;
                }

            case "Production Entity Material Balance":
                {
                    e.Control = MyModules.pden_material_bal;
                    break;
                }

            case "Production Entity Operator History":
                {
                    e.Control = MyModules.pden_oper_hist;
                    break;
                }

            case "Production Entity Status History":
                {
                    e.Control = MyModules.pden_status_hist;
                    break;
                }

            case "Production Entity Volume Disposition":
                {
                    e.Control = MyModules.pden_vol_disposition;
                    break;
                }

            case "Production Entity Unit Regime":
                {
                    e.Control = MyModules.pden_vol_regime;
                    break;
                }

            case "Production Entity Volume Summary":
                {
                    e.Control = MyModules.pden_summary;
                    break;
                }

            case "Production Entity Volume Summary Other":
                {
                    e.Control = MyModules.pden_vol_summ_other;
                    break;
                }

            case "Production Entity Volume Analysis":
                {
                    e.Control = MyModules.pden_volume_analysis;
                    break;
                }

            case "Production Entity Cross Referernce":
                {
                    e.Control = MyModules.pden_xref;
                    break;
                }
            // ------------------------------------ POOL ---------------------------
            case "Pool":
                {
                    if (MyModules.Pool is null)
                    {
                        MyModules.Pool = new SimpleODM.reservepoolproductionmodule.uc_pool( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.Pool;
                    break;
                }

            case "Pool Alias":
                {
                    e.Control = MyModules.pool_alias;
                    break;
                }

            case "Pool Area":
                {
                    e.Control = MyModules.pool_area;
                    break;
                }

            case "Pool Instrument":
                {
                    e.Control = MyModules.pool_instrument;
                    break;
                }

            case "Pool Version":
                {
                    e.Control = MyModules.pool_version;
                    break;
                }
            // --------------------------------------- Area --------------------------------------

            case "Area Management":
                {
                    if (MyModules.Area is null)
                    {
                        MyModules.Area = new SimpleODM.supportmodule.uc_area( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.Area;
                    break;
                }

            case "Area Alias":
                {
                    e.Control = MyModules.area_alias;
                    break;
                }

            case "Area Contain":
                {
                    e.Control = MyModules.area_contain;
                    break;
                }

            case "Area Description":
                {
                    e.Control = MyModules.area_description;
                    break;
                }
            // --------------------------------------- Applications ----------------------------
            case "Applications for Authority or Permission":
                {
                    if (MyModules.application4auth is null)
                    {
                        MyModules.application4auth = new SimpleODM.supportmodule.uc_application( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.application4auth;
                    break;
                }

            case "Applications Alias":
                {
                    e.Control = MyModules.application_alias;
                    break;
                }

            case "Applications Area":
                {
                    e.Control = MyModules.application_area;
                    break;
                }

            case "Applications Attachment":
                {
                    e.Control = MyModules.application_attach;
                    break;
                }

            case "Applications Business Associate":
                {
                    e.Control = MyModules.application_ba;
                    break;
                }

            case "Applications Description":
                {
                    e.Control = MyModules.application_desc;
                    break;
                }

            case "Applications Remark":
                {
                    e.Control = MyModules.application_remark;
                    break;
                }
            // -------------------------------------- Catalog ----------------------------------

            case "Catalog Additive Management":
                {
                    if (MyModules.cat_additive is null)
                    {
                        MyModules.cat_additive = new SimpleODM.supportmodule.uc_cat_additive( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.cat_additive;
                    break;
                }

            case "Catalog Equipment Management":
                {
                    if (MyModules.cat_equipment is null)
                    {
                        MyModules.cat_equipment = new SimpleODM.supportmodule.uc_Cat_equipment( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.cat_equipment;
                    break;
                }

            case "Catalog Additive Alias":
                {
                    e.Control = MyModules.cat_additive_alias;
                    break;
                }

            case "Catalog Additive Specification":
                {
                    e.Control = MyModules.cat_additive_spec;
                    break;
                }

            case "Catalog Additive Type":
                {
                    e.Control = MyModules.cat_additive_type;
                    break;
                }

            case "Catalog Additive Cross Reference":
                {
                    e.Control = MyModules.cat_additive_xref;
                    break;
                }

            case "Catalog Equipment Alias":
                {
                    e.Control = MyModules.Cat_equipment_alias;
                    break;
                }

            case "Catalog Equipment Specification":
                {
                    e.Control = MyModules.Cat_equipment_spec;
                    break;
                }
            // -------------------------------------- Facility ---------------------------------
            case "Facility Management":
                {
                    if (MyModules.FacilityModule is null)
                    {
                        MyModules.FacilityModule = new SimpleODM.facilitymodule.uc_facility( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.FacilityModule;
                    break;
                }

            case "Facility Alias":
                {
                    e.Control = MyModules.facility_alias;
                    break;
                }

            case "Facility Area":
                {
                    e.Control = MyModules.facility_area;
                    break;
                }

            case "Facility BA Service":
                {
                    e.Control = MyModules.facility_ba_service;
                    break;
                }

            case "Facility Classification":
                {
                    e.Control = MyModules.facility_class;
                    break;
                }

            case "Facility Description":
                {
                    e.Control = MyModules.facility_description;
                    break;
                }

            case "Facility Equipment":
                {
                    e.Control = MyModules.facility_equipment;
                    break;
                }

            case "Facility Field":
                {
                    e.Control = MyModules.facility_field;
                    break;
                }

            case "Facility License":
                {
                    e.Control = MyModules.facility_license;
                    break;
                }

            case "Facility License Alias":
                {
                    e.Control = MyModules.facility_license_alias;
                    break;
                }

            case "Facility License Area":
                {
                    e.Control = MyModules.facility_license_area;
                    break;
                }

            case "Facility License Conditions":
                {
                    e.Control = MyModules.facility_license_cond;
                    break;
                }

            case "Facility License Remarks":
                {
                    e.Control = MyModules.facility_license_remark;
                    break;
                }

            case "Facility License Status":
                {
                    e.Control = MyModules.facility_license_status;
                    break;
                }

            case "Facility License Type":
                {
                    e.Control = MyModules.facility_license_type;
                    break;
                }

            case "Facility License Violations":
                {
                    e.Control = MyModules.facility_license_violation;
                    break;
                }

            case "Facility Maintainance":
                {
                    e.Control = MyModules.facility_maintain;
                    break;
                }

            case "Facility Maintainance Status":
                {
                    e.Control = MyModules.facility_maintain_status;
                    break;
                }

            case "Facility Rate":
                {
                    e.Control = MyModules.facility_rate;
                    break;
                }

            case "Facility Restrictions":
                {
                    e.Control = MyModules.facility_restriction;
                    break;
                }

            case "Facility Status":
                {
                    e.Control = MyModules.facility_status;
                    break;
                }

            case "Facility Substance":
                {
                    e.Control = MyModules.facility_substance;
                    break;
                }

            case "Facility Version":
                {
                    e.Control = MyModules.facility_version;
                    break;
                }

            case "Facility Cross Reference":
                {
                    e.Control = MyModules.facility_xref;
                    break;
                }
            // '-------------------------------------- Equipment ------------------------------
            case "Equipment":
                {
                    if (MyModules.equipment is null)
                    {
                        MyModules.equipment = new SimpleODM.supportmodule.uc_equipment( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.equipment;
                    break;
                }

            case "Equipment Alias":
                {
                    e.Control = MyModules.equipment_alias;
                    break;
                }

            case "Equipment Business Associate":
                {
                    e.Control = MyModules.equipment_ba;
                    break;
                }

            case "Equipment Maintainance":
                {
                    e.Control = MyModules.equipment_maintain;
                    break;
                }

            case "Equipment Maintainance Status":
                {
                    e.Control = MyModules.equipment_maintain_status;
                    break;
                }

            case "Equipment Maintainance Type":
                {
                    e.Control = MyModules.equipment_maintain_type;
                    break;
                }

            case "Equipment Specification":
                {
                    e.Control = MyModules.equipment_spec;
                    break;
                }

            case "Equipment Specification Set":
                {
                    e.Control = MyModules.equipment_spec_set;
                    break;
                }

            case "Equipment Specification Set Spec":
                {
                    e.Control = MyModules.equipment_spec_set_spec;
                    break;
                }

            case "Equipment Status":
                {
                    e.Control = MyModules.equipment_status;
                    break;
                }

            case "Equipment Usage Statistics":
                {
                    e.Control = MyModules.equipment_use_stat;
                    break;
                }

            case "Equipment Cross Reference":
                {
                    e.Control = MyModules.equipment_crossreference;
                    break;
                }
            // -------------------------------------- BA ------------------------------
            case "Business Associate Data Management":
                {
                    if (MyModules.businessAssociate is null)
                    {
                        MyModules.businessAssociate = new SimpleODM.BusinessAssociateModule.uc_businessAssociate( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.businessAssociate;
                    break;
                }

            case "Business Associate Address":
                {
                    e.Control = MyModules.businessAssociate_address;
                    break;
                }

            case "Business Associate Alias":
                {
                    e.Control = MyModules.businessAssociate_alias;
                    break;
                }

            case "Business Associate Authority":
                {
                    e.Control = MyModules.businessAssociate_authority;
                    break;
                }

            case "Business Associate Consurtuim Service":
                {
                    e.Control = MyModules.businessAssociate_consortuimservice;
                    break;
                }

            case "Business Associate Contact Information":
                {
                    e.Control = MyModules.businessAssociate_contactinfo;
                    break;
                }

            case "Business Associate Crew":
                {
                    e.Control = MyModules.businessAssociate_crew;
                    break;
                }

            case "Business Associate Crew Memebers":
                {
                    e.Control = MyModules.businessAssociate_crew_memeber;
                    break;
                }

            case "Business Associate Description":
                {
                    e.Control = MyModules.businessAssociate_description;
                    break;
                }

            case "Business Associate Employee":
                {
                    e.Control = MyModules.businessAssociate_employee;
                    break;
                }

            case "Business Associate License":
                {
                    e.Control = MyModules.businessAssociate_license;
                    break;
                }

            case "Business Associate License Alias":
                {
                    e.Control = MyModules.businessAssociate_license_alias;
                    break;
                }

            case "Business Associate License Area":
                {
                    e.Control = MyModules.businessAssociate_license_area;
                    break;
                }

            case "Business Associate License Condition":
                {
                    e.Control = MyModules.businessAssociate_license_cond;
                    break;
                }

            case "Business Associate License Condition Violation":
                {
                    e.Control = MyModules.businessAssociate_license_violation;
                    break;
                }

            case "Business Associate License Remark":
                {
                    e.Control = MyModules.businessAssociate_license_remark;
                    break;
                }

            case "Business Associate License Status":
                {
                    e.Control = MyModules.businessAssociate_license_status;
                    break;
                }

            case "Business Associate License Type":
                {
                    e.Control = MyModules.businessAssociate_license_type;
                    break;
                }

            case "Business Associate License Condition Type":
                {
                    e.Control = MyModules.businessAssociate_license_cond_type;
                    break;
                }

            case "Business Associate License Condition Type Code":
                {
                    e.Control = MyModules.businessAssociate_license_cond_code;
                    break;
                }

            case "Business Associate Organization":
                {
                    e.Control = MyModules.businessAssociate_organization;
                    break;
                }

            case "Business Associate Permit":
                {
                    e.Control = MyModules.businessAssociate_permit;
                    break;
                }

            case "Business Associate Service":
                {
                    e.Control = MyModules.businessAssociate_services;
                    break;
                }

            case "Business Associate Service Address":
                {
                    e.Control = MyModules.businessAssociate_services_address;
                    break;
                }

            case "Business Associate Preference":
                {
                    e.Control = MyModules.businessAssociate_preference;
                    break;
                }

            case "Business Associate Cross Reference":
                {
                    e.Control = MyModules.businessAssociate_crosspreference;
                    break;
                }

            case "Security Groups Entitlements":
                {
                    e.Control = MyModules.entitlement_group;
                    break;
                }

            case "Security Groups":
                {
                    e.Control = MyModules.entitlement_security_group;
                    break;
                }

            case "Entitlements Type":
                {
                    e.Control = MyModules.entitlement;
                    break;
                }

            case "Business Associate Entitlement":
                {
                    e.Control = MyModules.entitlement_security_ba;
                    break;
                }

            case "Entitlements Components":
                {
                    e.Control = MyModules.ent_components;
                    break;
                }
            // -------------------------------------- End BA ------------------------------
            case "Land Data Management":
                {
                    if (MyModules.wellLandrights is null)
                    {
                        MyModules.wellLandrights = new SimpleODM.wellmodule.uc_well_Landrights( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.wellLandrights;
                    break;
                }
            // -------------------------------------- Well --------------------------------------------
            case "Well Set Management":
                {
                    if (MyModules.WellModule is null)
                    {
                        MyModules.WellModule = new SimpleODM.wellmodule.uc_NewEditWell( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.WellModule;
                    break;
                }

            case "Well Show":
                {
                    e.Control = MyModules.well_show;
                    break;
                }

            case "Well Show Remark":
                {
                    e.Control = MyModules.well_show_remark;
                    break;
                }

            case "Mud Sample":
                {
                    e.Control = MyModules.well_mud_sample;
                    break;
                }

            case "Mud Sample Resistivity":
                {
                    e.Control = MyModules.well_mud_sample_resistivity;
                    break;
                }

            case "Mud Sample Property":
                {
                    e.Control = MyModules.well_mud_sample_proerty;
                    break;
                }

            case "Air Drill":
                {
                    e.Control = MyModules.well_air_drill;
                    break;
                }

            case "Air Drill Interval":
                {
                    e.Control = MyModules.well_air_drill_Interval;
                    break;
                }

            case "Air Drill Interval Period":
                {
                    e.Control = MyModules.well_air_drill_interval_period;
                    break;
                }

            case "Horiz Drill":
                {
                    e.Control = MyModules.well_Horiz_Drill;
                    break;
                }

            case "Horiz Drill KOP":
                {
                    e.Control = MyModules.well_Horiz_Drill_drill_kop;
                    break;
                }

            case "Horiz Drill POE":
                {
                    e.Control = MyModules.well_Horiz_Drill_drill_poe;
                    break;
                }

            case "Horiz Drill SPOKE":
                {
                    e.Control = MyModules.well_Horiz_Drill_drill_spoke;
                    break;
                }

            case "Well Area":
                {
                    e.Control = MyModules.Wellarea;
                    break;
                }

            case "Well Misc.":
                {
                    e.Control = MyModules.Wellmisc;
                    break;
                }

            case "Well Remark":
                {
                    e.Control = MyModules.Wellremark;
                    break;
                }

            case "Well Activity":
                {
                    e.Control = MyModules.Well_Activity;
                    break;
                }

            case "Well Activity Conditions and Events":
                {
                    e.Control = MyModules.Well_Activity_Conditions_and_Events;
                    break;
                }

            case "Well Activity Duration":
                {
                    e.Control = MyModules.Well_Activity_Duration;
                    break;
                }

            case "Well Node":
                {
                    e.Control = MyModules.Well_Node;
                    break;
                }

            case "Well Node Area":
                {
                    e.Control = MyModules.Well_Node_Area;
                    break;
                }

            case "Well Node Geometry":
                {
                    e.Control = MyModules.Well_Node_Geometry;
                    break;
                }

            case "Well Node Metes and Bound":
                {
                    e.Control = MyModules.Well_Node_Metes_and_Bound;
                    break;
                }

            case "Well Node Stratigraphy":
                {
                    e.Control = MyModules.Well_Node;
                    break;
                }

            case "Well Core":
                {
                    e.Control = MyModules.Well_Core;
                    break;
                }

            case "Well Core Analysis":
                {
                    e.Control = MyModules.Well_Core_Aanlysis;
                    break;
                }

            case "Well Core Analysis Sample":
                {
                    e.Control = MyModules.Well_Core_Aanlysis_Sample;
                    break;
                }

            case "Well Core Analysis Sample Description":
                {
                    e.Control = MyModules.Well_Core_Aanlysis_Sample_description;
                    break;
                }

            case "Well Core Analysis Sample Remarks":
                {
                    e.Control = MyModules.Well_Core_Aanlysis_Sample_remark;
                    break;
                }

            case "Well Core Analysis Method":
                {
                    e.Control = MyModules.Well_Core_Aanlysis_Method;
                    break;
                }

            case "Well Core Analysis Remark":
                {
                    e.Control = MyModules.Well_Core_Aanlysis_Remark;
                    break;
                }

            case "Well Core Description":
                {
                    e.Control = MyModules.Well_Core_Description;
                    break;
                }

            case "Well Core Description Stratigraphy":
                {
                    e.Control = MyModules.Well_Core_Description_Stratigraphy;
                    break;
                }

            case "Well Core Shift":
                {
                    e.Control = MyModules.Well_Core_Shift;
                    break;
                }

            case "Well Core Remark":
                {
                    e.Control = MyModules.Well_Core_Remark;
                    break;
                }

            case "Well Facility":
                {
                    e.Control = MyModules.Well_Facility;
                    break;
                }

            case "Well Interpretation":
                {
                    break;
                }

            case "Well BA Services":
                {
                    e.Control = MyModules.Well_BA_Services;
                    break;
                }


            // If (MyModules.Well_Logs Is Nothing) Then
            // MyModules.Well_Logs = New SimpleODM.Logmodule.uc_log( ref _SimpleODMConfig, SimpleUtil)
            // End If
            // If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Logs.MyUWI) Then
            // MyModules.Well_Logs.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
            // End If
            // Case "WELLLOGJOB"

            // If (MyModules.Well_Log_job Is Nothing) Then
            // MyModules.Well_Log_job = New SimpleODM.Logmodule.uc_log_job( ref _SimpleODMConfig, SimpleUtil)
            // End If
            // If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Log_job.MyUWI) Then
            // MyModules.Well_Log_job.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
            // End If


            case "Well Geometry":
                {
                    e.Control = MyModules.Well_Geometry;
                    break;
                }

            case "Well Support Facility":
                {
                    e.Control = MyModules.Well_Support_Facility;
                    break;
                }

            case "Well Permit":
                {
                    e.Control = MyModules.Well_Permit;
                    break;
                }

            case "Well Survey":
                {
                    e.Control = MyModules.WellDirSRVYManager;
                    break;
                }

            case "Well Survey Geometry":
                {
                    e.Control = MyModules.WellDirSRVYManager;
                    break;
                }

            case "Well Survey Station":
                {
                    e.Control = MyModules.WellDirSRVYManager;
                    break;
                }

            case "Well Land Rights":
                {
                    e.Control = MyModules.wellLandrights;
                    break;
                }

            case "Well version":
                {
                    e.Control = MyModules.Wellversion;
                    break;
                }

            case "Well Alias":
                {
                    e.Control = MyModules.Wellalias;
                    break;
                }

            case "Well License":
                {
                    e.Control = MyModules.welllicense;
                    break;
                }

            case "Well License Violation":
                {
                    e.Control = MyModules.welllicense_violation;
                    break;
                }

            case "Well License Condition":
                {
                    e.Control = MyModules.welllicense_cond;
                    break;
                }

            case "Well License Area":
                {
                    e.Control = MyModules.welllicense_area;
                    break;
                }

            case "Well License Remark":
                {
                    e.Control = MyModules.welllicense_remark;
                    break;
                }

            case "Well License Status":
                {
                    e.Control = MyModules.welllicense_status;
                    break;
                }
            // --------------------------------------End Well --------------------------------------------
            // '--------------------------------------  ETL Module -----------------------------------------------------------------
            // Case "Mapping From To"
            // If (MyModules.MappingData Is Nothing) Then
            // MyModules.MappingData = New SimpleODM.etlmodule.uc_MAppingFromSource2Target()
            // End If
            // MyModules.MappingData.parentEtl = ETLConfig
            // e.Control = MyModules.Table_Mapping
            // Case "Table Mapping"
            // If (MyModules.Table_Mapping Is Nothing) Then
            // MyModules.Table_Mapping = New SimpleODM.etlmodule.uc_TableMapping(SimpleODMConfig)
            // End If
            // MyModules.Table_Mapping.ETL = ETLConfig
            // e.Control = MyModules.Table_Mapping
            // Case "Spreadsheet Mapping"
            // If (MyModules.spreadsheet_mapping Is Nothing) Then
            // MyModules.spreadsheet_mapping = New SimpleODM.etlmodule.uc_ExcelMapping(SimpleODMConfig)
            // End If
            // MyModules.spreadsheet_mapping.ETL = ETLConfig
            // e.Control = MyModules.spreadsheet_mapping
            // Case "WorkFlow Creator"
            // If (MyModules.WorkFlowManager Is Nothing) Then
            // MyModules.WorkFlowManager = New SimpleODM.etlmodule.uc_WorkFlow(SimpleODMConfig)
            // End If
            // MyModules.WorkFlowManager.ParentETL = ETLConfig
            // e.Control = MyModules.WorkFlowManager
            // '-----------------------------------------------End of ETL ------------------------------------------------------------------------------
            // ------------------------------------------ Well test Screens ------------------------------------------
            case "Well Tests":
                {
                    if (MyModules.well_test is null)
                    {
                        MyModules.well_test = new SimpleODM.welltestandpressuremodule.uc_WellTest( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.well_test;
                    break;
                }

            case "Well Test Cushion":
                {
                    e.Control = MyModules.well_test_cushion;
                    break;
                }

            case "Well Test Analysis":
                {
                    e.Control = MyModules.well_test_analysis;
                    break;
                }

            case "Well Test Mud":
                {
                    e.Control = MyModules.well_test_Mud;
                    break;
                }

            case "Well Test Equipment":
                {
                    e.Control = MyModules.well_test_Equipment;
                    break;
                }

            case "Well Test Shutoff":
                {
                    e.Control = MyModules.well_test_Shutoff;
                    break;
                }

            case "Well Test Strat and Formation":
                {
                    e.Control = MyModules.well_test_StratUnit;
                    break;
                }

            case "Well Test Recorder":
                {
                    e.Control = MyModules.well_test_Recorder;
                    break;
                }

            case "Well Test Pressure":
                {
                    e.Control = MyModules.well_test_Press;
                    break;
                }

            case "Well Test Pressure Measurment":
                {
                    e.Control = MyModules.well_test_PressMeas;
                    break;
                }

            case "Well Test Flow":
                {
                    e.Control = MyModules.well_test_Flow;
                    break;
                }

            case "Well Test Flow Measurment":
                {
                    e.Control = MyModules.well_test_FlowMeas;
                    break;
                }

            case "Well Test Recovery":
                {
                    e.Control = MyModules.well_test_Recovery;
                    break;
                }

            case "Well Test Contaminant":
                {
                    e.Control = MyModules.well_test_Contaminant;
                    break;
                }

            case "Well Test Period":
                {
                    e.Control = MyModules.well_test_Period;
                    break;
                }

            case "Well Test Remark":
                {
                    e.Control = MyModules.well_test_Remarks;
                    break;
                }

            case "Well Test Gas Pressure AOF 4pt":
                {
                    e.Control = MyModules.well_Pressureaod4pt;
                    break;
                }

            case "Well Test Gas Pressure AOF":
                {
                    e.Control = MyModules.well_Pressureaof;
                    break;
                }

            case "Well Test Gas Pressure BH":
                {
                    e.Control = MyModules.well_PressureBH;
                    break;
                }

            case "Well Test Gas Pressure":
                {
                    e.Control = MyModules.well_Pressure;
                    break;
                }
            // ----------------------------------------- Well Gas Pressure Modules --------------------------------
            case "Well Gas Pressure AOF 4pt":
                {
                    e.Control = MyModules.well_Pressureaod4pt;
                    break;
                }

            case "Well Gas Pressure AOF":
                {
                    e.Control = MyModules.well_Pressureaof;
                    break;
                }

            case "Well Gas Pressure BH":
                {
                    e.Control = MyModules.well_PressureBH;
                    break;
                }

            case "Well Gas Pressure":
                {
                    e.Control = MyModules.well_Pressure;
                    break;
                }
            // ------------------------------------------ end Well test Screens ------------------------------------------
            // --------------------------------------------------------------- Well Designer ---------------------------
            case "Well Design/Build Management":
                {
                    e.Control = MyModules.wellboreDesigner;
                    break;
                }

            case "Well Origin":
                {
                    if (MyModules.wellorigin is null)
                    {
                        MyModules.wellorigin = new SimpleODM.wellmodule.uc_wellorigin( ref _SimpleODMConfig, SimpleUtil);
                    }

                    if (SimpleODMConfig.Defaults.WELL_XREFMyUWI != MyModules.wellorigin.WellSetUWI)
                    {
                        MyModules.wellorigin.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI);
                    }

                    e.Control = MyModules.wellorigin;
                    break;
                }

            case "Wellbore Tubulars":
                {
                    e.Control = MyModules.wellboreTubular;
                    break;
                }

            case "Wellbore Tubulars Cement":
                {
                    e.Control = MyModules.wellboretubularcement;
                    break;
                }

            case "Wellbore Payzone":
                {
                    e.Control = MyModules.wellborepayzone;
                    break;
                }

            case "Wellbore Zone Interval":
                {
                    e.Control = MyModules.wellborezoneinterval;
                    break;
                }

            case "Wellbore Zone Interval Value":
                {
                    e.Control = MyModules.wellborezoneintervalvalue;
                    break;
                }

            case "Wellbore Porous Interval":
                {
                    e.Control = MyModules.wellborePorousinterval;
                    break;
                }

            case "Wellbore Plugback":
                {
                    e.Control = MyModules.wellborePlugback;
                    break;
                }

            case "WellBore Information Management":
                {
                    e.Control = MyModules.wellbore;
                    break;
                }

            case "Well Production String":
                {
                    e.Control = MyModules.Productionstring;
                    break;
                }

            case "Well Formation":
                {
                    e.Control = MyModules.prodstringformation;
                    break;
                }

            case "Well Completion":
                {
                    e.Control = MyModules.well_completion;
                    break;
                }

            case "Well Completion Location and Well Cross Reference":
                {
                    e.Control = MyModules.well_completion_xref;
                    break;
                }

            case "Well Completion String and Formation Connection":
                {
                    e.Control = MyModules.well_completion_String2Formation_link;
                    break;
                }

            case "Well Perforation":
                {
                    e.Control = MyModules.wellperf;
                    break;
                }

            case "Completion Contact Intervals":
                {
                    e.Control = MyModules.wellperf;
                    break;
                }

            case "Prod. String Equipment":
                {
                    e.Control = MyModules.well_equipments;
                    break;
                }

            case "Wellbore Equipment":
                {
                    e.Control = MyModules.well_equipments;
                    break;
                }

            case "Install Equipment On Well":
                {
                    if (MyModules.well_equipments_search is null)
                    {
                        MyModules.well_equipments_search = new SimpleODM.wellmodule.uc_well_equipment_search( ref _SimpleODMConfig, SimpleUtil);
                        MyModules.well_equipments_search.Loaddata();
                    }
                    // If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_equipments_search.MyUWI) Then

                    e.Control = MyModules.well_equipments_search;
                    break;
                }

            case "Prod. String Connected Facilities":
                {
                    e.Control = MyModules.Well_Facility;
                    break;
                }

            case "Prod. String Formation Tests":
                {
                    e.Control = MyModules.well_test;
                    break;
                }
            // -------------------------------  End Well Desginer -------------------------------------------------
            // ------------------------------------------------------------------------------------------------------------------------------------------------------
            // -----------------------------------  Lithiology Modules -----------------------------------------------------------------------------------------------

            case "Lithology Management":
                {
                    if (MyModules.LithModule is null)
                    {
                        MyModules.LithModule = new SimpleODM.stratigraphyandlithologyModule.uc_NewEditLithology( ref _SimpleODMConfig, SimpleUtil);
                        MyModules.LithModule.LoadLith();
                    }

                    e.Control = MyModules.LithModule;
                    break;
                }

            // -----------------------------------  End Lithiology Modules ----------------------------------------------------------------------------------------------
            // --------------------------------------- Stratigraphy --------------------------------------------------------
            case "Stratigraphy Management":
                {
                    if (MyModules.StratigraphyModule is null)
                    {
                        MyModules.StratigraphyModule = new SimpleODM.stratigraphyandlithologyModule.uc_strat_name_set( ref _SimpleODMConfig, SimpleUtil);
                        MyModules.StratigraphyModule.Loaddata();
                    }

                    e.Control = MyModules.StratigraphyModule;
                    break;
                }

            case "Stratigraphy Column Management":
                {
                    if (MyModules.StratColumnsModule is null)
                    {
                        MyModules.StratColumnsModule = new SimpleODM.stratigraphyandlithologyModule.uc_strat_column( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.StratColumnsModule;
                    break;
                }

            case "Stratigraphic Name Set Cross Reference":
                {
                    e.Control = MyModules.strat_xref;
                    break;
                }

            case "Stratigraphic Field Station":
                {
                    if (MyModules.strat_field_station is null)
                    {
                        MyModules.strat_field_station = new SimpleODM.stratigraphyandlithologyModule.uc_strat_field_station( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.strat_field_station;
                    break;
                }

            case "Stratigraphic Field Station Interpreted Age":
                {
                    e.Control = MyModules.strat_fld_interp_age;
                    break;
                }

            case "Stratigraphic Field Node":
                {
                    e.Control = MyModules.strat_field_node;
                    break;
                }

            case "Stratigraphic Field Node Version":
                {
                    e.Control = MyModules.strat_field_node_version;
                    break;
                }

            case "Stratigraphic Field Section":
                {
                    e.Control = MyModules.strat_field_section;
                    break;
                }

            case "Stratigraphic Field Geometry":
                {
                    e.Control = MyModules.strat_field_geometry;
                    break;
                }

            case "Stratigraphic Field Acquisition":
                {
                    e.Control = MyModules.strat_field_acqtn;
                    break;
                }

            case "Stratigraphic Unit":
                {
                    e.Control = MyModules.strat_unit;
                    break;
                }

            case "Stratigraphic Unit Alias":
                {
                    e.Control = MyModules.strat_alias;
                    break;
                }

            case "Stratigraphic Unit Column Unit":
                {
                    e.Control = MyModules.strat_col_unit;
                    break;
                }

            case "Stratigraphic Unit Equivalence":
                {
                    e.Control = MyModules.strat_equivalance;
                    break;
                }

            case "Stratigraphic Unit Hierarchy":
                {
                    e.Control = MyModules.strat_hierarchy;
                    break;
                }

            case "Stratigraphic Unit Hierarchy Description":
                {
                    e.Control = MyModules.strat_hierarchy_desc;
                    break;
                }

            case "Stratigraphic Unit Age":
                {
                    e.Control = MyModules.strat_unit_age;
                    break;
                }

            case "Stratigraphic Unit Description":
                {
                    e.Control = MyModules.strat_unit_description;
                    break;
                }

            case "Stratigraphic Unit Topology":
                {
                    e.Control = MyModules.strat_topo_relation;
                    break;
                }

            case "Stratigraphic Column":
                {
                    if (MyModules.StratColumnsModule is null)
                    {
                        MyModules.StratColumnsModule = new SimpleODM.stratigraphyandlithologyModule.uc_strat_column( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.StratColumnsModule;
                    break;
                }

            case "Stratigraphic Column Age":
                {
                    e.Control = MyModules.strat_col_unit_age;
                    break;
                }

            case "Stratigraphic Column Acquisition":
                {
                    e.Control = MyModules.strat_col_acqtn;
                    break;
                }

            case "Stratigraphic Column Unit":
                {
                    e.Control = MyModules.strat_col_unit;
                    break;
                }

            case "Stratigraphic Column Cross Reference":
                {
                    e.Control = MyModules.strat_col_xref;
                    break;
                }

            case "Stratigraphic Well Section":
                {
                    e.Control = MyModules.strat_well_section;
                    break;
                }

            case "Stratigraphic Well Section Intrep. Age":
                {
                    e.Control = MyModules.strat_well_interp_age;
                    break;
                }

            case "Stratigraphic Well Section Acquisition":
                {
                    e.Control = MyModules.strat_well_acqtn;
                    break;
                }
            // --------------------------------------- End Stratigraphy --------------------------------------------------------

            case "WellBore Management":
                {
                    if (MyModules.WellBoreModule is null)
                    {
                        MyModules.WellBoreModule = new SimpleODM.wellmodule.uc_NewEditwellbore( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.WellBoreModule;
                    break;
                }

            case "Equipment Management":
                {
                    if (MyModules.EquipmentModule is null)
                    {
                        MyModules.EquipmentModule = new SimpleODM.supportmodule.uc_equipment( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.EquipmentModule;
                    break;
                }

            case "Classification Management":
                {
                    if (MyModules.ClassificationModule is null)
                    {
                        MyModules.ClassificationModule = new SimpleODM.classhierandreportmdoule.uc_NewEditClassification( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.ClassificationModule;
                    break;
                }
            // ------------------------------------------------------------------------------------------------------------------------------------------------------
            // -----------------------------------  Utilities Modules -----------------------------------------------------------------------------------------------
            case "Generic Table Manager":
                {
                    if (MyModules.rvmwithrtableslistModule is null)
                    {
                        MyModules.rvmwithrtableslistModule = new rvmwithrtableslist_ctl( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.rvmwithrtableslistModule;
                    break;
                }

            case "Bulk Data Loader":
                {
                    if (MyModules.BulkDataLoading is null)
                    {
                        MyModules.BulkDataLoading = new uc_tableloader( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.BulkDataLoading;
                    break;
                }

            case "Defaults and Properties":
                {
                    if (MyModules.defaultsSetup is null)
                    {
                        MyModules.defaultsSetup = new uc_defaults_settings( ref _SimpleODMConfig, SimpleUtil);
                        // MyModules.defaultsSetup.Setup( ref _SimpleODMConfig, SimpleUtil)
                    }

                    if (MyModules.rvmwithrtableslistModule is null)
                    {
                        MyModules.rvmwithrtableslistModule = new rvmwithrtableslist_ctl( ref _SimpleODMConfig, SimpleUtil);
                    }

                    if (MyModules.Administrator is null)
                    {
                        MyModules.Administrator = new SimpleODM.SharedLib.uc_db_admin( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.defaultsSetup;
                    break;
                }

            case "Database Connections":
                {
                    if (MyModules.db_def is null)
                    {
                        MyModules.db_def = new SimpleODM.SharedLib.uc_dbdefine(  _SimpleODMConfig, SimpleUtil);
                    }

                    MyModules.db_def.MyLoginControl = MyModules.uc_login1;
                    e.Control = MyModules.db_def;
                    break;
                }

            case "Database Constraint Manager":
                {
                    // If (MyModules.ConstraintsModule Is Nothing) Then
                    // MyModules.ConstraintsModule = New SimpleODM.etlmodule.uc_constraint_manager(SimpleODMConfig)

                    // End If
                    // e.Control = MyModules.ConstraintsModule


                    // Case "Export Data"
                    // If (MyModules.ExportData Is Nothing) Then
                    // MyModules.ExportData = New SimpleODM.etlmodule.uc_exportTableData(SimpleODMConfig)
                    // End If
                    e.Control = MyModules.Administrator;
                    break;
                }

            case "Administrator":
                {
                    if (MyModules.Administrator is null)
                    {
                        MyModules.Administrator = new SimpleODM.SharedLib.uc_db_admin( ref _SimpleODMConfig, SimpleUtil);
                    }

                    e.Control = MyModules.Administrator;
                    break;
                }

            case "Login":
                {
                    if (MyModules.uc_login1 is null)
                    {
                        MyModules.uc_login1 = new SimpleODM.SharedLib.uc_login(  _SimpleODMConfig, SimpleUtil);
                    }

                    MyModules.uc_login1.LoadDBSchemas(ref _SimpleODMConfig);

                    // ------------------------------------------------------------------------------------------------------------------------------------------------------

                    e.Control = MyModules.uc_login1;
                    break;
                }

            default:
                {
                    e.Control = MyModules.no_priv;
                    break;
                }
        }

        // End If

        // If Not WindowsUIView1 Is Nothing Then
        // WindowsUIView1.ActiveContentContainer.Caption = SimpleODMconfig.Defaults.APPLICATIONNAME
        // End If



    }

    /* TODO ERROR: Skipped EndRegionDirectiveTrivia *//* TODO ERROR: Skipped EndRegionDirectiveTrivia *//* TODO ERROR: Skipped RegionDirectiveTrivia */
    public event LoginSucccessEventHandler LoginSucccess;

    public delegate void LoginSucccessEventHandler();

    public event LogoutEventHandler Logout;

    public delegate void LogoutEventHandler();

    public event LoginCancelEventHandler LoginCancel;

    public delegate void LoginCancelEventHandler();

    public event ShowDatabaseEventHandler ShowDatabase;

    public delegate void ShowDatabaseEventHandler();

    private void uc_login_LoginCancel()
    {
        LoginCancel?.Invoke();
    }

    public void PerformLoginProcedures()
    {
        DidIloginStatus = true;
        DidIInitForms = false;
        if (!DidIInitForms & DidIloginStatus)
        {
            if (SimpleODMConfig.PPDMContext.ConnectionType == ConnectionTypeEnum.User)
            {
                SplashScreenManager.ShowForm(typeof(WaitForm1));
                try
                {
                    SimpleODMConfig.PPDMContext.CreateEntities();
                }
                catch (Exception ex)
                {
                    SplashScreenManager.CloseForm();
                }

                try
                {
                    SimpleODMConfig.PPDMContext.GetPrimaryLOV();
                }
                // SimpleODMconfig.ppdmcontext.DMLScripter = New cls_scriptsGenerator(SimpleODMConfig)
                // SimpleODMconfig.ppdmcontext.DMLScripter.FillScriptUnits()
                catch (Exception ex)
                {
                    DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
                    XtraMessageBox.Show("Cannot Load Primary LOV Tables");
                }

                SplashScreenManager.CloseForm();
               // SimpleODMConfig.PPDMContext.LazyLoadingONOFF(true);
                // Try
                // ETLConfig = New SimpleODM.etlmodule.cls_ETL(SimpleODMConfig)
                // ETLconfig.ppdmcontext.SimpleConfig = SimpleODMConfig
                // ETLconfig.ppdmcontext.OpenConnection()
                // DidIInitForms = True
                // Catch ex As Exception
                // MessageBox.Show("Simple ODM ETL Componenets Not Installed")

                // End Try
                DidIInitForms = true;
            }
            else
            {
                InitDBAAdminForms();
            }
        }
    }

    private void uc_login_LoginSucccess()
    {
        PerformLoginProcedures();
        SimpleUtil.SimpleODMConfig = SimpleODMConfig;
        if (SimpleUtil.LOVTablesList.Count == 0)
        {
            if (SimpleODMConfig.healthchk.Tables == true)
            {
                SimpleUtil.CacheMostAccessedLOV();
            }
        }

        LoginSucccess?.Invoke();
    }

    private void uc_login_Logout()
    {
        Logout?.Invoke();
    }

    private void uc_login_ShowDatabase()
    {
        ShowDatabase?.Invoke();
    }

    /* TODO ERROR: Skipped EndRegionDirectiveTrivia */


}