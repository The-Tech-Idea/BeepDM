
using System.Collections.Generic;
using System.Data;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.FileManager
{
    public interface ITxtXlsCSVReader
    {
        List<ColumnDef> ColumnValuesDef { get; set; }
        IConnectionProperties ConnProp { get; set; }
        char Delimiter { get; set; }
       
        IDMEEditor DMEEditor { get; set; }
        List<EntityStructure> Entities { get; set; }
        IErrorsInfo ErrorObject { get; set; }
       
        DataSet FileData { get; set; }
        string FileName { get; set; }
        string FilePath { get; set; }
        IDMLogger Logger { get; set; }
        ConnectionState State { get; set; }

        IEnumerable<DataRow> getData(string sheet, bool firstRowIsColumnNames = false);
        List<EntityStructure> GetEntityStructures(bool refresh = false);
      
        ConnectionState GetFileState();
        IEnumerable<DataRow> GetFirstSheetData(bool firstRowIsColumnNames = false);
        DataTable GetFirstSheetData(bool firstRowIsColumnNames = false, int sheetno = 0);
        int GetSheetNumber(DataSet ls, string sheetname);
        IEnumerable<string> getWorksheetNames();
        ConnectionState OpenConnection();
        DataTable ReadDataTable(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100);
        DataTable ReadDataTable(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100);
        //List<object> ReadList(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100);
        //List<object> ReadList(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100);
    }
}