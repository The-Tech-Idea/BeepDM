


using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

using System.Reflection;
using System.Xml.Serialization;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using static TheTechIdea.DataManagment_Engine.Util;

namespace TheTechIdea.DataManagment_Engine
{
    public class Util : IUtil
    {
        public delegate T ObjectActivator<T>(params object[] args);
       public IDMLogger Logger { get; set; }
       public List<string> Namespacelist { get; set; } = new List<string>();
        public List<string> Classlist { get; set; } = new List<string>();
        public List<ParentChildObject> FunctionHierarchy { get; set; }
       
        public IErrorsInfo ErrorObject { get; set; }
        public IDMEEditor DME { get; set; }
        public object GetInstance(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
            return null;
        }
        public Type GetType(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return type;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return type;
            }
            return null;
        }
        public dynamic GetTypeFromString(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return type;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return type;
            }
            return null;
        }
        public ConnectionDriversConfig LinkConnection2Drivers(IConnectionProperties cn)
        {
            
            string vr = cn.DriverVersion;
            string pk = cn.DriverName;
            return DME.ConfigEditor.DataDrivers.Where(c => c.PackageName == pk && c.version == vr).FirstOrDefault();


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
                        pro.SetValue(obj, dr[column.ColumnName], null);
                    else
                        continue;
                }
            }
            return obj;
        }
        public  bool AddinInterfaceFilter(Type typeObj, Object criteriaObj)
        {
            if (typeObj.ToString() == criteriaObj.ToString())
                return true;
            else
                return false;
        }
        public Util(IDMLogger logger, IErrorsInfo per)
        {
            Logger = logger;
            ErrorObject = per;

        }
        public  DataTable CreateDataTable(string strFilePath)
        {
            DataTable dt = new DataTable();
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string[] headers = sr.ReadLine().Split(',');
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


            return dt;
        }
        public DataTable CreateDataTableVer1(Object[] array)
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
        public  DataTable CreateDataTableVer2(Object[] arr)
        {
            XmlSerializer serializer = new XmlSerializer(arr.GetType());
            System.IO.StringWriter sw = new System.IO.StringWriter();
            serializer.Serialize(sw, arr);
            System.Data.DataSet ds = new System.Data.DataSet();
            System.Data.DataTable dt = new System.Data.DataTable();
            System.IO.StringReader reader = new System.IO.StringReader(sw.ToString());

            ds.ReadXml(reader);
            return ds.Tables[0];
        }
        private void FillData(PropertyInfo[] properties, DataTable dt, Object o)
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
        public  string GetRelativePath(string fromPath, string toPath)
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

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
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
        public  ExpandoObject convertToExpando(object obj)
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
        public  void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
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
        public List<object> GetListByDataTable(DataTable dt, Type type, EntityStructure enttype)
        {

      
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
            foreach (DataRow row in dt.Rows)
            {
                // dynamic x = TypeHelpers.GetInstance(type);
                dynamic x = Activator.CreateInstance(type);
              //  var v = (dynamic)null;
                foreach (DataColumn item in dt.Columns)
                {
                    properties[item.ColumnName].SetValue(x, row[item.ColumnName], null);
                }
                //for (int i = 0; i <= enttype.Fields.Count - 1; i++)
                //{
                //    try
                //    {
                //        f = enttype.Fields[i].fieldname + "-" + enttype.Fields[i].fieldtype + "-" + row[enttype.Fields[i].fieldname];
                //        try
                //        {
                //            v = Convert.ChangeType(row[enttype.Fields[i].fieldname], Type.GetType(enttype.Fields[i].fieldtype));
                //            if ((row[enttype.Fields[i].fieldname] == null || row[enttype.Fields[i].fieldname] == "")) //&& (v.GetType().ToString()!="System.String")
                //            {
                //                v = null;
                //            }

                //        }
                //        catch (Exception)
                //        {

                //            v = null;
                //        }
                        
                       
                //        //  type.GetProperty(enttype.Fields[i].fieldname).SetValue(x, v, null);
                //        // Logger.WriteLog($"Creating Field and Value." + f);
                //    }
                //    catch (Exception ex)
                //    {

                //        Logger.WriteLog($"Error in Creating Record or Setting Value." + f + ":" + ex.Message);
                //    }



                //}

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

                    Console.WriteLine("        " + field.ColumnName + ": " + field.DataType);

                    EntityField f = new EntityField();


                    //  f.tablename = sheetname;
                    f.fieldname = field.ColumnName;
                    f.fieldtype = field.DataType.ToString();
                    f.ValueRetrievedFromParent = false;
                    f.EntityName = sheetname;
                    f.FieldIndex = y;
                    Fields.Add(f);
                    y += 1;

                }

                i += 1;
                entityData.Fields = new List<EntityField>();
                entityData.Fields.AddRange(Fields);

            }
            catch (Exception ex)
            {
                DME.AddLogMessage("Error", "Could not Create Entity Structure" + ex.Message, DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            return entityData;


        }
        public bool Download(string url, string downloadFileName, string downloadFilePath)
        {
            string downloadfile = downloadFilePath + downloadFileName;
            string httpPathWebResource = null;
            Boolean ifFileDownoadedchk = false;
            ifFileDownoadedchk = false;
            WebClient myWebClient = new WebClient();
            httpPathWebResource = url + downloadFileName;
            myWebClient.DownloadFile(httpPathWebResource, downloadfile);
            ifFileDownoadedchk = true;
            return ifFileDownoadedchk;
        }
        public  string GetInsertString(string EntityName, DataRow row, IMapping_rep Mapping, EntityStructure DataStruct)
        {

            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
            //   map= Mapping.FldMapping;
            string Insertstr = "insert into " + EntityName + " (";
            string Valuestr = ") values (";
            var insertfieldname = "";
            string datafieldname = "";
            string typefield = "";
            foreach (IMapping_rep_fields item in Mapping.FldMapping)
            {
                if (EntityName == Mapping.EntityName1)
                {
                    insertfieldname = item.FieldName1;
                    datafieldname = item.FieldName2;
                    typefield = item.FieldType2;
                }
                else
                {
                    insertfieldname = item.FieldName2;
                    datafieldname = item.FieldName1;
                    typefield = item.FieldType1;
                }
                Insertstr += insertfieldname + ",";
                switch (typefield)
                {
                    case "System.String":
                        Valuestr = "'" + row[datafieldname] + "',";
                        break;
                    case "System.Int":
                        Valuestr = "" + row[datafieldname] + ",";
                        break;
                    default:
                        Valuestr = "'" + row[datafieldname] + "',";
                        break;
                }


            }



            Insertstr.Remove(Insertstr.Length - 1);
            Valuestr.Remove(Insertstr.Length - 1);
            Valuestr += ")";
            return Insertstr + Valuestr;
        }
        /// <summary>
        /// Calculate percentage similarity of two strings
        /// <param name="source">Source String to Compare with</param>
        /// <param name="target">Targeted String to Compare</param>
        /// <returns>Return Similarity between two strings from 0 to 1.0</returns>
        /// </summary>
        public  double CalculateSimilarity( string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = ComputeLevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }
        /// <summary>
        /// Returns the number of steps required to transform the source string
        /// into the target string.
        /// </summary>
        private  int ComputeLevenshteinDistance(string source, string target)
        {
            if ((source == null) || (target == null)) return 0;
            if ((source.Length == 0) || (target.Length == 0)) return 0;
            if (source == target) return source.Length;

            int sourceWordCount = source.Length;
            int targetWordCount = target.Length;

            // Step 1
            if (sourceWordCount == 0)
                return targetWordCount;

            if (targetWordCount == 0)
                return sourceWordCount;

            int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];

            // Step 2
            for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;

            for (int i = 1; i <= sourceWordCount; i++)
            {
                for (int j = 1; j <= targetWordCount; j++)
                {
                    // Step 3
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    // Step 4
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceWordCount, targetWordCount];
        }
       public bool KMPSearch(string pat, string txt)
        {
            int M = pat.Length;
            int N = txt.Length;

            // create lps[] that will hold the longest 
            // prefix suffix values for pattern 
            int[] lps = new int[M];
            int j = 0; // index for pat[] 

            // Preprocess the pattern (calculate lps[] 
            // array) 
            computeLPSArray(ref pat, M, ref lps);

            int i = 0; // index for txt[] 
            bool found=false;
            while (i < N && found == false)
            {
                if (pat[j] == txt[i])
                {
                    j++;
                    i++;
                }
                if (j == M)
                {

                    found = true;
                    j = lps[j - 1];
                }

                // mismatch after j matches 
                else if (i < N && pat[j] != txt[i])
                {
                    // Do not match lps[0..lps[j-1]] characters, 
                    // they will match anyway 
                    if (j != 0)
                        j = lps[j - 1];
                    else
                        i = i + 1;
                }
            }
            return found;
        }

        void computeLPSArray(ref string pat, int M, ref int[] lps)
        {
            // length of the previous longest prefix suffix 
            int len = 0;
            int i = 1;
            lps[0] = 0; // lps[0] is always 0 

            // the loop calculates lps[i] for i = 1 to M-1 
            while (i < M)
            {
                if (pat[i] == pat[len])
                {
                    len++;
                    lps[i] = len;
                    i++;
                }
                else // (pat[i] != pat[len]) 
                {
                    // This is tricky. Consider the example. 
                    // AAACAAAA and i = 7. The idea is similar 
                    // to search step. 
                    if (len != 0)
                    {
                        len = lps[len - 1];

                        // Also, note that we do not increment 
                        // i here 
                    }
                    else // if (len == 0) 
                    {
                        lps[i] = len;
                        i++;
                    }
                }
            }
        }
     
    }

}
