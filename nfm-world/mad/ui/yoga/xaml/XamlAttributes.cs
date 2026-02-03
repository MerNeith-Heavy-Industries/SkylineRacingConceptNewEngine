namespace nfm_world.ui.yoga.xaml;

/// <summary>
/// Specifies a mapping between a XAML namespace and a CLR namespace.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class XmlnsDefinitionAttribute : Attribute
{
    public string XmlNamespace { get; }
    public string ClrNamespace { get; }

    public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
    {
        XmlNamespace = xmlNamespace;
        ClrNamespace = clrNamespace;
    }
}

/// <summary>
/// Marks a property as the content property for XAML.
/// Can be applied to a class with Name="PropertyName" or directly to a property.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public class ContentPropertyAttribute : Attribute
{
    /// <summary>
    /// The name of the content property. Required when applied to a class.
    /// </summary>
    public string Name { get; set; } = "";

    public ContentPropertyAttribute()
    {
    }

    public ContentPropertyAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Marks a class as usable during XAML initialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UsableDuringInitializationAttribute : Attribute
{
    public bool Usable { get; }

    public UsableDuringInitializationAttribute(bool usable)
    {
        Usable = usable;
    }
}

/// <summary>
/// Marks a property as having deferred content.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DeferredContentAttribute : Attribute
{
}

/// <summary>
/// Marks a collection as whitespace significant.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class WhitespaceSignificantCollectionAttribute : Attribute
{
}

/// <summary>
/// Marks a class to trim surrounding whitespace.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TrimSurroundingWhitespaceAttribute : Attribute
{
}
