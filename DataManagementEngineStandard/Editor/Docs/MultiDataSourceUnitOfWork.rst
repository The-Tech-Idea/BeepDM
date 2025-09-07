MultiDataSourceUnitOfWork
=========================

.. class:: MultiDataSourceUnitOfWork

   Extends the Unit of Work pattern to coordinate transactions across multiple data sources within a single logical operation.

   Responsibilities
   ----------------
   - Coordinates operations across multiple IDataSource instances
   - Manages distributed transaction-like behavior
   - Handles cross-source data consistency
   - Provides rollback capabilities across all participating sources
   - Tracks changes across heterogeneous data sources

   Key Methods
   -----------
   - AddDataSource(): Registers data source for participation
   - Commit(): Commits changes across all data sources
   - Rollback(): Rolls back changes across all data sources
   - RegisterOperation(): Records operations for each data source
   - GetParticipatingDataSources(): Lists all registered data sources

   Typical Flow
   ------------
   1. Create multi-source unit of work
   2. Register all participating data sources
   3. Execute operations across sources
   4. Attempt commit across all sources
   5. Rollback if any source fails

   Extension Points
   ----------------
   - Two-phase commit protocols
   - Custom consistency validation
   - Compensation-based transaction patterns
   - Cross-source conflict resolution

   Notes
   -----
   - Not true distributed transactions (no XA support)
   - Design operations to be idempotent for failure recovery
   - Consider eventual consistency patterns for complex scenarios
