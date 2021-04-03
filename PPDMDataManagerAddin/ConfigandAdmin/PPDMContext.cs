using SimpleODM.systemconfigandutil.MiscClasses;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleODM.systemconfigandutil
{
    #region"Enums"
    public enum PPDMVERSION
    {
        v38 = 0,
    v39 = 1
    }


    //public enum CTLTYPEENUM
    //{
    //    WELL = 0,
    //    WELLBORE = 1,
    //    WELLBORESEGMENT = 2,
    //    WELLBORECOMPLETION = 3,
    //    CONTACTINTERVAL = 4,
    //    WELLBORESTREAM = 5
    //}

    //public enum DbDataDirectionEnum
    //{
    //    Target = 1,
    //    Source = 2
    //}

    //public enum DbTypeEnum
    //{
    //    OracleDB = 1,
    //    SqlServerDB = 2,
    //    MySQLDB = 3,
    //    Sqlite = 4,
    //    DB2 = 5
    //}

    //public enum ConnectionTypeEnum
    //{
    //    Admin = 0,
    //    User = 1
    //}
    #endregion
    public class PPDMContext : IPPDMContext
    {

        public SqlConnection PPDMDatabaseConnection { get; set; }
        public string DatabaseConnectionstring { get; set; }
        static string resourcenamespacems = "";

        static string msres = "res://*/";
        public PPDMContext()
        {
            DBUtil = new DatabaseUtil(this);
        }
        public string EntityConnectionstring
        {
            get
            {
                return "metadata=" + _ppdmsystemmetadata + "|" + _paleometadata + "|" + _rtablesmetadata + "|" + _spatialmetadata + "|" + _zrtablesmetadata + "|" + _seismicmetadata + "|" + _applicationsandapprovalsmetadata + "|" + _landandlegalmetadata + "|" + _supportfacilitymodelmetadata + "|" + _wellgeneralinfometadata + "|" + _infoandprodmanagementmodelmetadata + "|" + _welllogandcoremodelmetadata + "|" + _locationmodelmetadata + "|" + _reservereportingmodelmetadata + "|" + _productionreportingmetadata + "|" + _stratigraphymetamodel + "|" + _lithologymodelmetadata + "|" + _supportmodelmodelmetadata + "|" + _sampleanalysismodelmetadata + "|" + _equipmentmodelmetadata + "|" + _workordersandprojectsmetamodel + "|" + _wellpressureantestsmodelmetadata + "|" + _productionentmetamodel + "|" + _welloperationsmodelmetadata + "|" + _datamanagementanduommodelmetadata + "|" + _reportshierandclassmodelmetadata + "|" + _reportingsetmodelmetadata + "|" + _businessassociatesmodelmetadata + ";"; // 
            }
        }

        #region "metadata"

        private string _ppdmsystemmetadata { get; set; } = msres + resourcenamespacems + "ppdmsystemmodel.csdl|" + msres + resourcenamespacems + "ppdmsystemmodel.ssdl|" + msres + resourcenamespacems + "ppdmsystemmodel.msl";



        private string _paleometadata = msres + resourcenamespacems + "paleomodel.csdl|" + msres + resourcenamespacems + "paleomodel.ssdl|" + msres + resourcenamespacems + "paleomodel.msl";



        private string _spatialmetadata = msres + resourcenamespacems + "spatialmodel.csdl|" + msres + resourcenamespacems + "spatialmodel.ssdl|" + msres + resourcenamespacems + "spatialmodel.msl";


        // -----------------------------------------------------------------------------------------------------------------
        private string _zrtablesmetadata = msres + resourcenamespacems + "zrtablesmodel.csdl|" + msres + resourcenamespacems + "zrtablesmodel.ssdl|" + msres + resourcenamespacems + "zrtablesmodel.msl";


        // -----------------------------------------------------------------------------------------------------------------

        private string _rtablesmetadata = msres + resourcenamespacems + "rtablesmodel.csdl|" + msres + resourcenamespacems + "rtablesmodel.ssdl|" + msres + resourcenamespacems + "rtablesmodel.msl";

        // -----------------------------------------------------------------------------------------------------------------
        private string _seismicmetadata = msres + resourcenamespacems + "seismicmodel.csdl|" + msres + resourcenamespacems + "seismicmodel.ssdl|" + msres + resourcenamespacems + "seismicmodel.msl";

        // -----------------------------------------------------------------------------------------------------------------
        private string _landandlegalmetadata = msres + resourcenamespacems + "landandlegalmanagementmodel.csdl|" + msres + resourcenamespacems + "landandlegalmanagementmodel.ssdl|" + msres + resourcenamespacems + "landandlegalmanagementmodel.msl";

        // -----------------------------------------------------------------------------------------------------------------
        private string _applicationsandapprovalsmetadata = msres + resourcenamespacems + "applicationsandapprovalsmodel.csdl|" + msres + resourcenamespacems + "applicationsandapprovalsmodel.ssdl|" + msres + resourcenamespacems + "applicationsandapprovalsmodel.msl";

        // -----------------------------------------------------------------------------------------------------------------
        private string _supportfacilitymodelmetadata = msres + resourcenamespacems + "supportfacilitymodel.csdl|" + msres + resourcenamespacems + "supportfacilitymodel.ssdl|" + msres + resourcenamespacems + "supportfacilitymodel.msl";


        // ------------------------------------------------------------------------------------------------------------------
        private string _reportingsetmodelmetadata = msres + resourcenamespacems + "emptyentitylayermodel.csdl|" + msres + resourcenamespacems + "emptyentitylayermodel.ssdl|" + msres + resourcenamespacems + "emptyentitylayermodel.msl";


        private string _infoandprodmanagementmodelmetadata = msres + resourcenamespacems + "infoandprodmanagementmodel.csdl|" + msres + resourcenamespacems + "infoandprodmanagementmodel.ssdl|" + msres + resourcenamespacems + "infoandprodmanagementmodel.msl";

        private string _welllogandcoremodelmetadata = msres + resourcenamespacems + "welllogcoreinteroretations.csdl|" + msres + resourcenamespacems + "welllogcoreinteroretations.ssdl|" + msres + resourcenamespacems + "welllogcoreinteroretations.msl";

        private string _locationmodelmetadata = msres + resourcenamespacems + "locationmodel.csdl|" + msres + resourcenamespacems + "locationmodel.ssdl|" + msres + resourcenamespacems + "locationmodel.msl";

        private string _businessassociatesmodelmetadata = msres + resourcenamespacems + "businessassociatesmodel.csdl|" + msres + resourcenamespacems + "businessassociatesmodel.ssdl|" + msres + resourcenamespacems + "businessassociatesmodel.msl";

        private string _reportshierandclassmodelmetadata = msres + resourcenamespacems + "reportshierandclassmodel.csdl|" + msres + resourcenamespacems + "reportshierandclassmodel.ssdl|" + msres + resourcenamespacems + "reportshierandclassmodel.msl";


        private string _datamanagementanduommodelmetadata = msres + resourcenamespacems + "datamanagementanduommodel.csdl|" + msres + resourcenamespacems + "datamanagementanduommodel.ssdl|" + msres + resourcenamespacems + "datamanagementanduommodel.msl";



        private string _equipmentmodelmetadata = msres + resourcenamespacems + "equipmentmodel.csdl|" + msres + resourcenamespacems + "equipmentmodel.ssdl|" + msres + resourcenamespacems + "equipmentmodel.msl";



        private string _sampleanalysismodelmetadata = msres + resourcenamespacems + "sampleanalysismodel.csdl|" + msres + resourcenamespacems + "sampleanalysismodel.ssdl|" + msres + resourcenamespacems + "sampleanalysismodel.msl";



        private string _uommodelmetadata = msres + resourcenamespacems + "uommodel.csdl|" + msres + resourcenamespacems + "uommodel.ssdl|" + msres + resourcenamespacems + "uommodel.msl";

        private string _supportmodelmodelmetadata = msres + resourcenamespacems + "supportmodel.csdl|" + msres + resourcenamespacems + "supportmodel.ssdl|" + msres + resourcenamespacems + "supportmodel.msl";
        private string _reservereportingmodelmetadata = msres + resourcenamespacems + "reservereportingmodel.csdl|" + msres + resourcenamespacems + "reservereportingmodel.ssdl|" + msres + resourcenamespacems + "reservereportingmodel.msl";


        private string _welloperationsmodelmetadata = msres + resourcenamespacems + "welloperationsmodel.csdl|" + msres + resourcenamespacems + "welloperationsmodel.ssdl|" + msres + resourcenamespacems + "welloperationsmodel.msl";


        private string _wellpressureantestsmodelmetadata = msres + resourcenamespacems + "wellpressureandtestsmodel.csdl|" + msres + resourcenamespacems + "wellpressureandtestsmodel.ssdl|" + msres + resourcenamespacems + "wellpressureandtestsmodel.msl";

        private string _lithologymodelmetadata = msres + resourcenamespacems + "lithologymodel.csdl|" + msres + resourcenamespacems + "lithologymodel.ssdl|" + msres + resourcenamespacems + "lithologymodel.msl";


        private string _rdatametadata = msres + resourcenamespacems + "rdatamodel.csdl|" + msres + resourcenamespacems + "rdatamodel.ssdl|" + msres + resourcenamespacems + "rdatamodel.msl";


        private string _productionreportingmetadata = msres + resourcenamespacems + "productionreportingmodel.csdl|" + msres + resourcenamespacems + "productionreportingmodel.ssdl|" + msres + resourcenamespacems + "productionreportingmodel.msl";


        private string _wellgeneralinfometadata = msres + resourcenamespacems + "wellgeneralinfomodel.csdl|" + msres + resourcenamespacems + "wellgeneralinfomodel.ssdl|" + msres + resourcenamespacems + "wellgeneralinfomodel.msl";

        private string _productionentmetamodel = msres + resourcenamespacems + "productionunitsmodel.csdl|" + msres + resourcenamespacems + "productionunitsmodel.ssdl|" + msres + resourcenamespacems + "productionunitsmodel.msl";

        private string _stratigraphymetamodel = msres + resourcenamespacems + "stratigraphymodel.csdl|" + msres + resourcenamespacems + "stratigraphymodel.ssdl|" + msres + resourcenamespacems + "stratigraphymodel.msl";


        private string _workordersandprojectsmetamodel = msres + resourcenamespacems + "workordersandprojectsmodel.csdl|" + msres + resourcenamespacems + "workordersandprojectsmodel.ssdl|" + msres + resourcenamespacems + "workordersandprojectsmodel.msl";

        #endregion
        #region "Entities Context"

        // Public WithEvents PPDMSystemEntitiesDataContext As ppdmsystem.ppdmsystemEntities

        private PaleoEntities _PaleoEntitiesDataContext;

        public PaleoEntities PaleoEntitiesDataContext
        {
            get
            {

                return _PaleoEntitiesDataContext;
            }
        }
        // ------------------------------------------------------------
        private spatialEntities _spatialEntitiesDataContext;

        public spatialEntities spatialEntitiesDataContext
        {
            get
            {
                return _spatialEntitiesDataContext;
            }
        }
        // ------------------------------------------------------------
        private zrtablesEntities _zrtablesEntitiesDataContext;

        public zrtablesEntities zrtablesEntitiesDataContext
        {
            get
            {
                return _zrtablesEntitiesDataContext;
            }
        }
        // ------------------------------------------------------------
        //private rtablesEntities _rtablesEntitiesDataContext;

        //public rtablesEntities rtablesEntitiesDataContext
        //{
        //    get
        //    {
        //        return _rtablesEntitiesDataContext;
        //    }
        //}
        // ------------------------------------------------------------
        private applicationsandapprovalsEntities _applicationsandapprovalsEntitiesDataContext;

        public applicationsandapprovalsEntities applicationsandapprovalsEntitiesDataContext
        {
            get
            {
                return _applicationsandapprovalsEntitiesDataContext;
            }
        }
        // ------------------------------------------------------------
        private seismicEntities _seismicEntitiesDataContext;

        public seismicEntities seismicEntitiesDataContext
        {
            get
            {
                return _seismicEntitiesDataContext;
            }
        }
        // ------------------------------------------------------------
        private LandandLegalEntities _LandandLegalEntitiesDataContext;

        public LandandLegalEntities LandandLegalEntitiesDataContext
        {
            get
            {
                return _LandandLegalEntitiesDataContext;
            }
        }
        // ------------------------------------------------------------
        private SupportFacilityEntities _SupportFacilitiesEntitiesDataContext;

        public SupportFacilityEntities SupportFacilitiesEntitiesDataContext
        {
            get
            {
                return _SupportFacilitiesEntitiesDataContext;
            }
        }

        private emptyEntityLayerEntities _EmptyLayer4reportingsetEntitiesDataContext;

        public emptyEntityLayerEntities EmptyLayer4reportingsetEntitiesDataContext
        {
            get
            {
                return _EmptyLayer4reportingsetEntitiesDataContext;
            }
        }
        // Private WithEvents _EmptyLayer4reportingsetEntitiesDataContext_ms As emptyEntityLayerEntities_ms
        // Public ReadOnly Property EmptyLayer4reportingsetEntitiesDataContext_ms() As emptyEntityLayerEntities_ms
        // Get
        // Return _EmptyLayer4reportingsetEntitiesDataContext_ms
        // End Get
        // End Property
        // '------------------------------------------------------------

        // ------------------------------------------------------------
        private InfoandProdManagementEntities _InfoandProdManagementEntitiesDataContext;

        public InfoandProdManagementEntities InfoandProdManagementEntitiesDataContext
        {
            get
            {
                return _InfoandProdManagementEntitiesDataContext;
            }
        }

        // ------------------------------------------------------------
        private welllogcoreinteroretationsEntities _welllogcoreinteroretationsEntitiesDataContext;

        public welllogcoreinteroretationsEntities welllogcoreinteroretationsEntitiesDataContext
        {
            get
            {
                return _welllogcoreinteroretationsEntitiesDataContext;
            }
        }
        // ------------------------------------------------------------
        private businessAssociatesEntities _businessAssociatesEntitiesDataContext;

        public businessAssociatesEntities businessAssociatesEntitiesDataContext
        {
            get
            {
                return _businessAssociatesEntitiesDataContext;
            }
        }

        // ------------------------------------------------------------
        private reportshierandclassEntities _reportshierandclassEntitiesDataContext;

        public reportshierandclassEntities reportshierandclassEntitiesDataContext
        {
            get
            {
                return _reportshierandclassEntitiesDataContext;
            }
        }

        // ------------------------------------------------------------
        private dataManagementandUomEntities _dataManagementandUomEntitiesDataContext;

        public dataManagementandUomEntities dataManagementandUomEntitiesDataContext
        {
            get
            {
                return _dataManagementandUomEntitiesDataContext;
            }
        }

        // ------------------------------------------------------------
        private sampleAnalysisEntities __SampleAnalysisModelMetaDataContext;

        public sampleAnalysisEntities SampleAnalysisModelMetaDataContext
        {
            get
            {
                return __SampleAnalysisModelMetaDataContext;
            }
        }

        // ------------------------------------------------------------
        private equipmentEntities __EquipmentModelMetaDataContext;

        public equipmentEntities EquipmentContext
        {
            get
            {
                return __EquipmentModelMetaDataContext;
            }
        }

        // ------------------------------------------------------------
        private reservereportingEntities _reservereportingContext;

        public reservereportingEntities reservereportingContext
        {
            get
            {
                return _reservereportingContext;
            }
        }

        // ------------------------------------------------------------
        private welloperationsEntities _welloperationsContext;

        public welloperationsEntities welloperationsContext
        {
            get
            {
                return _welloperationsContext;
            }
        }

        // ------------------------------------------------------------
        private wellpressureandtestsEntities _pressureandtestContext;

        public wellpressureandtestsEntities pressureandtestContext
        {
            get
            {
                return _pressureandtestContext;
            }
        }

        // ------------------------------------------------------------
        private supportEntities _supportContext;

        public supportEntities supprtContext
        {
            get
            {
                return _supportContext;
            }
        }

        // '------------------------------------------------------------
        // Private WithEvents _spatialContext As spatialEntities
        // Public ReadOnly Property spatialContext() As spatialEntities
        // Get
        // Return _spatialContext
        // End Get
        // End Property

        // ------------------------------------------------------------
        private stratigraphyEntities _stratigraphyContext;

        public stratigraphyEntities stratigraphyContext
        {
            get
            {
                return _stratigraphyContext;
            }
        }

        // ----------------------------------------------------------
        private lithologyEntities _lithologyContext;

        public lithologyEntities lithologyContext
        {
            get
            {
                return _lithologyContext;
            }
        }

        // ------------------------------------------------------------------------------

        private productionreportingEntities _ProductionReportingContext;

        public productionreportingEntities ProductionReportingContext
        {
            get
            {
                return _ProductionReportingContext;
            }
        }

        // ------------------------------------------------------------------------------
        private productionunitsEntities _productionentitiesContext;

        public productionunitsEntities productionentitiesContext
        {
            get
            {
                return _productionentitiesContext;
            }
        }

        // ------------------------------------------------------------------------------
        private WellGeneralInfoEntities _WellGeneralInfoContext;

        public WellGeneralInfoEntities WellGeneralInfoContext
        {
            get
            {
                return _WellGeneralInfoContext;
            }
        }

        private workordersandprojectsEntities _workordersandprojectsContext;

        public workordersandprojectsEntities workordersandprojectsContext
        {
            get
            {

                return _workordersandprojectsContext;
            }
        }

        private LocationEntities _LocationModelContext;

        public LocationEntities LocationModelContext
        {
            get
            {

                return _LocationModelContext;
            }
        }

        #endregion
        #region "Properties"
        public UserSettings UserSetting { get; set; } = new UserSettings();
        public string CurrentLocalDbName { get; set; } = "ppdm38.sdf";
        private EntityConnection _EntityConn;
        public DatabaseUtil DBUtil { get; set; }
        public event ReleaseDataEventHandler ReleaseData;

        public delegate void ReleaseDataEventHandler();

        public ConnectionTypeEnum ConnectionType { get; set; }
        public EntityConnection EntityConn
        {
            get
            {
                return _EntityConn;
            }
            set
            {
                _EntityConn = value;
            }
        }
        public PPDMVERSION VERSION { get; set; }
        public AppDomain Domain { get; set; }
        public List<RSOURCE_CLS> RSource { get; set; }
        public List<RPPDM_ROW_QUALITY_CLS> RPPDM_ROW_QUALITY { get; set; }
        public List<RWELL_LEVEL_TYPE_CLS> RWELL_LEVEL_TYPE { get; set; }
        public List<RWELL_XREF_TYPE_CLS> RWELL_XREF_TYPE { get; set; }
        public void GetPrimaryLOV()
        {
            RPPDM_ROW_QUALITY = EmptyLayer4reportingsetEntitiesDataContext.Database.SqlQuery<RPPDM_ROW_QUALITY_CLS>(" select * from R_PPDM_ROW_QUALITY", null/* TODO Change to default(_) if this is not a reference type */).ToList();
            RSource = EmptyLayer4reportingsetEntitiesDataContext.Database.SqlQuery<RSOURCE_CLS>(" select * from R_SOURCE", null/* TODO Change to default(_) if this is not a reference type */).ToList();
            RWELL_LEVEL_TYPE = EmptyLayer4reportingsetEntitiesDataContext.Database.SqlQuery<RWELL_LEVEL_TYPE_CLS>(" select * from R_WELL_LEVEL_TYPE", null/* TODO Change to default(_) if this is not a reference type */).ToList();
            RWELL_XREF_TYPE = EmptyLayer4reportingsetEntitiesDataContext.Database.SqlQuery<RWELL_XREF_TYPE_CLS>(" select * from R_WELL_XREF_TYPE", null/* TODO Change to default(_) if this is not a reference type */).ToList();
        }
        #endregion
        #region "Entity Tracking"
        private Dictionary<EntityKey, object> addedobj;
        private ICollection<System.Data.Objects.ObjectStateEntry> deletedobj;
        private ICollection<System.Data.Objects.ObjectStateEntry> modifiedobj;
        private object _obj;

        public event ValidateRecordEventHandler ValidateRecord;

        public delegate void ValidateRecordEventHandler(ObjectStateEntry obj, ObjectContext context, string EntityType, string TransType, ref bool Cancel);

        public event PostUpdateEventHandler PostUpdate;

        public delegate void PostUpdateEventHandler(ObjectStateEntry obj, ObjectContext context, string EntityType, string TransType, ref bool Cancel);

        public event PostInsertEventHandler PostInsert;

        public delegate void PostInsertEventHandler(ObjectStateEntry obj, ObjectContext context, string EntityType, string TransType, ref bool Cancel);

        public event PreUpdateEventHandler PreUpdate;

        public delegate void PreUpdateEventHandler(ObjectStateEntry obj, ObjectContext context, string EntityType, string TransType, ref bool Cancel);

        public event PreInsertEventHandler PreInsert;

        public delegate void PreInsertEventHandler(ObjectStateEntry obj, ObjectContext context, string EntityType, string TransType, ref bool Cancel);

        public List<ListofObjectsStateEntryPerConext> ListOFChangesInContexts { get; set; } = new List<ListofObjectsStateEntryPerConext>();

        public ListofObjectsStateEntryPerConext GetChangedinContext(ObjectContext context, string pContextName = "")
        {
            var x = new ListofObjectsStateEntryPerConext();
            if (pContextName.Length == 0)
            {
                x.ContextName = context.DefaultContainerName;
            }
            else
                x.ContextName = pContextName;


            var ListofEnt = new List<ChangedObject>();
            context.DetectChanges();
            try
            {
                foreach (var modified in context.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Modified))
                {
                    try
                    {
                        ListofEnt.Add(new ChangedObject(modified, "MODIFIED", context.DefaultContainerName));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Simple ODM");
                    }
                }

                foreach (var addedobj in context.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Added))
                {
                    try
                    {
                        ListofEnt.Add(new ChangedObject(addedobj, "ADDED", context.DefaultContainerName));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Simple ODM");
                    }
                    // -----------------------------------------------------
                }

                foreach (var DELobj in context.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Deleted))
                {
                    try
                    {
                        ListofEnt.Add(new ChangedObject(DELobj, "DELETED", context.DefaultContainerName));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Simple ODM");
                    }
                    // -----------------------------------------------------
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Simple ODM");
            }

            x.ListofObjects = ListofEnt;
            return x;
        }

        public void SaveDefaultValuesInContext(ref DbContext pcontext)
        {
            string strDateSent;
            bool retcancel;
            strDateSent = pcontext.Database.SqlQuery<DateTime>("select getdate()").First().ToString();

            var context = ((IObjectContextAdapter)pcontext).ObjectContext;
            var getentries = ((IObjectContextAdapter)pcontext).ObjectContext.ObjectStateManager;
            // GetChangedinContext(context)
            foreach (var modified in getentries.GetObjectStateEntries(System.Data.Entity.EntityState.Modified))
            {
                try
                {
                    string casttype = modified.EntitySet.Name.ToString();
                    retcancel = true;
                    ValidateRecord?.Invoke(modified, context, casttype, "Modified", ref retcancel);
                    if (retcancel)
                    {
                        PreUpdate?.Invoke(modified, context, casttype, "Modified", ref retcancel);
                        if (retcancel)
                        {
                            var _ROW_CHANGED_BY = modified.Entity.GetType().GetProperty("ROW_CHANGED_BY");
                            _ROW_CHANGED_BY.SetValue(modified.Entity, UserSetting.LOGINID, default);
                            var _ROW_CHANGED_DATE = modified.Entity.GetType().GetProperty("ROW_CHANGED_DATE");
                            _ROW_CHANGED_DATE.SetValue(modified.Entity, Convert.ToDateTime(strDateSent), null);
                            PostUpdate?.Invoke(modified, context, casttype, "Modified", ref retcancel);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Simple ODM");
                }
            }
            // 
            // 
            foreach (var addedobj in context.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Added))
            {
                try
                {
                    string casttype = addedobj.EntitySet.Name.ToString();
                    retcancel = true;
                    ValidateRecord?.Invoke(addedobj, context, casttype, "Added", ref retcancel);
                    if (retcancel)
                    {
                        PreInsert?.Invoke(addedobj, context, casttype, "Added", ref retcancel);
                        if (retcancel)
                        {
                            var _ROW_CREATED_BY = addedobj.Entity.GetType().GetProperty("ROW_CREATED_BY");
                            _ROW_CREATED_BY.SetValue(addedobj.Entity, Convert.ToString(UserSetting.LOGINID), default);
                            var _ROW_CREATED_DATE = addedobj.Entity.GetType().GetProperty("ROW_CREATED_DATE");
                            _ROW_CREATED_DATE.SetValue(addedobj.Entity, Convert.ToDateTime(strDateSent), null);
                            var _EFFICTIVE_DATE = addedobj.Entity.GetType().GetProperty("EFFECTIVE_DATE");
                            _EFFICTIVE_DATE.SetValue(addedobj.Entity, Convert.ToDateTime(strDateSent), null);
                            var _PPDM_GUID = addedobj.Entity.GetType().GetProperty("PPDM_GUID");
                            _PPDM_GUID.SetValue(addedobj.Entity, Guid.NewGuid().ToString(), null);
                            var _ACTIVE_IND = addedobj.Entity.GetType().GetProperty("ACTIVE_IND");
                            string _keyvalue = Convert.ToString(_ACTIVE_IND.GetValue(addedobj.Entity, null));
                            if (string.IsNullOrEmpty(_keyvalue) == true)
                            {
                                _ACTIVE_IND.SetValue(addedobj.Entity, "Y", null);
                            }
                        }

                        if (addedobj.Entity.GetType().FullName.ToUpper() == "SIMPLEODM.WELLGENERALINFO.WELL_COMPLETION")
                        {
                            // SavePR_STR_FORMATION_COMPLETION(addedobj, context, casttype, "Added", retCancel)
                        }
                        // Check for Observation Numbers
                        // Dim _PriKeyStr As String = ""
                        // For Each _props In addedobj.Entity.GetType.GetProperties
                        // If (_props.Name.IndexOf("OBS_NO") > 0) Or (_props.Name.IndexOf("SEQ_NO")) > 0 Then
                        // _PriKeyStr = ""

                        // Try
                        // For Each _key In addedobj.EntitySet.ElementType.KeyMembers

                        // If _key.Name <> _props.Name Then
                        // Dim _keyprop As PropertyInfo = addedobj.Entity.GetType.GetProperty(_key.Name)
                        // Dim _keyvalue As String = _keyprop.GetValue(addedobj.Entity, Nothing)
                        // _PriKeyStr = _PriKeyStr & _key.Name & "='" & _keyvalue & "' and "
                        // End If
                        // Next
                        // _PriKeyStr = _PriKeyStr.Substring(0, _PriKeyStr.IndexOf("and") - 1)
                        // Dim _ret As Integer = GetNext_OBS_NO(addedobj.Entity.GetType.Name, _props.Name, _PriKeyStr)
                        // Dim _propField As PropertyInfo = addedobj.Entity.GetType.GetProperty(_props.Name)
                        // Dim _retval As Integer = _propField.GetValue(addedobj.Entity, Nothing)
                        // If _retval = 0 Then
                        // _propField.SetValue(addedobj.Entity, _ret, Nothing)
                        // End If
                        // Catch ex As Exception
                        // MsgBox("Error in Fetching Entity Keys , " & ex.Message, MsgBoxStyle.Critical, "Simple ODM")
                        // End Try


                        // End If

                        // Next
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Simple ODM");
                }
                // -----------------------------------------------------
            }
        }

        public void CopyEntityFromOneDBtoAnother(ref ObjectContext Sourcecontext, object SourceEntity, ref ObjectContext Destcontext, object DestEntity)
        {
            PropertyInfo[] SourceProp = (PropertyInfo[])SourceEntity.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Where(p => !p.PropertyType.IsClass);
            PropertyInfo[] DestProp = (PropertyInfo[])DestEntity.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Where(p => !p.PropertyType.IsClass);
            foreach (var s in SourceProp)
            {
                foreach (var d in DestProp)
                {
                    if ((s.Name ?? "") == (d.Name ?? ""))
                    {
                        d.SetValue(DestEntity, s.GetValue(SourceEntity, null), null);
                    }
                }
            }
        }

        public bool CheckHavePrivOnEntity(ref ObjectStateEntry obj, ref ObjectContext context, string PropertyName)
        {
            return true;
        }

        public void DoAudit(ref ObjectStateEntry obj, ref ObjectContext context, string PropertyName, string PropertyValue)
        {
        }

        public void SetProperty(ref ObjectStateEntry obj, ref ObjectContext context, string PropertyName, string PropertyValue)
        {
            try
            {
                PropertyInfo _ChangedProperty = obj.Entity.GetType().GetProperty(PropertyName);
                _ChangedProperty.SetValue(obj.Entity, PropertyValue, default);
                DoAudit(ref obj, ref context, PropertyName, PropertyValue);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Simple ODM");
            }
        }

        public string GetProperty(ref ObjectStateEntry obj, ref ObjectContext context, string PropertyName)
        {
            string _retval = "";
            try
            {
                PropertyInfo _ChangedProperty = obj.Entity.GetType().GetProperty(PropertyName);
                _retval = Convert.ToString(_ChangedProperty.GetValue(obj.Entity, default));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Simple ODM");
            }

            return _retval;
        }


        #endregion
        #region "Connection Handling"

        public bool OpenConnection()
        {

            try
            {
                string entitystr = "";
                DatabaseConnectionstring = @"provider=System.Data.SqlClient;provider connection string="" Data Source=" + UserSetting.HOSTNAME + ";Initial Catalog=" + UserSetting.DATABASENAME + ";User ID=" + UserSetting.LOGINID + ";Password=" + UserSetting.PASSWORD + @";App=EntityFramework;MultipleActiveResultSets=True"; ;
                PPDMDatabaseConnection.ConnectionString = DatabaseConnectionstring;
                PPDMDatabaseConnection.Open();
                _EntityConn = new EntityConnection();
                _EntityConn.ConnectionString = EntityConnectionstring + DatabaseConnectionstring;
                _EntityConn.Open();
                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                MessageBox.Show("Error in Opening Database Connection");
                return false;

            };
            return true;
        }

        #endregion

    }


}
