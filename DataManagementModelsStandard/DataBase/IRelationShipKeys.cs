namespace TheTechIdea.Beep.DataBase
{
    public interface IRelationShipKeys
    {
        string EntityColumnID { get; set; }
        int EntityColumnSequenceID { get; set; }
        int RelatedColumnSequenceID { get; set; }
        string RelatedEntityColumnID { get; set; }
        string RelatedEntityID { get; set; }
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