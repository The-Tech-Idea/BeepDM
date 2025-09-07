DataImportManager
==================

.. class:: DataImportManager

   Manages data import operations from various sources into the BeepDM framework, handling validation and transformation during import.

   Responsibilities
   ----------------
   - Orchestrates data import workflows from multiple source types
   - Validates imported data against schema and business rules
   - Handles data transformation and cleansing during import
   - Manages import progress tracking and error reporting
   - Provides rollback capabilities for failed imports

   Key Methods
   -----------
   - ImportData(): Executes data import from specified source
   - ValidateImport(): Validates data before final import
   - TransformData(): Applies transformation rules during import
   - GetImportProgress(): Reports current import status
   - RollbackImport(): Reverts partially completed imports

   Typical Flow
   ------------
   1. Configure import source and destination
   2. Define validation and transformation rules
   3. Execute import with progress monitoring
   4. Validate imported data integrity
   5. Commit or rollback based on validation results

   Extension Points
   ----------------
   - Custom data source adapters
   - Validation rule engines
   - Transformation pipelines
   - Progress reporting mechanisms
   - Error handling strategies
