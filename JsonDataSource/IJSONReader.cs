using System.Collections.Generic;
using System.Data;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public interface IJSONReader
    {
        ConnectionProperties ConnProp { get; set; }
        IDMEEditor DMEEditor { get; set; }
        List<EntityStructure> Entities { get; set; }
        ConnectionState State { get; set; }

        void CreateClass(int sheetno = 0);
        void CreateClass(string sheetname);
        IEnumerable<string> GetEntities();
        List<EntityStructure> GetEntityStructures(bool refresh = false);
        ConnectionState GetFileState();
        int GetSheetNumber(DataSet ls, string sheetname);
        ConnectionState OpenConnection();
        DataTable ReadDataTable(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100);
        DataTable ReadDataTable(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100);
        List<object> ReadList(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100);
        List<object> ReadList(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100);
    }
}