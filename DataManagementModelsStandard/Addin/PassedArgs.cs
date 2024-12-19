using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Addin
{
    /// <summary>
    /// Represents the arguments passed to a method or event.
    /// </summary>
    public interface IPassedArgs
    {
        string Args { get; set; }
        IDM_Addin Addin { get; set; }
        string AddinName { get; set; }
        string AddinType { get; set; }
        string Category { get; set; }
        string CurrentEntity { get; set; }
        IDataSource DataSource { get; set; }
        string DatasourceName { get; set; }
        IDMDataView DMView { get; set; }
        string EventType { get; set; }
        int Id { get; set; }
        string ObjectName { get; set; }
        List<EntityStructure> Entities { get; set; }
        List<string> EntitiesNames { get; set; }
        List<ObjectItem> Objects { get; set; }
        string ObjectType { get; set; }
        int ParameterInt1 { get; set; }
        int ParameterInt2 { get; set; }
        int ParameterInt3 { get; set; }
        double Parameterdouble1 { get; set; }
        double Parameterdouble2 { get; set; }
        double Parameterdouble3 { get; set; }
        string ParameterString1 { get; set; }
        string ParameterString2 { get; set; }
        string ParameterString3 { get; set; }
        DateTime ParameterDate1 { get; set; }
        DateTime ParameterDate2 { get; set; }
        DateTime ParameterDate3 { get; set; }
        object ReturnData { get; set; }
        Type ReturnType { get; set; }
        object SentData { get; set; }
        Type SentType { get; set; }
        string Messege { get; set; }
        string ErrorCode { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        Errors Flag { get; set; }
        bool IsError { get; set; }
        bool Cancel { get; set; }
        Action TrueAction { get; set; }
        Action FalseAction { get; set; }
        Action NoneAction { get; set; }
        Action DefaultAction { get; set; }
        string ParameterName { get; set; }
         Action CurrentAction { get; set; }
         Action NextAction { get; set; }
         Action PreviousAction { get; set; }
        DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents the arguments passed to a method or event.
    /// </summary>
    public class PassedArgs : IPassedArgs

    {
        public string Args { get; set; } = string.Empty;
       
        public IDataSource DataSource { get; set; }
        public List<ObjectItem> Objects { get; set; } = new List<ObjectItem>();
        public IDM_Addin Addin { get; set; }
        public IDMDataView DMView { get; set; }
        public string CurrentEntity { get; set; }
        public string ObjectType { get; set; }
        public string AddinName { get; set; }
        public string ObjectName { get; set; }
        public string AddinType { get; set; }
        public string EventType { get; set; }
        public string DatasourceName { get; set; }
        public string Category { get; set; }
        public string ParameterString1 { get; set; }
        public string ParameterString2 { get; set; }
        public string ParameterString3 { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public int ParameterInt1 { get; set; }
        public int ParameterInt2 { get; set; }
        public int ParameterInt3 { get; set; }
        public double Parameterdouble1 { get; set; }
        public double Parameterdouble2 { get; set; }
        public double Parameterdouble3 { get; set; }
        public DateTime ParameterDate1 { get; set; }
        public DateTime ParameterDate2 { get; set; }
        public DateTime ParameterDate3 { get; set; }
        public object ReturnData { get; set; }
        public Type ReturnType { get; set; }
        public object SentData { get; set; }
        public Type SentType { get; set; }
        public string Messege { get; set; }
        public string ErrorCode { get; set; }
        public bool IsError { get; set; }
        public bool Cancel { get; set; }
        public int Id { get; set; }
        public Action TrueAction { get; set; }
        public Action FalseAction { get; set; }
        public Action CurrentAction { get; set; }
        public Action NextAction { get; set; }
        public Action PreviousAction { get; set; }
        public Action NoneAction { get; set; }
        public Action DefaultAction { get; set; }
        public string ParameterName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public Errors Flag { get; set; }
        public DateTime Timestamp { get; set; }
        public PassedArgs()
        {

        }
    }
    /// <summary>Represents an item in an object collection.</summary>
    public class ObjectItem
    {
        public Object obj { get; set; }
        public string Name { get; set; }
        public ObjectItem()
        {

        }
    }
}
