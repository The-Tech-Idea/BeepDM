using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TheTechIdea.Beep.Editor.Defaults
{
    // ──────────────────────────────────────────────────────────────────────────────
    // Naming convention for rule strings
    // ──────────────────────────────────────────────────────────────────────────────
    // A string that starts with ":" is an EXPRESSION — it will be parsed and
    // handed to the resolver pipeline.  Example: ":NOW", ":USERNAME", ":NEWGUID"
    //
    // A string without ":" is a LITERAL — it is used as-is, no resolving.
    // Example: "Active", "1", "pending"
    // ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single field default rule inside an <see cref="EntityDefaultsProfile"/>.
    /// </summary>
    public sealed class FieldDefaultRule
    {
        /// <summary>Name of the field this rule applies to (case-insensitive match).</summary>
        public string FieldName { get; }

        /// <summary>
        /// The rule string.
        /// Starts with ":" for expressions (e.g. ":NOW", ":USERNAME").
        /// No prefix for literals (e.g. "Active", "1").
        /// </summary>
        public string RuleString { get; }

        /// <summary>
        /// When <c>true</c> (default), the rule is applied only when the target field is
        /// null, empty or the default value for its type.  Set to <c>false</c> to always
        /// overwrite whatever the caller has already placed in the field.
        /// </summary>
        public bool ApplyOnlyIfNull { get; }

        public FieldDefaultRule(string fieldName, string ruleString, bool applyOnlyIfNull = true)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name must not be empty.", nameof(fieldName));

            FieldName       = fieldName;
            RuleString      = ruleString ?? string.Empty;
            ApplyOnlyIfNull = applyOnlyIfNull;
        }

        /// <summary>
        /// Returns <c>true</c> when this rule is an expression (starts with ":").
        /// </summary>
        public bool IsExpression => RuleString.TrimStart().StartsWith(":");

        /// <summary>
        /// Returns the expression body without the leading ":" for passing to the resolver.
        /// When <see cref="IsExpression"/> is false, returns <see cref="RuleString"/> unchanged
        /// (it is already a literal).
        /// </summary>
        public string ExpressionBody =>
            IsExpression ? RuleString.TrimStart().Substring(1).TrimStart() : RuleString;
    }

    // ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fluent builder that describes which default values should be applied to the
    /// fields of a named entity (<c>entityName</c>) when a new record is created.
    ///
    /// <example>
    /// <code>
    /// var profile = EntityDefaultsProfile.For("Users")
    ///     .Set("CreatedBy",  ":USERNAME")   // expression — resolved at runtime
    ///     .Set("CreatedAt",  ":NOW")         // expression
    ///     .Set("Id",         ":NEWGUID")     // expression
    ///     .Set("Status",     "Active")       // literal — used as-is
    ///     .Set("Version",    "1");           // literal
    ///
    /// DefaultsManager.RegisterProfile("mydb", profile);
    /// DefaultsManager.Apply(editor, "mydb", "Users", record);
    /// </code>
    /// </example>
    /// </summary>
    public sealed class EntityDefaultsProfile
    {
        private readonly List<FieldDefaultRule> _rules = new List<FieldDefaultRule>();

        /// <summary>Entity name this profile applies to.</summary>
        public string EntityName { get; }

        /// <summary>Ordered list of field default rules.</summary>
        public IReadOnlyList<FieldDefaultRule> Rules => new ReadOnlyCollection<FieldDefaultRule>(_rules);

        private EntityDefaultsProfile(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name must not be empty.", nameof(entityName));
            EntityName = entityName;
        }

        // ── Factory ───────────────────────────────────────────────────────────────

        /// <summary>Start building a profile for <paramref name="entityName"/>.</summary>
        public static EntityDefaultsProfile For(string entityName) => new EntityDefaultsProfile(entityName);

        // ── Fluent builder ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds (or replaces) a rule for <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="fieldName">Target field name.</param>
        /// <param name="ruleString">
        /// Rule string.  Start with ":" for an expression (e.g. ":NOW", ":USERNAME"),
        /// or omit the prefix for a literal value (e.g. "Active", "0").
        /// </param>
        /// <param name="applyOnlyIfNull">
        /// When <c>true</c> (default) the value is only applied when the field is empty/null.
        /// </param>
        public EntityDefaultsProfile Set(string fieldName, string ruleString, bool applyOnlyIfNull = true)
        {
            // Remove any existing rule for this field (case-insensitive).
            _rules.RemoveAll(r => r.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            _rules.Add(new FieldDefaultRule(fieldName, ruleString, applyOnlyIfNull));
            return this;
        }

        /// <summary>
        /// Convenience overload: adds a literal numeric value for <paramref name="fieldName"/>.
        /// </summary>
        public EntityDefaultsProfile Set(string fieldName, int literalValue, bool applyOnlyIfNull = true)
            => Set(fieldName, literalValue.ToString(), applyOnlyIfNull);

        /// <summary>
        /// Convenience overload: adds a literal boolean value for <paramref name="fieldName"/>.
        /// </summary>
        public EntityDefaultsProfile Set(string fieldName, bool literalValue, bool applyOnlyIfNull = true)
            => Set(fieldName, literalValue.ToString().ToLowerInvariant(), applyOnlyIfNull);

        /// <summary>Removes the rule for <paramref name="fieldName"/> if present.</summary>
        public EntityDefaultsProfile Remove(string fieldName)
        {
            _rules.RemoveAll(r => r.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            return this;
        }

        // ── Built-in template factories ───────────────────────────────────────────

        /// <summary>
        /// Returns an audit profile for <paramref name="entityName"/> with fields:
        /// CreatedBy, CreatedAt, ModifiedBy, ModifiedAt, IsActive, Version, RowGuid.
        /// </summary>
        public static EntityDefaultsProfile AuditTemplate(string entityName) =>
            For(entityName)
                .Set("CreatedBy",  ":USERNAME")
                .Set("CreatedAt",  ":NOW")
                .Set("ModifiedBy", ":USERNAME")
                .Set("ModifiedAt", ":NOW")
                .Set("IsActive",   "true")
                .Set("Version",    "1")
                .Set("RowGuid",    ":NEWGUID");

        /// <summary>
        /// Returns a timestamp-only profile for <paramref name="entityName"/> with fields:
        /// CreatedAt, ModifiedAt.
        /// </summary>
        public static EntityDefaultsProfile TimestampTemplate(string entityName) =>
            For(entityName)
                .Set("CreatedAt",  ":NOW")
                .Set("ModifiedAt", ":NOW");

        /// <summary>
        /// Returns a user-stamp profile for <paramref name="entityName"/> with fields:
        /// CreatedBy, ModifiedBy.
        /// </summary>
        public static EntityDefaultsProfile UserStampTemplate(string entityName) =>
            For(entityName)
                .Set("CreatedBy",  ":USERNAME")
                .Set("ModifiedBy", ":USERNAME");

        /// <summary>
        /// Returns an identity profile for <paramref name="entityName"/> with field: Id = NEWGUID.
        /// </summary>
        public static EntityDefaultsProfile IdentityTemplate(string entityName) =>
            For(entityName)
                .Set("Id", ":NEWGUID");

        /// <summary>
        /// Combines <see cref="IdentityTemplate"/> + <see cref="AuditTemplate"/>.
        /// </summary>
        public static EntityDefaultsProfile FullTemplate(string entityName) =>
            For(entityName)
                .Set("Id",         ":NEWGUID")
                .Set("CreatedBy",  ":USERNAME")
                .Set("CreatedAt",  ":NOW")
                .Set("ModifiedBy", ":USERNAME")
                .Set("ModifiedAt", ":NOW")
                .Set("IsActive",   "true")
                .Set("Version",    "1");
    }
}
