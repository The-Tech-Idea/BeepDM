UnitofWork
==========

.. class:: UnitofWork

   Implements the Unit of Work pattern to maintain a list of objects affected by business transactions and coordinates writing out changes.

   Responsibilities
   ----------------
   - Tracks changes to entities during a business transaction
   - Maintains object state and change tracking
   - Coordinates commit/rollback operations
   - Ensures data consistency across operations

   Key Methods
   -----------
   - Commit(): Persists all tracked changes to data source
   - Rollback(): Reverts all changes made during the unit of work
   - RegisterNew(): Marks entity for insertion
   - RegisterDirty(): Marks entity for update
   - RegisterRemoved(): Marks entity for deletion

   Typical Flow
   ------------
   1. Create unit of work instance
   2. Register entities for operations (new/dirty/removed)
   3. Perform business logic
   4. Call Commit() to persist or Rollback() to cancel

   Extension Points
   ----------------
   - Custom change tracking strategies
   - Validation hooks before commit
   - Custom conflict resolution
