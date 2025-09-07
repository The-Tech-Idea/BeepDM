UnitOfWorkFactory
=================

.. class:: UnitOfWorkFactory

   Factory class responsible for creating and configuring Unit of Work instances.

   Responsibilities
   ----------------
   - Creates UnitofWork instances with proper configuration
   - Manages UnitofWork lifecycle and dependencies
   - Provides different UnitofWork types based on context
   - Handles dependency injection for UnitofWork components

   Key Methods
   -----------
   - Create(): Creates a standard UnitofWork instance
   - CreateForDataSource(): Creates UnitofWork for specific data source
   - CreateMultiSource(): Creates MultiDataSourceUnitOfWork
   - ConfigureUnitOfWork(): Applies configuration to UnitofWork

   Typical Flow
   ------------
   1. Request UnitofWork from factory
   2. Factory determines appropriate type and configuration
   3. Factory creates and configures instance
   4. Returns ready-to-use UnitofWork

   Extension Points
   ----------------
   - Custom UnitofWork implementations
   - Configuration strategies
   - Dependency resolution patterns
