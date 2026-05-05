namespace AirlineFuelMS.Core.Attributes;

/// <summary>
/// Marks an entity property as searchable by the generic ApplyFilter helper.
/// Free-text search keywords are matched against all properties bearing this attribute (case-insensitive Contains).
/// Supported types: string, int, int?.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SearchAttribute : Attribute
{
}
