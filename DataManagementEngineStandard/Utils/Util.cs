using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Xml.Serialization;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Logger;
using System.Text.Json;
using System.Xml;
using TheTechIdea.Beep.Utilities;


namespace TheTechIdea.Beep.Utils
{
    public class Util : IUtil
    {
        public delegate T ObjectActivator<T>(params object[] args);
        public IDMLogger Logger { get; set; }
        public List<string> Namespacelist { get; set; } = new List<string>();
        public List<string> Classlist { get; set; } = new List<string>();
        public List<ParentChildObject> FunctionHierarchy { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IConfigEditor ConfigEditor { get; set; }
        public IDMEEditor DME { get; set; }
        public Util(IDMLogger logger, IErrorsInfo per, IConfigEditor pConfigEditor)
        {
            Logger = logger;
            ErrorObject = per;
            ConfigEditor = pConfigEditor;

        }
        public IBindingList ConvertDataTableToObservableList(DataTable dataTable, Type type)
        {
            var listType = typeof(ObservableBindingList<>).MakeGenericType(new Type[] { type });
            var list = (IBindingList)Activator.CreateInstance(listType);

            foreach (DataRow row in dataTable.Rows)
            {
                var item = Activator.CreateInstance(type);
                foreach (DataColumn column in dataTable.Columns)
                {
                    PropertyInfo property = item.GetType().GetProperty(column.ColumnName);
                    if (property != null && row[column] != DBNull.Value)
                    {
                        property.SetValue(item, Convert.ChangeType(row[column], property.PropertyType), null);
                    }
                }
                listType.GetMethod("Add").Invoke(list, new object[] { item });
            }

            return list;
        }

        public T CreateInstance<T>(params object[] paramArray)
        {
            return (T)Activator.CreateInstance(typeof(T), args: paramArray);
        }
        public TypeCode GetTypeCode(Type dest)
        {
            TypeCode retval = TypeCode.String;
            switch (dest.ToString())
            {
                case "System.String":
                    retval = TypeCode.String;
                    break;
                case "System.Decimal":
                    retval = TypeCode.Decimal;
                    break;
                case "System.DateTime":
                    retval = TypeCode.DateTime;
                    break;
                case "System.Char":
                    retval = TypeCode.Char;
                    break;
                case "System.Boolean":
                    retval = TypeCode.Boolean;
                    break;
                case "System.DBNull":
                    retval = TypeCode.DBNull;
                    break;
                case "System.Byte":
                    retval = TypeCode.Byte;
                    break;
                case "System.Int16":
                    retval = TypeCode.Int16;
                    break;
                case "System.Double":
                    retval = TypeCode.Double;
                    break;
                case "System.Int32":
                    retval = TypeCode.Int32;
                    break;
                case "System.Int64":
                    retval = TypeCode.Int64;
                    break;
                case "System.Single":
                    retval = TypeCode.Single;
                    break;
                case "System.Object":
                    retval = TypeCode.Object;
                    break;


            }
            return retval;
        }
        public bool IsObjectNumeric( object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
        public DataTable JsonToDataTable(string jsonString)
        {

            return DME.ConfigEditor.JsonLoader.JsonToDataTable(jsonString);
        }
        public ConnectionDriversConfig LinkConnection2Drivers(IConnectionProperties cn)
        {

            string vr = cn.DriverVersion;
            string pk = cn.DriverName;
            ConnectionDriversConfig retval=DME.ConfigEditor.DataDriversClasses.Where(c => c.PackageName.Equals(pk,StringComparison.InvariantCultureIgnoreCase) && c.version == vr).FirstOrDefault();
            if (retval == null)
            {
                retval = DME.ConfigEditor.DataDriversClasses.Where(c => c.PackageName.Equals(pk, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if(retval == null)
                {
                    retval = DME.ConfigEditor.DataDriversClasses.Where(c => c.DatasourceType== cn.DatabaseType).FirstOrDefault();
                    if(retval == null)
                    {
                        if (cn.Category == DatasourceCategory.FILE)
                        {
                            List<ConnectionDriversConfig> clss = DME.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
                            string ext = Path.GetExtension(cn.FileName).Replace(".", "");
                            retval = clss.Where(c => c.extensionstoHandle.Contains(ext)).FirstOrDefault();
                        }
                    }
                   
                }
                
            }
            return retval;


        }
        public static ObjectActivator<T> GetActivator<T>(ConstructorInfo ctor)
        {
            Type type = ctor.DeclaringType;
            ParameterInfo[] paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            ParameterExpression param =
                Expression.Parameter(typeof(object[]), "args");

            Expression[] argsExp =
                new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;

                Expression paramAccessorExp =
                    Expression.ArrayIndex(param, index);

                Expression paramCastExp =
                    Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            NewExpression newExp = Expression.New(ctor, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            LambdaExpression lambda =
                Expression.Lambda(typeof(ObjectActivator<T>), newExp, param);

            //compile it
            ObjectActivator<T> compiled = (ObjectActivator<T>)lambda.Compile();
            return compiled;
        }
        public ObservableBindingList<T> ConvertDataTableToObservableBindingList<T>(DataTable dt) where T : Entity,new()
        {
            ObservableBindingList<T> list = new ObservableBindingList<T>();

            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                list.Add(item);
            }

            return list;
        }
        public List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }
        private T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name == column.ColumnName)
                    {
                        var value = dr[column.ColumnName];

                        if (value == DBNull.Value)
                        {
                            value = pro.PropertyType.IsValueType ? Activator.CreateInstance(pro.PropertyType) : null;
                        }
                        else if (pro.PropertyType == typeof(char) && value is string str && str.Length == 1)
                        {
                            value = str[0];
                        }
                        else if (pro.PropertyType.IsEnum && value is string enumString)
                        {
                            value = Enum.Parse(pro.PropertyType, enumString);
                        }
                        else if (IsNumericType(pro.PropertyType) && value != null)
                        {
                            try
                            {
                                // Convert the value to the property type if it's a numeric type
                                value = Convert.ChangeType(value, pro.PropertyType);
                            }
                            catch (InvalidCastException ex)
                            {
                                // Handle the exception here (e.g., log it or throw a custom exception)
                                throw new InvalidCastException($"Cannot convert value to {pro.PropertyType}: {ex.Message}");
                            }
                        }

                        pro.SetValue(obj, value, null);
                    }
                }
            }
            return obj;
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }
        public ObservableCollection<T> ConvertToObservableCollection<T>(List<T> list)
        {
            var observableCollection = new ObservableCollection<T>(list);
            return observableCollection;
        }
        public bool AddinInterfaceFilter(Type typeObj, object criteriaObj)
        {
            if (typeObj.ToString() == criteriaObj.ToString())
                return true;
            else
                return false;
        }
        public DataTable CreateDataTableFromFile(string strFilePath)
        {
            DataTable dt = new DataTable();
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string l = sr.ReadLine();
                if (l != null)
                {
                    string[] headers = l.Split(',');
                    foreach (string header in headers)
                    {
                        dt.Columns.Add(header);
                    }
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i];
                        }
                        dt.Rows.Add(dr);
                    }


                }

            }


            return dt;
        }
        public DataTable CreateDataTableFromListofStrings(List<string> strings)
        {
            DataTable dt = new DataTable();
            string[] headers = strings[0].Split(',');
            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            for (int j = 1; j < strings.Count-1; j++)
            {
                string[] rows = strings[j].Split(',');
                DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    dr[i] = rows[i];
                }
                dt.Rows.Add(dr);
            }
            
            return dt;
        }
        public Type GetListType(object someList)
        {
            if (someList == null)
                return null;

            var type = someList.GetType();

            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>))
                return null;

            return type.GetGenericArguments()[0];
        }
        public DataTable ToDataTable(IList list, Type tp)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(tp);
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
            object[] values = new object[props.Count];
            foreach (var item in list)
            {
                for (int i = 0; i < values.Length; i++)
                    values[i] = props[i].GetValue(item) ?? DBNull.Value;
                table.Rows.Add(values);
            }
            return table;
        }
        public bool ToCSVFile(IList list, string filepath)
        {
            StreamWriter tw = new StreamWriter(filepath);
            string WriteValue = "";
            List<string> ls = new List<string>();
            if (list == null)
            {
                return false;
            }
            if(list.Count == 0)
            {
                return false;
            }
            var r1 = list[0];
            Type tp1 = r1.GetType();
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(tp1);
            //  var myObjectProperties = props.Select(x => x.Name);
            //Set the first row as your property names
           
            for (int i = 0; i < props.Count; i++)
            {
                ls.Add(props[i].Name);
            }
            WriteValue = string.Join(",", ls);
            tw.WriteLine(WriteValue);

            foreach (var item in list)
            {
                var csvRow = Environment.NewLine;
                for (int i = 0; i < ls.Count(); i++)
                {
                  
                    var value = props[i].GetValue(item) ?? DBNull.Value;
                    csvRow += $"{value},";
                }
                  
                csvRow.TrimEnd(',');
                tw.WriteLine(csvRow);
            }
            try
            {
                tw.Dispose();
                // File.WriteAllBytes(filepath, Encoding.ASCII.GetBytes(csvFile));
                return true;
            }
            catch (Exception ex)
            {
                tw.Dispose();
                DME.ErrorObject.Ex = ex;
                DME.ErrorObject.Flag = Errors.Failed;
                return false;
            }
            
           
        }
        public bool ToCSVFile(DataTable list, string filepath)
        {
            StreamWriter tw = new StreamWriter(filepath);
            string WriteValue = "";
            List<string> ls = new List<string>();
            if (list == null)
            {
                return false;
            }
          
            if (list.Rows.Count == 0)
            {
                return false;
            }
         
            foreach (DataColumn item in list.Columns)
            {
               ls.Add(item.ColumnName);
              
            }
             WriteValue = string.Join(",", ls);
            tw.WriteLine(WriteValue);
            for (int k = 0; k < list.Rows.Count; k++)
            {
                var csvRow = Environment.NewLine;
                for (int i = 0; i < ls.Count(); i++)
                {

                    var value = list.Rows[k][ls[i]].ToString() ;
                    csvRow += $"{value},";
                }

                csvRow.TrimEnd(',');
               tw.WriteLine(csvRow);
            }
            try
            {
                //  File.WriteAllBytes(filepath, Encoding.ASCII.GetBytes(csvFile));
                tw.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                tw.Dispose();
                DME.ErrorObject.Ex = ex;
                DME.ErrorObject.Flag = Errors.Failed;
                return false;
            }


        }
        public DataTable ToDataTable(Type tp)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(tp);
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
           
           
            return table;
        }
        public DataTable ToDataTable(IEntityStructure entity)
        {
            DataTable table = new DataTable();
            for (int i = 0; i < entity.Fields.Count; i++)
            {
                EntityField prop = entity.Fields[i];
                table.Columns.Add(prop.fieldname, Type.GetType(prop.fieldtype));
            }
            return table;
        }
        public DataTable CreateDataTableVer1(object[] array)
        {
            PropertyInfo[] properties = array.GetType().GetElementType().GetProperties();
            DataTable dt = CreateDataTable(properties);
            if (array.Length != 0)
            {
                foreach (object o in array)
                    FillData(properties, dt, o);
            }
            return dt;
        }
        private DataTable CreateDataTable(PropertyInfo[] properties)
        {
            DataTable dt = new DataTable();
            foreach (PropertyInfo pi in properties)
            {
                DataColumn dc = new DataColumn
                {
                    ColumnName = pi.Name,
                    DataType = pi.PropertyType
                };
                dt.Columns.Add(dc);
            }
            return dt;
        }
        public DataTable CreateDataTableVer2(object[] arr)
        {
            XmlSerializer serializer = new XmlSerializer(arr.GetType());
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, arr);
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            StringReader reader = new StringReader(sw.ToString());

            ds.ReadXml(reader);
            return ds.Tables[0];
        }
        private void FillData(PropertyInfo[] properties, DataTable dt, object o)
        {
            DataRow dr = dt.NewRow();
            foreach (PropertyInfo pi in properties)
            {
                if (pi.Name != null)
                    dr[pi.Name] = pi.GetValue(o, null);
            }
            dt.Rows.Add(dr);
        }
        // function that creates an object from the given data row
        public static T CreateItemFromRow<T>(DataRow row) where T : new()
        {
            // create a new object
            T item = new T();

            // set the item
            SetItemFromRow(item, row);

            // return 
            return item;
        }
        public static void SetItemFromRow<T>(T item, DataRow row) where T : new()
        {
            // go through each column
            foreach (DataColumn c in row.Table.Columns)
            {
                // find the property for the column
                PropertyInfo p = item.GetType().GetProperty(c.ColumnName);

                // if exists, set the value
                if (p != null && row[c] != DBNull.Value)
                {
                    p.SetValue(item, row[c], null);
                }
            }
        }
        public string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (!Path.HasExtension(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }
        public ExpandoObject convertToExpando(object obj)
        {
            //Get Properties Using Reflections
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            PropertyInfo[] properties = obj.GetType().GetProperties(flags);

            //Add Them to a new Expando
            ExpandoObject expando = new ExpandoObject();
            foreach (PropertyInfo property in properties)
            {
                AddProperty(expando, property.Name, property.GetValue(obj));
            }

            return expando;
        }
        public void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            //Take use of the IDictionary implementation
            var dictionary = (IDictionary<string, object>)expando;
            dictionary.Add(propertyName, propertyValue);

        }
        //public List<ExpandoObject> GetExpandoObject(DataTable dt, Type type, EntityStructure enttype)
        //{


        //    string f = "";
        //    List<ExpandoObject> Records = new List<ExpandoObject>();
        //    Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

        //    for (int i = 0; i <= enttype.Fields.Count - 1; i++)
        //    {
        //        properties.Add(enttype.Fields[i].fieldname, type.GetProperty(enttype.Fields[i].fieldname));
        //    }
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        //x = TypeHelpers.GetInstance(type);
        //        // x = Activator.CreateInstance(type);
        //        var accessor = TypeAccessor.Create(type);
        //       dynamic x = new ExpandoObject();
        //       x = convertToExpando(type);

        //        var v = (dynamic)null;
        //        for (int i = 0; i <= enttype.Fields.Count - 1; i++)
        //        {
        //            try
        //            {
        //                f = enttype.Fields[i].fieldname + "-" + enttype.Fields[i].fieldtype + "-" + row[enttype.Fields[i].fieldname];
        //                try
        //                {
        //                    v = Convert.ChangeType(row[enttype.Fields[i].fieldname], Type.GetType(enttype.Fields[i].fieldtype));
        //                    if ((row[enttype.Fields[i].fieldname] == null || row[enttype.Fields[i].fieldname] == "")) //&& (v.GetType().ToString()!="System.String")
        //                    {
        //                        v = null;
        //                    }

        //                }
        //                catch (Exception)
        //                {

        //                    v = null;
        //                    //Logger.WriteLog($"Error in Creating Record or Setting Value." + f);
        //                }
        //                accessor[x, enttype.Fields[i].fieldname] = v;
        //                //Dynamic.InvokeSet(x, enttype.Fields[i].fieldname, v);
        //                //properties[enttype.Fields[i].fieldname].SetValue(x, v, null);
        //                //  type.GetProperty(enttype.Fields[i].fieldname).SetValue(x, v, null);
        //                // Logger.WriteLog($"Creating Field and Value." + f);
        //            }
        //            catch (Exception ex)
        //            {

        //                Logger.WriteLog($"Error in Creating Record or Setting Value." + f + ":" + ex.Message);
        //            }



        //        }
        //        Records.Add(x);

        //        //}

        //    }
        //    return Records;
        //}
        public  object GetBindingListFromIList(IList inputList, Type itemType, EntityStructure entType)
        {
            Type observableType = typeof(ObservableBindingList<>).MakeGenericType(itemType);
            object[] args = new object[] { inputList };
            return Activator.CreateInstance(observableType, args);

          //  return bindingList;
        }
        public object GetBindingListByDataTable(DataTable dt, Type type, EntityStructure enttype)
        {
            Type observableType = typeof(ObservableBindingList<>).MakeGenericType(type);

            object[] args = new object[] {  };
            IBindingListView records= (IBindingListView)Activator.CreateInstance(observableType, args);
            //List<object> records = new List<object>();
            Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

            // Initialize properties dictionary based on EntityStructure
            foreach (var field in enttype.Fields)
            {
                PropertyInfo prop = type.GetProperty(field.fieldname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    properties.Add(field.fieldname.ToLower(), prop);
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                dynamic x = Activator.CreateInstance(type);

                foreach (DataColumn item in dt.Columns)
                {
                    string columnName = item.ColumnName.ToLower();
                    if (row[item.ColumnName] != DBNull.Value && properties.ContainsKey(columnName))
                    {
                        string stringValue = row[item.ColumnName].ToString();
                        Type targetType = Type.GetType(enttype.Fields.FirstOrDefault(p => p.fieldname.Equals(item.ColumnName, StringComparison.InvariantCultureIgnoreCase))?.fieldtype);
                        var convertedValue = Convert.ChangeType(row[item.ColumnName], targetType);
                        if (!string.IsNullOrWhiteSpace(stringValue))
                        {
                            properties[columnName].SetValue(x, convertedValue, null);
                        }
                    }
                }

                records.Add(x);
            }

           
            return records;
        }

        public List<object> GetListByDataTable(DataTable dt, Type type, EntityStructure enttype)
        {
            //  string f = "";
            List<object> Records = new List<object>();
            Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
          
            for (int i = 0; i <= enttype.Fields.Count - 1; i++)
            {
                properties.Add(enttype.Fields[i].fieldname, type.GetProperty(enttype.Fields[i].fieldname));
            }
            foreach (DataRow row in dt.Rows)
            {
                // dynamic x = TypeHelpers.GetInstance(type);
                dynamic x = Activator.CreateInstance(type);

                foreach (DataColumn item in dt.Columns)
                {
                    if (row[item.ColumnName] != DBNull.Value)
                    {
                        string st = row[item.ColumnName].ToString();
                        Type tp = Type.GetType(enttype.Fields.Where(p => p.fieldname.Equals(item.ColumnName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault().fieldtype);
                        var v = Convert.ChangeType(row[item.ColumnName],tp);
                        if (!string.IsNullOrEmpty(st) && !string.IsNullOrWhiteSpace(st))
                        {
                            properties[item.ColumnName].SetValue(x,v, null);
                        }
                    }

                }
              

                Records.Add(x);
            }
            return Records;
        }
        public List<object> GetListByDataTable(IDMEEditor editor, DataTable dt,string NameSpace,string Entityname)
        {
            List<EntityField> flds=new List<EntityField>();
            //  Create type from Table 
            foreach (DataColumn item in dt.Columns)
            {
                EntityField field = new EntityField();
                field.EntityName = dt.TableName;
                field.fieldname=item.ColumnName;
                field.fieldtype = item.DataType.ToString();
                field = SetField(item, field);
                flds.Add(field);

                //  properties[item.ColumnName].SetValue(x, row[item.ColumnName], null);
            }
            DMTypeBuilder.CreateNewObject(editor,Entityname, NameSpace, Entityname, flds);
            Type type = DMTypeBuilder.MyType;
            //  string f = "";
            List<object> Records = new List<object>();
            Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
            foreach (DataColumn item in dt.Columns)
            {
                properties.Add(item.ColumnName, type.GetProperty(item.ColumnName));
                //  properties[item.ColumnName].SetValue(x, row[item.ColumnName], null);
            }
            //for (int i = 0; i <= enttype.Fields.Count - 1; i++)
            //{
            //    properties.Add(enttype.Fields[i].fieldname, type.GetProperty(enttype.Fields[i].fieldname));
            //}
            //for (int i = 0; i <= enttype.Fields.Count - 1; i++)
            //{}
            foreach (DataRow row in dt.Rows)
            {
                // dynamic x = TypeHelpers.GetInstance(type);
                dynamic x = Activator.CreateInstance(type);
                //  var v = (dynamic)null;
                foreach (DataColumn item in dt.Columns)
                {
                    if (row[item.ColumnName] != DBNull.Value)
                    {
                        string st = row[item.ColumnName].ToString();
                        if (!string.IsNullOrEmpty(st) && !string.IsNullOrWhiteSpace(st))
                        {
                            properties[item.ColumnName].SetValue(x, row[item.ColumnName], null);
                        }
                    }

                }
               
                Records.Add(x);
            }
            return Records;
        }
        public EntityStructure GetEntityStructure(DataTable tb)
        {
            int i = 0;
            EntityStructure entityData = new EntityStructure();
            try
            {
                string sheetname;
                sheetname = tb.TableName;
                entityData.EntityName = sheetname;
                List<EntityField> Fields = new List<EntityField>();
                int y = 0;
                foreach (DataColumn field in tb.Columns)
                {
                    EntityField f = new EntityField();
                        f=SetField(field, f);
                    f.FieldIndex = y;
                    f.EntityName = sheetname;
                    Fields.Add(f);
                    y += 1;

                }

                i += 1;
                entityData.Fields = new List<EntityField>();
                entityData.Fields.AddRange(Fields);

            }
            catch (Exception ex)
            {
               // DME.AddLogMessage("Error", "Could not Create Entity Structure" + ex.Message, DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            return entityData;


        }
        public bool Download(string url, string downloadFileName, string downloadFilePath)
        {
            string downloadfile = downloadFilePath + downloadFileName;
            string httpPathWebResource = null;
            bool ifFileDownoadedchk = false;
            ifFileDownoadedchk = false;
            WebClient myWebClient = new WebClient();
            httpPathWebResource = url + downloadFileName;
            myWebClient.DownloadFile(httpPathWebResource, downloadfile);
            ifFileDownoadedchk = true;
            return ifFileDownoadedchk;
        }
        public Type GetTypeFromStringValue(string str)
        {
            byte byteValue;
            int intValue;
            double doubleValue;
            char charValue;
            bool boolValue;
            float floatValue;
            DateTime dateValue;
            decimal decimalValue;
            string strvalue = str;
            long longValue;
            // Place checks higher if if-else statement to give higher priority to type.
            if (int.TryParse(str, out intValue))
                return intValue.GetType();
            else if (double.TryParse(str, out doubleValue))
                return doubleValue.GetType();
            else if (char.TryParse(str, out charValue))
                return charValue.GetType();
            else if (bool.TryParse(str, out boolValue))
                return boolValue.GetType();
            else if (DateTime.TryParse(str, out dateValue))
                return dateValue.GetType();
            else if (decimal.TryParse(str, out decimalValue))
                return decimalValue.GetType();
            else if (long.TryParse(str, out longValue))
                return longValue.GetType();
            else if (byte.TryParse(str, out byteValue))
                return byteValue.GetType();
            else if (float.TryParse(str, out floatValue))
                return floatValue.GetType();
            else
                return strvalue.GetType();
        }
        public Type MakeGenericListofType(string typestring)
        {
            Type genericType = typeof(List<>); // Generic type definition
            string elementTypeName = typestring;
            Type elementType = Type.GetType(elementTypeName);

            Type specificType = genericType.MakeGenericType(elementType); // Construct specific generic type

         

            return specificType;
        }
        public Type MakeGenericType(string typestring)
        {
            string elementTypeName = typestring;
            Type type= Type.GetType(elementTypeName);
            return type.MakeGenericType();
            // return listType.MakeGenericType(types);
        }
        public Type MakeGenericType(string typestring,Type[] parameters)
        {
            string elementTypeName = typestring;
            Type type = Type.GetType(elementTypeName);
            return type.MakeGenericType(parameters);
            // return listType.MakeGenericType(types);
        }
        public EntityStructure GetEntityStructureFromType<T>()
        {
            EntityStructure entity = new EntityStructure();
            Type entityType = typeof(T);

            if (entity.Fields.Count == 0)
            {
                // Iterate over the properties of the type to create fields
                foreach (PropertyInfo propInfo in entityType.GetProperties())
                {
                    EntityField field = new EntityField
                    {
                        fieldname = propInfo.Name,
                        fieldtype = propInfo.PropertyType.FullName
                    };

                    // Additional attributes like Size1, IsAutoIncrement, AllowDBNull, and IsUnique
                    // might not be directly available or applicable for every property type.
                    // You'll need to set these based on the specific needs or defaults.

                    if (field.IsKey)
                    {
                        entity.PrimaryKeys.Add(field);
                    }

                    entity.Fields.Add(field);
                }
            }

            return entity;
        }
        public EntityStructure GetEntityStructureFromList<T>(List<T> list)
        {
            EntityStructure entity = new EntityStructure();

            // Check if the list is empty. If it's empty, we can still get the structure from the type T.
            if (list == null || !list.Any())
            {
                return GetEntityStructureFromType<T>();
            }

            // Get the type of the items in the list
            Type itemType = typeof(T);

            // Iterate over the properties of the type to create fields
            foreach (PropertyInfo propInfo in itemType.GetProperties())
            {
                EntityField field = new EntityField
                {
                    fieldname = propInfo.Name,
                    fieldtype = propInfo.PropertyType.ToString(),
                    // Additional attributes like Size1, IsAutoIncrement, AllowDBNull, and IsUnique
                    // might need to be inferred or set to default values as they are not directly available from PropertyInfo
                };

                // Additional logic to determine if the field is a key, etc.
                // This might involve custom attributes or conventions

                entity.Fields.Add(field);
            }

            return entity;
        }
        public EntityStructure GetEntityStructureFromType(Type type)
        {
            EntityStructure entity = new EntityStructure();
            if (entity.Fields.Count == 0)
            {
                // Iterate over the properties of the type to create fields
                foreach (PropertyInfo propInfo in type.GetProperties())
                {
                   entity= SetField(propInfo, entity);
                }
            }
            return entity;
        }
        private EntityField SetField(PropertyInfo propInfo, EntityField entity)
        {
            DbFieldCategory fldcat = DbFieldCategory.String;
            if (propInfo.PropertyType == typeof(string))
            {
                fldcat = DbFieldCategory.String;
            }
            else if (propInfo.PropertyType == typeof(int) || propInfo.PropertyType == typeof(long) || propInfo.PropertyType == typeof(float) || propInfo.PropertyType == typeof(double) || propInfo.PropertyType == typeof(decimal))
            {
                fldcat = DbFieldCategory.Numeric;
            }
            else if (propInfo.PropertyType == typeof(DateTime))
            {
                fldcat = DbFieldCategory.Date;
            }
            else if (propInfo.PropertyType == typeof(bool))
            {
                fldcat = DbFieldCategory.Boolean;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(Guid))
            {
                fldcat = DbFieldCategory.Guid;
            }
            else if (propInfo.PropertyType == typeof(JsonDocument))
            {
                fldcat = DbFieldCategory.Json;
            }
            else if (propInfo.PropertyType == typeof(XmlDocument))
            {
                fldcat = DbFieldCategory.Xml;
            }
            else if (propInfo.PropertyType == typeof(decimal))
            {
                fldcat = DbFieldCategory.Currency;
            }
            else if (propInfo.PropertyType.IsEnum)
            {
                fldcat = DbFieldCategory.Enum;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            EntityField field = new EntityField
            {
                fieldname = propInfo.Name,
                fieldtype = propInfo.PropertyType.FullName,
                fieldCategory = fldcat
            };
            // Additional attributes like Size1, IsAutoIncrement, AllowDBNull, and IsUnique
            // might not be directly available or applicable for every property type.
            // You'll need to set these based on the specific needs or defaults.

            return field;
        }
        private EntityField SetField(DataColumn col, EntityField entity)
        {
            DbFieldCategory fldcat = DbFieldCategory.String;
            if (col.DataType != null) {
                // set fldcat based on col.DataType using all posibilties
                if(col.DataType == typeof(string))
                {
                    fldcat = DbFieldCategory.String;
                }
                else if (col.DataType == typeof(int) || col.DataType == typeof(long) || col.DataType == typeof(float) || col.DataType == typeof(double) || col.DataType == typeof(decimal))
                {
                    fldcat = DbFieldCategory.Numeric;
                }
                else if (col.DataType == typeof(DateTime))
                {
                    fldcat = DbFieldCategory.Date;
                }
                else if (col.DataType == typeof(bool))
                {
                    fldcat = DbFieldCategory.Boolean;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
                else if (col.DataType == typeof(Guid))
                {
                    fldcat = DbFieldCategory.Guid;
                }
                else if (col.DataType == typeof(JsonDocument))
                {
                    fldcat = DbFieldCategory.Json;
                }
                else if (col.DataType == typeof(XmlDocument))
                {
                    fldcat = DbFieldCategory.Xml;
                }
                else if (col.DataType == typeof(decimal))
                {
                    fldcat = DbFieldCategory.Currency;
                }
                else if (col.DataType.IsEnum)
                {
                    fldcat = DbFieldCategory.Enum;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
                else if (col.DataType == typeof(byte[]))
                {
                    fldcat = DbFieldCategory.Binary;
                }
            }

            EntityField field = new EntityField
            {
                EntityName=col.Table.TableName,
                fieldname = col.ColumnName,
                fieldtype = col.DataType.ToString(),
                fieldCategory = fldcat,
                ValueRetrievedFromParent = false,
                FieldIndex = col.Ordinal

            };
            // Additional attributes like Size1, IsAutoIncrement, AllowDBNull, and IsUnique
            // might not be directly available or applicable for every property type.
            // You'll need to set these based on the specific needs or defaults.

            return field;
        }
        private EntityStructure SetField(PropertyInfo propInfo,EntityStructure entity)
        {
            DbFieldCategory fldcat = DbFieldCategory.String;
            if (propInfo.PropertyType == typeof(string))
            {
                fldcat = DbFieldCategory.String;
            }
            else if (propInfo.PropertyType == typeof(int) || propInfo.PropertyType == typeof(long) || propInfo.PropertyType == typeof(float) || propInfo.PropertyType == typeof(double) || propInfo.PropertyType == typeof(decimal))
            {
                fldcat = DbFieldCategory.Numeric;
            }
            else if (propInfo.PropertyType == typeof(DateTime))
            {
                fldcat = DbFieldCategory.Date;
            }
            else if (propInfo.PropertyType == typeof(bool))
            {
                fldcat = DbFieldCategory.Boolean;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(Guid))
            {
                fldcat = DbFieldCategory.Guid;
            }
            else if (propInfo.PropertyType == typeof(JsonDocument))
            {
                fldcat = DbFieldCategory.Json;
            }
            else if (propInfo.PropertyType == typeof(XmlDocument))
            {
                fldcat = DbFieldCategory.Xml;
            }
            else if (propInfo.PropertyType == typeof(decimal))
            {
                fldcat = DbFieldCategory.Currency;
            }
            else if (propInfo.PropertyType.IsEnum)
            {
                fldcat = DbFieldCategory.Enum;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            else if (propInfo.PropertyType == typeof(byte[]))
            {
                fldcat = DbFieldCategory.Binary;
            }
            EntityField field = new EntityField
            {
                fieldname = propInfo.Name,
                fieldtype = propInfo.PropertyType.FullName,
                fieldCategory = fldcat
            };
            // Additional attributes like Size1, IsAutoIncrement, AllowDBNull, and IsUnique
            // might not be directly available or applicable for every property type.
            // You'll need to set these based on the specific needs or defaults.
            if (field.IsKey)
            {
                entity.PrimaryKeys.Add(field);
            }
            entity.Fields.Add(field);
            return entity;
        }
        public EntityStructure GetEntityStructureFromListorTable(  dynamic retval)
        {
            EntityStructure entity = new EntityStructure();
            Type tp = retval.GetType();
            DataTable dt;
            if (entity.Fields.Count == 0)
            {
                if (tp.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IList)))
                {
                    dt = ToDataTable((IList)retval, GetListType(tp));
                }
                else
                {
                    dt = (DataTable)retval;

                }
                foreach (DataColumn item in dt.Columns)
                {
                    EntityField x = new EntityField();
                    try
                    {
                        x.fieldname = item.ColumnName;
                        x.fieldtype = item.DataType.ToString(); //"ColumnSize"
                        DbFieldCategory fieldCategory = DbFieldCategory.String;
                        if (item.DataType == typeof(string))
                        {
                            fieldCategory = DbFieldCategory.String;
                        }
                        else if (item.DataType == typeof(int) || item.DataType == typeof(long) || item.DataType == typeof(float) || item.DataType == typeof(double) || item.DataType == typeof(decimal))
                        {
                            fieldCategory = DbFieldCategory.Numeric;
                        }
                        else if (item.DataType == typeof(DateTime))
                        {
                            fieldCategory = DbFieldCategory.Date;
                        }
                        else if (item.DataType == typeof(bool))
                        {
                            fieldCategory = DbFieldCategory.Boolean;
                        }
                        else if (item.DataType == typeof(byte[]))
                        {
                            fieldCategory = DbFieldCategory.Binary;
                        }
                        else if (item.DataType == typeof(Guid))
                        {
                            fieldCategory = DbFieldCategory.Guid;
                        }
                        else if (item.DataType == typeof(JsonDocument))
                        {
                            fieldCategory = DbFieldCategory.Json;
                        }
                        else if (item.DataType == typeof(XmlDocument))
                        {
                            fieldCategory = DbFieldCategory.Xml;
                        }
                        else if (item.DataType == typeof(decimal))
                        {
                            fieldCategory = DbFieldCategory.Currency;
                        }
                        else if (item.DataType.IsEnum)
                        {
                            fieldCategory = DbFieldCategory.Enum;
                        }
                        x.fieldCategory = fieldCategory;
                        x.Size1 = item.MaxLength;
                        try
                        {
                            x.IsAutoIncrement = item.AutoIncrement;
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            x.AllowDBNull = item.AllowDBNull;
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            x.IsUnique = item.Unique;
                        }
                        catch (Exception)
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("Error in Creating Field Type");
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Ex = ex;
                    }

                    if (x.IsKey)
                    {
                        entity.PrimaryKeys.Add(x);
                    }


                    entity.Fields.Add(x);
                }

            }
            return entity;
        }
        public List<object> ConvertTableToList(DataTable dt, EntityStructure ent, Type enttype)
        {
            List<object> retval = new List<object>();

            foreach (DataRow dr in dt.Rows)
            {
                var entityInstance = Activator.CreateInstance(enttype);
                foreach (EntityField col in ent.Fields)
                {
                    PropertyInfo propertyInfo = entityInstance.GetType().GetProperty(col.fieldname);
                    if (propertyInfo != null && dr[col.fieldname] != DBNull.Value)
                    {
                        try
                        {
                            object value = Convert.ChangeType(dr[col.fieldname], propertyInfo.PropertyType);
                            propertyInfo.SetValue(entityInstance, value);
                        }
                        catch (Exception ex)
                        {
                            // Log the exception or handle it accordingly
                            // Consider how you want to handle the case where the conversion fails
                        }
                    }
                    else if (propertyInfo != null)
                    {
                        // If it's DBNull, but the property exists, set the property to its default value
                        var defaultValue = propertyInfo.PropertyType.IsValueType ? Activator.CreateInstance(propertyInfo.PropertyType) : null;
                        propertyInfo.SetValue(entityInstance, defaultValue);
                    }
                }
                retval.Add(entityInstance);
            }

            // If DataTable is empty, add a default instance with null properties
            if (dt.Rows.Count == 0)
            {
                var entityInstance = Activator.CreateInstance(enttype);
                foreach (EntityField col in ent.Fields)
                {
                    PropertyInfo propertyInfo = entityInstance.GetType().GetProperty(col.fieldname);
                    if (propertyInfo != null)
                    {
                        var defaultValue = propertyInfo.PropertyType.IsValueType ? Activator.CreateInstance(propertyInfo.PropertyType) : null;
                        propertyInfo.SetValue(entityInstance, defaultValue);
                    }
                }
                retval.Add(entityInstance);
            }

            return retval;
        }
        public DataRow ConvertItemClassToDataRow(EntityStructure ent)
        {
            DataTable dt = new DataTable();
            DataRow dr;
            foreach (EntityField col in ent.Fields)
            {
                DataColumn co=dt.Columns.Add(col.fieldname.ToUpper());
                co.DataType = Type.GetType(col.fieldtype);

            }
            
             dr= dt.NewRow();
            return dr;
        }
        public List<EntityField> GetFieldFromGeneratedObject(object dt, Type tp=null)
        {
            List<EntityField> retval = new List<EntityField>();
            DataRow dr;
            DataRowView dv;
            DataTable tb = new DataTable();
            if (dt.GetType().FullName == "System.Data.DataRowView")
            {
                dv = (DataRowView)dt;
                dr = dv.Row;
             
            }
            else
            if (dt.GetType().FullName == "System.Data.DataRow")
            {
                dr = (DataRow)dt;
            }
            else
            if(dt.GetType().FullName == "System.Data.DataTable")
            {
                tb = (DataTable) dt;
                dr = tb.NewRow();
            }
            else
            {
               
                foreach (PropertyInfo pr in tp.GetProperties() )
                {
                 
                    DataColumn co = tb.Columns.Add(pr.Name);
                    co.DataType = pr.PropertyType;

                   
                }
                dr = tb.NewRow();
            }
            foreach (DataColumn item in dr.Table.Columns)
            {
                EntityField f = new EntityField();
                f.fieldname = item.ColumnName;
                f.fieldtype = item.DataType.FullName;
                DbFieldCategory fieldCategory = DbFieldCategory.String;
                if (item.DataType == typeof(string))
                {
                    fieldCategory = DbFieldCategory.String;
                }
                else if (item.DataType == typeof(int) || item.DataType == typeof(long) || item.DataType == typeof(float) || item.DataType == typeof(double) || item.DataType == typeof(decimal))
                {
                    fieldCategory = DbFieldCategory.Numeric;
                }
                else if (item.DataType == typeof(DateTime))
                {
                    fieldCategory = DbFieldCategory.Date;
                }
                else if (item.DataType == typeof(bool))
                {
                    fieldCategory = DbFieldCategory.Boolean;
                }
                else if (item.DataType == typeof(byte[]))
                {
                    fieldCategory = DbFieldCategory.Binary;
                }
                else if (item.DataType == typeof(Guid))
                {
                    fieldCategory = DbFieldCategory.Guid;
                }
                else if (item.DataType == typeof(JsonDocument))
                {
                    fieldCategory = DbFieldCategory.Json;
                }
                else if (item.DataType == typeof(XmlDocument))
                {
                    fieldCategory = DbFieldCategory.Xml;
                }
                else if (item.DataType == typeof(decimal))
                {
                    fieldCategory = DbFieldCategory.Currency;
                }
                else if (item.DataType.IsEnum)
                {
                    fieldCategory = DbFieldCategory.Enum;
                }
                f.fieldCategory = fieldCategory;

                try
                {
                    f.IsAutoIncrement = item.AutoIncrement;
                }
                catch (Exception)
                {

                }
                try
                {
                    f.AllowDBNull = item.AllowDBNull;
                }
                catch (Exception)
                {


                }
               
                try
                {
                    f.Size1 =item.MaxLength;
                }
                catch (Exception)
                {

                }
                
                try
                {
                    f.IsUnique =item.Unique;
                }
                catch (Exception)
                {

                }
                retval.Add(f);
            }
            return retval;
        }
        public  Type GetEntityType(IDMEEditor DMEEditor, string EntityName,List<EntityField> Fields)
        {
            
            DMTypeBuilder.CreateNewObject(DMEEditor, EntityName, EntityName, Fields);
            return DMTypeBuilder.MyType;
        }
        public object GetEntityObject(IDMEEditor DMEEditor, string EntityName, List<EntityField> Fields)
        {
            return DMTypeBuilder.CreateNewObject(DMEEditor, EntityName, EntityName, Fields);
        }
        public DataRow GetDataRowFromobject(string EntityName, Type enttype,object UploadDataRow, EntityStructure DataStruct)
        {
            DataRow dr=null ;
            dynamic result = null;
              var ti = Activator.CreateInstance(enttype);
            // ICustomTypeDescriptor, IEditableObject, IDataErrorInfo, INotifyPropertyChanged
            if (UploadDataRow.GetType().FullName == "System.Data.DataRowView")
            {
                DataRowView dv = (DataRowView)UploadDataRow;
                dr = dv.Row;

            }
            else
            if (UploadDataRow.GetType().FullName == "System.Data.DataRow")
            {
                dr = (DataRow)UploadDataRow;
            }
            else
            {
                dr = ConvertItemClassToDataRow(DataStruct);
                foreach (EntityField col in DataStruct.Fields)
                {
                    try
                    {
                        PropertyInfo GetPropAInfo = UploadDataRow.GetType().GetProperty(col.fieldname, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

                        //if (GetPropAInfo.GetValue(UploadDataRow) != System.DBNull.Value)
                        //{
                        PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                        if (GetPropAInfo != null && PropAInfo !=null)
                        {
                            result = GetPropAInfo.GetValue(UploadDataRow);


                        }
                    }
                    catch (Exception ex)
                    {

                        result = DBNull.Value;
                    }
                   
                    if (result == null)
                    {
                        result = DBNull.Value;
                    }
                    //if (result != null && result != System.DBNull.Value)
                    //{
                    //    if (col.fieldtype.Contains("Date"))
                    //    {

                    //        DateTime? dt = (DateTime)result;
                    //        if (dt == DateTime.MinValue || dt == DateTime.MaxValue)
                    //        {
                    //            result = DBNull.Value;
                    //        }

                    //    }
                    //}

                    dr[col.fieldname] = result;
                }
            }
            return dr;
        }
        public object  MapObjectToAnother(IDMEEditor DMEEditor, string destentityname, EntityDataMap_DTL SelectedMapping ,object sourceobj)
        {
            object destobj = GetEntityObject(DMEEditor,destentityname, SelectedMapping.SelectedDestFields);
            foreach (Mapping_rep_fields col in SelectedMapping.FieldMapping)
            {
                try
                {
                    PropertyInfo SrcPropAInfo = sourceobj.GetType().GetProperty(col.FromFieldName);
                    PropertyInfo DestPropAInfo = destobj.GetType().GetProperty(col.ToFieldName);
                    var val= SrcPropAInfo.GetValue(sourceobj, null);
                    DestPropAInfo.SetValue(destobj, val, null);
                   
                }
                catch (Exception ex)
                {


                }
              
            }
            return destobj;


        }
        public object GetFieldValueFromObject(string fieldname, object sourceobj)
        {
            PropertyInfo SrcPropAInfo = sourceobj.GetType().GetProperty(fieldname);
            return SrcPropAInfo.GetValue(sourceobj, null);

        }
        public Type GetCollectionElementType(Type type)
        {
            if (null == type)
                throw new ArgumentNullException("type");

            // first try the generic way
            // this is easy, just query the IEnumerable<T> interface for its generic parameter
            var etype = typeof(IEnumerable<>);
            foreach (var bt in type.GetInterfaces())
                if (bt.IsGenericType && bt.GetGenericTypeDefinition() == etype)
                    return bt.GetGenericArguments()[0];

            // now try the non-generic way

            // if it's a dictionary we always return DictionaryEntry
            if (typeof(IDictionary).IsAssignableFrom(type))
                return typeof(DictionaryEntry);

            // if it's a list we look for an Item property with an int index parameter
            // where the property type is anything but object
            if (typeof(IList).IsAssignableFrom(type))
            {
                foreach (var prop in type.GetProperties())
                {
                    if ("Item" == prop.Name && typeof(object) != prop.PropertyType)
                    {
                        var ipa = prop.GetIndexParameters();
                        if (1 == ipa.Length && typeof(int) == ipa[0].ParameterType)
                        {
                            return prop.PropertyType;
                        }
                    }
                }
            }

            // if it's a collection, we look for an Add() method whose parameter is 
            // anything but object
            if (typeof(ICollection).IsAssignableFrom(type))
            {
                foreach (var meth in type.GetMethods())
                {
                    if ("Add" == meth.Name)
                    {
                        var pa = meth.GetParameters();
                        if (1 == pa.Length && typeof(object) != pa[0].ParameterType)
                            return pa[0].ParameterType;
                    }
                }
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
                return typeof(object);
            return null;
        }
        public IErrorsInfo SetFieldValueFromObject(string fieldname, object sourceobj, object value)
        {
            try
            {
                PropertyInfo SrcPropAInfo = sourceobj.GetType().GetProperty(fieldname);
                //dynamic v = Convert.ChangeType(value, SrcPropAInfo.PropertyType);
                int v;
                string vs;
                DateTime vd;
                Type valtype=value.GetType();
                Type rectype= SrcPropAInfo.GetType();
                //if (valtype == typeof(System.Int16)|| valtype == typeof(System.Int32) || valtype == typeof(System.Int64))
                //{
                //     v = (int)value;
                //    SrcPropAInfo.SetValue(v, sourceobj, null);

                //}
                //else if (valtype == typeof(System.String))
                //{
                //     vs = (string)value;
                //    SrcPropAInfo.SetValue(vs, sourceobj, null);

                //}
                //else if (valtype == typeof(System.DateTime))
                //{
                //    vd = (DateTime)value;
                //    SrcPropAInfo.SetValue(vd, sourceobj, null);
                //}
                if (SrcPropAInfo != null)
                {
                    Type t = Nullable.GetUnderlyingType(SrcPropAInfo.PropertyType) ?? SrcPropAInfo.PropertyType;
                    object safeValue = value == null ? null : Convert.ChangeType(value, t);
                    SrcPropAInfo.SetValue(sourceobj, safeValue, null);
                }
                //SrcPropAInfo.SetValue(sourceobj, Convert.ChangeType(value, SrcPropAInfo.PropertyType), null);

            }
            catch (Exception ex)
            {
                DME.AddLogMessage("Util", "Error Could not Set Value in Object " + value.ToString(), DateTime.Now, 0, value.ToString(), Errors.Failed);
            }
            return DME.ErrorObject;
        }
        public List<T> GetTypedList<T>(List<object> ls)
        {
             
            List<T> ret = ls
         .OfType<T>()
         .ToList();
            return ret;
        }
        #region "File Helpers"
        public virtual List<ConnectionProperties> LoadFiles(string[] filenames)
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                string extens =DME.ConfigEditor.CreateFileExtensionString();
                if(filenames.Length == 0)
                {
                    return null;
                }
                
                retval = CreateFileConnections(filenames);
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
               DME.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        public bool IsFileValid(string filename)
        {
            bool retval = false;
            string ext = Path.GetExtension(filename).Replace(".", "").ToLower();
            List<ConnectionDriversConfig> clss =DME.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            if (clss != null)
            {
                IEnumerable<string> extensionslist = clss.Select(p => p.extensionstoHandle);
                string extstring = string.Join(",", extensionslist);
                List<string> exts = extstring.Split(',').Distinct().ToList();
                retval = exts.Contains(ext);
            }
            return retval;
        }
        public string CreateFileExtensionString()
        {
            List<ConnectionDriversConfig> clss =DME.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            string retval = null;
            if (clss != null)
            {
                IEnumerable<string> extensionslist = clss.Select(p => p.extensionstoHandle);
                string extstring = string.Join(",", extensionslist);
                List<string> exts = extstring.Split(',').Distinct().ToList();

                foreach (string item in exts)
                {
                    retval += item + " files(*." + item + ")|*." + item + "|";


                }

            }

            retval += "All files(*.*)|*.*";
            return retval;
        }
        public string CreateFileExtensionString(string extens)
        {
            //    List<ConnectionDriversConfig> clss = DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            string retval = null;
            if (extens != null)
            {
                IEnumerable<string> extensionslist = extens.Split(',').AsEnumerable();
                string extstring = string.Join(",", extensionslist);
                List<string> exts = extstring.Split(',').Distinct().ToList();

                foreach (string item in exts)
                {
                    retval += item + " files(*." + item + ")|*." + item + "|";


                }

            }

            retval += "All files(*.*)|*.*";
            return retval;
        }
        public virtual List<ConnectionProperties> LoadFiles(string directoryname, string extens)
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                //   string extens = CreateFileExtensionString();
                string[] filenames = Directory.GetFiles(directoryname, CreateFileExtensionString(extens));
               
                retval = CreateFileConnections(filenames);
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
               DME.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        public List<ConnectionProperties> CreateFileConnections(string[] filenames)
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                foreach (string file in filenames)
                {
                    {
                        if (File.Exists(file))
                        {
                            string filename = Path.GetFileName(file);
                            if (FileHelper.FileExists(DME,file)==null)
                            {
                                ConnectionProperties c = CreateFileDataConnection(file);
                                if (c != null)
                                {
                                    retval.Add(c);
                                }
                            }
                           
                        }
                        else
                        {
                           DME.AddLogMessage("Bepp", $"File {file} Exist ", DateTime.Now, -1, null, Errors.Failed);
                        }

                    }
                }
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
               DME.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        public ConnectionProperties CreateFileDataConnection(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                   DME.AddLogMessage("Beep", $"Error Could not Find File {file}", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }
                if (FileHelper.FileExists(DME, file) != null)
                {
                    return FileHelper.FileExists(DME, file);
                }
                    string filename = Path.GetFileName(file);
                string ext = Path.GetExtension(file).Replace(".", "").ToLower();



                ConnectionProperties f = new ConnectionProperties
                {
                    FileName = filename,
                    FilePath = Path.GetDirectoryName(file),
                    Ext = ext,
                    ConnectionName = filename


                };
                //if (f.FilePath.Contains(DME.ConfigEditor.ExePath))
                //{
                //    f.FilePath = f.FilePath.Replace(DME.ConfigEditor.ExePath, ".\\");
                //}


                ConnectionDriversConfig c = GetConnectionDrivers(ext);
                if (c == null)
                {
                   DME.AddLogMessage("Beep", $"Error Could not Find Drivers for {filename}", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }
                if (c != null)
                {
                    f.DriverName = c.PackageName;
                    f.DriverVersion = c.version;
                    f.Category = c.DatasourceCategory;
                    switch (f.Ext.ToLower())
                    {
                        case "txt":
                            f.DatabaseType = DataSourceType.Text;
                            break;
                        case "csv":
                            f.DatabaseType = DataSourceType.CSV;
                            break;
                        case "xml":
                            f.DatabaseType = DataSourceType.XML;
                            break;
                        case "json":
                            f.DatabaseType = DataSourceType.Json;
                            break;
                        case "xls":
                            f.DatabaseType = DataSourceType.Xls;
                            break;
                        case "xlsx":
                            f.DatabaseType = DataSourceType.Xls;
                            break;
                        case "tsv":
                            f.DatabaseType = DataSourceType.TSV;
                            break;
                        case "parquet":
                            f.DatabaseType = DataSourceType.Parquet;
                            break;
                        case "avro":
                            f.DatabaseType = DataSourceType.Avro;
                            break;
                        case "orc":
                            f.DatabaseType = DataSourceType.ORC;
                            break;
                        case "onnx":
                            f.DatabaseType = DataSourceType.Onnx;
                            break;
                        case "ini":
                        case "cfg":
                            f.DatabaseType = DataSourceType.INI;
                            break;
                        case "log":
                            f.DatabaseType = DataSourceType.Log;
                            break;
                        case "pdf":
                            f.DatabaseType = DataSourceType.PDF;
                            break;
                        case "doc":
                        case "docx":
                            f.DatabaseType = DataSourceType.Doc;
                            break;
                        case "ppt":
                        case "pptx":
                            f.DatabaseType = DataSourceType.PPT;
                            break;
                        case "yaml":
                        case "yml":
                            f.DatabaseType = DataSourceType.YAML;
                            break;
                        case "md":
                        case "markdown":
                            f.DatabaseType = DataSourceType.Markdown;
                            break;
                        case "feather":
                            f.DatabaseType = DataSourceType.Feather;
                            break;
                        case "tfrecord":
                            f.DatabaseType = DataSourceType.TFRecord;
                            break;
                        case "recordio":
                            f.DatabaseType = DataSourceType.RecordIO;
                            break;
                        case "libsvm":
                            f.DatabaseType = DataSourceType.LibSVM;
                            break;
                        case "graphml":
                            f.DatabaseType = DataSourceType.GraphML;
                            break;
                        case "dicom":
                            f.DatabaseType = DataSourceType.DICOM;
                            break;
                        case "las":
                            f.DatabaseType = DataSourceType.LAS;
                            break;
                        default:
                            f.DatabaseType = DataSourceType.NONE;
                            break;
                    }

                    f.Category = DatasourceCategory.FILE;
                    return f;

                }
                else
                {
                   DME.AddLogMessage("Beep", $"Could not Load File {f.ConnectionName}", DateTime.Now, -1, null, Errors.Failed);
                }
                return f;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
               DME.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        public ConnectionDriversConfig GetConnectionDrivers(string ext)
        {
            List<ConnectionDriversConfig> configs = new List<ConnectionDriversConfig>();
            ConnectionDriversConfig driversConfig = null;
            configs =DME.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null && p.extensionstoHandle.Contains(ext)).ToList();
            if (configs.Count > 0)
            {
                //-----------  Get Favourite

                driversConfig = configs.FirstOrDefault(p => p.Favourite);
                //----------- Get Latest version if Favourite not available
                if (driversConfig == null)
                {
                    driversConfig = configs.OrderByDescending(p => p.version).FirstOrDefault();
                }

            }
            else
            {
               DME.AddLogMessage("Beep", $"Error Could not Find Drivers for extension {ext}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
            return driversConfig;

        }
        public IDataSource CreateDataSource(string filepath)
        {
            IDataSource ds = null;
            if (!File.Exists(filepath))
            {
               DME.AddLogMessage("Beep", $"Error Could not Find File {filepath}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
            string filename = Path.GetFileNameWithoutExtension(filepath);
            string ext = Path.GetExtension(filepath);
            // Find Drivers
            ConnectionDriversConfig driversConfig = GetConnectionDrivers(ext);
            if (driversConfig == null)
            {
               DME.AddLogMessage("Beep", $"Error Could not Find Drivers for {filename}", DateTime.Now, 0, null,     Errors.Failed);
                return null;
            }
            // Found Drivers
            ConnectionProperties cn = CreateFileDataConnection(filepath);
            if (cn != null)
            {
               DME.ConfigEditor.AddDataConnection(cn);
               DME.ConfigEditor.SaveDataconnectionsValues();
                ds =DME.GetDataSource(filename);
            }
            return ds;

        }
        public Tuple<IErrorsInfo, RootFolder> CreateProject(string folderpath, ProjectFolderType folderType = ProjectFolderType.Files)
        {
            RootFolder projectFolder = new RootFolder();
            projectFolder.FolderType = folderType;
            projectFolder.Folders = new List<Folder>();
            try
            {
                if (!string.IsNullOrEmpty(folderpath))
                {
                    if (Directory.Exists(folderpath))
                    {
                        string dirname = new DirectoryInfo(folderpath).Name;
                        projectFolder.Url = folderpath;
                        projectFolder.Name = dirname;
                        Folder folder = new Folder(folderpath);
                        folder = CreateFolderStructure(folderpath);
                        projectFolder.Folders.Add(folder);
                       DME.AddLogMessage("Success", "Added Project Folder ", DateTime.Now, 0, null, Errors.Ok);
                    }
                    else
                    {
                        projectFolder = null;
                       DME.AddLogMessage("Failed", "Project Folder not found ", DateTime.Now, 0, null, Errors.Failed);

                    }
                }
                else
                {
                    projectFolder = null;
                   DME.AddLogMessage("Failed", "Project Folder string is empty ", DateTime.Now, 0, null, Errors.Failed);
                }

            }
            catch (Exception ex)
            {
                string mes = "Could not Show File";
               DME.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return new(DME.ErrorObject, projectFolder);
        }
        public Folder CreateFolderStructure(Folder folder, string path)
        {

            IEnumerable<string> files = Directory.EnumerateFiles(path);
            foreach (string file in files)
            {

                ConnectionProperties conn = CreateFileDataConnection(file);
                if (conn != null)
                {
                    FFile files1 = new FFile(file);
                    files1.GuidID = conn.GuidID;
                    folder.Files.Add(files1);

                   DME.ConfigEditor.AddDataConnection(conn);
                }

            }
            IEnumerable<string> dr = Directory.EnumerateDirectories(path);
            if (dr.Any())
            {
                foreach (string drpath in dr)
                {
                    Folder folder1 = new Folder(drpath);
                    folder1.Name = new DirectoryInfo(drpath).Name;
                    CreateFolderStructure(folder1, drpath);
                }
            }
            return folder;

        }
        public Folder CreateFolderStructure(string path)
        {
            Folder folder = new Folder(path);
            folder.Folders = new List<Folder>();
            folder.Name = new DirectoryInfo(path).Name;
            folder.Url = path;
            IEnumerable<string> files = Directory.EnumerateFiles(path);
            foreach (string file in files)
            {

                ConnectionProperties conn = CreateFileDataConnection(file);
                if (conn != null)
                {
                    FFile files1 = new FFile(file);
                    files1.GuidID = conn.GuidID;
                    folder.Files.Add(files1);

                   DME.ConfigEditor.AddDataConnection(conn);
                }

            }
            IEnumerable<string> dr = Directory.EnumerateDirectories(path);
            if (dr.Any())
            {
                foreach (string drpath in dr)
                {
                    folder.Folders.Add(CreateFolderStructure(drpath));
                }
            }
            return folder;

        }
        public RootFolder CreateFolderStructure(string path, ProjectFolderType folderType = ProjectFolderType.Files)
        {
            RootFolder rootFolder = new RootFolder(path);
            rootFolder.Folders = new List<Folder>();
            rootFolder.FolderType = folderType;
            rootFolder.Name = new DirectoryInfo(path).Name;
            Folder folder = new Folder(path);

            IEnumerable<string> files = Directory.EnumerateFiles(path);
            foreach (string file in files)
            {


                ConnectionProperties conn = CreateFileDataConnection(file);
                if (conn != null)
                {
                    FFile files1 = new FFile(file);
                    files1.GuidID = conn.GuidID;
                    folder.Files.Add(files1);

                   DME.ConfigEditor.AddDataConnection(conn);
                }

            }
            IEnumerable<string> dr = Directory.EnumerateDirectories(path);
            if (dr.Any())
            {
                foreach (string drpath in dr)
                {
                    folder.Folders.Add(CreateFolderStructure(drpath));
                }
            }
            rootFolder.Folders.Add(folder);
            return rootFolder;

        }
        public static System.Data.DataView ApplyAppFilters(DataTable records, List<AppFilter> filters)
        {
            System.Data.DataView filteredRecords = new System.Data.DataView(records);

            // Apply each filter from the list
            foreach (AppFilter filter in filters)
            {
                string filterExpression = GenerateFilterExpression(filter);
                filteredRecords.RowFilter += (filteredRecords.RowFilter.Length > 0 ? " AND " : "") + filterExpression;
            }

            return filteredRecords;
        }
        public static IEnumerable<DataRow> ApplyAppFiltersNoDataView(DataTable records, List<AppFilter> filters)
        {
            IEnumerable<DataRow> filteredRows = records.AsEnumerable();

            // Apply each filter from the list
            foreach (AppFilter filter in filters)
            {
                filteredRows = filteredRows.Where(row => GenerateFilterExpression(row, filter));
            }

            return filteredRows;
        }
        public static string GenerateFilterExpression(AppFilter filter)
        {
            switch (filter.Operator)
            {
                case "equals":
                case "=":
                    return $"{filter.FieldName} = '{filter.FilterValue}'";
                case "contains":
                    return $"{filter.FieldName} LIKE '%{filter.FilterValue}%'";
                case ">":
                    return $"{filter.FieldName} > '{filter.FilterValue}'";
                case "<":
                    return $"{filter.FieldName} < '{filter.FilterValue}'";
                case ">=":
                    return $"{filter.FieldName} >= '{filter.FilterValue}'";
                case "<=":
                    return $"{filter.FieldName} <= '{filter.FilterValue}'";
                case "<>":
                case "!=":
                    return $"{filter.FieldName} <> '{filter.FilterValue}'";
                case "between":
                    return $"{filter.FieldName} >= '{filter.FilterValue}' AND {filter.FieldName} <= '{filter.FilterValue1}'";
                default:
                    throw new ArgumentException($"Invalid filter operator: {filter.Operator}");
            }
        }
        public static bool GenerateFilterExpression(DataRow record, AppFilter filter)
        {
            var fieldValue = record[filter.FieldName];

            switch (filter.Operator)
            {
                case "equals":
                case "=":
                    return fieldValue.Equals(filter.FilterValue);
                case "contains":
                    return fieldValue.ToString().Contains(filter.FilterValue);
                case ">":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) > 0;
                case "<":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) < 0;
                case ">=":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) >= 0;
                case "<=":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) <= 0;
                case "<>":
                case "!=":
                    return !fieldValue.Equals(filter.FilterValue);
                case "between":
                    var value1 = filter.FilterValue;
                    var value2 = filter.FilterValue1;

                    return Comparer.Default.Compare(fieldValue, value1) >= 0 &&
                           Comparer.Default.Compare(fieldValue, value2) <= 0;
                default:
                    throw new ArgumentException($"Invalid filter operator: {filter.Operator}");
            }
        }
        #endregion
    }
}


