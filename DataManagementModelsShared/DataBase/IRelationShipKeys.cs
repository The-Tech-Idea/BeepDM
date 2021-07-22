namespace TheTechIdea.Beep.DataBase
{
    public interface IRelationShipKeys
    {
        string EntityColumnID { get; set; }
        int EntityColumnSequenceID { get; set; }
        int ParentColumnSequenceID { get; set; }
        string ParentEntityColumnID { get; set; }
        string ParentEntityID { get; set; }
        string RalationName { get; set; }
    }
    public interface IChildRelation
    {
        string child_table { get; set; }
        string child_column { get; set; }
        string parent_table { get; set; }
        string parent_column { get; set; }
        string Constraint_Name { get; set; }
        string RalationName { get; set; }
    }
   
}