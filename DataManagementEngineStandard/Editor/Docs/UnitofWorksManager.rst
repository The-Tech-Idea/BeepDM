UnitofWorksManager
==================

.. class:: UnitofWorksManager

   Manages the lifecycle and coordination of multiple Unit of Work instances within the data management framework.

   Responsibilities
   ----------------
   - Creates and manages multiple UnitofWork instances
   - Coordinates nested and parallel unit of work operations
   - Manages unit of work scope and lifetime
   - Provides centralized commit/rollback coordination
   - Handles unit of work dependencies and ordering

   Key Methods
   -----------
   - CreateUnitOfWork(): Creates new UnitofWork instance
   - BeginTransaction(): Starts new transactional scope
   - CommitAll(): Commits all active units of work
   - RollbackAll(): Rolls back all active units of work
   - GetActiveUnitsOfWork(): Lists currently active units

   Typical Flow
   ------------
   1. Begin transaction scope with manager
   2. Create multiple unit of work instances as needed
   3. Execute operations within each unit of work
   4. Coordinate final commit or rollback across all units
   5. Clean up completed units of work

   Extension Points
   ----------------
   - Custom unit of work lifecycle management
   - Dependency resolution between units
   - Nested transaction support
   - Performance monitoring and metrics
