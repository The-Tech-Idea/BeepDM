UnitOfWork
==========

.. class:: UnitofWork<T>

   Represents a unit of work for managing entities of type T.

   **Type Parameters:**
   
   - **T**: The type of entity.

   Methods
   -------

   .. method:: Commit()

      Commits changes made in the unit of work.

      :returns: An object containing information about any errors that occurred during the commit.
      :rtype: Task<IErrorsInfo>

   .. method:: UndoLastChange()

      Undoes the last change made in the unit of work.

   Properties
   ----------

   .. attribute:: IsDirty

      :type: bool
      :value: True if the object is dirty; otherwise, false.

   .. attribute:: DMEEditor

      :type: IDMEEditor
      :value: The instance of the editor managing this unit of work.

   .. attribute:: EntityStructure

      :type: EntityStructure
      :value: The structure of the entity.
