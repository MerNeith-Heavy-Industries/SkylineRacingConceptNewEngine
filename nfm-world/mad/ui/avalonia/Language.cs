// ReSharper disable once CheckNamespace
namespace Avalonia.Metadata;

public interface IAddChild
{
    void AddChild(object child);
}

public interface IAddChild<T> : IAddChild
{
    void AddChild(T child);
}

public class ContentAttribute : Attribute
{
        
}

public class WhitespaceSignificantCollectionAttribute : Attribute
{

}

public class TrimSurroundingWhitespaceAttribute : Attribute
{

}

public class XmlnsDefinitionAttribute : Attribute
{
    public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
    {
            
    }
}

public class UsableDuringInitializationAttribute : Attribute
{
    public UsableDuringInitializationAttribute(bool usable)
    {
            
    }
}

public class DeferredContentAttribute : Attribute
{
        
}