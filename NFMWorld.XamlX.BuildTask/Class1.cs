using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;
using Microsoft.Build.Framework;  
using Microsoft.Build.Utilities;  
using Mono.Cecil;
using XamlX.Parsers;
using Task = Microsoft.Build.Utilities.Task;

namespace NFMWorld.XamlX.BuildTask;

public class CompileXamlTask : Task  
{  
    [Required] public string AssemblyPath { get; set; } = "";  
    [Required] public string ReferencesFile { get; set; } = "";  
    [Required] public string[] XamlFiles { get; set; } = [];  
  
    public override bool Execute()  
    {  
        var refs = File.ReadAllLines(ReferencesFile).Concat([AssemblyPath]);  
        var typeSystem = new CecilTypeSystem(refs, AssemblyPath);  
        var assembly = typeSystem.FindAssembly(Path.GetFileNameWithoutExtension(AssemblyPath))  
            ?? throw new InvalidOperationException("Assembly not found");  
        var asm = typeSystem.GetAssembly(assembly);  
        var config = new TransformerConfiguration(typeSystem, assembly,  
            new XamlLanguageTypeMappings(typeSystem));  
        var compiler = new XamlILCompiler(config,  
            new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>(),  
            true);  
  
        var contextType = new TypeDefinition("_XamlRuntime", "XamlContext",  
            TypeAttributes.Class, asm.MainModule.TypeSystem.Object);  
        asm.MainModule.Types.Add(contextType);  
        var contextBuilder = typeSystem.CreateTypeBuilder(contextType);  
        var contextTypeDef = compiler.CreateContextType(contextBuilder);  
  
        foreach (var file in XamlFiles)  
        {  
            var xml = File.ReadAllText(file);  
            var doc = XDocumentXamlParser.Parse(xml);  
            compiler.Transform(doc);  
            // Emit Populate/Build into a placeholder type or modify existing types  
            // For simplicity, this example assumes a placeholder type exists  
            var placeholderType = asm.MainModule.Types.First(t => t.Name == "GeneratedXamlType");  
            compiler.Compile(doc, typeSystem.CreateTypeBuilder(placeholderType), contextTypeDef,  
                "Populate", "Build", "XamlNamespaceInfo", file, null);  
        }  
  
        asm.Write(AssemblyPath);  
        return !Log.HasLoggedErrors;  
    }  
}