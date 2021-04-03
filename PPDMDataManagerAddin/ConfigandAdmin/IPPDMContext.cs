using SimpleODM.systemconfigandutil.MiscClasses;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;

namespace SimpleODM.systemconfigandutil
{
    public interface IPPDMContext
    {
        applicationsandapprovalsEntities applicationsandapprovalsEntitiesDataContext { get; }
        businessAssociatesEntities businessAssociatesEntitiesDataContext { get; }
        ConnectionTypeEnum ConnectionType { get; set; }
        string CurrentLocalDbName { get; set; }
        string DatabaseConnectionstring { get; set; }
        dataManagementandUomEntities dataManagementandUomEntitiesDataContext { get; }
        DatabaseUtil DBUtil { get; set; }
        AppDomain Domain { get; set; }
        emptyEntityLayerEntities EmptyLayer4reportingsetEntitiesDataContext { get; }
        EntityConnection EntityConn { get; set; }
        string EntityConnectionstring { get; }
        equipmentEntities EquipmentContext { get; }
        InfoandProdManagementEntities InfoandProdManagementEntitiesDataContext { get; }
        LandandLegalEntities LandandLegalEntitiesDataContext { get; }
        List<ListofObjectsStateEntryPerConext> ListOFChangesInContexts { get; set; }
        lithologyEntities lithologyContext { get; }
        LocationEntities LocationModelContext { get; }
        PaleoEntities PaleoEntitiesDataContext { get; }
        SqlConnection PPDMDatabaseConnection { get; set; }
        wellpressureandtestsEntities pressureandtestContext { get; }
        productionunitsEntities productionentitiesContext { get; }
        productionreportingEntities ProductionReportingContext { get; }
        reportshierandclassEntities reportshierandclassEntitiesDataContext { get; }
        reservereportingEntities reservereportingContext { get; }
        List<RPPDM_ROW_QUALITY_CLS> RPPDM_ROW_QUALITY { get; set; }
        List<RSOURCE_CLS> RSource { get; set; }
        List<RWELL_LEVEL_TYPE_CLS> RWELL_LEVEL_TYPE { get; set; }
        List<RWELL_XREF_TYPE_CLS> RWELL_XREF_TYPE { get; set; }
        sampleAnalysisEntities SampleAnalysisModelMetaDataContext { get; }
        seismicEntities seismicEntitiesDataContext { get; }
        spatialEntities spatialEntitiesDataContext { get; }
        stratigraphyEntities stratigraphyContext { get; }
        SupportFacilityEntities SupportFacilitiesEntitiesDataContext { get; }
        supportEntities supprtContext { get; }
        UserSettings UserSetting { get; set; }
        PPDMVERSION VERSION { get; set; }
        WellGeneralInfoEntities WellGeneralInfoContext { get; }
        welllogcoreinteroretationsEntities welllogcoreinteroretationsEntitiesDataContext { get; }
        welloperationsEntities welloperationsContext { get; }
        workordersandprojectsEntities workordersandprojectsContext { get; }
        zrtablesEntities zrtablesEntitiesDataContext { get; }

        event PPDMContext.PostInsertEventHandler PostInsert;
        event PPDMContext.PostUpdateEventHandler PostUpdate;
        event PPDMContext.PreInsertEventHandler PreInsert;
        event PPDMContext.PreUpdateEventHandler PreUpdate;
        event PPDMContext.ReleaseDataEventHandler ReleaseData;
        event PPDMContext.ValidateRecordEventHandler ValidateRecord;

        bool CheckHavePrivOnEntity(ref ObjectStateEntry obj, ref ObjectContext context, string PropertyName);
        void CopyEntityFromOneDBtoAnother(ref ObjectContext Sourcecontext, object SourceEntity, ref ObjectContext Destcontext, object DestEntity);
        void DoAudit(ref ObjectStateEntry obj, ref ObjectContext context, string PropertyName, string PropertyValue);
        ListofObjectsStateEntryPerConext GetChangedinContext(ObjectContext context, string pContextName = "");
        void GetPrimaryLOV();
        string GetProperty(ref ObjectStateEntry obj, ref ObjectContext context, string PropertyName);
        bool OpenConnection();
        void SaveDefaultValuesInContext(ref DbContext pcontext);
        void SetProperty(ref ObjectStateEntry obj, ref ObjectContext context, string PropertyName, string PropertyValue);
    }
}