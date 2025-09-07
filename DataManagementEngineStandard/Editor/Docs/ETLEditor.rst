ETLEditor
=========

.. class:: ETLEditor

   Manages ETL (Extract, Transform, Load) operations and pipeline configuration within the BeepDM framework.

   Responsibilities
   ----------------
   - Configures and executes ETL pipelines
   - Manages data transformation rules and mappings
   - Coordinates data movement between sources
   - Handles ETL script generation and execution
   - Monitors ETL operation progress and errors

   Key Methods
   -----------
   - RunETL(): Executes configured ETL operations
   - CreateScript(): Generates ETL scripts from configuration
   - ValidateMapping(): Validates field mappings and transformations
   - ProcessBatch(): Handles batch processing operations
   - MonitorProgress(): Tracks ETL operation status

   Typical Flow
   ------------
   1. Configure source and destination data sources
   2. Define field mappings and transformations
   3. Validate configuration and mappings
   4. Execute ETL pipeline
   5. Monitor progress and handle errors

   Extension Points
   ----------------
   - Custom transformation functions
   - Pipeline step interceptors
   - Custom data validation rules
   - Progress reporting hooks
