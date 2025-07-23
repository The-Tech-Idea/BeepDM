UnitOfWorkFactory
=================

.. class:: UnitOfWorkFactory

   A factory class for creating and retrieving instances of UnitOfWork.

   Methods
   -------

   .. method:: CreateUnitOfWork(Type entityType, IDMEEditor dMEEditor, string datasourceName, string entityName, string primarykey)

      Creates a new instance of UnitOfWork for the specified entity type.

      :param entityType: The type of the entity.
      :param dMEEditor: The IDMEEditor instance.
      :param datasourceName: The name of the data source.
      :param entityName: The name of the entity.
      :param primarykey: The primary key of the entity.
      :returns: A new instance of UnitOfWork.
      :rtype: object