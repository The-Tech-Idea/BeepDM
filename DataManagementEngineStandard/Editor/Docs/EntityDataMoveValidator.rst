EntityDataMoveValidator
=======================

.. class:: EntityDataMoveValidator

   Validates data movement operations between entities and data sources to ensure data integrity and business rule compliance.

   Responsibilities
   ----------------
   - Validates data before movement between sources
   - Ensures data integrity constraints are maintained
   - Checks business rules and validation rules
   - Validates schema compatibility between source and destination
   - Provides detailed validation error reporting

   Key Methods
   -----------
   - ValidateMove(): Validates complete data movement operation
   - ValidateEntity(): Validates individual entity for movement
   - CheckConstraints(): Verifies data integrity constraints
   - ValidateSchema(): Ensures schema compatibility
   - GetValidationErrors(): Retrieves detailed validation results

   Typical Flow
   ------------
   1. Receive data movement request with source and destination
   2. Validate schema compatibility between endpoints
   3. Check each entity against business rules
   4. Verify data integrity constraints
   5. Report validation results and errors

   Extension Points
   ----------------
   - Custom validation rule engines
   - Pluggable constraint checking
   - Business rule integration
   - Schema transformation validation
   - Custom error reporting formats

   Validation Categories
   --------------------
   - Schema validation (field types, constraints)
   - Business rule validation (domain-specific rules)
   - Referential integrity validation
   - Data quality validation (nulls, formats, ranges)
