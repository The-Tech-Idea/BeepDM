DataSyncManager
===============

.. class:: DataSyncManager

   A manager class for handling data synchronization tasks.

   Methods
   -------

   .. method:: GetNewRecordsFromSourceData(DataSyncSchema schema)

      Retrieves new records from the source data based on the schema's LastSyncDate.

      :param schema: The DataSyncSchema defining the synchronization process.
      :returns: A collection of new records from the source data.
      :rtype: Task<object>

   .. method:: GetUpdatedRecordsFromSourceData(DataSyncSchema schema)

      Retrieves updated records from the source data based on the schema's LastSyncDate.

      :param schema: The DataSyncSchema defining the synchronization process.
      :returns: A collection of updated records from the source data.
      :rtype: Task<object>
