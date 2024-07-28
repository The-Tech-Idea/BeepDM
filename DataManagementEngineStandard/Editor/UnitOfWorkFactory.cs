using DataManagementModels.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>A factory class for creating and retrieving instances of UnitOfWork.</summary>

    public static class UnitOfWorkFactory
    {
        /// <summary>Creates a new instance of UnitOfWork for the specified entity type.</summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="dmeEditor">The IDMEEditor instance.</param>
        /// <param name="datasourceName">The name of the data source.</param>
        /// <returns>A new instance of UnitOfWork.</returns>
        public static object CreateUnitOfWork(Type entityType, IDMEEditor dMEEditor, string datasourceName, string entityName, string primarykey)
        {
            // Create the specific UnitOfWork type using the entityType
            Type uowGenericType = typeof(UnitofWork<>).MakeGenericType(entityType);

            // Prepare the arguments for the constructor
            object[] constructorArgs = new object[] { dMEEditor, datasourceName, entityName, primarykey };

            // Create an instance of UnitOfWork<T> with the specific constructor
            // Dynamically handle the instance since we can't cast to a specific IUnitofWork<T> at compile time
            object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);
            return uowInstance;
        }

        public static object CreateUnitOfWork(Type entityType, IDMEEditor dMEEditor, string datasourceName, string entityName, EntityStructure entityStructure, string primarykey)
        {
            // Create the specific UnitOfWork type using the entityType
            Type uowGenericType = typeof(UnitofWork<>).MakeGenericType(entityType);

            // Prepare the arguments for the constructor
            object[] constructorArgs = new object[] { dMEEditor, datasourceName, entityName, entityStructure, primarykey };

            // Create an instance of UnitOfWork<T> with the specific constructor
            object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);
            return uowInstance;
        }
        public static object CreateUnitOfWork(Type entityType, IDMEEditor dMEEditor, string datasourceName, string entityName)
        {
            // Create the specific UnitOfWork type using the entityType
            Type uowGenericType = typeof(UnitofWork<>).MakeGenericType(entityType);

            // Prepare the arguments for the constructor
            object[] constructorArgs = new object[] { dMEEditor, datasourceName, entityName };

            // Create an instance of UnitOfWork<T> with the specific constructor
            // Dynamically handle the instance since we can't cast to a specific IUnitofWork<T> at compile time
            object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);
            return uowInstance;
        }

        public static object CreateUnitOfWork(Type entityType, IDMEEditor dMEEditor, string datasourceName, string entityName, EntityStructure entityStructure)
        {
            // Create the specific UnitOfWork type using the entityType
            Type uowGenericType = typeof(UnitofWork<>).MakeGenericType(entityType);

            // Prepare the arguments for the constructor
            object[] constructorArgs = new object[] { dMEEditor, datasourceName, entityName, entityStructure };

            // Create an instance of UnitOfWork<T> with the specific constructor
            object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);
            return uowInstance;
        }

        /// <summary>Gets a unit of work for a specified entity.</summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <param name="DMEEditor">The DMEEditor instance.</param>
        /// <returns>A unit of work object for the specified entity.</returns>
        /// <exception cref="ArgumentException">Thrown when the entity name is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the CreateUnitOfWork method is not found or when the unit of work cannot be created.</exception>
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
