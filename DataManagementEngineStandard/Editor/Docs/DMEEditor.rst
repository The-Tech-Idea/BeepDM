DMEEditor
=========

.. class:: DMEEditor

   Central orchestrator and primary entry point for all data management operations in the BeepDM framework.

   Responsibilities
   ----------------
   - Manages data source connections and lifecycle
   - Coordinates ETL operations and workflows  
   - Provides unified interface for CRUD operations across multiple data sources
   - Manages configuration, logging, and error handling
   - Orchestrates unit of work and transaction management

   Key Methods
   -----------
   - GetDataSource(): Retrieves registered data sources
   - CreateUnitOfWork(): Creates transaction-like work units
   - RunETL(): Executes ETL operations
   - ImportData(): Manages data import workflows
   - SyncData(): Coordinates data synchronization

   Typical Flow
   ------------
   1. Initialize editor with configuration
   2. Register data sources
   3. Create unit of work for transactional operations
   4. Execute operations (CRUD, ETL, sync)
   5. Commit or rollback changes

   Extension Points
   ----------------
   - Plugin architecture for custom data sources
   - Event hooks for operation lifecycle
   - Custom validation and transformation pipelines
