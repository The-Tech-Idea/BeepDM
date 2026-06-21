using System;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>
/// Field presenter contract shared by WPF and WinForms.
/// Wraps a platform-specific visual control for a single data field.
/// </summary>
public interface IFieldPresenter
{
    string FieldName { get; }
    string FieldType { get; }
    string Label { get; set; }
    object? Value { get; set; }
    bool IsReadOnly { get; set; }
    bool IsRequired { get; set; }
    bool IsVisible { get; set; }
    bool IsEnabled { get; set; }
    string? ValidationError { get; set; }
    string? Prompt { get; set; }
    object? QueryValue { get; set; }
    QueryOperator QueryOperator { get; set; }
    bool IsQueryEnabled { get; set; }

    /// <summary>Platform-specific visual element.</summary>
    object? View { get; }

    event EventHandler<object?> ValueChanged;

    void SetValue(object? value);
    void Clear();
    bool Validate();

    /// <summary>Factory: unique key for this presenter type.</summary>
    string Key { get; }

    /// <summary>Factory: returns true if this presenter can render the given field definition.</summary>
    bool CanPresent(object fieldDefinition);

    /// <summary>Factory: creates the platform-specific visual control.</summary>
    object CreateEditor(object fieldDefinition);

    /// <summary>Factory: applies metadata (label, prompt, format) to the platform control.</summary>
    void ApplyMetadata(object editor, object fieldDefinition);
}
