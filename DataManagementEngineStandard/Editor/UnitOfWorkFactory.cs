using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.DataBase;
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
        public static object GetUnitOfWork(string entityName, string dataSourceName, IDMEEditor DMEEditor)
        {
            // Get the type of the class from the string name
            Type tp = Type.GetType(entityName);

            // Check if the type is null or doesn't meet the necessary criteria
            if (tp == null || !typeof(Entity).IsAssignableFrom(tp) || tp.GetInterface(nameof(INotifyPropertyChanged)) == null)
            {
                throw new ArgumentException("Invalid type name: " + entityName);
            }


            // Get the UnitOfWorkFactory.CreateUnitOfWork method
            MethodInfo method = typeof(UnitOfWorkFactory).GetMethod("CreateUnitOfWork");

            // Check if the method was found
            if (method == null)
            {
                throw new InvalidOperationException("Couldn't find CreateUnitOfWork method.");
            }

            // Make the method generic and invoke it
            MethodInfo generic = method.MakeGenericMethod(tp);
            var unitOfWork = generic.Invoke(null, new object[] { tp, DMEEditor, dataSourceName });

            // Check if unitOfWork is null or doesn't meet the necessary criteria
            if (unitOfWork == null || !typeof(UnitofWork<>).MakeGenericType(tp).IsInstanceOfType(unitOfWork))
            {
                throw new InvalidOperationException("Couldn't create UnitOfWork.");
            }

            // Perform some initialization on unitOfWork here if needed.

            return unitOfWork;
        }

      
    }
}
