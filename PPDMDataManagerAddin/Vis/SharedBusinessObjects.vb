Imports SimpleODM.SharedLib
Imports System.ComponentModel
Imports DevExpress.LookAndFeel
Imports System.Globalization
Imports System.Threading

Imports SimpleODM.systemconfigandutil
Imports DevExpress.XtraBars.Docking2010.Views.WindowsUI
Imports DevExpress.XtraEditors
Imports DevExpress.XtraSplashScreen
Imports SimpleODM.Logmodule


Partial Public Class MyForms
    Public WithEvents SimpleODMConfig As SimpleODM.systemconfigandutil.PPDMConfig
    Public WithEvents WellDirSRVYManager As SimpleODM.wellmodule.uc_wellDirSurvey
    Public WithEvents WellTestManager As SimpleODM.welltestandpressuremodule.uc_WellTest

    Public WithEvents ClassificationModule As SimpleODM.classhierandreportmdoule.uc_NewEditClassification
    Public WithEvents AreaModule As SimpleODM.supportmodule.uc_area

    Public WithEvents EquipmentModule As SimpleODM.supportmodule.uc_equipment
    Public WithEvents FacilityModule As SimpleODM.facilitymodule.uc_facility

    Public WithEvents WellModule As SimpleODM.wellmodule.uc_NewEditWell
    Public WithEvents Wellversion As SimpleODM.wellmodule.uc_wellversion
    Public WithEvents Wellalias As SimpleODM.wellmodule.uc_well_alias
    Public WithEvents Wellarea As SimpleODM.wellmodule.uc_well_area
    Public WithEvents wellorigin As SimpleODM.wellmodule.uc_wellorigin
    Public WithEvents Wellremark As SimpleODM.wellmodule.uc_well_remark
    Public WithEvents Wellmisc As SimpleODM.wellmodule.uc_well_misc_data
    Public WithEvents WellFacility As SimpleODM.wellmodule.uc_well_facility
    Public WithEvents Well_Activity As SimpleODM.wellmodule.uc_well_activity
    Public WithEvents Well_Activity_Conditions_and_Events As SimpleODM.wellmodule.uc_well_activities_cause
    Public WithEvents Well_Activity_Duration As SimpleODM.wellmodule.uc_well_activities_duration
    Public WithEvents Well_Node As SimpleODM.wellmodule.uc_well_node
    Public WithEvents Well_Node_Area As SimpleODM.wellmodule.uc_well_node_area
    Public WithEvents Well_Node_Geometry As SimpleODM.wellmodule.uc_well_node_geometry
    Public WithEvents Well_Node_Metes_and_Bound As SimpleODM.wellmodule.uc_well_node_metesandbound
    Public WithEvents Well_Node_Stratigraphy As SimpleODM.wellmodule.uc_well_node_stratunit
    Public WithEvents Well_Core As SimpleODM.wellmodule.uc_well_core
    Public WithEvents Well_Core_Aanlysis As SimpleODM.wellmodule.uc_well_core_analysis
    Public WithEvents Well_Core_Aanlysis_Sample As SimpleODM.wellmodule.uc_well_core_analysis_sample
    Public WithEvents Well_Core_Aanlysis_Sample_description As SimpleODM.wellmodule.uc_well_core_analysis_sample_description
    Public WithEvents Well_Core_Aanlysis_Sample_remark As SimpleODM.wellmodule.uc_well_core_analysis_sample_remark
    Public WithEvents Well_Core_Aanlysis_Method As SimpleODM.wellmodule.uc_well_core_analysis_method
    Public WithEvents Well_Core_Aanlysis_Remark As SimpleODM.wellmodule.uc_well_core_analysis_remark
    Public WithEvents Well_Core_Description As SimpleODM.wellmodule.uc_well_core_description
    Public WithEvents Well_Core_Description_Stratigraphy As SimpleODM.wellmodule.uc_well_core_description_strat_unit
    Public WithEvents Well_Core_Shift As SimpleODM.wellmodule.uc_well_core_shift
    Public WithEvents Well_Core_Remark As SimpleODM.wellmodule.uc_well_core_remark
    Public WithEvents Well_Facility As SimpleODM.wellmodule.uc_well_facility
    Public WithEvents Well_BA_Services As SimpleODM.wellmodule.uc_well_ba_services
    Public WithEvents Well_Geometry As SimpleODM.wellmodule.uc_well_geometry
    Public WithEvents Well_Interpretation As SimpleODM.wellmodule.uc_well_Licenses
    Public WithEvents Well_Dictionaries As SimpleODM.wellmodule.uc_well_Licenses

    Public WithEvents Well_Support_Facility As SimpleODM.wellmodule.uc_well_support_facility
    Public WithEvents Well_Permit As SimpleODM.wellmodule.uc_well_License_permit_Types
    Public WithEvents welllicense As SimpleODM.wellmodule.uc_well_Licenses
    Public WithEvents welllicense_violation As SimpleODM.wellmodule.uc_well_license_violation
    Public WithEvents welllicense_status As SimpleODM.wellmodule.uc_well_license_status
    Public WithEvents welllicense_remark As SimpleODM.wellmodule.uc_well_license_remark
    Public WithEvents welllicense_cond As SimpleODM.wellmodule.uc_well_license_cond
    Public WithEvents welllicense_area As SimpleODM.wellmodule.uc_well_license_area
    Public WithEvents wellLandrights As SimpleODM.wellmodule.uc_well_Landrights
    Public WithEvents well_mud_sample As SimpleODM.wellmodule.uc_well_mud_sample
    Public WithEvents well_mud_sample_property As SimpleODM.wellmodule.uc_well_mud_sample_property
    Public WithEvents well_mud_sample_resistivity As SimpleODM.wellmodule.uc_well_mud_sample_resistivity
    Public WithEvents WellBoreModule As SimpleODM.wellmodule.uc_NewEditwellbore
    '----------- Well Bore Controls -------------------------------------------
    Public WithEvents wellboreDesigner As SimpleODM.wellmodule.uc_WellDesigner
    Public WithEvents wellboreTubular As SimpleODM.wellmodule.uc_wellboretubular
    Public WithEvents wellbore As SimpleODM.wellmodule.uc_NewEditwellbore
    Public WithEvents Productionstring As SimpleODM.wellmodule.uc_prodstring
    Public WithEvents prodstringformation As SimpleODM.wellmodule.uc_prodstringformation
    Public WithEvents well_completion As SimpleODM.wellmodule.uc_completion
    Public WithEvents well_completion_xref As SimpleODM.wellmodule.uc_completion_xref
    Public WithEvents well_completion_String2Formation_link As SimpleODM.wellmodule.uc_completion_String2Formation_link
    Public WithEvents wellperf As SimpleODM.wellmodule.uc_preforation
    Public WithEvents well_equipments As SimpleODM.wellmodule.uc_well_equipment
    Public WithEvents well_equipments_search As SimpleODM.wellmodule.uc_well_equipment_search

    Public WithEvents wellborepayzone As SimpleODM.wellmodule.uc_wellborepayzone
    Public WithEvents wellborePlugback As SimpleODM.wellmodule.uc_wellborePlugback
    Public WithEvents wellborePorousinterval As SimpleODM.wellmodule.uc_wellborePorousinterval
    Public WithEvents wellborezoneinterval As SimpleODM.wellmodule.uc_wellborezoneinterval
    Public WithEvents wellborezoneintervalvalue As SimpleODM.wellmodule.uc_wellborezoneintervalvalue
    Public WithEvents wellboretubularcement As SimpleODM.wellmodule.uc_wellboretubularCement
    '-------------------------- Drilling Controls -----------------------------------
    Public WithEvents well_show As SimpleODM.wellmodule.uc_well_show
    Public WithEvents well_show_remark As SimpleODM.wellmodule.uc_well_show_remark
    Public WithEvents well_air_drill As SimpleODM.wellmodule.uc_well_air_drill
    Public WithEvents well_air_drill_Interval As SimpleODM.wellmodule.uc_well_air_drill_Interval
    Public WithEvents well_air_drill_interval_period As SimpleODM.wellmodule.uc_well_air_drill_interval_period
    Public WithEvents well_Horiz_Drill As SimpleODM.wellmodule.uc_well_Horiz_Drill
    Public WithEvents well_Horiz_Drill_drill_kop As SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_kop
    Public WithEvents well_Horiz_Drill_drill_poe As SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_poe
    Public WithEvents well_Horiz_Drill_drill_spoke As SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_KOP_spoke
    '------------------------ Well test Controls ----------------------------------
    Public WithEvents well_test As SimpleODM.welltestandpressuremodule.uc_WellTest
    Public WithEvents well_test_analysis As SimpleODM.welltestandpressuremodule.uc_welltestAnalysis
    Public WithEvents well_test_cushion As SimpleODM.welltestandpressuremodule.uc_WellTestCushion
    Public WithEvents well_test_Period As SimpleODM.welltestandpressuremodule.uc_welltest_period
    Public WithEvents well_test_Contaminant As SimpleODM.welltestandpressuremodule.uc_welltestContaminant
    Public WithEvents well_test_Equipment As SimpleODM.welltestandpressuremodule.uc_welltestEquipment
    Public WithEvents well_test_Flow As SimpleODM.welltestandpressuremodule.uc_welltestFlow
    Public WithEvents well_test_FlowMeas As SimpleODM.welltestandpressuremodule.uc_welltestFlowMeas
    Public WithEvents well_test_Mud As SimpleODM.welltestandpressuremodule.uc_welltestMud
    Public WithEvents well_test_Press As SimpleODM.welltestandpressuremodule.uc_welltestPress
    Public WithEvents well_test_PressMeas As SimpleODM.welltestandpressuremodule.uc_welltestPressMeas
    Public WithEvents well_test_Recorder As SimpleODM.welltestandpressuremodule.uc_welltestRecorder
    Public WithEvents well_test_Recovery As SimpleODM.welltestandpressuremodule.uc_welltestRecovery
    Public WithEvents well_test_Remarks As SimpleODM.welltestandpressuremodule.uc_welltestRemarks
    Public WithEvents well_test_Shutoff As SimpleODM.welltestandpressuremodule.uc_welltestShutoff
    Public WithEvents well_test_StratUnit As SimpleODM.welltestandpressuremodule.uc_welltestStratunit
    Public WithEvents well_Pressure As SimpleODM.welltestandpressuremodule.uc_well_pressure
    Public WithEvents well_Pressureaof As SimpleODM.welltestandpressuremodule.uc_well_pressure_aof
    Public WithEvents well_Pressureaod4pt As SimpleODM.welltestandpressuremodule.uc_well_pressure_aof_4pt
    Public WithEvents well_PressureBH As SimpleODM.welltestandpressuremodule.uc_well_pressure_bh
    '--------------------------------------------------------------------------
    ' Public WithEvents CatalogueEquip As SimpleODM.supportmodule.uc_NewEditCatEquipment
    'Public WithEvents ConstraintsModule As SimpleODM.etlmodule.uc_constraint_manager
    'Public WithEvents WellScoreCard As SimpleODM.wellmodule.uc_wellscorecard
    ' Public WithEvents ExportData As SimpleODM.etlmodule.uc_exportTableData
    '----------------- ETL Usercontrols -------------------------
    'Public WithEvents LoadFilesinDirectory As SimpleODM.etlmodule.uc_LoadFileIndirectory
    'Public WithEvents spreadsheet_mapping As SimpleODM.etlmodule.uc_ExcelMapping
    'Public WithEvents MappingData As SimpleODM.etlmodule.uc_MAppingFromSource2Target

    'Public WithEvents WorkFlowManager As SimpleODM.etlmodule.uc_WorkFlow
    'Public WithEvents WebService_Mapping As SimpleODM.etlmodule.uc_WebServiceMapping
    'Public WithEvents Table_Mapping As SimpleODM.etlmodule.uc_TableMapping
    'Public WithEvents WorkFlowRun As SimpleODM.wellmodule.uc_completion
    '------------------------------------------------------------------
    Public WithEvents LithModule As SimpleODM.stratigraphyandlithologyModule.uc_NewEditLithology
    '--------------------------- Stratigraphy Modules ------------------------------------------------------
    Public WithEvents StratigraphyModule As SimpleODM.stratigraphyandlithologyModule.uc_strat_name_set
    Public WithEvents strat_unit As SimpleODM.stratigraphyandlithologyModule.uc_strat_unit
    Public WithEvents strat_xref As SimpleODM.stratigraphyandlithologyModule.uc_strat_name_set_xref
    Public WithEvents strat_alias As SimpleODM.stratigraphyandlithologyModule.uc_strat_alias
    Public WithEvents strat_topo_relation As SimpleODM.stratigraphyandlithologyModule.uc_strat_topo_relation
    Public WithEvents strat_unit_age As SimpleODM.stratigraphyandlithologyModule.uc_strat_unit_age
    Public WithEvents strat_unit_description As SimpleODM.stratigraphyandlithologyModule.uc_strat_unit_description
    Public WithEvents strat_acqtn_method As SimpleODM.stratigraphyandlithologyModule.uc_strat_acqtn_method
    Public WithEvents strat_interp_corr As SimpleODM.stratigraphyandlithologyModule.uc_strat_interp_corr
    Public WithEvents strat_equivalance As SimpleODM.stratigraphyandlithologyModule.uc_strat_equivalance
    Public WithEvents strat_hierarchy As SimpleODM.stratigraphyandlithologyModule.uc_strat_hierarchy
    Public WithEvents strat_hierarchy_desc As SimpleODM.stratigraphyandlithologyModule.uc_strat_hierarchy_desc
    Public WithEvents StratColumnsModule As SimpleODM.stratigraphyandlithologyModule.uc_strat_column
    Public WithEvents strat_col_acqtn As SimpleODM.stratigraphyandlithologyModule.uc_strat_col_acqtn
    Public WithEvents strat_col_unit As SimpleODM.stratigraphyandlithologyModule.uc_strat_col_unit
    Public WithEvents strat_col_unit_age As SimpleODM.stratigraphyandlithologyModule.uc_strat_col_unit_age
    Public WithEvents strat_col_xref As SimpleODM.stratigraphyandlithologyModule.uc_strat_col_xref
    Public WithEvents strat_field_station As SimpleODM.stratigraphyandlithologyModule.uc_strat_field_station
    Public WithEvents strat_field_section As SimpleODM.stratigraphyandlithologyModule.uc_strat_field_section
    Public WithEvents strat_field_node As SimpleODM.stratigraphyandlithologyModule.uc_strat_field_node
    Public WithEvents strat_field_geometry As SimpleODM.stratigraphyandlithologyModule.uc_strat_field_geometry
    Public WithEvents strat_field_acqtn As SimpleODM.stratigraphyandlithologyModule.uc_strat_field_acqtn
    Public WithEvents strat_fld_interp_age As SimpleODM.stratigraphyandlithologyModule.uc_strat_fld_interp_age
    Public WithEvents strat_field_node_version As SimpleODM.stratigraphyandlithologyModule.uc_strat_node_version
    Public WithEvents strat_well_section As SimpleODM.stratigraphyandlithologyModule.uc_strat_well_section
    Public WithEvents strat_well_acqtn As SimpleODM.stratigraphyandlithologyModule.uc_strat_well_acqtn
    Public WithEvents strat_well_interp_age As SimpleODM.stratigraphyandlithologyModule.uc_well_strat_interp_age
    '--------------------------------------------------------------------------------------------------------------
    '-------------------------------------------Business Associate ------------------------------------------------
    Public WithEvents businessAssociate As SimpleODM.BusinessAssociateModule.uc_businessAssociate
    Public WithEvents businessAssociate_address As SimpleODM.BusinessAssociateModule.uc_businessAssociate_address
    Public WithEvents businessAssociate_alias As SimpleODM.BusinessAssociateModule.uc_businessAssociate_alias
    Public WithEvents businessAssociate_authority As SimpleODM.BusinessAssociateModule.uc_businessAssociate_authority
    Public WithEvents businessAssociate_consortuimservice As SimpleODM.BusinessAssociateModule.uc_businessAssociate_consortuimservice
    Public WithEvents businessAssociate_contactinfo As SimpleODM.BusinessAssociateModule.uc_businessAssociate_contactinfo
    Public WithEvents businessAssociate_crew As SimpleODM.BusinessAssociateModule.uc_businessAssociate_crew
    Public WithEvents businessAssociate_crew_memeber As SimpleODM.BusinessAssociateModule.uc_businessAssociate_crew_memeber
    Public WithEvents businessAssociate_crosspreference As SimpleODM.BusinessAssociateModule.uc_businessAssociate_crosspreference
    Public WithEvents businessAssociate_description As SimpleODM.BusinessAssociateModule.uc_businessAssociate_description
    Public WithEvents businessAssociate_employee As SimpleODM.BusinessAssociateModule.uc_businessAssociate_employee
    Public WithEvents businessAssociate_license As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license
    Public WithEvents businessAssociate_license_alias As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_alias
    Public WithEvents businessAssociate_license_area As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_area
    Public WithEvents businessAssociate_license_cond As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond
    Public WithEvents businessAssociate_license_cond_code As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond_code
    Public WithEvents businessAssociate_license_cond_type As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond_type
    Public WithEvents businessAssociate_license_remark As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_remark
    Public WithEvents businessAssociate_license_status As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_status
    Public WithEvents businessAssociate_license_type As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_type
    Public WithEvents businessAssociate_license_violation As SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_violation
    Public WithEvents businessAssociate_organization As SimpleODM.BusinessAssociateModule.uc_businessAssociate_organization
    Public WithEvents businessAssociate_permit As SimpleODM.BusinessAssociateModule.uc_businessAssociate_permit
    Public WithEvents businessAssociate_preference As SimpleODM.BusinessAssociateModule.uc_businessAssociate_preference
    Public WithEvents businessAssociate_services As SimpleODM.BusinessAssociateModule.uc_businessAssociate_services
    Public WithEvents businessAssociate_services_address As SimpleODM.BusinessAssociateModule.uc_businessAssociate_services_address
    Public WithEvents entitlement As SimpleODM.BusinessAssociateModule.uc_entitlement
    Public WithEvents ent_components As SimpleODM.BusinessAssociateModule.uc_ent_components
    Public WithEvents entitlement_group As SimpleODM.BusinessAssociateModule.uc_entitlement_group
    Public WithEvents entitlement_security_ba As SimpleODM.BusinessAssociateModule.uc_entitlement_security_ba
    Public WithEvents entitlement_security_group As SimpleODM.BusinessAssociateModule.uc_entitlement_security_group
    Public WithEvents entitlement_security_group_xref As SimpleODM.BusinessAssociateModule.uc_entitlement_security_group_xref
    '-------------------------------------------End Business Associate --------------------------------------------
    Public WithEvents equipment As SimpleODM.supportmodule.uc_equipment
    Public WithEvents equipment_alias As SimpleODM.supportmodule.uc_equipment_alias
    Public WithEvents equipment_ba As SimpleODM.supportmodule.uc_equipment_ba
    Public WithEvents equipment_spec As SimpleODM.supportmodule.uc_equipment_spec
    Public WithEvents equipment_spec_set As SimpleODM.supportmodule.uc_equipment_spec_set
    Public WithEvents equipment_spec_set_spec As SimpleODM.supportmodule.uc_equipment_spec_set_spec
    Public WithEvents equipment_crossreference As SimpleODM.supportmodule.uc_equipment_crossreference
    Public WithEvents equipment_maintain As SimpleODM.supportmodule.uc_equipment_maintain
    Public WithEvents equipment_maintain_status As SimpleODM.supportmodule.uc_equipment_maintain_status
    Public WithEvents equipment_maintain_type As SimpleODM.supportmodule.uc_equipment_maintain_type
    Public WithEvents equipment_status As SimpleODM.supportmodule.uc_equipment_status
    Public WithEvents equipment_use_stat As SimpleODM.supportmodule.uc_equipment_use_stat
    '------------------------------------------ Facilities ----------------------------------------------------
    Public WithEvents facility_alias As SimpleODM.facilitymodule.uc_facility_alias
    Public WithEvents facility_area As SimpleODM.facilitymodule.uc_facility_area
    Public WithEvents facility_ba_service As SimpleODM.facilitymodule.uc_facility_ba_service
    Public WithEvents facility_class As SimpleODM.facilitymodule.uc_facility_class
    Public WithEvents facility_description As SimpleODM.facilitymodule.uc_facility_description
    Public WithEvents facility_equipment As SimpleODM.facilitymodule.uc_facility_equipment
    Public WithEvents facility_field As SimpleODM.facilitymodule.uc_facility_field
    Public WithEvents facility_license As SimpleODM.facilitymodule.uc_facility_license
    Public WithEvents facility_license_alias As SimpleODM.facilitymodule.uc_facility_license_alias
    Public WithEvents facility_license_area As SimpleODM.facilitymodule.uc_facility_license_area
    Public WithEvents facility_license_cond As SimpleODM.facilitymodule.uc_facility_license_cond
    Public WithEvents facility_license_remark As SimpleODM.facilitymodule.uc_facility_license_remark
    Public WithEvents facility_license_status As SimpleODM.facilitymodule.uc_facility_license_status
    Public WithEvents facility_license_type As SimpleODM.facilitymodule.uc_facility_license_type
    Public WithEvents facility_license_violation As SimpleODM.facilitymodule.uc_facility_license_violation
    Public WithEvents facility_maintain As SimpleODM.facilitymodule.uc_facility_maintain
    Public WithEvents facility_maintain_status As SimpleODM.facilitymodule.uc_facility_maintain_status
    Public WithEvents facility_rate As SimpleODM.facilitymodule.uc_facility_rate
    Public WithEvents facility_restriction As SimpleODM.facilitymodule.uc_facility_restriction
    Public WithEvents facility_status As SimpleODM.facilitymodule.uc_facility_status
    Public WithEvents facility_substance As SimpleODM.facilitymodule.uc_facility_substance
    Public WithEvents facility_version As SimpleODM.facilitymodule.uc_facility_version
    Public WithEvents facility_xref As SimpleODM.facilitymodule.uc_facility_xref
    '------------------------------------ Catalog -------------------------------
    Public WithEvents cat_equipment As SimpleODM.supportmodule.uc_Cat_equipment
    Public WithEvents Cat_equipment_alias As SimpleODM.supportmodule.uc_Cat_equipment_alias
    Public WithEvents Cat_equipment_spec As SimpleODM.supportmodule.uc_Cat_equipment_spec
    Public WithEvents cat_additive As SimpleODM.supportmodule.uc_cat_additive
    Public WithEvents cat_additive_alias As SimpleODM.supportmodule.uc_cat_additive_alias
    Public WithEvents cat_additive_spec As SimpleODM.supportmodule.uc_cat_additive_spec
    Public WithEvents cat_additive_type As SimpleODM.supportmodule.uc_cat_additive_type
    Public WithEvents cat_additive_xref As SimpleODM.supportmodule.uc_cat_additive_xref
    '------------------------- --- Applications ------------------------------

    Public WithEvents application4auth As SimpleODM.supportmodule.uc_application
    Public WithEvents application_alias As SimpleODM.supportmodule.uc_application_alias
    Public WithEvents application_area As SimpleODM.supportmodule.uc_application_area
    Public WithEvents application_attach As SimpleODM.supportmodule.uc_application_attach

    Public WithEvents application_ba As SimpleODM.supportmodule.uc_application_ba
    Public WithEvents application_desc As SimpleODM.supportmodule.uc_application_desc
    Public WithEvents application_remark As SimpleODM.supportmodule.uc_application_remark
    '----------------------------- Area ---------------------------------------------
    Public WithEvents Area As SimpleODM.supportmodule.uc_area
    Public WithEvents area_alias As SimpleODM.supportmodule.uc_area_alias
    Public WithEvents area_contain As SimpleODM.supportmodule.uc_area_contain
    Public WithEvents area_description As SimpleODM.supportmodule.uc_area_description
    '------------------------------ Pool --------------------------------------------

    Public WithEvents Pool As SimpleODM.reservepoolproductionmodule.uc_pool
    Public WithEvents pool_alias As SimpleODM.reservepoolproductionmodule.uc_pool_alias
    Public WithEvents pool_area As SimpleODM.reservepoolproductionmodule.uc_pool_area
    Public WithEvents pool_instrument As SimpleODM.reservepoolproductionmodule.uc_pool_instrument
    Public WithEvents pool_version As SimpleODM.reservepoolproductionmodule.uc_pool_version


    '------------------------------ Production Entities --------------------------------------------
    Public WithEvents pden As SimpleODM.reservepoolproductionmodule.uc_pden
    Public WithEvents pden_alloc_factor As SimpleODM.reservepoolproductionmodule.uc_pden_alloc_factor
    Public WithEvents pden_area As SimpleODM.reservepoolproductionmodule.uc_pden_area
    Public WithEvents pden_business_assoc As SimpleODM.reservepoolproductionmodule.uc_pden_business_assoc
    Public WithEvents pden_decline_case As SimpleODM.reservepoolproductionmodule.uc_pden_decline_case
    Public WithEvents pden_decline_condition As SimpleODM.reservepoolproductionmodule.uc_pden_decline_condition
    Public WithEvents pden_decline_segment As SimpleODM.reservepoolproductionmodule.uc_pden_decline_segment
    Public WithEvents pden_facility As SimpleODM.reservepoolproductionmodule.uc_pden_facility
    Public WithEvents pden_field As SimpleODM.reservepoolproductionmodule.uc_pden_field
    Public WithEvents pden_flow_measurement As SimpleODM.reservepoolproductionmodule.uc_pden_flow_measurement
    Public WithEvents pden_in_area As SimpleODM.reservepoolproductionmodule.uc_pden_in_area
    Public WithEvents pden_land_right As SimpleODM.reservepoolproductionmodule.uc_pden_land_right
    Public WithEvents pden_lease_unit As SimpleODM.reservepoolproductionmodule.uc_pden_lease_unit
    Public WithEvents pden_material_bal As SimpleODM.reservepoolproductionmodule.uc_pden_material_bal
    Public WithEvents pden_oper_hist As SimpleODM.reservepoolproductionmodule.uc_pden_oper_hist
    Public WithEvents pden_other As SimpleODM.reservepoolproductionmodule.uc_pden_other
    Public WithEvents pden_pool As SimpleODM.reservepoolproductionmodule.uc_pden_pool
    Public WithEvents pden_pr_str_allowable As SimpleODM.reservepoolproductionmodule.uc_pden_pr_str_allowable
    Public WithEvents pden_pr_str_form As SimpleODM.reservepoolproductionmodule.uc_pden_pr_str_form
    Public WithEvents pden_prod_string As SimpleODM.reservepoolproductionmodule.uc_pden_prod_string
    Public WithEvents pden_prod_string_xref As SimpleODM.reservepoolproductionmodule.uc_pden_prod_string_xref
    Public WithEvents pden_resent As SimpleODM.reservepoolproductionmodule.uc_pden_resent
    Public WithEvents pden_resent_class As SimpleODM.reservepoolproductionmodule.uc_pden_resent_class
    Public WithEvents pden_status_hist As SimpleODM.reservepoolproductionmodule.uc_pden_status_hist
    Public WithEvents pden_summary As SimpleODM.reservepoolproductionmodule.uc_pden_summary
    Public WithEvents pden_vol_disposition As SimpleODM.reservepoolproductionmodule.uc_pden_vol_disposition
    Public WithEvents pden_vol_regime As SimpleODM.reservepoolproductionmodule.uc_pden_vol_regime
    Public WithEvents pden_vol_summ_other As SimpleODM.reservepoolproductionmodule.uc_pden_vol_summ_other
    Public WithEvents pden_volume_analysis As SimpleODM.reservepoolproductionmodule.uc_pden_volume_analysis
    Public WithEvents pden_well As SimpleODM.reservepoolproductionmodule.uc_pden_well
    Public WithEvents pden_xref As SimpleODM.reservepoolproductionmodule.uc_pden_xref
    '------------------------------ Reserve and Classification --------------------------------------------
    Public WithEvents resent_class As SimpleODM.reservepoolproductionmodule.uc_resent_class
    Public WithEvents resent_eco_run As SimpleODM.reservepoolproductionmodule.uc_resent_eco_run
    Public WithEvents resent_eco_schedule As SimpleODM.reservepoolproductionmodule.uc_resent_eco_schedule
    Public WithEvents resent_eco_volume As SimpleODM.reservepoolproductionmodule.uc_resent_eco_volume
    Public WithEvents resent_prod_property As SimpleODM.reservepoolproductionmodule.uc_resent_prod_property
    Public WithEvents resent_product As SimpleODM.reservepoolproductionmodule.uc_resent_product
    Public WithEvents resent_revision_cat As SimpleODM.reservepoolproductionmodule.uc_resent_revision_cat
    Public WithEvents resent_vol_regime As SimpleODM.reservepoolproductionmodule.uc_resent_vol_regime
    Public WithEvents resent_vol_revision As SimpleODM.reservepoolproductionmodule.uc_resent_vol_revision
    Public WithEvents resent_vol_summary As SimpleODM.reservepoolproductionmodule.uc_resent_vol_summary
    Public WithEvents resent_xref As SimpleODM.reservepoolproductionmodule.uc_resent_xref
    Public WithEvents reserve_class As SimpleODM.reservepoolproductionmodule.uc_reserve_class
    Public WithEvents reserve_class_calc As SimpleODM.reservepoolproductionmodule.uc_reserve_class_calc
    Public WithEvents reserve_class_formula As SimpleODM.reservepoolproductionmodule.uc_reserve_class_formula
    Public WithEvents reserve_entity As SimpleODM.reservepoolproductionmodule.uc_reserve_entity
    '------------------------------------------------------------------------------------------------------
    '------------------------------------ Lithology -------------------------------------------------------
    Public WithEvents lith_comp_grain_size As SimpleODM.stratigraphyandlithologyModule.uc_lith_comp_grain_size
    Public WithEvents lith_component As SimpleODM.stratigraphyandlithologyModule.uc_lith_component
    Public WithEvents lith_component_color As SimpleODM.stratigraphyandlithologyModule.uc_lith_component_color
    Public WithEvents lith_dep_ent_int As SimpleODM.stratigraphyandlithologyModule.uc_lith_dep_ent_int
    Public WithEvents lith_desc_other As SimpleODM.stratigraphyandlithologyModule.uc_lith_desc_other
    Public WithEvents lith_diagenesis As SimpleODM.stratigraphyandlithologyModule.uc_lith_diagenesis
    Public WithEvents lith_grain_size As SimpleODM.stratigraphyandlithologyModule.uc_lith_grain_size
    Public WithEvents lith_interval As SimpleODM.stratigraphyandlithologyModule.uc_lith_interval
    Public WithEvents lith_log As SimpleODM.stratigraphyandlithologyModule.uc_lith_log
    Public WithEvents lith_log_ba_service As SimpleODM.stratigraphyandlithologyModule.uc_lith_log_ba_service
    Public WithEvents lith_log_remark As SimpleODM.stratigraphyandlithologyModule.uc_lith_log_remark
    Public WithEvents lith_measured_sec As SimpleODM.stratigraphyandlithologyModule.uc_lith_measured_sec
    Public WithEvents lith_porosity As SimpleODM.stratigraphyandlithologyModule.uc_lith_porosity
    Public WithEvents lith_rock_color As SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_color
    Public WithEvents lith_rock_structure As SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_structure
    Public WithEvents lith_rock_type As SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_type
    Public WithEvents lith_sample As SimpleODM.stratigraphyandlithologyModule.uc_lith_sample
    Public WithEvents lith_sample_collection As SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_collection
    Public WithEvents lith_sample_desc As SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_desc
    Public WithEvents lith_sample_prep As SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_prep
    Public WithEvents lith_sample_prep_math As SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_prep_math
    Public WithEvents lith_structure As SimpleODM.stratigraphyandlithologyModule.uc_lith_structure
    '------------------------------------------------------------------------------------------------
    Public WithEvents Well_Logs As SimpleODM.Logmodule.uc_log
    Public WithEvents Well_Logs_Activities As SimpleODM.Logmodule.uc_log_link_to_activity
    Public WithEvents Well_Log_job As SimpleODM.Logmodule.uc_log_job
    Public WithEvents Well_Log_curve As SimpleODM.Logmodule.uc_log_curve
    Public WithEvents Well_Log_loader As SimpleODM.Logmodule.uc_log_Loader
    Public WithEvents Well_Log_dictionary As SimpleODM.Logmodule.uc_log_dictionary
    Public WithEvents well_log_parameter As SimpleODM.Logmodule.uc_log_parameter
    Public WithEvents well_log_parameter_array As SimpleODM.Logmodule.uc_log_parameter_array
    Public WithEvents well_log_job_trip_remark As SimpleODM.Logmodule.uc_log_job_trip_remark
    Public WithEvents well_log_job_trip_pass As SimpleODM.Logmodule.uc_log_job_trip_pass
    Public WithEvents well_log_job_trip As SimpleODM.Logmodule.uc_log_job_trip
    Public WithEvents well_log_dictionary_proc As SimpleODM.Logmodule.uc_log_dictionary_proc
    Public WithEvents well_log_dictionary_param_value As SimpleODM.Logmodule.uc_log_dictionary_param_value
    Public WithEvents well_log_dictionary_param_cls_Types As SimpleODM.Logmodule.uc_log_dictionary_param_cls_Types
    Public WithEvents well_log_dictionary_param_cls As SimpleODM.Logmodule.uc_log_dictionary_param_cls
    Public WithEvents well_log_dictionary_param As SimpleODM.Logmodule.uc_log_dictionary_param
    Public WithEvents well_log_dictionary_curve_cls As SimpleODM.Logmodule.uc_log_dictionary_curve_cls
    Public WithEvents well_log_dictionary_curve As SimpleODM.Logmodule.uc_log_dictionary_curve
    Public WithEvents well_log_dictionary_ba As SimpleODM.Logmodule.uc_log_dictionary_ba
    Public WithEvents well_log_dictionary_alias As SimpleODM.Logmodule.uc_log_dictionary_alias
    Public WithEvents well_log_curve_class As SimpleODM.Logmodule.uc_log_curve_class
    Public WithEvents well_log_cls_crv_cls As SimpleODM.Logmodule.uc_log_cls_crv_cls
    Public WithEvents well_log_class As SimpleODM.Logmodule.uc_log_class
    Public WithEvents well_log_remark As SimpleODM.Logmodule.uc_log_remark
    Public WithEvents well_log_manager As SimpleODM.Logmodule.uc_log_manager
    Public WithEvents well_log_files_manager As SimpleODM.Logmodule.uc_log_manager

    '-------------------------------- Record Managment RM_INFORMATION_ITEM -------------------------------

    'Public WithEvents rm_info_item As SimpleODM.RecordManagementModule.uc_rm_info_item
    'Public WithEvents rm_info_rm_Manager As SimpleODM.RecordManagementModule.uc_rm_Manager
    'Public WithEvents rm_info_item_physical_item As SimpleODM.RecordManagementModule.uc_rm_info_item_physical_item

    Public WithEvents reportbuilder As SimpleODM.SharedLib.uc_simpleReporting_lite
    Public WithEvents Excelreportbuilder As SimpleODM.SharedLib.uc_spreadsheet
    '------------------------------------------------------------------------------------------------------
    Public WithEvents uc_login1 As uc_login
    Public WithEvents db_def As SimpleODM.SharedLib.uc_dbdefine
    Public WithEvents no_priv As uc_NoPrivControl
    Public WithEvents rvmwithrtableslistModule As rvmwithrtableslist_ctl
    Public WithEvents defaultsSetup As uc_defaults_settings
    Public WithEvents Administrator As uc_db_admin
    '--------------------------------------------------------------------------------------
    ' Public WithEvents BulkDataLoading As uc_SimpleTableLoading
    Public WithEvents BulkDataLoading As uc_tableloader
    Public Event LoginSucccess()
    Public Event Logout()

    Public Event LoginCancel()
    Public Event ShowDatabase()


    Public Sub New()

    End Sub
    Private Sub Uc_login1_Logout() Handles uc_login1.Logout
        RaiseEvent Logout()
    End Sub
    Private Sub Uc_login1_ShowDatabase() Handles uc_login1.ShowDatabase
        'WindowsUIView1.Controller.Activate(DatabaseContainer)
        RaiseEvent ShowDatabase()
    End Sub

    Private Sub _uc_login_LoginCancel() Handles uc_login1.LoginCancel
        RaiseEvent LoginCancel()
    End Sub
    Private Sub _uc_login_LoginSucccess() Handles uc_login1.LoginSucccess
        RaiseEvent LoginSucccess()

    End Sub
    Protected Overrides Sub Finalize()

        MyBase.Finalize()
    End Sub
    Public Event WellSelected(ByVal UWI As String)
    Private Sub _WellModule_WellSelected(UWI As String) Handles WellModule.WellSelected
        RaiseEvent WellSelected(UWI)

    End Sub
