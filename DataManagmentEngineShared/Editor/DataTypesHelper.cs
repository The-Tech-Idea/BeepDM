using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;

using System;
using System.Collections.Generic;
using System.IO;

using TheTechIdea.Util;
using System.ComponentModel;
using System.Linq;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public class DataTypesHelper : IDataTypesHelper
    {

        public DataTypesHelper()
        {
           // mapping = new List<DatatypeMapping>();
        }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }

        public IDMEEditor DMEEditor { get; set; }
        public List<DatatypeMapping> mapping { get; set; }
     
        private string NetDataTypeDef1 = "byte,sbyte,int,uint,short,ushort,long,ulong,float,double,char,bool,object,string,decimal,DateTime";
        private string NetDataTypeDef2 = ",System.Byte[],System.SByte[],System.Byte,System.SByte,System.Int32,System.UInt32,System.Int16,System.UInt16,System.Int64,System.UInt64,System.Single,System.Double,System.Char,System.Boolean,System.Object,System.String,System.Decimal,System.DateTime,System.TimeSpan,System.DateTimeOffset,System.Guid,System.Xml";


        public List<string> GetDataClasses()
        {
            List<string> p = new List<string>();
            foreach (AssemblyClassDefinition cls in DMEEditor.ConfigEditor.DataSources)
            {
                p.Add(cls.className);
            }
            return p;
        }
        public DataTypesHelper(IDMLogger plogger, IDMEEditor pDMEEditor, IErrorsInfo per)
        {
            //  DMEEditor = pbs;
            Logger = Logger;
            DMEEditor = pDMEEditor;
            ErrorObject = per;


        }
        public string GetDataType(string DSname, EntityField fld)
        {
            string retval = null;
            try
            {
                if (DSname != null)
                {
                    if (DMEEditor.ConfigEditor.DataTypesMap == null)
                    {
                        DMEEditor.ConfigEditor.ReadDataTypeFile();
                    }
                    AssemblyClassDefinition classhandler = DMEEditor.GetDataSourceClass(DSname);
                    if (classhandler != null)
                    {
                        DatatypeMapping dt = null;
                        if (fld.fieldtype == "System.String")  //-- Fist Check all Data field that Contain Length
                        {
                            if (fld.Size1 > 0) //-- String Type first
                            {
                                dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.NetDataType == fld.fieldtype && x.DataType.Contains("N")).FirstOrDefault();
                                if (dt != null)

                                    retval = dt.DataType.Replace("(N)", "(" + fld.Size1.ToString() + ")");


                            } else
                            {
                                dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.NetDataType == fld.fieldtype).FirstOrDefault();
                                retval = dt.DataType;
                            }

                        }
                        else  //---- numeric
                        {
                            //if (fld.Size1 > 0)
                            //{
                            //    if (fld.Size2 > 0)
                            //    {
                            //        dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.NetDataType == fld.fieldtype && x.DataType.Contains("N,S")).FirstOrDefault();
                            //        retval = dt.DataType.Replace("(N,S)", "(" + fld.Size1.ToString() + "," + fld.Size2.ToString() + ")");

                            //    }
                            //    else
                            //    {
                            //        dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.NetDataType == fld.fieldtype && x.DataType.Contains("(N)")).FirstOrDefault();

                            //        if (dt != null)
                            //        {
                            //            retval = dt.DataType.Replace("(N)", "(" + fld.Size1.ToString() + ")");
                            //        }
                                  
                            //    }
                            //}
                            if (fld.NumericPrecision > 0)
                            {
                                if (fld.NumericScale > 0)
                                {
                                    dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.NetDataType == fld.fieldtype && x.DataType.Contains("N,S")).FirstOrDefault();
                                    if (dt != null)
                                    {
                                        retval = dt.DataType.Replace("(N,S)", "(" + fld.NumericPrecision.ToString() + "," + fld.NumericScale.ToString() + ")");
                                    }
                                      

                                }
                                else
                                {
                                    dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.NetDataType == fld.fieldtype && x.DataType.Contains("(N)")).FirstOrDefault();
                                    if (dt != null) 
                                    {
                                        retval = dt.DataType.Replace("(N)", "(" + fld.NumericPrecision.ToString() + ")");
                                        
                                    }else
                                    {
                                        dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.NetDataType == fld.fieldtype && x.DataType.Contains("(N,S)")).FirstOrDefault();
                                       
                                    }
                                    if (dt != null)
                                    {
                                        retval = dt.DataType.Replace("(N,S)", "(" + fld.NumericPrecision.ToString() + ",0)");
                                    }
                                   
                                }
                            }
                        }
                      




                        if (retval == null)
                        {
                            dt = DMEEditor.ConfigEditor.DataTypesMap.Where(x => x.DataSourceName == classhandler.className && x.NetDataType == fld.fieldtype).FirstOrDefault();
                            retval = dt.DataType;
                        }
                    
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", "Could not Find Class Handler " + fld.EntityName + "_" + fld.fieldname, DateTime.Now, -1, null, Errors.Failed);
                    }
                 
                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", "Could not Convert Field Type to Provider Type " + fld.EntityName + "_"+fld.fieldname, DateTime.Now, -1, null, Errors.Failed);
                }
              


            }
            catch (Exception ex)
            {
                string mes = "";
                retval = null;
                DMEEditor.AddLogMessage(ex.Message, "Could not Convert Field Type to Provider Type " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return retval;
        }


        public string[] GetNetDataTypes()
        {
            string[] a = NetDataTypeDef1.Split(',');
            Array.Sort<string>(a);
            return a;
        }
        public string[] GetNetDataTypes2()
        {
            string[] a = NetDataTypeDef2.Split(',');
            //mapping = new BindingList<DatatypeMapping>();
            Array.Sort<string>(a);
            return a;
        }

    }
}
