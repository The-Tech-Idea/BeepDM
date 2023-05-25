namespace TheTechIdea.Beep.DataBase
{
    public interface IDatatypeMapping
    {
         int ID { get; set; }
         string GuidID { get; set; }
        string DataSourceName { get; set; }
        string NetDataType { get; set; }
        string DataType { get; set; }
       
    }

}