End Class
Partial Public Class SharedBusinessObjects
    Public WithEvents WindowsUIView1 As DevExpress.XtraBars.Docking2010.Views.WindowsUI.WindowsUIView
    Dim WithEvents bkwork As New BackgroundWorker
    Public WithEvents SimpleODMConfig As SimpleODM.systemconfigandutil.PPDMConfig
    Public Property SimpleUtil As New SharedLib.util
    Dim ParentDoc As DevExpress.XtraBars.Docking2010.Views.WindowsUI.Document
    ' Public Protection As ProtectionModule
    ' Public WithEvents ETLConfig As SimpleODM.etlmodule.cls_ETL
    Public Title As String
    Public Property DidIInitForms As Boolean = False
    Public Property DidIloginStatus As Boolean = False

    Public WithEvents MyModules As New MyForms
    Dim workinitprec As Integer = 0
    Public Event FormsInitProgress(ByVal Prec As Integer)
    Public Event FormsInitFinish()


    Dim InitForms As Boolean = False
    Dim WellBoreItem As String
    Dim TransType As String
    Dim ucType As String

    Public Sub New()

        'MyModules.uc_login1 = New SimpleODM.SharedLib.uc_login()
        'MyModules.db_def = New SimpleODM.SharedLib.uc_dbdefine()
        'MyModules.no_priv = New SimpleODM.SharedLib.uc_NoPrivControl
        SimpleUtil.SimpleODMConfig = SimpleODMConfig
        SimpleUtil.f_HeightRatio = 1000
        SimpleUtil.f_WidthRatio = 1800

    End Sub
    Public Sub IniTheForms()
        'InitMyForms()
        SimpleODMConfig.Defaults.WELLBORECOMPLETIONIDENTIFIER = "WELLCOMPLETION"
        SimpleODMConfig.Defaults.WELLBOREIDENTIFIER = "WELLBORE"
        SimpleODMConfig.Defaults.WELLIDENTIFIER = "WELL"
        SimpleODMConfig.Defaults.SOURCE = "PPDM"
        SimpleODMConfig.Defaults.ROW_SOURCE = "PPDM"



        WindowsUIView1.Caption = SimpleODMConfig.Defaults.APPLICATIONNAME
    End Sub

    Protected Overrides Sub Finalize()

        MyBase.Finalize()
    End Sub

    Public Sub InitDBAAdminForms()
        Dim Result As Integer = 0

    End Sub
    Public Sub ShowControlInForm(ByRef Ctl As Windows.Forms.UserControl, Optional ByRef parentcontrol As Object = Nothing, Optional Title As String = "")
        Dim frm As New Windows.Forms.Form
        frm.Width = Ctl.Width
        frm.Height = Ctl.Height
        frm.FormBorderStyle = FormBorderStyle.FixedToolWindow
        Ctl.Dock = Windows.Forms.DockStyle.Fill
        frm.Controls.Add(Ctl)
        If Not String.IsNullOrEmpty(Title) Then

            frm.Text = Title

        End If
        If parentcontrol Is Nothing Then
            frm.Show()
        Else
            frm.Show(parentcontrol)
        End If

    End Sub
