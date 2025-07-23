ETL Class
=========

.. class:: ETL

    Represents an Extract, Transform, Load (ETL) process.

    Methods
    -------

    .. method:: __init__(self, DMEEditor)

        Initializes a new instance of the ETL class.
        
        :param DMEEditor: The DME editor to use for the ETL process.

    .. method:: CreateScriptHeader(self, Srcds, progress, token)

        Creates the header of an ETL script.

        :param Srcds: The data source object.
        :param progress: The progress object to report progress.
        :param token: The cancellation token to cancel the operation.
        :raises ArgumentNullException: Thrown when Srcds is null.

    .. method:: GetCreateEntityScript(self, ds, entities, progress, token)

        Generates a list of ETL script details for creating entities from a data source.

        :param ds: The data source to retrieve entities from.
        :param entities: The list of entities to create scripts for.
        :param progress: Object to report progress during the script generation.
        :param token: A cancellation token.
        :return: List of ETL script details for creating entities.

    .. method:: CopyEntitiesStructure(self, sourceds, destds, entities, progress, token, CreateMissingEntity)

        Copies the structure of specified entities from a source data source to a destination data source.

        :param sourceds: The source data source.
        :param destds: The destination data source.
        :param entities: A list of entity names to copy.
        :param progress: An object to report progress.
        :param token: A cancellation token.
        :param CreateMissingEntity: Flag to create missing entities.
