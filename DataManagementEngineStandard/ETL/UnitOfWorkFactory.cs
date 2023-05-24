using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor
{
    public static class UnitOfWorkFactory
    {
        public static object CreateUnitOfWork(Type entityType, IDMEEditor dmeEditor, string datasourceName)
        {
            var unitOfWorkType = typeof(UnitofWork<>).MakeGenericType(entityType);
            return Activator.CreateInstance(unitOfWorkType, dmeEditor, datasourceName);
        }
    }
}