#Region "Windows UI "
#Region "Details Screens Calls"

    Public Function CreateScreenHandler(ByVal Name As String, ByRef Func As EventHandler) As MenuItem
        Dim item As New MenuItem

        item.Name = Name
        'item.  Other options

        AddHandler item.Click, Func

        Return item
    End Function

    Public Event WellSelected(ByVal UWI As String)
    Private Sub MyModules_WellSelected(UWI As String) Handles MyModules.WellSelected
        RaiseEvent WellSelected(UWI)
    End Sub


    Public Event ShowControlOnTile(Title As String, pType As String, trans As String)

    Dim SimplePRintFrm As XtraForm
    Private Sub SimpleODMConfig_ShowCtlInView(Title As String, pType As String, trans As String) Handles SimpleODMConfig.ShowCtlInView
        If IsNothing(MyModules.BulkDataLoading) Then
            MyModules.BulkDataLoading = New uc_tableloader(SimpleODMConfig, SimpleUtil)
        End If
        If IsNothing(SimplePRintFrm) Then
            SimplePRintFrm = New XtraForm
            SimplePRintFrm.Width = MyModules.BulkDataLoading.Width + 50
            SimplePRintFrm.Height = MyModules.BulkDataLoading.Height + 50
            SimplePRintFrm.Controls.Add(MyModules.BulkDataLoading)
        End If
        MyModules.BulkDataLoading.Dock = Windows.Forms.DockStyle.Fill
        If MyModules.BulkDataLoading.TableName <> SimpleODMConfig.Defaults.CURRENT_TABLE Then
            MyModules.BulkDataLoading.LoadTableSchemas(SimpleODMConfig.Defaults.CURRENT_TABLE)
        End If
        SimplePRintFrm.StartPosition = FormStartPosition.CenterScreen
        SimplePRintFrm.Text = "Data Loading Module"
        SimplePRintFrm.ShowDialog()
    End Sub

    Private Sub SimpleODMConfig_ShowControlOnTile(Title As String, pType As String, trans As String) Handles SimpleODMConfig.ShowCtlInTile
        WellBoreItem = Title
        ucType = pType
        TransType = trans
        Try
            Select Case ucType
                Case "PPDMSYSTEMTABLES"
                    ' Simpleconfig.ShowControlInTile(Me.TitleLabel.Text, "PPDMSYSTEMTABLES", "EDIT")
                Case "PPDMSYSTEMAUDIT"
                    ' Simpleconfig.ShowControlInTile(Me.TitleLabel.Text, "PPDMSYSTEMAUDIT", "EDIT")
                Case "PPDMSYSTEMRULES"
                    ' Simpleconfig.ShowControlInTile(Me.TitleLabel.Text, "PPDMSYSTEMRULES", "EDIT")
                Case "PPDMSYSTEMQC"
                    ' Simpleconfig.ShowControlInTile(Me.TitleLabel.Text, "PPDMSYSTEMQC", "EDIT")
                    '-------------------------------------- Log Data Management -----------------------------------------
                Case "WELLLOG"

                    If (MyModules.Well_Logs Is Nothing) Then
                        MyModules.Well_Logs = New SimpleODM.Logmodule.uc_log(SimpleODMConfig, SimpleUtil)
                    End If
                    MyModules.Well_Logs.WellON = True
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Logs.MyUWI) Then
                        MyModules.Well_Logs.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If


                Case "LOGLOADER"

                    If (MyModules.well_log_manager Is Nothing) Then
                        MyModules.well_log_manager = New SimpleODM.Logmodule.uc_log_manager(SimpleODMConfig, SimpleUtil)
                    End If




                Case "WELLLOGMANAGER"
                    If (MyModules.well_log_manager Is Nothing) Then
                        MyModules.well_log_manager = New SimpleODM.Logmodule.uc_log_manager(SimpleODMConfig, SimpleUtil)
                    End If

                    MyModules.Well_Logs.LoadManager()


                Case "LOGDICT", "WELLLOGDICTIONARY", "WELLLOGDICTIONARYAPP"

                    If (MyModules.Well_Log_dictionary Is Nothing) Then
                        MyModules.Well_Log_dictionary = New SimpleODM.Logmodule.uc_log_dictionary(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.Well_Log_dictionary.DictID) Then
                        MyModules.Well_Log_dictionary.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID)
                    End If


                Case "LOGACTIVITY"

                    If (MyModules.Well_Logs_Activities Is Nothing) Then
                        MyModules.Well_Logs_Activities = New SimpleODM.Logmodule.uc_log_link_to_activity(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Logs_Activities.MyUWI) Or (MyModules.Well_Logs_Activities.MyWELL_LOG_ID <> SimpleODMConfig.Defaults.WELL_LOG_ID) Or (MyModules.Well_Logs_Activities.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Then
                        MyModules.Well_Logs_Activities.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If


                Case "WELLLOGJOB", "WELLLOGJOBAPP"

                    If (MyModules.Well_Log_job Is Nothing) Then
                        MyModules.Well_Log_job = New SimpleODM.Logmodule.uc_log_job(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Log_job.MyUWI) Then
                        MyModules.Well_Log_job.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLLOGCURVE", "WELLLOGCURVEAPP"

                    If (MyModules.Well_Log_curve Is Nothing) Then
                        MyModules.Well_Log_curve = New SimpleODM.Logmodule.uc_log_curve(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Log_curve.MyUWI) Then
                        MyModules.Well_Log_curve.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE)
                    End If
                Case "LOGJOB"

                    If (MyModules.Well_Log_job Is Nothing) Then
                        MyModules.Well_Log_job = New SimpleODM.Logmodule.uc_log_job(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Log_job.MyUWI) Then
                        MyModules.Well_Log_job.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If


                Case "WELLLOGJOBTRIP"                   '  Well Log Job Trip
                    If (MyModules.well_log_job_trip Is Nothing) Then
                        MyModules.well_log_job_trip = New SimpleODM.Logmodule.uc_log_job_trip(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_log_job_trip.MyUWI) Or (MyModules.well_log_job_trip.MyWELL_LOG_JOB_ID <> SimpleODMConfig.Defaults.WELL_LOG_JOB_ID) Or (MyModules.well_log_job_trip.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Then
                        MyModules.well_log_job_trip.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_JOB_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLLOGJOBTRIPPASS"                '  Well Log Job Trip Pass
                    If (MyModules.well_log_job_trip_pass Is Nothing) Then
                        MyModules.well_log_job_trip_pass = New SimpleODM.Logmodule.uc_log_job_trip_pass(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_log_job_trip_pass.MyUWI) Or (MyModules.well_log_job_trip_pass.MyWELL_LOG_JOB_ID <> SimpleODMConfig.Defaults.WELL_LOG_JOB_ID) Or (MyModules.well_log_job_trip_pass.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Or (MyModules.well_log_job_trip_pass.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Then
                        MyModules.well_log_job_trip_pass.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_JOB_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_LOG_TRIP_OBS_NO)
                    End If
                Case "WELLLOGJOBTRIPREMARK"                  '   Well Log Job Trip Remark
                    If (MyModules.well_log_job_trip_remark Is Nothing) Then
                        MyModules.well_log_job_trip_remark = New SimpleODM.Logmodule.uc_log_job_trip_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_log_job_trip_remark.MyUWI) Or (MyModules.well_log_job_trip_remark.MyWELL_LOG_JOB_ID <> SimpleODMConfig.Defaults.WELL_LOG_JOB_ID) Or (MyModules.well_log_job_trip_remark.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Or (MyModules.well_log_job_trip_remark.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Then
                        MyModules.well_log_job_trip_remark.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_JOB_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_LOG_TRIP_OBS_NO)
                    End If
                Case "LOGDICTALIAS"                     '    Well Log Dictionary Alias
                    If (MyModules.well_log_dictionary_alias Is Nothing) Then
                        MyModules.well_log_dictionary_alias = New SimpleODM.Logmodule.uc_log_dictionary_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_alias.DictID) Then
                        MyModules.well_log_dictionary_alias.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID)
                    End If
                Case "LOGDICTCURVE"                         '   Well Log Dictionary Curve
                    If (MyModules.well_log_dictionary_curve Is Nothing) Then
                        MyModules.well_log_dictionary_curve = New SimpleODM.Logmodule.uc_log_dictionary_curve(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_curve.DictID) Then
                        MyModules.well_log_dictionary_curve.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID)
                    End If
                Case "LOGDICTCURVECLASS"                '     Well Log Dictionary Curve Classification
                    If (MyModules.well_log_dictionary_curve_cls Is Nothing) Then
                        MyModules.well_log_dictionary_curve_cls = New SimpleODM.Logmodule.uc_log_dictionary_curve_cls(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_curve_cls.DictID) Or (SimpleODMConfig.Defaults.WELL_LOG_DICT_CURVE_ID <> MyModules.well_log_dictionary_curve_cls.myDict_Curve_id) Then
                        MyModules.well_log_dictionary_curve_cls.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID, SimpleODMConfig.Defaults.WELL_LOG_DICT_CURVE_ID)
                    End If
                Case "LOGDICTPARAMETER"                          '    Well Log Dictionary Parameter
                    If (MyModules.well_log_dictionary_param Is Nothing) Then
                        MyModules.well_log_dictionary_param = New SimpleODM.Logmodule.uc_log_dictionary_param(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_param.DictID) Then
                        MyModules.well_log_dictionary_param.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID)
                    End If
                Case "LOGDICTPARAMETERCLASS"                              '      Well Log Dictionary Parameter Classification
                    If (MyModules.well_log_dictionary_param_cls Is Nothing) Then
                        MyModules.well_log_dictionary_param_cls = New SimpleODM.Logmodule.uc_log_dictionary_param_cls(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_param_cls.DictID) Or (SimpleODMConfig.Defaults.WELL_LOG_DICT_PARAM_ID <> MyModules.well_log_dictionary_param_cls.Dict_Parameter_id) Then
                        MyModules.well_log_dictionary_param_cls.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID, SimpleODMConfig.Defaults.WELL_LOG_DICT_PARAM_ID)
                    End If
                Case "LOGDICTPARAMETERCLASSTYPES"                    '        Well Log Dictionary Parameter Classification Types
                    If (MyModules.well_log_dictionary_param_cls_Types Is Nothing) Then
                        MyModules.well_log_dictionary_param_cls_Types = New SimpleODM.Logmodule.uc_log_dictionary_param_cls_Types(SimpleODMConfig, SimpleUtil)
                    End If
                    'If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_param_cls_Types.m) Then
                    MyModules.well_log_dictionary_param_cls_Types.Loaddata()
                    'End If
                Case "LOGDICTPARAMETERVALUE"             ' Well Log Dictionary Parameter Values
                    If (MyModules.well_log_dictionary_param_value Is Nothing) Then
                        MyModules.well_log_dictionary_param_value = New SimpleODM.Logmodule.uc_log_dictionary_param_value(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_param_value.DictID) Or (SimpleODMConfig.Defaults.WELL_LOG_DICT_PARAM_ID <> MyModules.well_log_dictionary_param_value.Dict_Parameter_id) Then
                        MyModules.well_log_dictionary_param_value.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID, SimpleODMConfig.Defaults.WELL_LOG_DICT_PARAM_ID)
                    End If
                Case "LOGDICTBA"                                 '    Well Log Dictionary Business Associate
                    If (MyModules.well_log_dictionary_ba Is Nothing) Then
                        MyModules.well_log_dictionary_ba = New SimpleODM.Logmodule.uc_log_dictionary_ba(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_ba.DictID) Then
                        MyModules.well_log_dictionary_ba.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID)
                    End If
                Case "LOGDICTPROCEDURE"                '  Well Log Dictionary Procedure
                    If (MyModules.well_log_dictionary_proc Is Nothing) Then
                        MyModules.well_log_dictionary_proc = New SimpleODM.Logmodule.uc_log_dictionary_proc(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_LOG_DICT_ID <> MyModules.well_log_dictionary_proc.DictID) Then
                        MyModules.well_log_dictionary_proc.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_DICT_ID)
                    End If
                Case "WELLLOGCLASSES", "WELLLOGCLASSESAPP"                         '  Well Log Classification,
                    If (MyModules.well_log_class Is Nothing) Then
                        MyModules.well_log_class = New uc_log_class(SimpleODMConfig, SimpleUtil)

                    End If
                    If (MyModules.well_log_class.MyUWI <> SimpleODMConfig.Defaults.WELL_XREFMyUWI) Or (MyModules.well_log_class.MyWELL_LOG_ID <> SimpleODMConfig.Defaults.WELL_LOG_ID) Or (MyModules.well_log_class.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Then

                        MyModules.well_log_class.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If



                Case "WELLLOGREMARK", "WELLLOGREMARKAPP"                        '  Well Log Remark
                    If (MyModules.well_log_remark Is Nothing) Then
                        MyModules.well_log_remark = New uc_log_remark(SimpleODMConfig, SimpleUtil)

                    End If
                    If (MyModules.well_log_remark.MyUWI <> SimpleODMConfig.Defaults.WELL_XREFMyUWI) Or (MyModules.well_log_remark.MyWELL_LOG_ID <> SimpleODMConfig.Defaults.WELL_LOG_ID) Or (MyModules.well_log_remark.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Then

                        MyModules.well_log_remark.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                    'Case "WELLLOGCURVEORFRAME"
                    '    If (MyModules.BulkDataLoading Is Nothing) Then
                    '        MyModules.BulkDataLoading = New uc_SimpleTableLoading(SimpleODMConfig, SimpleUtil)

                    '    End If
                    '    If MyModules.BulkDataLoading.TableName <> SimpleODMConfig.Defaults.CURRENT_TABLE Then
                    '        MyModules.BulkDataLoading.TableName = SimpleODMConfig.Defaults.CURRENT_TABLE
                    '        MyModules.BulkDataLoading.LoadTableSchemas(SimpleODMConfig.Defaults.CURRENT_TABLE)
                    '    End If


                Case "WELLLOGPARAMETER", "WELLLOGPARAMETERAPP"                '  Well Log Parameters
                    If (MyModules.well_log_parameter Is Nothing) Then
                        MyModules.well_log_parameter = New uc_log_parameter(SimpleODMConfig, SimpleUtil)

                    End If
                    If (MyModules.well_log_parameter.MyUWI <> SimpleODMConfig.Defaults.WELL_XREFMyUWI) Or (MyModules.well_log_parameter.MyWELL_LOG_ID <> SimpleODMConfig.Defaults.WELL_LOG_ID) Or (MyModules.well_log_parameter.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Then

                        MyModules.well_log_parameter.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLLOGPARAMETERARRAY"                   '  Well Log Parameters
                    If (MyModules.well_log_parameter_array Is Nothing) Then
                        MyModules.well_log_parameter_array = New uc_log_parameter_array(SimpleODMConfig, SimpleUtil)

                    End If
                    If (MyModules.well_log_parameter_array.MyUWI <> SimpleODMConfig.Defaults.WELL_XREFMyUWI) Or (MyModules.well_log_parameter_array.MyWELL_LOG_ID <> SimpleODMConfig.Defaults.WELL_LOG_ID) Or (MyModules.well_log_parameter_array.MyWELL_LOG_SOURCE <> SimpleODMConfig.Defaults.SOURCE) Or (MyModules.well_log_parameter_array.MyWELL_LOG_PARAMETER_SEQ_NO <> SimpleODMConfig.Defaults.WELL_LOG_PARAMETER_SEQ_NO) Then

                        MyModules.well_log_parameter_array.Loaddata(SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI, MyModules.well_log_parameter_array.MyWELL_LOG_PARAMETER_SEQ_NO)
                    End If


                    '----------------------------------------------------------------------------------------------------
                    '--------------------------------------Bulk Data Loader-----------------------------------------------
                Case "BULKDATA"
                    If (MyModules.BulkDataLoading Is Nothing) Then
                        MyModules.BulkDataLoading = New uc_tableloader(SimpleODMConfig, SimpleUtil)

                    End If
                    If MyModules.BulkDataLoading.TableName <> SimpleODMConfig.Defaults.CURRENT_TABLE Then
                        MyModules.BulkDataLoading.TableName = SimpleODMConfig.Defaults.CURRENT_TABLE
                        MyModules.BulkDataLoading.LoadTableSchemas(SimpleODMConfig.Defaults.CURRENT_TABLE)
                    End If
                    '------------------------------------ Lithology -------------------------------------------------------
                Case "LITHLOG"
                    If (MyModules.lith_log Is Nothing) Then
                        MyModules.lith_log = New SimpleODM.stratigraphyandlithologyModule.uc_lith_log(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.MEAS_SECTION_ID <> MyModules.lith_log.MEASSECTIONID) Or (SimpleODMConfig.Defaults.MEAS_SECTION_SOURCE <> MyModules.lith_log.MEASSECTIONSOURCE) Then
                        MyModules.lith_log.Loaddata(SimpleODMConfig.Defaults.MEAS_SECTION_ID, SimpleODMConfig.Defaults.MEAS_SECTION_SOURCE)
                    End If
                Case "LITHLOGREMARK"
                    If (MyModules.lith_log_remark Is Nothing) Then
                        MyModules.lith_log_remark = New SimpleODM.stratigraphyandlithologyModule.uc_lith_log_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_log_remark.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_log_remark.LITHLOGSOURCE) Then
                        MyModules.lith_log_remark.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE)
                    End If
                Case "LITHLOGBASERVICE"
                    If (MyModules.lith_log_ba_service Is Nothing) Then
                        MyModules.lith_log_ba_service = New SimpleODM.stratigraphyandlithologyModule.uc_lith_log_ba_service(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_log_ba_service.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_log_ba_service.LITHLOGSOURCE) Then
                        MyModules.lith_log_ba_service.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE)
                    End If
                Case "LITHLOGENVINT"
                    If (MyModules.lith_dep_ent_int Is Nothing) Then
                        MyModules.lith_dep_ent_int = New SimpleODM.stratigraphyandlithologyModule.uc_lith_dep_ent_int(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_dep_ent_int.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_dep_ent_int.LITHLOGSOURCE) Then
                        MyModules.lith_dep_ent_int.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE)
                    End If
                Case "LITHLOGDEPTHINT"
                    If (MyModules.lith_interval Is Nothing) Then
                        MyModules.lith_interval = New SimpleODM.stratigraphyandlithologyModule.uc_lith_interval(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_interval.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_interval.LITHLOGSOURCE) Then
                        MyModules.lith_interval.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE)
                    End If
                Case "LITHINTERVALROCKTYPE"
                    If (MyModules.lith_rock_type Is Nothing) Then
                        MyModules.lith_rock_type = New SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_type(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_rock_type.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_rock_type.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_rock_type.DEPTHOBSNO) Then
                        MyModules.lith_rock_type.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO)
                    End If
                Case "LITHINTERVALROCKTYPECOMP"
                    If (MyModules.lith_component Is Nothing) Then
                        MyModules.lith_component = New SimpleODM.stratigraphyandlithologyModule.uc_lith_component(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_component.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_component.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_component.DEPTHOBSNO) Or (SimpleODMConfig.Defaults.ROCK_TYPE <> MyModules.lith_component.ROCKTYPE) Or (SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO <> MyModules.lith_component.ROCKTYPEOBSNO) Then
                        MyModules.lith_component.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO)
                    End If
                Case "LITHCOMPCOLOR"
                    If (MyModules.lith_component_color Is Nothing) Then
                        MyModules.lith_component_color = New SimpleODM.stratigraphyandlithologyModule.uc_lith_component_color(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_component_color.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_component_color.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_component_color.DEPTHOBSNO) Or (SimpleODMConfig.Defaults.ROCK_TYPE <> MyModules.lith_component_color.ROCKTYPE) Or (SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO <> MyModules.lith_component_color.ROCKTYPEOBSNO) Or (SimpleODMConfig.Defaults.COMPONENT_NAME <> MyModules.lith_component_color.COMPONENTNAME) Then
                        MyModules.lith_component_color.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO, SimpleODMConfig.Defaults.COMPONENT_NAME)
                    End If
                Case "LITHCOMPGRAINSIZE"
                    If (MyModules.lith_comp_grain_size Is Nothing) Then
                        MyModules.lith_comp_grain_size = New SimpleODM.stratigraphyandlithologyModule.uc_lith_comp_grain_size(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_comp_grain_size.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_comp_grain_size.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_comp_grain_size.DEPTHOBSNO) Or (SimpleODMConfig.Defaults.ROCK_TYPE <> MyModules.lith_comp_grain_size.ROCKTYPE) Or (SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO <> MyModules.lith_comp_grain_size.ROCKTYPEOBSNO) Or (SimpleODMConfig.Defaults.COMPONENT_NAME <> MyModules.lith_comp_grain_size.COMPONENTNAME) Then
                        MyModules.lith_comp_grain_size.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO, SimpleODMConfig.Defaults.COMPONENT_NAME)
                    End If
                Case "LITHINTERVALROCKTYPEDIAG"
                    If (MyModules.lith_diagenesis Is Nothing) Then
                        MyModules.lith_diagenesis = New SimpleODM.stratigraphyandlithologyModule.uc_lith_diagenesis(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_diagenesis.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_diagenesis.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_diagenesis.DEPTHOBSNO) Or (SimpleODMConfig.Defaults.ROCK_TYPE <> MyModules.lith_diagenesis.ROCKTYPE) Or (SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO <> MyModules.lith_diagenesis.ROCKTYPEOBSNO) Then
                        MyModules.lith_diagenesis.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO)
                    End If
                Case "LITHINTERVALROCKTYPEGRAINSIZE"
                    If (MyModules.lith_grain_size Is Nothing) Then
                        MyModules.lith_grain_size = New SimpleODM.stratigraphyandlithologyModule.uc_lith_grain_size(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_grain_size.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_grain_size.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_grain_size.DEPTHOBSNO) Or (SimpleODMConfig.Defaults.ROCK_TYPE <> MyModules.lith_grain_size.ROCKTYPE) Or (SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO <> MyModules.lith_grain_size.ROCKTYPEOBSNO) Then
                        MyModules.lith_grain_size.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO)
                    End If

                Case "LITHINTERVALROCKTYPEPOROSITY"
                    If (MyModules.lith_porosity Is Nothing) Then
                        MyModules.lith_porosity = New SimpleODM.stratigraphyandlithologyModule.uc_lith_porosity(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_porosity.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_porosity.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_porosity.DEPTHOBSNO) Or (SimpleODMConfig.Defaults.ROCK_TYPE <> MyModules.lith_porosity.ROCKTYPE) Or (SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO <> MyModules.lith_porosity.ROCKTYPEOBSNO) Then
                        MyModules.lith_porosity.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO)
                    End If
                Case "LITHINTERVALROCKTYPECOLOR"
                    If (MyModules.lith_rock_color Is Nothing) Then
                        MyModules.lith_rock_color = New SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_color(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_rock_color.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_rock_color.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_rock_color.DEPTHOBSNO) Or (SimpleODMConfig.Defaults.ROCK_TYPE <> MyModules.lith_rock_color.ROCKTYPE) Or (SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO <> MyModules.lith_rock_color.ROCKTYPEOBSNO) Then
                        MyModules.lith_rock_color.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO)
                    End If
                Case "LITHINTERVALROCKTYPESTRUCTURE"
                    If (MyModules.lith_rock_structure Is Nothing) Then
                        MyModules.lith_rock_structure = New SimpleODM.stratigraphyandlithologyModule.uc_lith_rock_structure(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_rock_structure.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_rock_structure.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_rock_structure.DEPTHOBSNO) Or (SimpleODMConfig.Defaults.ROCK_TYPE <> MyModules.lith_rock_structure.ROCKTYPE) Or (SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO <> MyModules.lith_rock_structure.ROCKTYPEOBSNO) Then
                        MyModules.lith_rock_structure.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO, SimpleODMConfig.Defaults.ROCK_TYPE, SimpleODMConfig.Defaults.ROCK_TYPE_OBS_NO)
                    End If
                Case "LITHINTERVALSTRUCTURE"
                    If (MyModules.lith_structure Is Nothing) Then
                        MyModules.lith_structure = New SimpleODM.stratigraphyandlithologyModule.uc_lith_structure(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID <> MyModules.lith_structure.LITHOLOGYLOGID) Or (SimpleODMConfig.Defaults.LITH_LOG_SOURCE <> MyModules.lith_structure.LITHLOGSOURCE) Or (SimpleODMConfig.Defaults.DEPTH_OBS_NO <> MyModules.lith_structure.DEPTHOBSNO) Then
                        MyModules.lith_structure.Loaddata(SimpleODMConfig.Defaults.LITHOLOGY_LOG_ID, SimpleODMConfig.Defaults.LITH_LOG_SOURCE, SimpleODMConfig.Defaults.DEPTH_OBS_NO)
                    End If


                Case "LITHSAMPLECOLLECTION"
                    If (MyModules.lith_sample_collection Is Nothing) Then
                        MyModules.lith_sample_collection = New SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_collection(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITH_SAMPLE_ID <> MyModules.lith_sample_collection.LITHSAMPLEID) Then
                        MyModules.lith_sample_collection.Loaddata(SimpleODMConfig.Defaults.LITH_SAMPLE_ID)
                    End If
                Case "LITHSAMPLEDESC"
                    If (MyModules.lith_sample_desc Is Nothing) Then
                        MyModules.lith_sample_desc = New SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_desc(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITH_SAMPLE_ID <> MyModules.lith_sample_desc.LITHSAMPLEID) Then
                        MyModules.lith_sample_desc.Loaddata(SimpleODMConfig.Defaults.LITH_SAMPLE_ID)
                    End If
                Case "LITHSAMPLEPREP"
                    If (MyModules.lith_sample_prep Is Nothing) Then
                        MyModules.lith_sample_prep = New SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_prep(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITH_SAMPLE_ID <> MyModules.lith_sample_prep.LITHSAMPLEID) Then
                        MyModules.lith_sample_prep.Loaddata(SimpleODMConfig.Defaults.LITH_SAMPLE_ID)
                    End If
                Case "LITHSAMPLEPREPMETH"
                    If (MyModules.lith_sample_prep_math Is Nothing) Then
                        MyModules.lith_sample_prep_math = New SimpleODM.stratigraphyandlithologyModule.uc_lith_sample_prep_math(SimpleODMConfig, SimpleUtil)
                    End If
                    MyModules.lith_sample_prep_math.Loaddata()

                Case "LITHDESCOTHER"
                    If (MyModules.lith_desc_other Is Nothing) Then
                        MyModules.lith_desc_other = New SimpleODM.stratigraphyandlithologyModule.uc_lith_desc_other(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.LITH_SAMPLE_ID <> MyModules.lith_desc_other.LITHSAMPLEID) Then
                        MyModules.lith_desc_other.Loaddata(SimpleODMConfig.Defaults.LITH_SAMPLE_ID)
                    End If
                    '------------------------------------ Reserve Entities and Classisifactions --------------------
                Case "RESERVECLASSECO"
                    If (MyModules.resent_eco_run Is Nothing) Then
                        MyModules.resent_eco_run = New SimpleODM.reservepoolproductionmodule.uc_resent_eco_run(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_eco_run.RESERVECLASSid) Or (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_eco_run.RESENTid) Then
                        MyModules.resent_eco_run.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID)
                    End If
                Case "RESERVECLASSPRODUCT"
                    If (MyModules.resent_product Is Nothing) Then
                        MyModules.resent_product = New SimpleODM.reservepoolproductionmodule.uc_resent_product(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_product.RESERVECLASSid) Or (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_product.RESENTid) Then
                        MyModules.resent_product.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID)
                    End If
                Case "RESERVECLASSECOPARAM"
                    If (MyModules.resent_eco_schedule Is Nothing) Then
                        MyModules.resent_eco_schedule = New SimpleODM.reservepoolproductionmodule.uc_resent_eco_schedule(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_eco_schedule.RESERVECLASSid) Or (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_eco_schedule.RESENTid) Or (SimpleODMConfig.Defaults.ECONOMICS_RUN_ID <> MyModules.resent_eco_schedule.ECONOMICSRUNID) Then
                        MyModules.resent_eco_schedule.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.ECONOMICS_RUN_ID)
                    End If
                Case "RESERVECLASSECOVOLUME"
                    If (MyModules.resent_eco_volume Is Nothing) Then
                        MyModules.resent_eco_volume = New SimpleODM.reservepoolproductionmodule.uc_resent_eco_volume(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_eco_volume.RESERVECLASSid) Or (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_eco_volume.RESENTid) Or (SimpleODMConfig.Defaults.ECONOMICS_RUN_ID <> MyModules.resent_eco_volume.ECONOMICSRUNID) Then
                        MyModules.resent_eco_volume.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.ECONOMICS_RUN_ID)
                    End If
                Case "RESERVEPRODPROP"
                    If (MyModules.resent_prod_property Is Nothing) Then
                        MyModules.resent_prod_property = New SimpleODM.reservepoolproductionmodule.uc_resent_prod_property(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_prod_property.RESERVECLASSid) Or (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_prod_property.RESENTid) Or (SimpleODMConfig.Defaults.PRODUCT_TYPE <> MyModules.resent_prod_property.Producttype) Then
                        MyModules.resent_prod_property.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE)
                    End If
                Case "RESERVEPRODVOLSUMMARY"
                    If (MyModules.resent_vol_summary Is Nothing) Then
                        MyModules.resent_vol_summary = New SimpleODM.reservepoolproductionmodule.uc_resent_vol_summary(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_vol_summary.RESERVECLASSid) Or (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_vol_summary.RESENTid) Or (SimpleODMConfig.Defaults.PRODUCT_TYPE <> MyModules.resent_vol_summary.Producttype) Then
                        MyModules.resent_vol_summary.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE)
                    End If
                Case "RESERVEVOLREVISIONS"
                    If (MyModules.resent_vol_revision Is Nothing) Then
                        MyModules.resent_vol_revision = New SimpleODM.reservepoolproductionmodule.uc_resent_vol_revision(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_vol_revision.RESERVECLASSid) Or (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_vol_revision.RESENTid) Or (SimpleODMConfig.Defaults.PRODUCT_TYPE <> MyModules.resent_vol_revision.Producttype) Or (SimpleODMConfig.Defaults.SUMMARY_ID <> MyModules.resent_vol_revision.SUMMARYID) Or (SimpleODMConfig.Defaults.SUMMARY_OBS_NO <> MyModules.resent_vol_revision.SUMMARYOBSNO) Then
                        MyModules.resent_vol_revision.Loaddata(SimpleODMConfig.Defaults.RESENT_ID, SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE, SimpleODMConfig.Defaults.SUMMARY_ID, SimpleODMConfig.Defaults.SUMMARY_OBS_NO)
                    End If
                Case "RESERVECLASSIFICATIONSFORMULA"
                    If (MyModules.reserve_class_formula Is Nothing) Then
                        MyModules.reserve_class_formula = New SimpleODM.reservepoolproductionmodule.uc_reserve_class_formula(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.reserve_class_formula.RESERVECLASSID) Then
                        MyModules.reserve_class_formula.Loaddata(SimpleODMConfig.Defaults.RESERVE_CLASS_ID)
                    End If
                Case "RESERVECLASSIFICATIONSFORMULACALC"
                    If (MyModules.reserve_class_calc Is Nothing) Then
                        MyModules.reserve_class_calc = New SimpleODM.reservepoolproductionmodule.uc_reserve_class_calc(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.reserve_class_calc.RESERVECLASSID) Or (SimpleODMConfig.Defaults.FORMULA_ID <> MyModules.reserve_class_calc.FORMULAID) Then
                        MyModules.reserve_class_calc.Loaddata(SimpleODMConfig.Defaults.RESERVE_CLASS_ID, SimpleODMConfig.Defaults.FORMULA_ID)
                    End If
                Case "RESERVECLASSIFICATIONS"
                    If (MyModules.reserve_class Is Nothing) Then
                        MyModules.reserve_class = New SimpleODM.reservepoolproductionmodule.uc_reserve_class(SimpleODMConfig, SimpleUtil)
                    End If
                    ' If (SimpleODMconfig.Defaults.RESENT_ID <> MyModules.resent_revision_cat.RESENTid) Then
                    MyModules.reserve_class.Loaddata()
                Case "RESERVEENTITYCLASS"
                    If (MyModules.resent_class Is Nothing) Then
                        MyModules.resent_class = New SimpleODM.reservepoolproductionmodule.uc_resent_class(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_class.RESENTid) Then
                        MyModules.resent_class.Loaddata(SimpleODMConfig.Defaults.RESENT_ID)
                    End If
                Case "RESERVECROSSREF"
                    If (MyModules.resent_xref Is Nothing) Then
                        MyModules.resent_xref = New SimpleODM.reservepoolproductionmodule.uc_resent_xref(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_xref.RESENTid) Then
                        MyModules.resent_xref.Loaddata(SimpleODMConfig.Defaults.RESENT_ID)
                    End If
                Case "RESERVEUNITREGIME"
                    If (MyModules.resent_vol_regime Is Nothing) Then
                        MyModules.resent_vol_regime = New SimpleODM.reservepoolproductionmodule.uc_resent_vol_regime(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.RESENT_ID <> MyModules.resent_vol_regime.RESENTid) Then
                        MyModules.resent_vol_regime.Loaddata(SimpleODMConfig.Defaults.RESENT_ID)
                    End If
                Case "RESERVEREVISION"
                    If (MyModules.resent_revision_cat Is Nothing) Then
                        MyModules.resent_revision_cat = New SimpleODM.reservepoolproductionmodule.uc_resent_revision_cat(SimpleODMConfig, SimpleUtil)
                    End If
                    ' If (SimpleODMconfig.Defaults.RESENT_ID <> MyModules.resent_revision_cat.RESENTid) Then
                    MyModules.resent_revision_cat.Loaddata()
                    ' End If
                    '------------------------------------ PDEN Production Entities -------

                Case "PDENBA"
                    If (MyModules.pden_business_assoc Is Nothing) Then
                        MyModules.pden_business_assoc = New SimpleODM.reservepoolproductionmodule.uc_pden_business_assoc(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_business_assoc.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_business_assoc.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_business_assoc.PDENSOURCE) Then
                        MyModules.pden_business_assoc.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENAREA"
                    If (MyModules.pden_area Is Nothing) Then
                        MyModules.pden_area = New SimpleODM.reservepoolproductionmodule.uc_pden_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_area.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_area.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_area.PDENSOURCE) Then
                        MyModules.pden_area.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENOTHER"
                    If (MyModules.pden_other Is Nothing) Then
                        MyModules.pden_other = New SimpleODM.reservepoolproductionmodule.uc_pden_other(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_other.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_other.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_other.PDENSOURCE) Then
                        MyModules.pden_other.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENWELL"
                    If (MyModules.pden_well Is Nothing) Then
                        MyModules.pden_well = New SimpleODM.reservepoolproductionmodule.uc_pden_well(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_well.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_well.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_well.PDENSOURCE) Then
                        MyModules.pden_well.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENPRODSTRING"
                    If (MyModules.pden_prod_string Is Nothing) Then
                        MyModules.pden_prod_string = New SimpleODM.reservepoolproductionmodule.uc_pden_prod_string(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_prod_string.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_prod_string.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_prod_string.PDENSOURCE) Then
                        MyModules.pden_prod_string.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENRESERVE"
                    If (MyModules.pden_resent Is Nothing) Then
                        MyModules.pden_resent = New SimpleODM.reservepoolproductionmodule.uc_pden_resent(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_resent.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_resent.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_resent.PDENSOURCE) Then
                        MyModules.pden_resent.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENFORMATION"
                    If (MyModules.pden_pr_str_form Is Nothing) Then
                        MyModules.pden_pr_str_form = New SimpleODM.reservepoolproductionmodule.uc_pden_pr_str_form(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_pr_str_form.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_pr_str_form.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_pr_str_form.PDENSOURCE) Then
                        MyModules.pden_pr_str_form.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENPOOL"
                    If (MyModules.pden_pool Is Nothing) Then
                        MyModules.pden_pool = New SimpleODM.reservepoolproductionmodule.uc_pden_pool(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_pool.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_pool.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_pool.PDENSOURCE) Then
                        MyModules.pden_pool.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENLEASE"
                    If (MyModules.pden_lease_unit Is Nothing) Then
                        MyModules.pden_lease_unit = New SimpleODM.reservepoolproductionmodule.uc_pden_lease_unit(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_lease_unit.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_lease_unit.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_lease_unit.PDENSOURCE) Then
                        MyModules.pden_lease_unit.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENRESERVECLASS"
                    If (MyModules.pden_resent_class Is Nothing) Then
                        MyModules.pden_resent_class = New SimpleODM.reservepoolproductionmodule.uc_pden_resent_class(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_resent_class.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_resent_class.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_resent_class.PDENSOURCE) Then
                        MyModules.pden_resent_class.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENFACILITY"
                    If (MyModules.pden_facility Is Nothing) Then
                        MyModules.pden_facility = New SimpleODM.reservepoolproductionmodule.uc_pden_facility(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_facility.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_facility.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_facility.PDENSOURCE) Then
                        MyModules.pden_facility.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENFIELD"

                    If (MyModules.pden_field Is Nothing) Then
                        MyModules.pden_field = New SimpleODM.reservepoolproductionmodule.uc_pden_field(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_field.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_field.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_field.PDENSOURCE) Then
                        MyModules.pden_field.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENALLOCFACTOR"
                    If (MyModules.pden_alloc_factor Is Nothing) Then
                        MyModules.pden_alloc_factor = New SimpleODM.reservepoolproductionmodule.uc_pden_alloc_factor(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_alloc_factor.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_alloc_factor.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_alloc_factor.PDENSOURCE) Then
                        MyModules.pden_alloc_factor.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENDECLINECASE"
                    If (MyModules.pden_decline_case Is Nothing) Then
                        MyModules.pden_decline_case = New SimpleODM.reservepoolproductionmodule.uc_pden_decline_case(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_decline_case.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_decline_case.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_decline_case.PDENSOURCE) Then
                        MyModules.pden_decline_case.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENDECLINECASECOND"
                    If (MyModules.pden_decline_condition Is Nothing) Then
                        MyModules.pden_decline_condition = New SimpleODM.reservepoolproductionmodule.uc_pden_decline_condition(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_decline_condition.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_decline_condition.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_decline_condition.PDENSOURCE) Or (SimpleODMConfig.Defaults.CASE_ID <> MyModules.pden_decline_condition.CASEID) Or (SimpleODMConfig.Defaults.PRODUCT_TYPE <> MyModules.pden_decline_condition.PRODUCTTYPE) Then
                        MyModules.pden_decline_condition.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE, SimpleODMConfig.Defaults.CASE_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE)
                    End If
                Case "PDENDECLINECASESEG"
                    If (MyModules.pden_decline_segment Is Nothing) Then
                        MyModules.pden_decline_segment = New SimpleODM.reservepoolproductionmodule.uc_pden_decline_segment(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_decline_segment.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_decline_segment.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_decline_segment.PDENSOURCE) Or (SimpleODMConfig.Defaults.CASE_ID <> MyModules.pden_decline_segment.CASEID) Or (SimpleODMConfig.Defaults.PRODUCT_TYPE <> MyModules.pden_decline_segment.PRODUCTTYPE) Then
                        MyModules.pden_decline_segment.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE, SimpleODMConfig.Defaults.CASE_ID, SimpleODMConfig.Defaults.PRODUCT_TYPE)
                    End If
                Case "PDENFLOWMEASURE"
                    If (MyModules.pden_flow_measurement Is Nothing) Then
                        MyModules.pden_flow_measurement = New SimpleODM.reservepoolproductionmodule.uc_pden_flow_measurement(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_flow_measurement.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_flow_measurement.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_flow_measurement.PDENSOURCE) Then
                        MyModules.pden_flow_measurement.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENINAREA"
                    If (MyModules.pden_in_area Is Nothing) Then
                        MyModules.pden_in_area = New SimpleODM.reservepoolproductionmodule.uc_pden_in_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_in_area.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_in_area.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_in_area.PDENSOURCE) Then
                        MyModules.pden_in_area.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENPRODSTRTOPDENCROSSREF"
                    If (MyModules.pden_prod_string_xref Is Nothing) Then
                        MyModules.pden_prod_string_xref = New SimpleODM.reservepoolproductionmodule.uc_pden_prod_string_xref(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_prod_string_xref.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_prod_string_xref.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_prod_string_xref.PDENSOURCE) Then
                        MyModules.pden_prod_string_xref.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENALLOWABLE"
                    If (MyModules.pden_pr_str_allowable Is Nothing) Then
                        MyModules.pden_pr_str_allowable = New SimpleODM.reservepoolproductionmodule.uc_pden_pr_str_allowable(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_pr_str_allowable.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_pr_str_allowable.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_pr_str_allowable.PDENSOURCE) Or (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.pden_pr_str_allowable.UWI) Or (SimpleODMConfig.Defaults.PROD_STRING_SOURCE <> MyModules.pden_pr_str_allowable.STRINGSOURCE) Or (SimpleODMConfig.Defaults.PDEN_PRS_XREF_SEQ_NO <> MyModules.pden_pr_str_allowable.PDEN_PRS_XREF_SEQ_NO) Then
                        MyModules.pden_pr_str_allowable.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE, SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PROD_STRING_SOURCE, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PDEN_PRS_XREF_SEQ_NO)
                    End If
                Case "PDENMATERIALBAL"
                    If (MyModules.pden_material_bal Is Nothing) Then
                        MyModules.pden_material_bal = New SimpleODM.reservepoolproductionmodule.uc_pden_material_bal(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_material_bal.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_material_bal.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_material_bal.PDENSOURCE) Then
                        MyModules.pden_material_bal.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENOPERHIST"
                    If (MyModules.pden_oper_hist Is Nothing) Then
                        MyModules.pden_oper_hist = New SimpleODM.reservepoolproductionmodule.uc_pden_oper_hist(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_oper_hist.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_oper_hist.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_oper_hist.PDENSOURCE) Then
                        MyModules.pden_oper_hist.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENSTATUSHIST"
                    If (MyModules.pden_status_hist Is Nothing) Then
                        MyModules.pden_status_hist = New SimpleODM.reservepoolproductionmodule.uc_pden_status_hist(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_status_hist.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_status_hist.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_status_hist.PDENSOURCE) Then
                        MyModules.pden_status_hist.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENVOLDISP"
                    If (MyModules.pden_vol_disposition Is Nothing) Then
                        MyModules.pden_vol_disposition = New SimpleODM.reservepoolproductionmodule.uc_pden_vol_disposition(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_vol_disposition.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_vol_disposition.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_vol_disposition.PDENSOURCE) Then
                        MyModules.pden_vol_disposition.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENVOLREGIME"
                    If (MyModules.pden_vol_regime Is Nothing) Then
                        MyModules.pden_vol_regime = New SimpleODM.reservepoolproductionmodule.uc_pden_vol_regime(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_vol_regime.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_vol_regime.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_vol_regime.PDENSOURCE) Then
                        MyModules.pden_vol_regime.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENVOLSUMMARY"
                    If (MyModules.pden_summary Is Nothing) Then
                        MyModules.pden_summary = New SimpleODM.reservepoolproductionmodule.uc_pden_summary(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_summary.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_summary.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_summary.PDENSOURCE) Then
                        MyModules.pden_summary.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENVOLSUMMARYOTHER"
                    If (MyModules.pden_vol_summ_other Is Nothing) Then
                        MyModules.pden_vol_summ_other = New SimpleODM.reservepoolproductionmodule.uc_pden_vol_summ_other(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_vol_summ_other.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_vol_summ_other.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_vol_summ_other.PDENSOURCE) Then
                        MyModules.pden_vol_summ_other.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENVOLANALYSIS"
                    If (MyModules.pden_volume_analysis Is Nothing) Then
                        MyModules.pden_volume_analysis = New SimpleODM.reservepoolproductionmodule.uc_pden_volume_analysis(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_volume_analysis.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_volume_analysis.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_volume_analysis.PDENSOURCE) Then
                        MyModules.pden_volume_analysis.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                Case "PDENCROSSREF"
                    If (MyModules.pden_xref Is Nothing) Then
                        MyModules.pden_xref = New SimpleODM.reservepoolproductionmodule.uc_pden_xref(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.PDEN_ID <> MyModules.pden_xref.PDENID) Or (SimpleODMConfig.Defaults.PDEN_TYPE <> MyModules.pden_xref.PDENTYPE) Or (SimpleODMConfig.Defaults.PDEN_SOURCE <> MyModules.pden_xref.PDENSOURCE) Then
                        MyModules.pden_xref.Loaddata(SimpleODMConfig.Defaults.PDEN_ID, SimpleODMConfig.Defaults.PDEN_TYPE, SimpleODMConfig.Defaults.PDEN_SOURCE)
                    End If
                    '------------------------------------ POOL ---------------------------
                Case "POOLALIAS"
                    If (MyModules.pool_alias Is Nothing) Then
                        MyModules.pool_alias = New SimpleODM.reservepoolproductionmodule.uc_pool_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.POOLID <> MyModules.pool_alias.PoolID) Then
                        MyModules.pool_alias.Loaddata(SimpleODMConfig.Defaults.POOLID)
                    End If
                Case "POOLAREA"
                    If (MyModules.pool_area Is Nothing) Then
                        MyModules.pool_area = New SimpleODM.reservepoolproductionmodule.uc_pool_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.POOLID <> MyModules.pool_area.PoolID) Then
                        MyModules.pool_area.Loaddata(SimpleODMConfig.Defaults.POOLID)
                    End If
                Case "POOLINSTRUMENT"
                    If (MyModules.pool_instrument Is Nothing) Then
                        MyModules.pool_instrument = New SimpleODM.reservepoolproductionmodule.uc_pool_instrument(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.POOLID <> MyModules.pool_instrument.PoolID) Then
                        MyModules.pool_instrument.Loaddata(SimpleODMConfig.Defaults.POOLID)
                    End If
                Case "POOLVERSION"
                    If (MyModules.pool_version Is Nothing) Then
                        MyModules.pool_version = New SimpleODM.reservepoolproductionmodule.uc_pool_version(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.POOLID <> MyModules.pool_version.PoolID) Then
                        MyModules.pool_version.Loaddata(SimpleODMConfig.Defaults.POOLID)
                    End If
                    '------------------------------------- Area --------------------------
                Case "AREAALIAS"
                    If (MyModules.area_alias Is Nothing) Then
                        MyModules.area_alias = New SimpleODM.supportmodule.uc_area_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.AREA <> MyModules.area_alias.Areaid) Or (SimpleODMConfig.Defaults.AREA_TYPE <> MyModules.area_alias.AreaType) Then
                        MyModules.area_alias.Loaddata(SimpleODMConfig.Defaults.AREA, SimpleODMConfig.Defaults.AREA_TYPE)
                    End If
                Case "AREACONTAIN"
                    If (MyModules.area_contain Is Nothing) Then
                        MyModules.area_contain = New SimpleODM.supportmodule.uc_area_contain(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.AREA <> MyModules.area_contain.Areaid) Or (SimpleODMConfig.Defaults.AREA_TYPE <> MyModules.area_contain.AreaType) Then
                        MyModules.area_contain.Loaddata(SimpleODMConfig.Defaults.AREA, SimpleODMConfig.Defaults.AREA_TYPE)
                    End If
                Case "AREADESCRIPTION"
                    If (MyModules.area_description Is Nothing) Then
                        MyModules.area_description = New SimpleODM.supportmodule.uc_area_description(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.AREA <> MyModules.area_description.Areaid) Or (SimpleODMConfig.Defaults.AREA_TYPE <> MyModules.area_description.AreaType) Then
                        MyModules.area_description.Loaddata(SimpleODMConfig.Defaults.AREA, SimpleODMConfig.Defaults.AREA_TYPE)
                    End If

                    '------------------------------------- Applications -------------------
                Case "APPLICATIONALIAS"
                    If (MyModules.application_alias Is Nothing) Then
                        MyModules.application_alias = New SimpleODM.supportmodule.uc_application_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.APPLICATION_ID <> MyModules.application_alias.AppiD) Then
                        MyModules.application_alias.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID)
                    End If
                Case "APPLICATIONAREA"
                    If (MyModules.application_area Is Nothing) Then
                        MyModules.application_area = New SimpleODM.supportmodule.uc_application_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.APPLICATION_ID <> MyModules.application_area.AppiD) Then
                        MyModules.application_area.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID)
                    End If
                Case "APPLICATIONATTACH"
                    If (MyModules.application_attach Is Nothing) Then
                        MyModules.application_attach = New SimpleODM.supportmodule.uc_application_attach(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.APPLICATION_ID <> MyModules.application_attach.AppiD) Then
                        MyModules.application_attach.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID)
                    End If
                Case "APPLICATIONBA"
                    If (MyModules.application_ba Is Nothing) Then
                        MyModules.application_ba = New SimpleODM.supportmodule.uc_application_ba(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.APPLICATION_ID <> MyModules.application_ba.AppiD) Then
                        MyModules.application_ba.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID)
                    End If
                Case "APPLICATIONDESC"
                    If (MyModules.application_desc Is Nothing) Then
                        MyModules.application_desc = New SimpleODM.supportmodule.uc_application_desc(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.APPLICATION_ID <> MyModules.application_desc.AppiD) Then
                        MyModules.application_desc.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID)
                    End If
                Case "APPLICATIONREMARK"
                    If (MyModules.application_remark Is Nothing) Then
                        MyModules.application_remark = New SimpleODM.supportmodule.uc_application_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.APPLICATION_ID <> MyModules.application_remark.AppiD) Then
                        MyModules.application_remark.Loaddata(SimpleODMConfig.Defaults.APPLICATION_ID)
                    End If
                    '------------------------------------- Catalog ------------------------
                Case "CAT_ADDITIVEALIAS"
                    If (MyModules.cat_additive_alias Is Nothing) Then
                        MyModules.cat_additive_alias = New SimpleODM.supportmodule.uc_cat_additive_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID <> MyModules.cat_additive_alias.CatAdditiveID) Then
                        MyModules.cat_additive_alias.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID)
                    End If
                Case "CAT_ADDITIVESPEC"
                    If (MyModules.cat_additive_spec Is Nothing) Then
                        MyModules.cat_additive_spec = New SimpleODM.supportmodule.uc_cat_additive_spec(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID <> MyModules.cat_additive_spec.CatAdditiveID) Then
                        MyModules.cat_additive_spec.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID)
                    End If
                Case "CAT_ADDITIVETYPE"
                    If (MyModules.cat_additive_type Is Nothing) Then
                        MyModules.cat_additive_type = New SimpleODM.supportmodule.uc_cat_additive_type(SimpleODMConfig, SimpleUtil)
                    End If

                    MyModules.cat_additive_type.Loaddata()

                Case "CAT_ADDITIVEXREF"
                    If (MyModules.cat_additive_xref Is Nothing) Then
                        MyModules.cat_additive_xref = New SimpleODM.supportmodule.uc_cat_additive_xref(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID <> MyModules.cat_additive_xref.CatAdditiveID) Then
                        MyModules.cat_additive_xref.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_ADDITIVE_ID)
                    End If
                Case "CAT_EQUIPALIAS"
                    If (MyModules.Cat_equipment_alias Is Nothing) Then
                        MyModules.Cat_equipment_alias = New SimpleODM.supportmodule.uc_Cat_equipment_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.CATALOGUE_EQUIP_ID <> MyModules.Cat_equipment_alias.CatEquipID) Then
                        MyModules.Cat_equipment_alias.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_EQUIP_ID)
                    End If
                Case "CAT_EQUIPSPEC"
                    If (MyModules.Cat_equipment_spec Is Nothing) Then
                        MyModules.Cat_equipment_spec = New SimpleODM.supportmodule.uc_Cat_equipment_spec(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.CATALOGUE_EQUIP_ID <> MyModules.Cat_equipment_spec.CatEquipID) Then
                        MyModules.Cat_equipment_spec.Loaddata(SimpleODMConfig.Defaults.CATALOGUE_EQUIP_ID)
                    End If
                    '------------------------------------- Facility ------------------------

                Case "FACILITYALIAS"
                    If (MyModules.facility_alias Is Nothing) Then
                        MyModules.facility_alias = New SimpleODM.facilitymodule.uc_facility_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_alias.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_alias.FacilityType) Then
                        MyModules.facility_alias.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYAREA"
                    If (MyModules.facility_area Is Nothing) Then
                        MyModules.facility_area = New SimpleODM.facilitymodule.uc_facility_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_area.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_area.FacilityType) Then
                        MyModules.facility_area.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYBASERVICE"
                    If (MyModules.facility_ba_service Is Nothing) Then
                        MyModules.facility_ba_service = New SimpleODM.facilitymodule.uc_facility_ba_service(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_ba_service.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_ba_service.FacilityType) Then
                        MyModules.facility_ba_service.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYCLASS"
                    If (MyModules.facility_class Is Nothing) Then
                        MyModules.facility_class = New SimpleODM.facilitymodule.uc_facility_class(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_class.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_class.FacilityType) Then
                        MyModules.facility_class.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYDESCRIPTION"
                    If (MyModules.facility_description Is Nothing) Then
                        MyModules.facility_description = New SimpleODM.facilitymodule.uc_facility_description(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_description.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_description.FacilityType) Then
                        MyModules.facility_description.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYEQUIPMENT"
                    If (MyModules.facility_equipment Is Nothing) Then
                        MyModules.facility_equipment = New SimpleODM.facilitymodule.uc_facility_equipment(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_equipment.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_equipment.FacilityType) Then
                        MyModules.facility_equipment.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYFIELD"
                    If (MyModules.facility_field Is Nothing) Then
                        MyModules.facility_field = New SimpleODM.facilitymodule.uc_facility_field(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_field.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_field.FacilityType) Then
                        MyModules.facility_field.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYLICENSE"
                    If (MyModules.facility_license Is Nothing) Then
                        MyModules.facility_license = New SimpleODM.facilitymodule.uc_facility_license(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_license.FacilityType) Then
                        MyModules.facility_license.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYLICENSEALIAS"
                    If (MyModules.facility_license_alias Is Nothing) Then
                        MyModules.facility_license_alias = New SimpleODM.facilitymodule.uc_facility_license_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_license_alias.FacilityType) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.facility_license_alias.LicenseID) Then
                        MyModules.facility_license_alias.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "FACILITYLICENSEAREA"
                    If (MyModules.facility_license_area Is Nothing) Then
                        MyModules.facility_license_area = New SimpleODM.facilitymodule.uc_facility_license_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_license_area.FacilityType) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.facility_license_area.LicenseID) Then
                        MyModules.facility_license_area.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "FACILITYLICENSECOND"
                    If (MyModules.facility_license_cond Is Nothing) Then
                        MyModules.facility_license_cond = New SimpleODM.facilitymodule.uc_facility_license_cond(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_license_cond.FacilityType) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.facility_license_cond.LicenseID) Then
                        MyModules.facility_license_cond.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "FACILITYLICENSEREMARK"
                    If (MyModules.facility_license_remark Is Nothing) Then
                        MyModules.facility_license_remark = New SimpleODM.facilitymodule.uc_facility_license_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_license_remark.FacilityType) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.facility_license_remark.LicenseID) Then
                        MyModules.facility_license_remark.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "FACILITYLICENSESTATUS"
                    If (MyModules.facility_license_status Is Nothing) Then
                        MyModules.facility_license_status = New SimpleODM.facilitymodule.uc_facility_license_status(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_license_status.FacilityType) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.facility_license_status.LicenseID) Then
                        MyModules.facility_license_status.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "FACILITYLICENSETYPE"
                    If (MyModules.facility_license_type Is Nothing) Then
                        MyModules.facility_license_type = New SimpleODM.facilitymodule.uc_facility_license_type(SimpleODMConfig, SimpleUtil)
                    End If
                    ' If (SimpleODMconfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMconfig.Defaults.FACILITY_TYPE <> MyModules.facility_license.FacilityType) Then
                    MyModules.facility_license_type.Loaddata()
                    ' End If
                Case "FACILITYLICENSEVIOLATION"
                    If (MyModules.facility_license_violation Is Nothing) Then
                        MyModules.facility_license_violation = New SimpleODM.facilitymodule.uc_facility_license_violation(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_license.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_license_violation.FacilityType) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.facility_license_violation.LicenseID) Then
                        MyModules.facility_license_violation.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "FACILITYMAINTAIN"
                    If (MyModules.facility_maintain Is Nothing) Then
                        MyModules.facility_maintain = New SimpleODM.facilitymodule.uc_facility_maintain(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_maintain.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_maintain.FacilityType) Then
                        MyModules.facility_maintain.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYMAINTAINSTATUS"
                    If (MyModules.facility_maintain_status Is Nothing) Then
                        MyModules.facility_maintain_status = New SimpleODM.facilitymodule.uc_facility_maintain_status(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_maintain_status.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_maintain_status.FacilityType) Or (SimpleODMConfig.Defaults.MAINTAIN_ID <> MyModules.facility_maintain_status.MAINTAINID) Then
                        MyModules.facility_maintain_status.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE, SimpleODMConfig.Defaults.MAINTAIN_ID)
                    End If
                Case "FACILITYRATE"
                    If (MyModules.facility_rate Is Nothing) Then
                        MyModules.facility_rate = New SimpleODM.facilitymodule.uc_facility_rate(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_rate.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_rate.FacilityType) Then
                        MyModules.facility_rate.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYRESTRICTION"
                    If (MyModules.facility_restriction Is Nothing) Then
                        MyModules.facility_restriction = New SimpleODM.facilitymodule.uc_facility_restriction(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_restriction.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_restriction.FacilityType) Then
                        MyModules.facility_restriction.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYSTATUS"
                    If (MyModules.facility_status Is Nothing) Then
                        MyModules.facility_status = New SimpleODM.facilitymodule.uc_facility_status(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_status.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_status.FacilityType) Then
                        MyModules.facility_status.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYSUBSTANCE"
                    If (MyModules.facility_substance Is Nothing) Then
                        MyModules.facility_substance = New SimpleODM.facilitymodule.uc_facility_substance(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_substance.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_substance.FacilityType) Then
                        MyModules.facility_substance.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYVERSION"
                    If (MyModules.facility_version Is Nothing) Then
                        MyModules.facility_version = New SimpleODM.facilitymodule.uc_facility_version(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_version.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_version.FacilityType) Then
                        MyModules.facility_version.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                Case "FACILITYXREF"
                    If (MyModules.facility_xref Is Nothing) Then
                        MyModules.facility_xref = New SimpleODM.facilitymodule.uc_facility_xref(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FACILITY_ID <> MyModules.facility_xref.FacilityID) Or (SimpleODMConfig.Defaults.FACILITY_TYPE <> MyModules.facility_xref.FacilityType) Then
                        MyModules.facility_xref.Loaddata(SimpleODMConfig.Defaults.FACILITY_ID, SimpleODMConfig.Defaults.FACILITY_TYPE)
                    End If
                    '--------------------------------------Equipment ------------------------


                Case "EQUIPMENTALIAS"
                    If (MyModules.equipment_alias Is Nothing) Then
                        MyModules.equipment_alias = New SimpleODM.supportmodule.uc_equipment_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.EQUIPMENTID <> MyModules.equipment_alias.EquipmentID) Then
                        MyModules.equipment_alias.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID)
                    End If
                Case "EQUIPMENTBA"
                    If (MyModules.equipment_ba Is Nothing) Then
                        MyModules.equipment_ba = New SimpleODM.supportmodule.uc_equipment_ba(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.EQUIPMENTID <> MyModules.equipment_ba.EquipmentID) Then
                        MyModules.equipment_ba.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID)
                    End If
                Case "EQUIPMENTMAINTAIN"
                    If (MyModules.equipment_maintain Is Nothing) Then
                        MyModules.equipment_maintain = New SimpleODM.supportmodule.uc_equipment_maintain(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.EQUIPMENTID <> MyModules.equipment_maintain.EquipmentID) Then
                        MyModules.equipment_maintain.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID)
                    End If

                Case "EQUIPMENTMAINTAINSTATUS"
                    If (MyModules.equipment_maintain_status Is Nothing) Then
                        MyModules.equipment_maintain_status = New SimpleODM.supportmodule.uc_equipment_maintain_status(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.EQUIPMENTID <> MyModules.equipment_maintain_status.EquipmentID) Or (SimpleODMConfig.Defaults.EQUIP_MAIN_ID <> MyModules.equipment_maintain_status.EQUIPMAINID) Then
                        MyModules.equipment_maintain_status.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID, SimpleODMConfig.Defaults.EQUIP_MAIN_ID)
                    End If
                Case "EQUIPMENTMAINTAINTYPE"
                    If (MyModules.equipment_maintain_type Is Nothing) Then
                        MyModules.equipment_maintain_type = New SimpleODM.supportmodule.uc_equipment_maintain_type(SimpleODMConfig, SimpleUtil)
                    End If
                    '  If (SimpleODMconfig.Defaults.EQUIPMENTID <> MyModules.equipment_maintain.EquipmentID) Then
                    MyModules.equipment_maintain_type.Loaddata()
                    '  End If
                Case "EQUIPMENTSPEC"
                    If (MyModules.equipment_spec Is Nothing) Then
                        MyModules.equipment_spec = New SimpleODM.supportmodule.uc_equipment_spec(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.EQUIPMENTID <> MyModules.equipment_spec.EquipmentID) Then
                        MyModules.equipment_spec.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID)
                    End If
                Case "EQUIPMENTSPECSET"
                    If (MyModules.equipment_spec_set Is Nothing) Then
                        MyModules.equipment_spec_set = New SimpleODM.supportmodule.uc_equipment_spec_set(SimpleODMConfig, SimpleUtil)
                    End If
                    'If (SimpleODMconfig.Defaults.EQUIPMENTID <> MyModules.equipment_spec_set.EquipmentID) Then
                    MyModules.equipment_spec_set.Loaddata()
                    ' End If
                Case "EQUIPMENTSPECSETSPEC"
                    If (MyModules.equipment_spec_set_spec Is Nothing) Then
                        MyModules.equipment_spec_set_spec = New SimpleODM.supportmodule.uc_equipment_spec_set_spec(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.SPEC_SET_ID <> MyModules.equipment_spec_set_spec.SPECSETID) Then
                        MyModules.equipment_spec_set_spec.Loaddata(SimpleODMConfig.Defaults.SPEC_SET_ID)
                    End If

                Case "EQUIPMENTSTATUS"
                    If (MyModules.equipment_status Is Nothing) Then
                        MyModules.equipment_status = New SimpleODM.supportmodule.uc_equipment_status(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.EQUIPMENTID <> MyModules.equipment_status.EquipmentID) Then
                        MyModules.equipment_status.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID)
                    End If
                Case "EQUIPMENTUSAGE"
                    If (MyModules.equipment_use_stat Is Nothing) Then
                        MyModules.equipment_use_stat = New SimpleODM.supportmodule.uc_equipment_use_stat(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.EQUIPMENTID <> MyModules.equipment_use_stat.EquipmentID) Then
                        MyModules.equipment_use_stat.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID)
                    End If
                Case "EQUIPMENTCROSSREFERNCE"
                    If (MyModules.equipment_crossreference Is Nothing) Then
                        MyModules.equipment_crossreference = New SimpleODM.supportmodule.uc_equipment_crossreference(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.EQUIPMENTID <> MyModules.equipment_crossreference.EquipmentID) Then
                        MyModules.equipment_crossreference.Loaddata(SimpleODMConfig.Defaults.EQUIPMENTID)
                    End If

                    '-------------------------------------- BA ------------------------------
                Case "ENTITLEMENTSCOMPONENTS"
                    If (MyModules.ent_components Is Nothing) Then
                        MyModules.ent_components = New SimpleODM.BusinessAssociateModule.uc_ent_components(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.ENTITLEMENT_ID <> MyModules.ent_components.EntID) Then
                        MyModules.ent_components.Loaddata(SimpleODMConfig.Defaults.ENTITLEMENT_ID)
                    End If
                Case "ENTITLEMENTSFORGROUP"
                    If (MyModules.entitlement_group Is Nothing) Then
                        MyModules.entitlement_group = New SimpleODM.BusinessAssociateModule.uc_entitlement_group(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.SECURITY_GROUP_ID <> MyModules.entitlement_group.SECURITY_GROUPID) Then
                        MyModules.entitlement_group.Loaddata(SimpleODMConfig.Defaults.SECURITY_GROUP_ID)
                    End If
                Case "ENTITLEMENTGROUPS"
                    If (MyModules.entitlement_security_group Is Nothing) Then
                        MyModules.entitlement_security_group = New SimpleODM.BusinessAssociateModule.uc_entitlement_security_group(SimpleODMConfig, SimpleUtil)
                    End If

                    MyModules.entitlement_security_group.Loaddata()
                Case "ENTITLEMENTS"
                    If (MyModules.entitlement Is Nothing) Then
                        MyModules.entitlement = New SimpleODM.BusinessAssociateModule.uc_entitlement(SimpleODMConfig, SimpleUtil)
                    End If

                    MyModules.entitlement.Loaddata()
                Case "BUSINESSASSOCIATEENTITLEMENT"
                    If (MyModules.entitlement_security_ba Is Nothing) Then
                        MyModules.entitlement_security_ba = New SimpleODM.BusinessAssociateModule.uc_entitlement_security_ba(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.entitlement_security_ba.BA) Then
                        MyModules.entitlement_security_ba.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATEADDRESS"
                    If (MyModules.businessAssociate_address Is Nothing) Then
                        MyModules.businessAssociate_address = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_address(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_address.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_address.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATEALIAS"
                    If (MyModules.businessAssociate_alias Is Nothing) Then
                        MyModules.businessAssociate_alias = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_alias.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_alias.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATEAUTHORITY"
                    If (MyModules.businessAssociate_authority Is Nothing) Then
                        MyModules.businessAssociate_authority = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_authority(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_authority.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_authority.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATECONSURTUIMSERVICE"
                    If (MyModules.businessAssociate_consortuimservice Is Nothing) Then
                        MyModules.businessAssociate_consortuimservice = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_consortuimservice(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_consortuimservice.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_consortuimservice.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATECONTACTINFO"
                    If (MyModules.businessAssociate_contactinfo Is Nothing) Then
                        MyModules.businessAssociate_contactinfo = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_contactinfo(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_contactinfo.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_contactinfo.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATECREW"
                    If (MyModules.businessAssociate_crew Is Nothing) Then
                        MyModules.businessAssociate_crew = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_crew(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_crew.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_crew.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATECREWMEMEBERS"
                    If (MyModules.businessAssociate_crew_memeber Is Nothing) Then
                        MyModules.businessAssociate_crew_memeber = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_crew_memeber(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_crew_memeber.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.CREW_ID <> MyModules.businessAssociate_crew_memeber.MyCrewID) Then
                        MyModules.businessAssociate_crew_memeber.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.CREW_ID)
                    End If
                Case "BUSINESSASSOCIATEEMPLOYEE"
                    If (MyModules.businessAssociate_employee Is Nothing) Then
                        MyModules.businessAssociate_employee = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_employee(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_employee.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_employee.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATELICENSE"
                    If (MyModules.businessAssociate_license Is Nothing) Then
                        MyModules.businessAssociate_license = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_license.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATELICENSEALIAS"
                    If (MyModules.businessAssociate_license_alias Is Nothing) Then
                        MyModules.businessAssociate_license_alias = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_alias.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.businessAssociate_license_alias.MyLicenseID) Then
                        MyModules.businessAssociate_license_alias.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "BUSINESSASSOCIATELICENSEAREA"
                    If (MyModules.businessAssociate_license_area Is Nothing) Then
                        MyModules.businessAssociate_license_area = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_area.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.businessAssociate_license_area.MyLicenseID) Then
                        MyModules.businessAssociate_license_area.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "BUSINESSASSOCIATELICENSECONDITION"
                    If (MyModules.businessAssociate_license_cond Is Nothing) Then
                        MyModules.businessAssociate_license_cond = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_cond.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.businessAssociate_license_cond.MyLicenseID) Then
                        MyModules.businessAssociate_license_cond.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "BUSINESSASSOCIATELICENSETYPECONDVIOLATION"
                    If (MyModules.businessAssociate_license_violation Is Nothing) Then
                        MyModules.businessAssociate_license_violation = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_violation(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_violation.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.businessAssociate_license_violation.MyLicenseID) Or (SimpleODMConfig.Defaults.CONDITION_ID <> MyModules.businessAssociate_license_violation.MyConditionID) Then
                        MyModules.businessAssociate_license_violation.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID, SimpleODMConfig.Defaults.CONDITION_ID)
                    End If
                Case "BUSINESSASSOCIATELICENSEREMARK"
                    If (MyModules.businessAssociate_license_remark Is Nothing) Then
                        MyModules.businessAssociate_license_remark = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_remark.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.businessAssociate_license_remark.MyLicenseID) Then
                        MyModules.businessAssociate_license_remark.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "BUSINESSASSOCIATELICENSESTATUS"
                    If (MyModules.businessAssociate_license_status Is Nothing) Then
                        MyModules.businessAssociate_license_status = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_status(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_status.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.businessAssociate_license_status.MyLicenseID) Then
                        MyModules.businessAssociate_license_status.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "BUSINESSASSOCIATELICENSETYPE"
                    If (MyModules.businessAssociate_license_type Is Nothing) Then
                        MyModules.businessAssociate_license_type = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_type(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_type.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_license_type.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATELICENSETYPECONDTYPE"
                    If (MyModules.businessAssociate_license_cond_type Is Nothing) Then
                        MyModules.businessAssociate_license_cond_type = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond_type(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_cond_type.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.businessAssociate_license_status.MyLicenseID) Then
                        MyModules.businessAssociate_license_cond_type.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "BUSINESSASSOCIATELICENSETYPECONDTYPECODE"
                    If (MyModules.businessAssociate_license_cond_code Is Nothing) Then
                        MyModules.businessAssociate_license_cond_code = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_license_cond_code(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_license_cond_code.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.LICENSE_TYPE <> MyModules.businessAssociate_license_cond_code.MyLicenseTYPEID) Or (SimpleODMConfig.Defaults.CONDITION_TYPE <> MyModules.businessAssociate_license_cond_code.MyLicenseCONDTYPEID) Then
                        MyModules.businessAssociate_license_cond_code.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.LICENSE_TYPE, SimpleODMConfig.Defaults.CONDITION_TYPE)
                    End If
                Case "BUSINESSASSOCIATEORGANIZATION"
                    If (MyModules.businessAssociate_organization Is Nothing) Then
                        MyModules.businessAssociate_organization = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_organization(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_organization.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_organization.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATEPERMIT"
                    If (MyModules.businessAssociate_permit Is Nothing) Then
                        MyModules.businessAssociate_permit = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_permit(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_permit.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_permit.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATESERVICE"
                    If (MyModules.businessAssociate_services Is Nothing) Then
                        MyModules.businessAssociate_services = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_services(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_services.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_services.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATESERVICEADDRESS"
                    If (MyModules.businessAssociate_services_address Is Nothing) Then
                        MyModules.businessAssociate_services_address = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_services_address(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_services_address.MyBusinessAssociateID) Or (SimpleODMConfig.Defaults.SERVICE_TYPE <> MyModules.businessAssociate_services_address.MyServiceType) Or (SimpleODMConfig.Defaults.SERVICE_SEQ_NO <> MyModules.businessAssociate_services_address.MyServiceSeqNo) Then
                        MyModules.businessAssociate_services_address.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE, SimpleODMConfig.Defaults.SERVICE_TYPE, SimpleODMConfig.Defaults.SERVICE_SEQ_NO)
                    End If
                Case "BUSINESSASSOCIATEPREFERENCE"
                    If (MyModules.businessAssociate_preference Is Nothing) Then
                        MyModules.businessAssociate_preference = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_preference(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_preference.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_preference.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATECROSSREFERENCE"

                    If (MyModules.businessAssociate_crosspreference Is Nothing) Then
                        MyModules.businessAssociate_crosspreference = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_crosspreference(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_crosspreference.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_crosspreference.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                Case "BUSINESSASSOCIATEDESCRIPTION"

                    If (MyModules.businessAssociate_description Is Nothing) Then
                        MyModules.businessAssociate_description = New SimpleODM.BusinessAssociateModule.uc_businessAssociate_description(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE <> MyModules.businessAssociate_description.MyBusinessAssociateID) Then
                        MyModules.businessAssociate_description.Loaddata(SimpleODMConfig.Defaults.BUSINESS_ASSOCIATE)
                    End If
                    '-------------------------------------- End BA ------------------------------
                    '-------------------------------------Strat------------------------------------------


                Case "STRATFIELDINTREPAGE"
                    If (MyModules.strat_fld_interp_age Is Nothing) Then
                        MyModules.strat_fld_interp_age = New SimpleODM.stratigraphyandlithologyModule.uc_strat_fld_interp_age(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FIELD_STATION_ID <> MyModules.strat_fld_interp_age.STRATFIELDid) Then
                        MyModules.strat_fld_interp_age.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID, SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID)
                    End If
                Case "STRATFIELDNODEVERSION"
                    If (MyModules.strat_field_node_version Is Nothing) Then
                        MyModules.strat_field_node_version = New SimpleODM.stratigraphyandlithologyModule.uc_strat_node_version(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FIELD_STATION_ID <> MyModules.strat_field_node_version.STRATFIELDid) Then
                        MyModules.strat_field_node_version.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID, SimpleODMConfig.Defaults.NODE_ID, SimpleODMConfig.Defaults.SOURCE)
                    End If
                Case "STRATFIELDNODE"
                    If (MyModules.strat_field_node Is Nothing) Then
                        MyModules.strat_field_node = New SimpleODM.stratigraphyandlithologyModule.uc_strat_field_node(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FIELD_STATION_ID <> MyModules.strat_field_node.STRATFIELDid) Then
                        MyModules.strat_field_node.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID)
                    End If
                Case "STRATFIELDSECTION"
                    If (MyModules.strat_field_section Is Nothing) Then
                        MyModules.strat_field_section = New SimpleODM.stratigraphyandlithologyModule.uc_strat_field_section(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FIELD_STATION_ID <> MyModules.strat_field_section.STRATFIELDid) Then
                        MyModules.strat_field_section.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID)
                    End If
                Case "STRATFIELDFEOMETRY"
                    If (MyModules.strat_field_geometry Is Nothing) Then
                        MyModules.strat_field_geometry = New SimpleODM.stratigraphyandlithologyModule.uc_strat_field_geometry(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FIELD_STATION_ID <> MyModules.strat_field_geometry.STRATFIELDid) Then
                        MyModules.strat_field_geometry.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID)
                    End If

                Case "STRATFIELDACQUISITION"
                    If (MyModules.strat_field_acqtn Is Nothing) Then
                        MyModules.strat_field_acqtn = New SimpleODM.stratigraphyandlithologyModule.uc_strat_field_acqtn(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.FIELD_STATION_ID <> MyModules.strat_field_geometry.STRATFIELDid) Then
                        MyModules.strat_field_acqtn.Loaddata(SimpleODMConfig.Defaults.FIELD_STATION_ID, SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID)
                    End If

                Case "STRATNAMESETXREF"
                    If (MyModules.strat_xref Is Nothing) Then
                        MyModules.strat_xref = New SimpleODM.stratigraphyandlithologyModule.uc_strat_name_set_xref(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_xref.StratNameSetID) Then
                        MyModules.strat_xref.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET)
                    End If
                Case "STRATUNIT"
                    If (MyModules.strat_unit Is Nothing) Then
                        MyModules.strat_unit = New SimpleODM.stratigraphyandlithologyModule.uc_strat_unit(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_unit.StratNameSetID) Then
                        MyModules.strat_unit.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET)
                    End If
                Case "STRATUNITALIAS"
                    If (MyModules.strat_alias Is Nothing) Then
                        MyModules.strat_alias = New SimpleODM.stratigraphyandlithologyModule.uc_strat_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_alias.StratNameSetID) Or (SimpleODMConfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_alias.StratUnitID) Then
                        MyModules.strat_alias.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID)
                    End If

                Case "STRATUNITEQUIVALANCE"
                    If (MyModules.strat_equivalance Is Nothing) Then
                        MyModules.strat_equivalance = New SimpleODM.stratigraphyandlithologyModule.uc_strat_equivalance(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_equivalance.StratNameSetID) Or (SimpleODMConfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_equivalance.StratUnitID) Then
                        MyModules.strat_equivalance.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID)
                    End If
                Case "STRATUNITHIERARCHY"
                    If (MyModules.strat_hierarchy Is Nothing) Then
                        MyModules.strat_hierarchy = New SimpleODM.stratigraphyandlithologyModule.uc_strat_hierarchy(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_hierarchy.StratNameSetID) Or (SimpleODMConfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_hierarchy.StratUnitID) Then
                        MyModules.strat_hierarchy.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID)
                    End If
                Case "STRATHIERARCYDESCR"
                    If (MyModules.strat_hierarchy_desc Is Nothing) Then
                        MyModules.strat_hierarchy_desc = New SimpleODM.stratigraphyandlithologyModule.uc_strat_hierarchy_desc(SimpleODMConfig, SimpleUtil)
                    End If
                    'If (SimpleODMconfig.Defaults.STRAT_NAME_SET <> MyModules.strat_hierarchy_desc.StratNameSetID) Or (SimpleODMconfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_hierarchy_desc.StratUnitID) Then
                    MyModules.strat_hierarchy_desc.Loaddata()
                    ' End If
                Case "STRATUNITAGE"
                    If (MyModules.strat_unit_age Is Nothing) Then
                        MyModules.strat_unit_age = New SimpleODM.stratigraphyandlithologyModule.uc_strat_unit_age(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_unit_age.StratNameSetID) Or (SimpleODMConfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_unit_age.StratUnitID) Then
                        MyModules.strat_unit_age.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID)
                    End If
                Case "STRATUNITDESCRIPTION"
                    If (MyModules.strat_unit_description Is Nothing) Then
                        MyModules.strat_unit_description = New SimpleODM.stratigraphyandlithologyModule.uc_strat_unit_description(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_unit_description.StratNameSetID) Or (SimpleODMConfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_unit_description.StratUnitID) Then
                        MyModules.strat_unit_description.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID)
                    End If
                Case "STRATUNITTOPOLOGY"
                    If (MyModules.strat_topo_relation Is Nothing) Then
                        MyModules.strat_topo_relation = New SimpleODM.stratigraphyandlithologyModule.uc_strat_topo_relation(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_topo_relation.StratNameSetID) Or (SimpleODMConfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_topo_relation.StratUnitID) Then
                        MyModules.strat_topo_relation.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID)
                    End If

                Case "STRATCOLUMNAGE"
                    If (MyModules.strat_col_unit_age Is Nothing) Then
                        MyModules.strat_col_unit_age = New SimpleODM.stratigraphyandlithologyModule.uc_strat_col_unit_age(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_col_unit_age.StratNameSetID) Or (SimpleODMConfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_col_unit_age.StratUnitID) Or (SimpleODMConfig.Defaults.STRAT_COLUMN_ID <> MyModules.strat_col_unit_age.StratColumnID) Or (SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE <> MyModules.strat_col_unit_age.StratColumnSource) Or (SimpleODMConfig.Defaults.INTERP_ID <> MyModules.strat_col_unit_age.IntrepID) Then
                        MyModules.strat_col_unit_age.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_COLUMN_ID, SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID)
                    End If
                Case "STRATCOLUMNACQTN"
                    If (MyModules.strat_col_acqtn Is Nothing) Then
                        MyModules.strat_col_acqtn = New SimpleODM.stratigraphyandlithologyModule.uc_strat_col_acqtn(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_NAME_SET <> MyModules.strat_col_acqtn.StratNameSetID) Or (SimpleODMConfig.Defaults.STRAT_UNIT_ID <> MyModules.strat_col_acqtn.StratUnitID) Or (SimpleODMConfig.Defaults.STRAT_COLUMN_ID <> MyModules.strat_col_acqtn.StratColumnID) Or (SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE <> MyModules.strat_col_acqtn.StratColumnSource) Or (SimpleODMConfig.Defaults.INTERP_ID <> MyModules.strat_col_acqtn.IntrepID) Then
                        MyModules.strat_col_acqtn.Loaddata(SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_COLUMN_ID, SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID)
                    End If
                Case "STRATCOLUMNUNIT"
                    If (MyModules.strat_col_unit Is Nothing) Then
                        MyModules.strat_col_unit = New SimpleODM.stratigraphyandlithologyModule.uc_strat_col_unit(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_COLUMN_ID <> MyModules.strat_col_unit.StratColumnID) Or (SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE <> MyModules.strat_col_unit.StratColumnSource) Then
                        MyModules.strat_col_unit.Loaddata(SimpleODMConfig.Defaults.STRAT_COLUMN_ID, SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE)
                    End If
                Case "STRATCOLUMNCROSSREF"
                    If (MyModules.strat_col_xref Is Nothing) Then
                        MyModules.strat_col_xref = New SimpleODM.stratigraphyandlithologyModule.uc_strat_col_xref(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.STRAT_COLUMN_ID <> MyModules.strat_col_xref.StratColumnID) Or (SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE <> MyModules.strat_col_xref.StratColumnSource) Then
                        MyModules.strat_col_xref.Loaddata(SimpleODMConfig.Defaults.STRAT_COLUMN_ID, SimpleODMConfig.Defaults.STRAT_COLUMN_SOURCE)
                    End If

                Case "WELLFORMATION"
                    If (MyModules.strat_well_section Is Nothing) Then
                        MyModules.strat_well_section = New SimpleODM.stratigraphyandlithologyModule.uc_strat_well_section(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.strat_well_section.UWI) Then
                        MyModules.strat_well_section.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLSECTIONINTREPAGE"
                    If (MyModules.strat_well_interp_age Is Nothing) Then
                        MyModules.strat_well_interp_age = New SimpleODM.stratigraphyandlithologyModule.uc_well_strat_interp_age(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.strat_well_interp_age.UWI) Then
                        MyModules.strat_well_interp_age.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID)
                    End If
                Case "WELLSECTIONACQUISTION"
                    If (MyModules.strat_well_acqtn Is Nothing) Then
                        MyModules.strat_well_acqtn = New SimpleODM.stratigraphyandlithologyModule.uc_strat_well_acqtn(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.strat_well_acqtn.UWI) Then
                        MyModules.strat_well_acqtn.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.STRAT_NAME_SET, SimpleODMConfig.Defaults.STRAT_UNIT_ID, SimpleODMConfig.Defaults.INTERP_ID)
                    End If
                Case "WELLBORETUBULARCEMENT"
                    If (MyModules.wellboretubularcement Is Nothing) Then
                        MyModules.wellboretubularcement = New SimpleODM.wellmodule.uc_wellboretubularCement(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellboretubularcement.MyUWI) Then
                        MyModules.wellboretubularcement.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.TUBING_OBS_NO, SimpleODMConfig.Defaults.TUBING_TYPE, SimpleODMConfig.Defaults.TUBING_SOURCE)
                    End If
                Case "WELLBORETUBULAR"

                    If (MyModules.wellboreTubular Is Nothing) Then
                        MyModules.wellboreTubular = New SimpleODM.wellmodule.uc_wellboretubular(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellboreTubular.MyUWI) Or (SimpleODMConfig.Defaults.TUBING_OBS_NO <> MyModules.wellboreTubular.TubingObsNo) Then
                        MyModules.wellboreTubular.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.TUBING_OBS_NO)
                    End If
                Case "WELLBOREPAYZONE"

                    If (MyModules.wellborepayzone Is Nothing) Then
                        MyModules.wellborepayzone = New SimpleODM.wellmodule.uc_wellborepayzone(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellborepayzone.MyUWI) Or (SimpleODMConfig.Defaults.ZONE_ID <> MyModules.wellborepayzone.ZoneID) Then
                        MyModules.wellborepayzone.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ZONE_ID, SimpleODMConfig.Defaults.ZONE_SOURCE)
                    End If
                Case "WELLBOREZONEINTERVAL"

                    If (MyModules.wellborezoneinterval Is Nothing) Then
                        MyModules.wellborezoneinterval = New SimpleODM.wellmodule.uc_wellborezoneinterval(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellborezoneinterval.MyUWI) Or (SimpleODMConfig.Defaults.ZONE_ID <> MyModules.wellborezoneinterval.ZoneID) Then
                        MyModules.wellborezoneinterval.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ZONE_ID, SimpleODMConfig.Defaults.INTERVAL_ID)
                    End If
                Case "WELLBOREPOROUSINTERVAL"
                    If (MyModules.wellborePorousinterval Is Nothing) Then
                        MyModules.wellborePorousinterval = New SimpleODM.wellmodule.uc_wellborePorousinterval(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellborePorousinterval.MyUWI) Or (SimpleODMConfig.Defaults.POROUS_INTERVAL_ID <> MyModules.wellborePorousinterval.PorousIntervalID) Then
                        MyModules.wellborePorousinterval.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.POROUS_INTERVAL_ID)
                    End If
                Case "WELLZONEINTERVALVALUE"
                    If (MyModules.wellborezoneintervalvalue Is Nothing) Then
                        MyModules.wellborezoneintervalvalue = New SimpleODM.wellmodule.uc_wellborezoneintervalvalue(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellborezoneintervalvalue.MyUWI) Or (SimpleODMConfig.Defaults.ZONE_ID <> MyModules.wellborezoneintervalvalue.ZoneID) Or (SimpleODMConfig.Defaults.INTERVAL_ID <> MyModules.wellborezoneintervalvalue.IntervalID) Then
                        MyModules.wellborezoneintervalvalue.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ZONE_ID, SimpleODMConfig.Defaults.INTERVAL_ID, SimpleODMConfig.Defaults.ZONE_SOURCE, SimpleODMConfig.Defaults.INTERVAL_SOURCE)
                    End If
                Case "WELLBOREPLUGBACK"
                    If (MyModules.wellborePlugback Is Nothing) Then
                        MyModules.wellborePlugback = New SimpleODM.wellmodule.uc_wellborePlugback(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellborePlugback.MyUWI) Or (SimpleODMConfig.Defaults.PLUGBACK_OBS_NO <> MyModules.wellborePlugback.PlugbackgObsNo) Then
                        MyModules.wellborePlugback.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PLUGBACK_OBS_NO)
                    End If
                Case "PRESSUREAOF4PT"
                    If (MyModules.well_Pressureaod4pt Is Nothing) Then
                        MyModules.well_Pressureaod4pt = New SimpleODM.welltestandpressuremodule.uc_well_pressure_aof_4pt(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_Pressureaod4pt.UWI) Or (SimpleODMConfig.Defaults.PRESSURE_OBS_NO <> MyModules.well_Pressureaod4pt.PRESSURE_OBS_NO) Or (SimpleODMConfig.Defaults.AOF_OBS_NO <> MyModules.well_Pressureaod4pt.AOF_OBS_NO) Then
                        MyModules.well_Pressureaod4pt.loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PRESSURE_OBS_NO, SimpleODMConfig.Defaults.AOF_OBS_NO, SimpleODMConfig.Defaults.PRESSURE_AOF_SOURCE)
                    End If
                Case "PRESSUREAOF"
                    If (MyModules.well_Pressureaof Is Nothing) Then
                        MyModules.well_Pressureaof = New SimpleODM.welltestandpressuremodule.uc_well_pressure_aof(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_Pressureaof.myuwi) Or (SimpleODMConfig.Defaults.PRESSURE_OBS_NO <> MyModules.well_Pressureaof.PRESSURE_OBS_NO) Then
                        MyModules.well_Pressureaof.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PRESSURE_OBS_NO, SimpleODMConfig.Defaults.PRESSURE_SOURCE)
                    End If
                Case "PRESSUREBH"
                    If (MyModules.well_PressureBH Is Nothing) Then
                        MyModules.well_PressureBH = New SimpleODM.welltestandpressuremodule.uc_well_pressure_bh(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_PressureBH.UWI) Or (SimpleODMConfig.Defaults.PRESSURE_OBS_NO <> MyModules.well_PressureBH.PRESSURE_OBS_NO) Then
                        MyModules.well_PressureBH.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.PRESSURE_OBS_NO, SimpleODMConfig.Defaults.PRESSURE_SOURCE)
                    End If
                Case "WELLTESTPRESSURE"
                    If (MyModules.well_Pressure Is Nothing) Then
                        MyModules.well_Pressure = New SimpleODM.welltestandpressuremodule.uc_well_pressure(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_Pressure.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_Pressure.RUN_NUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_Pressure.TEST_NUM) Then
                        MyModules.well_Pressure.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.SOURCE)
                    End If

                Case "WELLLICVIOLATIONS"
                    If (MyModules.welllicense_violation Is Nothing) Then
                        MyModules.welllicense_violation = New SimpleODM.wellmodule.uc_well_license_violation(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.welllicense_violation.MyUWI) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.welllicense_violation.LicenceID) Then
                        MyModules.welllicense_violation.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID, SimpleODMConfig.Defaults.CONDITION_ID)
                    End If
                Case "WELLLICSTATUS"
                    If (MyModules.welllicense_status Is Nothing) Then
                        MyModules.welllicense_status = New SimpleODM.wellmodule.uc_well_license_status(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.welllicense_status.MyUWI) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.welllicense_status.LicenceID) Then
                        MyModules.welllicense_status.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "WELLLICCONDTION"
                    If (MyModules.welllicense_cond Is Nothing) Then
                        MyModules.welllicense_cond = New SimpleODM.wellmodule.uc_well_license_cond(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.welllicense_cond.MyUWI) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.welllicense_cond.LicenceID) Then
                        MyModules.welllicense_cond.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "WELLLICAREA"
                    If (MyModules.welllicense_area Is Nothing) Then
                        MyModules.welllicense_area = New SimpleODM.wellmodule.uc_well_license_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.welllicense_area.MyUWI) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.welllicense_area.LicenceID) Then
                        MyModules.welllicense_area.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If
                Case "WELLLICREMARK"
                    If (MyModules.welllicense_remark Is Nothing) Then
                        MyModules.welllicense_remark = New SimpleODM.wellmodule.uc_well_license_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.welllicense_remark.MyUWI) Or (SimpleODMConfig.Defaults.LICENSE_ID <> MyModules.welllicense_remark.LicenceID) Then
                        MyModules.welllicense_remark.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.LICENSE_ID)
                    End If


                Case "WELLTESTREMARKS"
                    If (MyModules.well_test_Remarks Is Nothing) Then
                        MyModules.well_test_Remarks = New SimpleODM.welltestandpressuremodule.uc_welltestRemarks(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Remarks.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Remarks._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Remarks._TESTNUM) Then
                        MyModules.well_test_Remarks.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If
                Case "WELLTESTMUD"
                    If (MyModules.well_test_Mud Is Nothing) Then
                        MyModules.well_test_Mud = New SimpleODM.welltestandpressuremodule.uc_welltestMud(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Mud.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Mud._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Mud._TESTNUM) Then
                        MyModules.well_test_Mud.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If
                Case "WELLTESTEQUIP"
                    If (MyModules.well_test_Equipment Is Nothing) Then
                        MyModules.well_test_Equipment = New SimpleODM.welltestandpressuremodule.uc_welltestEquipment(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Equipment.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Equipment._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Equipment._TESTNUM) Then
                        MyModules.well_test_Equipment.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If
                Case "WELLTESTSHUTOFF"
                    If (MyModules.well_test_Shutoff Is Nothing) Then
                        MyModules.well_test_Shutoff = New SimpleODM.welltestandpressuremodule.uc_welltestShutoff(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Shutoff.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Shutoff._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Shutoff._TESTNUM) Then
                        MyModules.well_test_Shutoff.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If
                Case "WELLTESTSTRAT"
                    If (MyModules.well_test_StratUnit Is Nothing) Then
                        MyModules.well_test_StratUnit = New SimpleODM.welltestandpressuremodule.uc_welltestStratunit(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_StratUnit.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_StratUnit._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_StratUnit._TESTNUM) Then
                        MyModules.well_test_StratUnit.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If
                Case "WELLTESTRECORDER"
                    If (MyModules.well_test_Recorder Is Nothing) Then
                        MyModules.well_test_Recorder = New SimpleODM.welltestandpressuremodule.uc_welltestRecorder(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Recorder.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Recorder._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Recorder._TESTNUM) Then
                        MyModules.well_test_Recorder.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If
                Case "WELLTESTPRESS"
                    If (MyModules.well_test_Press Is Nothing) Then
                        MyModules.well_test_Press = New SimpleODM.welltestandpressuremodule.uc_welltestPress(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Press.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Press._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Press._TESTNUM) Or (SimpleODMConfig.Defaults.PERIOD_OBS_NO <> MyModules.well_test_Press.period_obs_no) Then
                        MyModules.well_test_Press.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.PERIOD_OBS_NO, SimpleODMConfig.Defaults.PERIOD_TYPE)
                    End If
                Case "WELLTESTPRESSMEAS"
                    If (MyModules.well_test_PressMeas Is Nothing) Then
                        MyModules.well_test_PressMeas = New SimpleODM.welltestandpressuremodule.uc_welltestPressMeas(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_PressMeas.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_PressMeas._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_PressMeas._TESTNUM) Then
                        MyModules.well_test_PressMeas.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If
                Case "WELLTESTFLOW"
                    If (MyModules.well_test_Flow Is Nothing) Then
                        MyModules.well_test_Flow = New SimpleODM.welltestandpressuremodule.uc_welltestFlow(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Flow.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Flow._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Flow._TESTNUM) Or (SimpleODMConfig.Defaults.PERIOD_OBS_NO <> MyModules.well_test_Flow.period_obs_no) Then
                        MyModules.well_test_Flow.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.PERIOD_OBS_NO, SimpleODMConfig.Defaults.PERIOD_TYPE)
                    End If
                Case "WELLTESTFLOWMEAS"
                    If (MyModules.well_test_FlowMeas Is Nothing) Then
                        MyModules.well_test_FlowMeas = New SimpleODM.welltestandpressuremodule.uc_welltestFlowMeas(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_FlowMeas.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_FlowMeas._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_FlowMeas._TESTNUM) Then
                        MyModules.well_test_FlowMeas.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If
                Case "WELLTESTRECOVERY"
                    If (MyModules.well_test_Recovery Is Nothing) Then
                        MyModules.well_test_Recovery = New SimpleODM.welltestandpressuremodule.uc_welltestRecovery(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Recovery.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Recovery._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Recovery._TESTNUM) Or (SimpleODMConfig.Defaults.PERIOD_OBS_NO <> MyModules.well_test_Recovery.period_obs_no) Then
                        MyModules.well_test_Recovery.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.PERIOD_OBS_NO, SimpleODMConfig.Defaults.PERIOD_TYPE)
                    End If
                Case "WELLTESTCONTAMINANT"
                    If (MyModules.well_test_Contaminant Is Nothing) Then
                        MyModules.well_test_Contaminant = New SimpleODM.welltestandpressuremodule.uc_welltestContaminant(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Contaminant.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Contaminant._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Contaminant._TESTNUM) Or (SimpleODMConfig.Defaults.RECOVERY_OBS_NO <> MyModules.well_test_Contaminant.Recovery_Obs_no) Then
                        MyModules.well_test_Contaminant.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.RECOVERY_OBS_NO)
                    End If
                Case "WELLTESTPERIOD"
                    If (MyModules.well_test_Period Is Nothing) Then
                        MyModules.well_test_Period = New SimpleODM.welltestandpressuremodule.uc_welltest_period(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_Period.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_Period._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_Period._TESTNUM) Then
                        MyModules.well_test_Period.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.TESTTYPE)
                    End If

                    ' WindowsUIView1.Controller.Activate(WellTestAnalysisPage)
                Case "WELLTESTANALYSIS"
                    If (MyModules.well_test_analysis Is Nothing) Then
                        MyModules.well_test_analysis = New SimpleODM.welltestandpressuremodule.uc_welltestAnalysis(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_analysis.MyUWI) Or (SimpleODMConfig.Defaults.RUNNUM <> MyModules.well_test_analysis._RUNNUM) Or (SimpleODMConfig.Defaults.TESTNUM <> MyModules.well_test_analysis._TESTNUM) Or (SimpleODMConfig.Defaults.PERIOD_OBS_NO <> MyModules.well_test_analysis.period_obs_no) Then
                        MyModules.well_test_analysis.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM, SimpleODMConfig.Defaults.POOLNAME, SimpleODMConfig.Defaults.PERIOD_OBS_NO, SimpleODMConfig.Defaults.PERIOD_TYPE)
                    End If

                    ' WindowsUIView1.Controller.Activate(WellTestAnalysisPage)
                Case "WELLTESTCUSHION"
                    If (MyModules.well_test_cushion Is Nothing) Then
                        MyModules.well_test_cushion = New SimpleODM.welltestandpressuremodule.uc_WellTestCushion(SimpleODMConfig, SimpleUtil)
                    End If
                    ' ParentDoc = WindowsUIView1.Controller.View.ActiveDocument
                    If SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test_cushion.MyUWI Then
                        MyModules.well_test_cushion.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.TESTTYPE, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                        ' WindowsUIView1.Controller.Activate(WellTestCushionPage)
                    End If
                Case "WELLORIGIN"
                    If (MyModules.wellorigin Is Nothing) Then
                        MyModules.wellorigin = New SimpleODM.wellmodule.uc_wellorigin(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellorigin.WellSetUWI) Then
                        MyModules.wellorigin.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLBORE"
                    If (MyModules.wellbore Is Nothing) Then
                        MyModules.wellbore = New SimpleODM.wellmodule.uc_NewEditwellbore(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellbore.WellSetUWI) Or (SimpleODMConfig.Defaults.WELLBOREID <> MyModules.wellbore.WellboreUWI) Then
                        MyModules.wellbore.LoadWellBoreForWellSet(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELLBOREID)
                    End If

                    ' WindowsUIView1.Controller.Activate(WellBoreInformationManagementPage)
                Case "PRODSTRING"
                    If (MyModules.Productionstring Is Nothing) Then
                        MyModules.Productionstring = New SimpleODM.wellmodule.uc_prodstring(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELLBOREID <> MyModules.Productionstring.MyUWI) Or (SimpleODMConfig.Defaults.STRING_ID <> MyModules.Productionstring.STRING_ID) Then
                        MyModules.Productionstring.Loaddata(SimpleODMConfig.Defaults.WELLBOREID, TransType, SimpleODMConfig.Defaults.STRING_ID)
                    End If

                    ' WindowsUIView1.Controller.Activate(WellProdstringPage)
                Case "FORMATION"
                    If (MyModules.prodstringformation Is Nothing) Then
                        MyModules.prodstringformation = New SimpleODM.wellmodule.uc_prodstringformation(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.prodstringformation.MyUWI) Or (SimpleODMConfig.Defaults.STRING_ID <> MyModules.prodstringformation.STRING_ID) Then
                        MyModules.prodstringformation.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.STRING_ID, TransType, SimpleODMConfig.Defaults.PROD_STRING_SOURCE, SimpleODMConfig.Defaults.PR_STR_FORM_OBS_NO)
                    End If



                Case "COMPLETION"
                    If (MyModules.well_completion Is Nothing) Then
                        MyModules.well_completion = New SimpleODM.wellmodule.uc_completion(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_completion.MyUWI) Or (SimpleODMConfig.Defaults.STRING_ID <> MyModules.well_completion.string_ID) Or (SimpleODMConfig.Defaults.PR_STR_FORM_OBS_NO <> MyModules.well_completion.PR_STR_FORM_OBS_NO) Or (SimpleODMConfig.Defaults.COMPLETION_OBS_NO <> MyModules.well_completion.COMPLETION_OB_NO) Then
                        MyModules.well_completion.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PROD_STRING_SOURCE, SimpleODMConfig.Defaults.PR_STR_FORM_OBS_NO, TransType, SimpleODMConfig.Defaults.COMPLETION_OBS_NO, SimpleODMConfig.Defaults.COMPLETION_SOURCE)
                    End If
                    MyModules.well_completion.SetLabel()
                Case "WELLCOMPXREF"
                    If (MyModules.well_completion_xref Is Nothing) Then
                        MyModules.well_completion_xref = New SimpleODM.wellmodule.uc_completion_xref(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_completion_xref.MyUWI) Or (SimpleODMConfig.Defaults.COMPLETION_OBS_NO <> MyModules.well_completion_xref.COMPLETION_OBS) Then
                        MyModules.well_completion_xref.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.COMPLETION_OBS_NO)
                    End If
                Case "WELLCOMPSTRING2FORM"
                    If (MyModules.well_completion_String2Formation_link Is Nothing) Then
                        MyModules.well_completion_String2Formation_link = New SimpleODM.wellmodule.uc_completion_String2Formation_link(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_completion_String2Formation_link.WellSetUWI) Or (SimpleODMConfig.Defaults.COMPLETION_OBS_NO <> MyModules.well_completion_String2Formation_link.COMPLETIONOBSNO) Or (SimpleODMConfig.Defaults.COMPLETION_SOURCE <> MyModules.well_completion_String2Formation_link.COMPLETIONSOURCE) Then
                        MyModules.well_completion_String2Formation_link.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.COMPLETION_OBS_NO, SimpleODMConfig.Defaults.COMPLETION_SOURCE)
                    End If
                Case "PERFORATION"
                    If (MyModules.wellperf Is Nothing) Then
                        MyModules.wellperf = New SimpleODM.wellmodule.uc_preforation(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellperf.MyUWI) Or (SimpleODMConfig.Defaults.PERFORATION_OBS_NO <> MyModules.wellperf.PERFORATION_OBS_NO) Or (SimpleODMConfig.Defaults.COMPLETION_OBS_NO <> MyModules.wellperf.Completion_OB_NO) Then
                        MyModules.wellperf.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.COMPLETION_SOURCE, SimpleODMConfig.Defaults.COMPLETION_OBS_NO, TransType, SimpleODMConfig.Defaults.PERFORATION_OBS_NO)
                    End If
                Case "WELLCOMPCI"
                    If (MyModules.wellperf Is Nothing) Then
                        MyModules.wellperf = New SimpleODM.wellmodule.uc_preforation(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellperf.MyUWI) Or (SimpleODMConfig.Defaults.PERFORATION_OBS_NO <> MyModules.wellperf.PERFORATION_OBS_NO) Or (SimpleODMConfig.Defaults.COMPLETION_OBS_NO <> MyModules.wellperf.Completion_OB_NO) Then
                        MyModules.wellperf.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.COMPLETION_SOURCE, SimpleODMConfig.Defaults.COMPLETION_OBS_NO, TransType, SimpleODMConfig.Defaults.PERFORATION_OBS_NO)
                    End If


                    ' WindowsUIView1.Controller.Activate(WellPerforationPage)
                Case "PRODSTRINGEQUIPMENT"
                    If (MyModules.well_equipments Is Nothing) Then
                        MyModules.well_equipments = New SimpleODM.wellmodule.uc_well_equipment(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_equipments.MyUWI) Or (SimpleODMConfig.Defaults.STRING_ID <> MyModules.well_equipments._STRING_ID) Then

                        MyModules.well_equipments.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PROD_STRING_SOURCE)
                    End If

                    Me.MyModules.well_equipments.SetTitleLabel()
                    ' WindowsUIView1.Controller.Activate(ProdStringEquipmentPage)
                Case "WELLBOREEQUIPMENT"
                    If (MyModules.well_equipments Is Nothing) Then
                        MyModules.well_equipments = New SimpleODM.wellmodule.uc_well_equipment(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_equipments.MyUWI) Or (String.IsNullOrEmpty(MyModules.well_equipments._STRING_ID) = True) Then
                        MyModules.well_equipments.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, "", "", SimpleODMConfig.Defaults.EQUIPMENTID)
                    End If
                    MyModules.well_equipments._STRING_ID = ""
                    MyModules.well_equipments._STRING_SOURCE = ""
                    Me.MyModules.well_equipments.SetTitleLabel()
                Case "EQUIPMENTSEARCH"
                    If (MyModules.well_equipments_search Is Nothing) Then
                        MyModules.well_equipments_search = New SimpleODM.wellmodule.uc_well_equipment_search(SimpleODMConfig, SimpleUtil)
                    End If
                    '  If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_equipments_search.MyUWI) Then
                    MyModules.well_equipments_search.Loaddata()
                    ' End If
                    ' WindowsUIView1.Controller.Activate(WellboreEquipmentPage)
                Case "PRODSTRINGFACILITIES"
                    If (MyModules.Well_Facility Is Nothing) Then
                        MyModules.Well_Facility = New SimpleODM.wellmodule.uc_well_facility(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Facility.MyUWI) Or (SimpleODMConfig.Defaults.STRING_ID <> MyModules.Well_Facility.STRINGID) Then
                        MyModules.Well_Facility.loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PROD_STRING_SOURCE)
                    End If
                    MyModules.Well_Facility.SetTitle()
                Case "WBFACILITIES"
                    If (MyModules.Well_Facility Is Nothing) Then
                        MyModules.Well_Facility = New SimpleODM.wellmodule.uc_well_facility(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Facility.MyUWI) Or (SimpleODMConfig.Defaults.STRING_ID = "" And MyModules.Well_Facility.STRINGID.Length > 0) Then
                        MyModules.Well_Facility.loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType)
                    End If
                    MyModules.Well_Facility.STRINGID = ""
                    MyModules.Well_Facility.STRINGSOURCE = ""
                    MyModules.Well_Facility.SetTitle()
                    ' WindowsUIView1.Controller.Activate(ProdStringConnectedFacilitiesPage)
                Case "PRODSTRINGTEST"
                    If (MyModules.well_test Is Nothing) Then
                        MyModules.well_test = New SimpleODM.welltestandpressuremodule.uc_WellTest(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test.MyUWI) Then
                        MyModules.well_test.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType, SimpleODMConfig.Defaults.STRING_ID, SimpleODMConfig.Defaults.PROD_STRING_SOURCE, SimpleODMConfig.Defaults.PR_STR_FORM_OBS_NO, SimpleODMConfig.Defaults.RUNNUM, SimpleODMConfig.Defaults.TESTNUM)
                    End If

                Case "WELLDESIGN"
                    If (MyModules.wellboreDesigner Is Nothing) Then
                        MyModules.wellboreDesigner = New SimpleODM.wellmodule.uc_WellDesigner(SimpleODMConfig, SimpleUtil)
                    End If
                    '  ParentDoc = WindowsUIView1.Controller.Manager.View.ActiveDocument
                    MyModules.wellboreDesigner.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)

                    '  WindowsUIView1.Controller.Activate(WellTestdocumentPage)
                    '-------------------------------  End Well Desginer -------------------------------------------------
                    '-------------------------------- Start of Well Details Modules -------------------------------------
                Case "WELLREMARK"
                    If (MyModules.Wellremark Is Nothing) Then
                        MyModules.Wellremark = New SimpleODM.wellmodule.uc_well_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Wellremark.MyUWI) Then
                        MyModules.Wellremark.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If

                Case "WELLMISC"
                    If (MyModules.Wellmisc Is Nothing) Then
                        MyModules.Wellmisc = New SimpleODM.wellmodule.uc_well_misc_data(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Wellmisc.MyUWI) Then
                        MyModules.Wellmisc.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If

                Case "WELLAREA"
                    If (MyModules.Wellarea Is Nothing) Then
                        MyModules.Wellarea = New SimpleODM.wellmodule.uc_well_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Wellarea.MyUWI) Then
                        MyModules.Wellarea.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If

                Case "WELLTEST"
                    If (MyModules.well_test Is Nothing) Then
                        MyModules.well_test = New SimpleODM.welltestandpressuremodule.uc_WellTest(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_test.MyUWI) Then
                        MyModules.well_test.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, "EDIT")
                    End If


                Case "WELLPRESS"
                    If (MyModules.well_Pressure Is Nothing) Then
                        MyModules.well_Pressure = New SimpleODM.welltestandpressuremodule.uc_well_pressure(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_Pressure.MyUWI) Then
                        MyModules.well_Pressure.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If

                Case "WELLFACILITY"
                    If (MyModules.Well_Facility Is Nothing) Then
                        MyModules.Well_Facility = New SimpleODM.wellmodule.uc_well_facility(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Facility.MyUWI) Then
                        MyModules.Well_Facility.loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, TransType)
                    End If
                    MyModules.Well_Facility.SetTitle()
                Case "WELLVERSION"
                    If (MyModules.Wellversion Is Nothing) Then
                        MyModules.Wellversion = New SimpleODM.wellmodule.uc_wellversion(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Wellversion.MyUWI) Then
                        MyModules.Wellversion.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLALIAS"
                    If (MyModules.Wellalias Is Nothing) Then
                        MyModules.Wellalias = New SimpleODM.wellmodule.uc_well_alias(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Wellalias.MyUWI) Then
                        MyModules.Wellalias.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLLAND" 'Land Rights
                    If (MyModules.wellLandrights Is Nothing) Then
                        MyModules.wellLandrights = New SimpleODM.wellmodule.uc_well_Landrights(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellLandrights.MyUWI) Then
                        MyModules.wellLandrights.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLLIC"
                    If (MyModules.welllicense Is Nothing) Then
                        MyModules.welllicense = New SimpleODM.wellmodule.uc_well_Licenses(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.welllicense.MyUWI) Then
                        MyModules.welllicense.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If


                Case "WELLBASERVICES"
                    If (MyModules.Well_BA_Services Is Nothing) Then
                        MyModules.Well_BA_Services = New SimpleODM.wellmodule.uc_well_ba_services(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_BA_Services.MyUWI) Then
                        MyModules.Well_BA_Services.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If



                Case "WELLGEO" 'Geometry"
                    If (MyModules.Well_Geometry Is Nothing) Then
                        MyModules.Well_Geometry = New SimpleODM.wellmodule.uc_well_geometry(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Geometry.MyUWI) Then
                        MyModules.Well_Geometry.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_NUMERIC_ID)
                    End If
                Case "WELLSRV" 'Survey"
                    If (MyModules.WellDirSRVYManager Is Nothing) Then
                        MyModules.WellDirSRVYManager = New SimpleODM.wellmodule.uc_wellDirSurvey(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.WellDirSRVYManager.UWI) Then
                        MyModules.WellDirSRVYManager.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If


                Case "WELLSUPFAC" 'Support Facility"
                    If (MyModules.Well_Support_Facility Is Nothing) Then
                        MyModules.Well_Support_Facility = New SimpleODM.wellmodule.uc_well_support_facility(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Support_Facility.MyUWI) Then
                        MyModules.Well_Support_Facility.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLPERMIT" 'Permit"
                    If (MyModules.Well_Permit Is Nothing) Then
                        MyModules.Well_Permit = New SimpleODM.wellmodule.uc_well_License_permit_Types(SimpleODMConfig, SimpleUtil)
                    End If

                    MyModules.Well_Permit.LoadData()

                Case "WELLNODE"
                    If (MyModules.Well_Node Is Nothing) Then
                        MyModules.Well_Node = New SimpleODM.wellmodule.uc_well_node(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Node.MyUWI) Then
                        MyModules.Well_Node.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLNODEAREA"
                    If (MyModules.Well_Node_Area Is Nothing) Then
                        MyModules.Well_Node_Area = New SimpleODM.wellmodule.uc_well_node_area(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Node_Area.MyUWI) Then
                        MyModules.Well_Node_Area.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.NODE_ID)
                    End If
                Case "WELLNODEGEO"
                    If (MyModules.Well_Node_Geometry Is Nothing) Then
                        MyModules.Well_Node_Geometry = New SimpleODM.wellmodule.uc_well_node_geometry(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Node_Geometry.MyUWI) Then
                        MyModules.Well_Node_Geometry.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.NODE_ID)
                    End If
                Case "WELLNODEMETEANDBOUND"
                    If (MyModules.Well_Node_Metes_and_Bound Is Nothing) Then
                        MyModules.Well_Node_Metes_and_Bound = New SimpleODM.wellmodule.uc_well_node_metesandbound(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Node_Metes_and_Bound.MyUWI) Then
                        MyModules.Well_Node_Metes_and_Bound.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.NODE_ID)
                    End If
                Case "WELLNODESTRAT"
                    If (MyModules.Well_Node_Stratigraphy Is Nothing) Then
                        MyModules.Well_Node_Stratigraphy = New SimpleODM.wellmodule.uc_well_node_stratunit(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Node_Stratigraphy.MyUWI) Then
                        MyModules.Well_Node_Stratigraphy.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.NODE_ID)
                    End If
                Case "WELLACTIVITY"
                    If (MyModules.Well_Activity Is Nothing) Then
                        MyModules.Well_Activity = New SimpleODM.wellmodule.uc_well_activity(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Activity.MyUWI) Then
                        MyModules.Well_Activity.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLACTIVITYCAUSE"
                    If (MyModules.Well_Activity_Conditions_and_Events Is Nothing) Then
                        MyModules.Well_Activity_Conditions_and_Events = New SimpleODM.wellmodule.uc_well_activities_cause(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Activity_Conditions_and_Events.MyUWI) Then
                        MyModules.Well_Activity_Conditions_and_Events.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ACTIVITY_OBS_NO)
                    End If
                Case "WELLACTIVITYDURATION"
                    If (MyModules.Well_Activity_Duration Is Nothing) Then
                        MyModules.Well_Activity_Duration = New SimpleODM.wellmodule.uc_well_activities_duration(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Activity_Duration.MyUWI) Then
                        MyModules.Well_Activity_Duration.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.ACTIVITY_OBS_NO)
                    End If
                Case "WELLCORE"
                    If (MyModules.Well_Core Is Nothing) Then
                        MyModules.Well_Core = New SimpleODM.wellmodule.uc_well_core(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core.MyUWI) Then
                        MyModules.Well_Core.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLCOREANALYSIS"
                    If (MyModules.Well_Core_Aanlysis Is Nothing) Then
                        MyModules.Well_Core_Aanlysis = New SimpleODM.wellmodule.uc_well_core_analysis(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Aanlysis.MyUWI) Then
                        MyModules.Well_Core_Aanlysis.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID)
                    End If
                Case "WELLCOREANALYSISSAMPLE"
                    If (MyModules.Well_Core_Aanlysis_Sample Is Nothing) Then
                        MyModules.Well_Core_Aanlysis_Sample = New SimpleODM.wellmodule.uc_well_core_analysis_sample(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Aanlysis_Sample.MyUWI) Then
                        MyModules.Well_Core_Aanlysis_Sample.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO)
                    End If
                Case "WELLCOREANALYSISSAMPLEREMARK"
                    If (MyModules.Well_Core_Aanlysis_Sample_description Is Nothing) Then
                        MyModules.Well_Core_Aanlysis_Sample_description = New SimpleODM.wellmodule.uc_well_core_analysis_sample_description(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Aanlysis_Sample_description.MyUWI) Then
                        MyModules.Well_Core_Aanlysis_Sample_description.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO, SimpleODMConfig.Defaults.CORE_SAMPLE_ANALYSIS_OBS_NO, SimpleODMConfig.Defaults.CORE_SAMPLE_NUM)
                    End If
                Case "WELLCOREANALYSISSAMPLEDESCRIPTION"
                    If (MyModules.Well_Core_Aanlysis_Sample_remark Is Nothing) Then
                        MyModules.Well_Core_Aanlysis_Sample_remark = New SimpleODM.wellmodule.uc_well_core_analysis_sample_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Aanlysis_Sample_remark.MyUWI) Then
                        MyModules.Well_Core_Aanlysis_Sample_remark.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO, SimpleODMConfig.Defaults.CORE_SAMPLE_ANALYSIS_OBS_NO, SimpleODMConfig.Defaults.CORE_SAMPLE_NUM)
                    End If
                Case "WELLCOREANALYSISMETHOD"
                    If (MyModules.Well_Core_Aanlysis_Method Is Nothing) Then
                        MyModules.Well_Core_Aanlysis_Method = New SimpleODM.wellmodule.uc_well_core_analysis_method(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Aanlysis_Method.MyUWI) Then
                        MyModules.Well_Core_Aanlysis_Method.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO)
                    End If
                Case "WELLCOREANALYSISREMARK"
                    If (MyModules.Well_Core_Aanlysis_Remark Is Nothing) Then
                        MyModules.Well_Core_Aanlysis_Remark = New SimpleODM.wellmodule.uc_well_core_analysis_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Aanlysis_Remark.MyUWI) Then
                        MyModules.Well_Core_Aanlysis_Remark.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_ANALYSIS_OBS_NO)
                    End If
                Case "WELLCOREDESCRIPTION"
                    If (MyModules.Well_Core_Description Is Nothing) Then
                        MyModules.Well_Core_Description = New SimpleODM.wellmodule.uc_well_core_description(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Description.MyUWI) Then
                        MyModules.Well_Core_Description.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID)
                    End If
                Case "WELLCOREDESCRIPTIONSTRAT"
                    If (MyModules.Well_Core_Description_Stratigraphy Is Nothing) Then
                        MyModules.Well_Core_Description_Stratigraphy = New SimpleODM.wellmodule.uc_well_core_description_strat_unit(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Description.MyUWI) Then
                        MyModules.Well_Core_Description_Stratigraphy.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID, SimpleODMConfig.Defaults.CORE_DESCRIPTION_OBS_NO)
                    End If
                Case "WELLCORESHIFT"
                    If (MyModules.Well_Core_Shift Is Nothing) Then
                        MyModules.Well_Core_Shift = New SimpleODM.wellmodule.uc_well_core_shift(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Shift.MyUWI) Then
                        MyModules.Well_Core_Shift.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID)
                    End If
                Case "WELLCOREREMARK"
                    If (MyModules.Well_Core_Remark Is Nothing) Then
                        MyModules.Well_Core_Remark = New SimpleODM.wellmodule.uc_well_core_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Core_Remark.MyUWI) Then
                        MyModules.Well_Core_Remark.LoadData(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.CORE_ID)
                    End If

                Case "WELLMUDSAMPLE"
                    If (MyModules.well_mud_sample Is Nothing) Then
                        MyModules.well_mud_sample = New SimpleODM.wellmodule.uc_well_mud_sample(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_mud_sample.MyUWI) Then
                        MyModules.well_mud_sample.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLMUDRESISTIVITY"
                    If (MyModules.well_mud_sample_resistivity Is Nothing) Then
                        MyModules.well_mud_sample_resistivity = New SimpleODM.wellmodule.uc_well_mud_sample_resistivity(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_mud_sample_resistivity.MyUWI) Then
                        MyModules.well_mud_sample_resistivity.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.Mud_Sample_ID)
                    End If


                Case "WELLMUDPROPERTY"
                    If (MyModules.well_mud_sample_property Is Nothing) Then
                        MyModules.well_mud_sample_property = New SimpleODM.wellmodule.uc_well_mud_sample_property(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_mud_sample_property.MyUWI) Then
                        MyModules.well_mud_sample_property.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.Mud_Sample_ID)
                    End If
                Case "WELLAIRDRILL"
                    If (MyModules.well_air_drill Is Nothing) Then
                        MyModules.well_air_drill = New SimpleODM.wellmodule.uc_well_air_drill(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_air_drill.MyUWI) Then
                        MyModules.well_air_drill.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLAIRDRILLINTERVAL"
                    If (MyModules.well_air_drill_Interval Is Nothing) Then
                        MyModules.well_air_drill_Interval = New SimpleODM.wellmodule.uc_well_air_drill_Interval(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_air_drill_Interval.MyUWI) Then
                        MyModules.well_air_drill_Interval.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.AIR_DRILL_OBS_NO, SimpleODMConfig.Defaults.AIR_DRILL_INTERVAL_SOURCE)
                    End If
                Case "WELLAIRDRILLINTERVALPERIOD"
                    If (MyModules.well_air_drill_interval_period Is Nothing) Then
                        MyModules.well_air_drill_interval_period = New SimpleODM.wellmodule.uc_well_air_drill_interval_period(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_air_drill_interval_period.MyUWI) Then
                        MyModules.well_air_drill_interval_period.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.AIR_DRILL_OBS_NO, SimpleODMConfig.Defaults.AIR_DRILL_INTERVAL_SOURCE, SimpleODMConfig.Defaults.AIR_DRILL_INTERVAL_DEPTH_OBS_NO)
                    End If
                Case "WELLHORIZDRILL"
                    If (MyModules.well_Horiz_Drill Is Nothing) Then
                        MyModules.well_Horiz_Drill = New SimpleODM.wellmodule.uc_well_Horiz_Drill(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_Horiz_Drill.MyUWI) Then
                        MyModules.well_Horiz_Drill.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLHORIZDRILLKOP"
                    If (MyModules.well_Horiz_Drill_drill_kop Is Nothing) Then
                        MyModules.well_Horiz_Drill_drill_kop = New SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_kop(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_Horiz_Drill_drill_kop.MyUWI) Then
                        MyModules.well_Horiz_Drill_drill_kop.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLHORIZDRILLPOE"
                    If (MyModules.well_Horiz_Drill_drill_poe Is Nothing) Then
                        MyModules.well_Horiz_Drill_drill_poe = New SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_poe(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_Horiz_Drill_drill_poe.MyUWI) Then
                        MyModules.well_Horiz_Drill_drill_poe.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLHORIZDRILLSPOKE"
                    If (MyModules.well_Horiz_Drill_drill_spoke Is Nothing) Then
                        MyModules.well_Horiz_Drill_drill_spoke = New SimpleODM.wellmodule.uc_well_Horiz_Drill_drill_KOP_spoke(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_Horiz_Drill_drill_spoke.MyUWI) Then
                        MyModules.well_Horiz_Drill_drill_spoke.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.KICKOFF_POINT_OBSNO)
                    End If
                Case "WELLSHOW"
                    If (MyModules.well_show Is Nothing) Then
                        MyModules.well_show = New SimpleODM.wellmodule.uc_well_show(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_show.MyUWI) Then
                        MyModules.well_show.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                    End If
                Case "WELLSHOWREMARK"
                    If (MyModules.well_show_remark Is Nothing) Then
                        MyModules.well_show_remark = New SimpleODM.wellmodule.uc_well_show_remark(SimpleODMConfig, SimpleUtil)
                    End If
                    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_show_remark.MyUWI) Then
                        MyModules.well_show_remark.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.SHOW_TYPE, SimpleODMConfig.Defaults.SHOW_OBS_NO, SimpleODMConfig.Defaults.SHOW_SOURCE)
                    End If


            End Select
        Catch ex As Exception
            XtraMessageBox.Show("Error Init Module : " & ex.Message, "Simple ODM", Windows.Forms.MessageBoxButtons.OK)
        End Try
        '-------------------------------  End Well Desginer -------------------------------------------------
        RaiseEvent ShowControlOnTile(Title, pType, trans)
    End Sub

#End Region
#Region "Metro Windows Control"
    Public Property ActiveContainer As Page

    Private Sub WindowsUIView1_ControlReleased(sender As Object, e As DevExpress.XtraBars.Docking2010.Views.DeferredControlLoadEventArgs) Handles WindowsUIView1.ControlReleased

    End Sub

    Private Sub WindowsUIView1_ControlReleasing(sender As Object, e As DevExpress.XtraBars.Docking2010.Views.ControlReleasingEventArgs) Handles WindowsUIView1.ControlReleasing

    End Sub
    Public Sub WindowsUIView1_QueryControl(sender As System.Object, e As DevExpress.XtraBars.Docking2010.Views.QueryControlEventArgs) Handles WindowsUIView1.QueryControl

        DidIInitForms = True
        ' If SimpleODMconfig.ppdmcontext.ConnectionType = ConnectionTypeEnum.User Then
        'Dim x As New FlyoutProperties
        'x.Orientation = Orientation.Vertical
        'x.Alignment = ContentAlignment.MiddleCenter

        'WindowsUIView1.ShowFlyoutDialog(New DevExpress.XtraBars.Docking2010.Views.WindowsUI.Flyout(x))
        'WindowsUIView1.ShowSearchPanel()
        ' ActiveContainer = WindowsUIView1.ActiveContentContainer
        Select Case e.Document.Caption.Trim
            Case "SpreadSheet Reporting"
                If (MyModules.Excelreportbuilder Is Nothing) Then
                    MyModules.Excelreportbuilder = New SimpleODM.SharedLib.uc_spreadsheet(SimpleODMConfig, SimpleUtil)

                End If

                e.Control = MyModules.Excelreportbuilder
            Case "Simple Report Builder"
                If (MyModules.reportbuilder Is Nothing) Then
                    MyModules.reportbuilder = New SimpleODM.SharedLib.uc_simpleReporting_lite(SimpleODMConfig, SimpleUtil)

                End If

                e.Control = MyModules.reportbuilder
                '-----------------------------------------Log Data Management---------------------------------------------
            Case "Well Log Loader"
                If (MyModules.well_log_manager Is Nothing) Then
                    MyModules.well_log_manager = New SimpleODM.Logmodule.uc_log_manager(SimpleODMConfig, SimpleUtil)

                End If

                e.Control = MyModules.well_log_manager

            Case "Log File Loader"

                e.Control = MyModules.well_log_files_manager
            Case "Log Data Manager"

                If (MyModules.Well_Logs Is Nothing) Then
                    MyModules.Well_Logs = New SimpleODM.Logmodule.uc_log(SimpleODMConfig, SimpleUtil)
                End If

                'If MyModules.Well_Logs.WellON = True Then
                '    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Logs.MyUWI) Then
                '        MyModules.Well_Logs.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                '    End If
                'Else
                MyModules.Well_Logs.LoadManager()
                'End If

                e.Control = MyModules.Well_Logs

                '-------------------------------------------------
            Case "Well Log Curve"

                If (MyModules.Well_Log_curve Is Nothing) Then
                    MyModules.Well_Log_curve = New SimpleODM.Logmodule.uc_log_curve(SimpleODMConfig, SimpleUtil)
                End If
                If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Log_curve.MyUWI) Then
                    MyModules.Well_Log_curve.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI, SimpleODMConfig.Defaults.WELL_LOG_ID, SimpleODMConfig.Defaults.SOURCE)
                End If
                e.Control = MyModules.Well_Log_curve

            Case "Log Data Dictionary Manager"
                If (MyModules.Well_Log_dictionary Is Nothing) Then
                    MyModules.Well_Log_dictionary = New SimpleODM.Logmodule.uc_log_dictionary(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.Well_Log_dictionary
            Case "Log Job Data Management"
                e.Control = MyModules.Well_Log_job
            Case "Well Log Job"
                e.Control = MyModules.Well_Log_job
            Case "Log Data Dictionary Manager"
                e.Control = MyModules.Well_Log_dictionary
            Case "Well Logs"
                e.Control = MyModules.Well_Logs
            Case "Log Activity"
                e.Control = MyModules.Well_Logs_Activities
            Case "Well Log Loader"
                e.Control = MyModules.Well_Log_loader
            Case "Well Log Job Trip"                    ' Well Log Job Trip
                e.Control = MyModules.well_log_job_trip
            Case "Well Log Job Trip Pass"                  'Well Log Job Trip Pass
                e.Control = MyModules.well_log_job_trip_pass
            Case "Well Log Job Trip Remark"                   '  Well Log Job Trip Remark
                e.Control = MyModules.well_log_job_trip_remark
            Case "Well Log Dictionary Alias"                        ' Well Log Dictionary Alias
                e.Control = MyModules.well_log_dictionary_alias
            Case "Well Log Dictionary Curve"                           ' Well Log Dictionary Curve
                e.Control = MyModules.well_log_dictionary_curve
            Case "Well Log Dictionary Curve Classification"                 '    Well Log Dictionary Curve Classification
                e.Control = MyModules.well_log_dictionary_curve_cls
            Case "Well Log Dictionary Parameter"                             ' Well Log Dictionary Parameter
                e.Control = MyModules.well_log_dictionary_param
            Case "Well Log Dictionary Parameter Classification"                                '    Well Log Dictionary Parameter Classification
                e.Control = MyModules.well_log_dictionary_param_cls
            Case "Well Log Dictionary Parameter Classification Types"                    '        Well Log Dictionary Parameter Classification Types
                e.Control = MyModules.well_log_dictionary_param_cls_Types
            Case "Well Log Dictionary Parameter Values"             ' Well Log Dictionary Parameter Values
                e.Control = MyModules.well_log_dictionary_param_value
            Case "Well Log Dictionary Business Associate"                                 '    Well Log Dictionary Business Associate
                e.Control = MyModules.well_log_dictionary_ba
            Case "Well Log Dictionary Procedure"                  'Well Log Dictionary Procedure
                e.Control = MyModules.well_log_dictionary_proc
            Case "Well Log Classification"                          '  Well Log Classification
                e.Control = MyModules.well_log_class
            Case "Well Log Remark"                            ' Well Log Remark
                e.Control = MyModules.well_log_remark
            Case "Well Log Parameters"                   '  Well Log Parameters
                e.Control = MyModules.well_log_parameter
            Case "Well Log Parameters Array"
                e.Control = MyModules.well_log_parameter_array
                '------------------------------------ Lithology -------------------------------------------------------
            Case "Lithlogy Measured Section"
                If (MyModules.lith_measured_sec Is Nothing) Then
                    MyModules.lith_measured_sec = New SimpleODM.stratigraphyandlithologyModule.uc_lith_measured_sec(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.lith_measured_sec
            Case "Descriptive Record of Litholgy"
                e.Control = MyModules.lith_log
            Case "Descriptive Record of Litholgy Remarks"
                e.Control = MyModules.lith_log_remark
            Case "Descriptive Record of Litholgy BA Services"
                e.Control = MyModules.lith_log_ba_service
            Case "An Interpreted depositional Env. over Specified Interval of a Descriptive Record of Litholgy"
                e.Control = MyModules.lith_dep_ent_int
            Case "A Depth Interval Descriptive Record of Litholgy"
                e.Control = MyModules.lith_interval
            Case "Description of Rock Type Comprising an Interval"
                e.Control = MyModules.lith_rock_type
            Case "Description of Major or minor Rock Components"
                e.Control = MyModules.lith_component
            Case "Color Description of Major or minor Rock Components"
                e.Control = MyModules.lith_component_color
            Case "Measured Sizes in Rock Components"
                e.Control = MyModules.lith_comp_grain_size
            Case "Description of the Post Depositional Alterations"
                e.Control = MyModules.lith_diagenesis
            Case "Description of Grain or Crystal sizes of Rock Components"
                e.Control = MyModules.lith_grain_size
            Case "The Observed Porosity of Rock Components"
                e.Control = MyModules.lith_porosity
            Case "Description of Color of the Rock Type"
                e.Control = MyModules.lith_rock_color
            Case "Description of Physical Structure Rock Type"
                e.Control = MyModules.lith_rock_structure
            Case "Physical Structure within a major rocktype or Sub Interval"
                e.Control = MyModules.lith_structure



            Case "Lithology Sample"
                If (MyModules.lith_sample Is Nothing) Then
                    MyModules.lith_sample = New SimpleODM.stratigraphyandlithologyModule.uc_lith_sample(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.lith_sample
            Case "Lithology Sample Collection"
                e.Control = MyModules.lith_sample_collection
            Case "Lithology Sample Description"
                e.Control = MyModules.lith_sample_desc
            Case "Describe the Physical or Chemical Process used to perpare the Sample"
                e.Control = MyModules.lith_sample_prep
            Case "Describe the Methods used to perpare the Sample"
                e.Control = MyModules.lith_sample_prep_math
            Case "Other Descriptions to the Lithogloy Sample"
                e.Control = MyModules.lith_desc_other
                '------------------------------------ Reserve Entities and Classisifactions --------------------
            Case "Reserve Entities"
                If (MyModules.reserve_entity Is Nothing) Then
                    MyModules.reserve_entity = New SimpleODM.reservepoolproductionmodule.uc_reserve_entity(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.reserve_entity
            Case "Reserve Entity Economic Run"
                e.Control = MyModules.resent_eco_run
            Case "Reserve Entity Product"
                e.Control = MyModules.resent_product
            Case "Reserve Entity Economic Run Parameters"
                e.Control = MyModules.resent_eco_schedule
            Case "Reserve Entity Economic Run Volume"
                e.Control = MyModules.resent_eco_volume
            Case "Reserve Entity Product Properties"
                e.Control = MyModules.resent_prod_property
            Case "Reserve Entity Product Volume Summary"
                e.Control = MyModules.resent_vol_summary
            Case "Reserve Entity Product Volume Revision"
                e.Control = MyModules.resent_vol_revision
            Case "Reserve Class Formula"
                e.Control = MyModules.reserve_class_formula
            Case "Reserve Class Formula Calculation"
                e.Control = MyModules.reserve_class_calc
            Case "Reserve Classifications"
                e.Control = MyModules.reserve_class
            Case "Reserve Entities Class"
                e.Control = MyModules.resent_class
            Case "Reserve Entities Cross Reference"
                e.Control = MyModules.resent_xref
            Case "Reserve Revision Category"
                e.Control = MyModules.resent_revision_cat
            Case "Volume Unit Regime"
                e.Control = MyModules.resent_vol_regime
                '------------------------------------ PDEN Production Entities -------
            Case "Production Entites"
                If (MyModules.pden Is Nothing) Then
                    MyModules.pden = New SimpleODM.reservepoolproductionmodule.uc_pden(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.pden
            Case "Production Entity as Business Associate"
                e.Control = MyModules.pden_business_assoc
            Case "Production Entity as Area"
                e.Control = MyModules.pden_area
            Case "Production Entity as Other"
                e.Control = MyModules.pden_other
            Case "Production Entity as Well"
                e.Control = MyModules.pden_well
            Case "Production Entity as Production String"
                e.Control = MyModules.pden_prod_string
            Case "Production Entity as Reserve"
                e.Control = MyModules.pden_resent
            Case "Production Entity as Formation"
                e.Control = MyModules.pden_pr_str_form
            Case "Production Entity as Pool (Reservoir)"
                e.Control = MyModules.pden_pool
            Case "Production Entity as Lease"
                e.Control = MyModules.pden_lease_unit
            Case "Production Entity as Reserve Class"
                e.Control = MyModules.pden_resent_class
            Case "Production Entity as Facility"
                e.Control = MyModules.pden_facility
            Case "Production Entity as Field"
                e.Control = MyModules.pden_field
            Case "Production Entity Allocation Factor"
                e.Control = MyModules.pden_alloc_factor
            Case "Production Entity Decline Forcast Case"
                e.Control = MyModules.pden_decline_case
            Case "Production Entity Decline Forcast Case Conditions"
                e.Control = MyModules.pden_decline_condition
            Case "Production Entity Decline Forcast Case Segments"
                e.Control = MyModules.pden_decline_segment
            Case "Production Entity Flow Measurement"
                e.Control = MyModules.pden_flow_measurement
            Case "Production Entity In Area"
                e.Control = MyModules.pden_in_area
            Case "Production Entity Production String to PDEN Cross Reference"
                e.Control = MyModules.pden_prod_string_xref
            Case "Production Entity Production String Contribution Allowable"
                e.Control = MyModules.pden_pr_str_allowable
            Case "Production Entity Material Balance"
                e.Control = MyModules.pden_material_bal
            Case "Production Entity Operator History"
                e.Control = MyModules.pden_oper_hist
            Case "Production Entity Status History"
                e.Control = MyModules.pden_status_hist
            Case "Production Entity Volume Disposition"
                e.Control = MyModules.pden_vol_disposition
            Case "Production Entity Unit Regime"
                e.Control = MyModules.pden_vol_regime
            Case "Production Entity Volume Summary"
                e.Control = MyModules.pden_summary
            Case "Production Entity Volume Summary Other"
                e.Control = MyModules.pden_vol_summ_other
            Case "Production Entity Volume Analysis"
                e.Control = MyModules.pden_volume_analysis
            Case "Production Entity Cross Referernce"
                e.Control = MyModules.pden_xref
                '------------------------------------ POOL ---------------------------
            Case "Pool"
                If (MyModules.Pool Is Nothing) Then
                    MyModules.Pool = New SimpleODM.reservepoolproductionmodule.uc_pool(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.Pool
            Case "Pool Alias"
                e.Control = MyModules.pool_alias
            Case "Pool Area"
                e.Control = MyModules.pool_area
            Case "Pool Instrument"
                e.Control = MyModules.pool_instrument
            Case "Pool Version"
                e.Control = MyModules.pool_version
                '--------------------------------------- Area --------------------------------------

            Case "Area Management"
                If (MyModules.Area Is Nothing) Then
                    MyModules.Area = New SimpleODM.supportmodule.uc_area(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.Area
            Case "Area Alias"

                e.Control = MyModules.area_alias
            Case "Area Contain"
                e.Control = MyModules.area_contain
            Case "Area Description"
                e.Control = MyModules.area_description
                '--------------------------------------- Applications ----------------------------
            Case "Applications for Authority or Permission"
                If (MyModules.application4auth Is Nothing) Then
                    MyModules.application4auth = New SimpleODM.supportmodule.uc_application(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.application4auth
            Case "Applications Alias"
                e.Control = MyModules.application_alias
            Case "Applications Area"
                e.Control = MyModules.application_area
            Case "Applications Attachment"
                e.Control = MyModules.application_attach
            Case "Applications Business Associate"
                e.Control = MyModules.application_ba
            Case "Applications Description"
                e.Control = MyModules.application_desc
            Case "Applications Remark"
                e.Control = MyModules.application_remark
                '-------------------------------------- Catalog ----------------------------------

            Case "Catalog Additive Management"
                If (MyModules.cat_additive Is Nothing) Then
                    MyModules.cat_additive = New SimpleODM.supportmodule.uc_cat_additive(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.cat_additive
            Case "Catalog Equipment Management"
                If (MyModules.cat_equipment Is Nothing) Then
                    MyModules.cat_equipment = New SimpleODM.supportmodule.uc_Cat_equipment(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.cat_equipment
            Case "Catalog Additive Alias"
                e.Control = MyModules.cat_additive_alias
            Case "Catalog Additive Specification"
                e.Control = MyModules.cat_additive_spec
            Case "Catalog Additive Type"
                e.Control = MyModules.cat_additive_type
            Case "Catalog Additive Cross Reference"
                e.Control = MyModules.cat_additive_xref
            Case "Catalog Equipment Alias"
                e.Control = MyModules.Cat_equipment_alias
            Case "Catalog Equipment Specification"
                e.Control = MyModules.Cat_equipment_spec
                '-------------------------------------- Facility ---------------------------------
            Case "Facility Management"
                If (MyModules.FacilityModule Is Nothing) Then
                    MyModules.FacilityModule = New SimpleODM.facilitymodule.uc_facility(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.FacilityModule

            Case "Facility Alias"
                e.Control = MyModules.facility_alias
            Case "Facility Area"
                e.Control = MyModules.facility_area
            Case "Facility BA Service"
                e.Control = MyModules.facility_ba_service
            Case "Facility Classification"
                e.Control = MyModules.facility_class
            Case "Facility Description"
                e.Control = MyModules.facility_description
            Case "Facility Equipment"
                e.Control = MyModules.facility_equipment
            Case "Facility Field"
                e.Control = MyModules.facility_field
            Case "Facility License"
                e.Control = MyModules.facility_license
            Case "Facility License Alias"
                e.Control = MyModules.facility_license_alias
            Case "Facility License Area"
                e.Control = MyModules.facility_license_area
            Case "Facility License Conditions"
                e.Control = MyModules.facility_license_cond
            Case "Facility License Remarks"
                e.Control = MyModules.facility_license_remark
            Case "Facility License Status"
                e.Control = MyModules.facility_license_status
            Case "Facility License Type"
                e.Control = MyModules.facility_license_type
            Case "Facility License Violations"
                e.Control = MyModules.facility_license_violation
            Case "Facility Maintainance"
                e.Control = MyModules.facility_maintain
            Case "Facility Maintainance Status"
                e.Control = MyModules.facility_maintain_status
            Case "Facility Rate"
                e.Control = MyModules.facility_rate
            Case "Facility Restrictions"
                e.Control = MyModules.facility_restriction
            Case "Facility Status"
                e.Control = MyModules.facility_status
            Case "Facility Substance"
                e.Control = MyModules.facility_substance
            Case "Facility Version"
                e.Control = MyModules.facility_version
            Case "Facility Cross Reference"
                e.Control = MyModules.facility_xref
                ''-------------------------------------- Equipment ------------------------------
            Case "Equipment"
                If (MyModules.equipment Is Nothing) Then
                    MyModules.equipment = New SimpleODM.supportmodule.uc_equipment(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.equipment

            Case "Equipment Alias"
                e.Control = MyModules.equipment_alias
            Case "Equipment Business Associate"
                e.Control = MyModules.equipment_ba
            Case "Equipment Maintainance"
                e.Control = MyModules.equipment_maintain
            Case "Equipment Maintainance Status"
                e.Control = MyModules.equipment_maintain_status
            Case "Equipment Maintainance Type"
                e.Control = MyModules.equipment_maintain_type
            Case "Equipment Specification"
                e.Control = MyModules.equipment_spec
            Case "Equipment Specification Set"
                e.Control = MyModules.equipment_spec_set
            Case "Equipment Specification Set Spec"
                e.Control = MyModules.equipment_spec_set_spec
            Case "Equipment Status"
                e.Control = MyModules.equipment_status
            Case "Equipment Usage Statistics"
                e.Control = MyModules.equipment_use_stat
            Case "Equipment Cross Reference"
                e.Control = MyModules.equipment_crossreference
                '-------------------------------------- BA ------------------------------
            Case "Business Associate Data Management"
                If (MyModules.businessAssociate Is Nothing) Then
                    MyModules.businessAssociate = New SimpleODM.BusinessAssociateModule.uc_businessAssociate(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.businessAssociate
            Case "Business Associate Address"
                e.Control = MyModules.businessAssociate_address
            Case "Business Associate Alias"
                e.Control = MyModules.businessAssociate_alias
            Case "Business Associate Authority"
                e.Control = MyModules.businessAssociate_authority
            Case "Business Associate Consurtuim Service"
                e.Control = MyModules.businessAssociate_consortuimservice
            Case "Business Associate Contact Information"
                e.Control = MyModules.businessAssociate_contactinfo
            Case "Business Associate Crew"
                e.Control = MyModules.businessAssociate_crew
            Case "Business Associate Crew Memebers"

                e.Control = MyModules.businessAssociate_crew_memeber
            Case "Business Associate Description"
                e.Control = MyModules.businessAssociate_description
            Case "Business Associate Employee"
                e.Control = MyModules.businessAssociate_employee
            Case "Business Associate License"
                e.Control = MyModules.businessAssociate_license
            Case "Business Associate License Alias"
                e.Control = MyModules.businessAssociate_license_alias
            Case "Business Associate License Area"
                e.Control = MyModules.businessAssociate_license_area
            Case "Business Associate License Condition"
                e.Control = MyModules.businessAssociate_license_cond
            Case "Business Associate License Condition Violation"
                e.Control = MyModules.businessAssociate_license_violation
            Case "Business Associate License Remark"
                e.Control = MyModules.businessAssociate_license_remark
            Case "Business Associate License Status"
                e.Control = MyModules.businessAssociate_license_status
            Case "Business Associate License Type"
                e.Control = MyModules.businessAssociate_license_type
            Case "Business Associate License Condition Type"
                e.Control = MyModules.businessAssociate_license_cond_type
            Case "Business Associate License Condition Type Code"
                e.Control = MyModules.businessAssociate_license_cond_code
            Case "Business Associate Organization"
                e.Control = MyModules.businessAssociate_organization
            Case "Business Associate Permit"
                e.Control = MyModules.businessAssociate_permit
            Case "Business Associate Service"
                e.Control = MyModules.businessAssociate_services
            Case "Business Associate Service Address"
                e.Control = MyModules.businessAssociate_services_address
            Case "Business Associate Preference"
                e.Control = MyModules.businessAssociate_preference
            Case "Business Associate Cross Reference"
                e.Control = MyModules.businessAssociate_crosspreference
            Case "Security Groups Entitlements"
                e.Control = MyModules.entitlement_group
            Case "Security Groups"
                e.Control = MyModules.entitlement_security_group
            Case "Entitlements Type"
                e.Control = MyModules.entitlement
            Case "Business Associate Entitlement"
                e.Control = MyModules.entitlement_security_ba
            Case "Entitlements Components"
                e.Control = MyModules.ent_components
                '-------------------------------------- End BA ------------------------------
            Case "Land Data Management"
                If (MyModules.wellLandrights Is Nothing) Then
                    MyModules.wellLandrights = New SimpleODM.wellmodule.uc_well_Landrights(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.wellLandrights
                ' -------------------------------------- Well --------------------------------------------
            Case "Well Set Management"
                If (MyModules.WellModule Is Nothing) Then
                    MyModules.WellModule = New SimpleODM.wellmodule.uc_NewEditWell(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.WellModule
            Case "Well Show"
                e.Control = MyModules.well_show
            Case "Well Show Remark"
                e.Control = MyModules.well_show_remark
            Case "Mud Sample"
                e.Control = MyModules.well_mud_sample
            Case "Mud Sample Resistivity"
                e.Control = MyModules.well_mud_sample_resistivity
            Case "Mud Sample Property"
                e.Control = MyModules.well_mud_sample_property
            Case "Air Drill"

                e.Control = MyModules.well_air_drill
            Case "Air Drill Interval"
                e.Control = MyModules.well_air_drill_Interval
            Case "Air Drill Interval Period"
                e.Control = MyModules.well_air_drill_interval_period
            Case "Horiz Drill"
                e.Control = MyModules.well_Horiz_Drill
            Case "Horiz Drill KOP"
                e.Control = MyModules.well_Horiz_Drill_drill_kop
            Case "Horiz Drill POE"
                e.Control = MyModules.well_Horiz_Drill_drill_poe
            Case "Horiz Drill SPOKE"
                e.Control = MyModules.well_Horiz_Drill_drill_spoke
            Case "Well Area"
                e.Control = MyModules.Wellarea
            Case "Well Misc."
                e.Control = MyModules.Wellmisc
            Case "Well Remark"
                e.Control = MyModules.Wellremark
            Case "Well Activity"
                e.Control = MyModules.Well_Activity
            Case "Well Activity Conditions and Events"
                e.Control = MyModules.Well_Activity_Conditions_and_Events
            Case "Well Activity Duration"
                e.Control = MyModules.Well_Activity_Duration
            Case "Well Node"
                e.Control = MyModules.Well_Node
            Case "Well Node Area"
                e.Control = MyModules.Well_Node_Area
            Case "Well Node Geometry"
                e.Control = MyModules.Well_Node_Geometry
            Case "Well Node Metes and Bound"
                e.Control = MyModules.Well_Node_Metes_and_Bound
            Case "Well Node Stratigraphy"
                e.Control = MyModules.Well_Node
            Case "Well Core"
                e.Control = MyModules.Well_Core
            Case "Well Core Analysis"
                e.Control = MyModules.Well_Core_Aanlysis
            Case "Well Core Analysis Sample"
                e.Control = MyModules.Well_Core_Aanlysis_Sample
            Case "Well Core Analysis Sample Description"
                e.Control = MyModules.Well_Core_Aanlysis_Sample_description
            Case "Well Core Analysis Sample Remarks"
                e.Control = MyModules.Well_Core_Aanlysis_Sample_remark
            Case "Well Core Analysis Method"
                e.Control = MyModules.Well_Core_Aanlysis_Method
            Case "Well Core Analysis Remark"
                e.Control = MyModules.Well_Core_Aanlysis_Remark
            Case "Well Core Description"
                e.Control = MyModules.Well_Core_Description
            Case "Well Core Description Stratigraphy"
                e.Control = MyModules.Well_Core_Description_Stratigraphy
            Case "Well Core Shift"
                e.Control = MyModules.Well_Core_Shift
            Case "Well Core Remark"
                e.Control = MyModules.Well_Core_Remark
            Case "Well Facility"
                e.Control = MyModules.Well_Facility
            Case "Well Interpretation"
            Case "Well BA Services"
                e.Control = MyModules.Well_BA_Services


                '    If (MyModules.Well_Logs Is Nothing) Then
                '        MyModules.Well_Logs = New SimpleODM.Logmodule.uc_log(SimpleODMConfig, SimpleUtil)
                '    End If
                '    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Logs.MyUWI) Then
                '        MyModules.Well_Logs.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                '    End If
                'Case "WELLLOGJOB"

                '    If (MyModules.Well_Log_job Is Nothing) Then
                '        MyModules.Well_Log_job = New SimpleODM.Logmodule.uc_log_job(SimpleODMConfig, SimpleUtil)
                '    End If
                '    If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.Well_Log_job.MyUWI) Then
                '        MyModules.Well_Log_job.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                '    End If


            Case "Well Geometry"
                e.Control = MyModules.Well_Geometry
            Case "Well Support Facility"
                e.Control = MyModules.Well_Support_Facility
            Case "Well Permit"
                e.Control = MyModules.Well_Permit
            Case "Well Survey"
                e.Control = MyModules.WellDirSRVYManager
            Case "Well Survey Geometry"
                e.Control = MyModules.WellDirSRVYManager
            Case "Well Survey Station"
                e.Control = MyModules.WellDirSRVYManager
            Case "Well Land Rights"
                e.Control = MyModules.wellLandrights
            Case "Well version"
                e.Control = MyModules.Wellversion
            Case "Well Alias"
                e.Control = MyModules.Wellalias
            Case "Well License"
                e.Control = MyModules.welllicense
            Case "Well License Violation"
                e.Control = MyModules.welllicense_violation
            Case "Well License Condition"
                e.Control = MyModules.welllicense_cond
            Case "Well License Area"
                e.Control = MyModules.welllicense_area
            Case "Well License Remark"
                e.Control = MyModules.welllicense_remark
            Case "Well License Status"
                e.Control = MyModules.welllicense_status
                ' --------------------------------------End Well --------------------------------------------
                '    '--------------------------------------  ETL Module -----------------------------------------------------------------
                'Case "Mapping From To"
                '    If (MyModules.MappingData Is Nothing) Then
                '        MyModules.MappingData = New SimpleODM.etlmodule.uc_MAppingFromSource2Target()
                '    End If
                '    MyModules.MappingData.parentEtl = ETLConfig
                '    e.Control = MyModules.Table_Mapping
                'Case "Table Mapping"
                '    If (MyModules.Table_Mapping Is Nothing) Then
                '        MyModules.Table_Mapping = New SimpleODM.etlmodule.uc_TableMapping(SimpleODMConfig)
                '    End If
                '    MyModules.Table_Mapping.ETL = ETLConfig
                '    e.Control = MyModules.Table_Mapping
                'Case "Spreadsheet Mapping"
                '    If (MyModules.spreadsheet_mapping Is Nothing) Then
                '        MyModules.spreadsheet_mapping = New SimpleODM.etlmodule.uc_ExcelMapping(SimpleODMConfig)
                '    End If
                '    MyModules.spreadsheet_mapping.ETL = ETLConfig
                '    e.Control = MyModules.spreadsheet_mapping
                'Case "WorkFlow Creator"
                '    If (MyModules.WorkFlowManager Is Nothing) Then
                '        MyModules.WorkFlowManager = New SimpleODM.etlmodule.uc_WorkFlow(SimpleODMConfig)
                '    End If
                '    MyModules.WorkFlowManager.ParentETL = ETLConfig
                '    e.Control = MyModules.WorkFlowManager
                '    '-----------------------------------------------End of ETL ------------------------------------------------------------------------------
                '------------------------------------------ Well test Screens ------------------------------------------
            Case "Well Tests"
                If (MyModules.well_test Is Nothing) Then
                    MyModules.well_test = New SimpleODM.welltestandpressuremodule.uc_WellTest(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.well_test
            Case "Well Test Cushion"
                e.Control = MyModules.well_test_cushion
            Case "Well Test Analysis"
                e.Control = MyModules.well_test_analysis
            Case "Well Test Mud"
                e.Control = MyModules.well_test_Mud
            Case "Well Test Equipment"
                e.Control = MyModules.well_test_Equipment
            Case "Well Test Shutoff"
                e.Control = MyModules.well_test_Shutoff
            Case "Well Test Strat and Formation"
                e.Control = MyModules.well_test_StratUnit
            Case "Well Test Recorder"
                e.Control = MyModules.well_test_Recorder
            Case "Well Test Pressure"
                e.Control = MyModules.well_test_Press
            Case "Well Test Pressure Measurment"
                e.Control = MyModules.well_test_PressMeas
            Case "Well Test Flow"
                e.Control = MyModules.well_test_Flow
            Case "Well Test Flow Measurment"
                e.Control = MyModules.well_test_FlowMeas
            Case "Well Test Recovery"
                e.Control = MyModules.well_test_Recovery
            Case "Well Test Contaminant"
                e.Control = MyModules.well_test_Contaminant
            Case "Well Test Period"
                e.Control = MyModules.well_test_Period
            Case "Well Test Remark"
                e.Control = MyModules.well_test_Remarks
            Case "Well Test Gas Pressure AOF 4pt"

                e.Control = MyModules.well_Pressureaod4pt
            Case "Well Test Gas Pressure AOF"

                e.Control = MyModules.well_Pressureaof
            Case "Well Test Gas Pressure BH"

                e.Control = MyModules.well_PressureBH
            Case "Well Test Gas Pressure"

                e.Control = MyModules.well_Pressure
                '----------------------------------------- Well Gas Pressure Modules --------------------------------
            Case "Well Gas Pressure AOF 4pt"
                e.Control = MyModules.well_Pressureaod4pt
            Case "Well Gas Pressure AOF"
                e.Control = MyModules.well_Pressureaof
            Case "Well Gas Pressure BH"
                e.Control = MyModules.well_PressureBH
            Case "Well Gas Pressure"

                e.Control = MyModules.well_Pressure
                '------------------------------------------ end Well test Screens ------------------------------------------
                '--------------------------------------------------------------- Well Designer ---------------------------
            Case "Well Design/Build Management"
                e.Control = MyModules.wellboreDesigner
            Case "Well Origin"
                If (MyModules.wellorigin Is Nothing) Then
                    MyModules.wellorigin = New SimpleODM.wellmodule.uc_wellorigin(SimpleODMConfig, SimpleUtil)
                End If
                If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.wellorigin.WellSetUWI) Then
                    MyModules.wellorigin.Loaddata(SimpleODMConfig.Defaults.WELL_XREFMyUWI)
                End If
                e.Control = MyModules.wellorigin
            Case "Wellbore Tubulars"
                e.Control = MyModules.wellboreTubular
            Case "Wellbore Tubulars Cement"
                e.Control = MyModules.wellboretubularcement
            Case "Wellbore Payzone"
                e.Control = MyModules.wellborepayzone
            Case "Wellbore Zone Interval"
                e.Control = MyModules.wellborezoneinterval
            Case "Wellbore Zone Interval Value"
                e.Control = MyModules.wellborezoneintervalvalue
            Case "Wellbore Porous Interval"
                e.Control = MyModules.wellborePorousinterval
            Case "Wellbore Plugback"
                e.Control = MyModules.wellborePlugback
            Case "WellBore Information Management"
                e.Control = MyModules.wellbore
            Case "Well Production String"
                e.Control = MyModules.Productionstring
            Case "Well Formation"
                e.Control = MyModules.prodstringformation
            Case "Well Completion"
                e.Control = MyModules.well_completion
            Case "Well Completion Location and Well Cross Reference"
                e.Control = MyModules.well_completion_xref
            Case "Well Completion String and Formation Connection"
                e.Control = MyModules.well_completion_String2Formation_link

            Case "Well Perforation"
                e.Control = MyModules.wellperf
            Case "Completion Contact Intervals"
                e.Control = MyModules.wellperf
            Case "Prod. String Equipment"
                e.Control = MyModules.well_equipments
            Case "Wellbore Equipment"
                e.Control = MyModules.well_equipments
            Case "Install Equipment On Well"
                If (MyModules.well_equipments_search Is Nothing) Then
                    MyModules.well_equipments_search = New SimpleODM.wellmodule.uc_well_equipment_search(SimpleODMConfig, SimpleUtil)
                    MyModules.well_equipments_search.Loaddata()
                End If
                '  If (SimpleODMConfig.Defaults.WELL_XREFMyUWI <> MyModules.well_equipments_search.MyUWI) Then

                e.Control = MyModules.well_equipments_search
            Case "Prod. String Connected Facilities"

                e.Control = MyModules.Well_Facility
            Case "Prod. String Formation Tests"
                e.Control = MyModules.well_test
                '-------------------------------  End Well Desginer -------------------------------------------------
                '------------------------------------------------------------------------------------------------------------------------------------------------------
                '-----------------------------------  Lithiology Modules -----------------------------------------------------------------------------------------------

            Case "Lithology Management"
                If (MyModules.LithModule Is Nothing) Then
                    MyModules.LithModule = New SimpleODM.stratigraphyandlithologyModule.uc_NewEditLithology(SimpleODMConfig, SimpleUtil)
                    MyModules.LithModule.LoadLith()
                End If
                e.Control = MyModules.LithModule

                '-----------------------------------  End Lithiology Modules ----------------------------------------------------------------------------------------------
                '--------------------------------------- Stratigraphy --------------------------------------------------------
            Case "Stratigraphy Management"
                If (MyModules.StratigraphyModule Is Nothing) Then
                    MyModules.StratigraphyModule = New SimpleODM.stratigraphyandlithologyModule.uc_strat_name_set(SimpleODMConfig, SimpleUtil)
                    MyModules.StratigraphyModule.Loaddata()
                End If
                e.Control = MyModules.StratigraphyModule
            Case "Stratigraphy Column Management"
                If (MyModules.StratColumnsModule Is Nothing) Then
                    MyModules.StratColumnsModule = New SimpleODM.stratigraphyandlithologyModule.uc_strat_column(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.StratColumnsModule
            Case "Stratigraphic Name Set Cross Reference"
                e.Control = MyModules.strat_xref

            Case "Stratigraphic Field Station"
                If (MyModules.strat_field_station Is Nothing) Then
                    MyModules.strat_field_station = New SimpleODM.stratigraphyandlithologyModule.uc_strat_field_station(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.strat_field_station
            Case "Stratigraphic Field Station Interpreted Age"
                e.Control = MyModules.strat_fld_interp_age
            Case "Stratigraphic Field Node"
                e.Control = MyModules.strat_field_node
            Case "Stratigraphic Field Node Version"
                e.Control = MyModules.strat_field_node_version
            Case "Stratigraphic Field Section"
                e.Control = MyModules.strat_field_section
            Case "Stratigraphic Field Geometry"
                e.Control = MyModules.strat_field_geometry
            Case "Stratigraphic Field Acquisition"
                e.Control = MyModules.strat_field_acqtn

            Case "Stratigraphic Unit"
                e.Control = MyModules.strat_unit
            Case "Stratigraphic Unit Alias"
                e.Control = MyModules.strat_alias
            Case "Stratigraphic Unit Column Unit"
                e.Control = MyModules.strat_col_unit
            Case "Stratigraphic Unit Equivalence"
                e.Control = MyModules.strat_equivalance
            Case "Stratigraphic Unit Hierarchy"
                e.Control = MyModules.strat_hierarchy
            Case "Stratigraphic Unit Hierarchy Description"
                e.Control = MyModules.strat_hierarchy_desc
            Case "Stratigraphic Unit Age"
                e.Control = MyModules.strat_unit_age
            Case "Stratigraphic Unit Description"
                e.Control = MyModules.strat_unit_description
            Case "Stratigraphic Unit Topology"
                e.Control = MyModules.strat_topo_relation

            Case "Stratigraphic Column"
                If (MyModules.StratColumnsModule Is Nothing) Then
                    MyModules.StratColumnsModule = New SimpleODM.stratigraphyandlithologyModule.uc_strat_column(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.StratColumnsModule
            Case "Stratigraphic Column Age"
                e.Control = MyModules.strat_col_unit_age
            Case "Stratigraphic Column Acquisition"
                e.Control = MyModules.strat_col_acqtn
            Case "Stratigraphic Column Unit"
                e.Control = MyModules.strat_col_unit
            Case "Stratigraphic Column Cross Reference"
                e.Control = MyModules.strat_col_xref

            Case "Stratigraphic Well Section"
                e.Control = MyModules.strat_well_section
            Case "Stratigraphic Well Section Intrep. Age"
                e.Control = MyModules.strat_well_interp_age
            Case "Stratigraphic Well Section Acquisition"
                e.Control = MyModules.strat_well_acqtn
                '--------------------------------------- End Stratigraphy --------------------------------------------------------

            Case "WellBore Management"
                If (MyModules.WellBoreModule Is Nothing) Then
                    MyModules.WellBoreModule = New SimpleODM.wellmodule.uc_NewEditwellbore(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.WellBoreModule

            Case "Equipment Management"
                If (MyModules.EquipmentModule Is Nothing) Then
                    MyModules.EquipmentModule = New SimpleODM.supportmodule.uc_equipment(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.EquipmentModule
            Case "Classification Management"
                If (MyModules.ClassificationModule Is Nothing) Then
                    MyModules.ClassificationModule = New SimpleODM.classhierandreportmdoule.uc_NewEditClassification(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.ClassificationModule
                '------------------------------------------------------------------------------------------------------------------------------------------------------
                '-----------------------------------  Utilities Modules -----------------------------------------------------------------------------------------------
            Case "Generic Table Manager"
                If (MyModules.rvmwithrtableslistModule Is Nothing) Then
                    MyModules.rvmwithrtableslistModule = New rvmwithrtableslist_ctl(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.rvmwithrtableslistModule
            Case "Bulk Data Loader"
                If (MyModules.BulkDataLoading Is Nothing) Then
                    MyModules.BulkDataLoading = New uc_tableloader(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.BulkDataLoading
            Case "Defaults and Properties"
                If (MyModules.defaultsSetup Is Nothing) Then

                    MyModules.defaultsSetup = New uc_defaults_settings(SimpleODMConfig, SimpleUtil)
                    ' MyModules.defaultsSetup.Setup(SimpleODMConfig, SimpleUtil)
                End If
                If (MyModules.rvmwithrtableslistModule Is Nothing) Then
                    MyModules.rvmwithrtableslistModule = New rvmwithrtableslist_ctl(SimpleODMConfig, SimpleUtil)
                End If
                If (MyModules.Administrator Is Nothing) Then

                    MyModules.Administrator = New SimpleODM.SharedLib.uc_db_admin(SimpleODMConfig, SimpleUtil)
                End If
                e.Control = MyModules.defaultsSetup
            Case "Database Connections"
                If (MyModules.db_def Is Nothing) Then
                    MyModules.db_def = New SimpleODM.SharedLib.uc_dbdefine(SimpleODMConfig, SimpleUtil)
                End If
                MyModules.db_def.MyLoginControl = MyModules.uc_login1
                e.Control = MyModules.db_def
            Case "Database Constraint Manager"
                'If (MyModules.ConstraintsModule Is Nothing) Then
                '    MyModules.ConstraintsModule = New SimpleODM.etlmodule.uc_constraint_manager(SimpleODMConfig)

                'End If
                'e.Control = MyModules.ConstraintsModule


                'Case "Export Data"
                '    If (MyModules.ExportData Is Nothing) Then
                '        MyModules.ExportData = New SimpleODM.etlmodule.uc_exportTableData(SimpleODMConfig)
                '    End If
                e.Control = MyModules.Administrator
            Case "Administrator"
                If (MyModules.Administrator Is Nothing) Then

                    MyModules.Administrator = New SimpleODM.SharedLib.uc_db_admin(SimpleODMConfig, SimpleUtil)
                End If


                e.Control = MyModules.Administrator
            Case "Login"
                If (MyModules.uc_login1 Is Nothing) Then

                    MyModules.uc_login1 = New SimpleODM.SharedLib.uc_login(SimpleODMConfig, SimpleUtil)
                End If

                MyModules.uc_login1.LoadDBSchemas(SimpleODMConfig)
                e.Control = MyModules.uc_login1

                '------------------------------------------------------------------------------------------------------------------------------------------------------

            Case Else
                e.Control = MyModules.no_priv
        End Select

        'End If

        'If Not WindowsUIView1 Is Nothing Then
        '    WindowsUIView1.ActiveContentContainer.Caption = SimpleODMconfig.Defaults.APPLICATIONNAME
        'End If



    End Sub

#End Region
#End Region
#Region "Login Control Events Handling"

    Public Event LoginSucccess()
    Public Event Logout()
    Public Event LoginCancel()
    Public Event ShowDatabase()
    Private Sub uc_login_LoginCancel() Handles MyModules.LoginCancel
        RaiseEvent LoginCancel()
    End Sub
    Public Sub PerformLoginProcedures()
        DidIloginStatus = True
        DidIInitForms = False
        If (Not DidIInitForms) And (DidIloginStatus) Then

            If SimpleODMConfig.PPDMContext.ConnectionType = ConnectionTypeEnum.User Then
                SplashScreenManager.ShowForm(GetType(WaitForm1))
                Try
                    SimpleODMConfig.PPDMContext.CreateEntities()
                Catch ex As Exception
                    SplashScreenManager.CloseForm()

                End Try

                Try
                    SimpleODMConfig.PPDMContext.GetPrimaryLOV()
                    'SimpleODMconfig.ppdmcontext.DMLScripter = New cls_scriptsGenerator(SimpleODMConfig)
                    'SimpleODMconfig.ppdmcontext.DMLScripter.FillScriptUnits()
                Catch ex As Exception
                    DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm()
                    MsgBox("Cannot Load Primary LOV Tables")
                End Try
                SplashScreenManager.CloseForm()
                SimpleODMConfig.PPDMContext.LazyLoadingONOFF(True)
                'Try
                '    ETLConfig = New SimpleODM.etlmodule.cls_ETL(SimpleODMConfig)
                '    ETLconfig.ppdmcontext.SimpleConfig = SimpleODMConfig
                '    ETLconfig.ppdmcontext.OpenConnection()
                '    DidIInitForms = True
                'Catch ex As Exception
                '    MessageBox.Show("Simple ODM ETL Componenets Not Installed")

                'End Try
                DidIInitForms = True

            Else
                InitDBAAdminForms()
            End If
        End If
    End Sub
    Private Sub uc_login_LoginSucccess() Handles MyModules.LoginSucccess
        PerformLoginProcedures()
        SimpleUtil.SimpleODMConfig = SimpleODMConfig
        If SimpleUtil.LOVTablesList.Count = 0 Then
            If SimpleODMConfig.healthchk.Tables = True Then
                SimpleUtil.CacheMostAccessedLOV()
            End If

        End If

        RaiseEvent LoginSucccess()
    End Sub

    Private Sub uc_login_Logout() Handles MyModules.Logout
        RaiseEvent Logout()
    End Sub

    Private Sub uc_login_ShowDatabase() Handles MyModules.ShowDatabase
        RaiseEvent ShowDatabase()
    End Sub

#End Region



End Class
