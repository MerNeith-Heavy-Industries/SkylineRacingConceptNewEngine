using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NFMWorld.XamlX.Core;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;
using Task = Microsoft.Build.Utilities.Task;

namespace NFMWorld.XamlX.BuildTask;

/// <summary>
/// MSBuild task that post-processes a compiled assembly to inject XAML-generated code.
/// This task reads XAML files linked to classes via x:Class, finds those types in the assembly,
/// and generates Populate/Build methods that initialize the UI tree.
/// </summary>
public class CompileXamlTask : Task
{
    /// <summary>
    /// Path to the compiled assembly to be processed.
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = "";

    /// <summary>
    /// File containing reference assembly paths, one per line.
    /// </summary>
    [Required]
    public string ReferencesFile { get; set; } = "";

    /// <summary>
    /// List of XAML files to compile.
    /// </summary>
    [Required]
    public ITaskItem[] XamlFiles { get; set; } = [];

    /// <summary>
    /// Output path for the modified assembly.
    /// </summary>
    public string OutputPath { get; set; } = "";

    private List<XamlDiagnostic> _diagnostics = new();

    public override bool Execute()
    {
        try
        {
            if (XamlFiles.Length == 0)
            {
                Log.LogMessage(MessageImportance.Normal, "No XAML files to compile.");
                return true;
            }

            var outputPath = string.IsNullOrEmpty(OutputPath) ? AssemblyPath : OutputPath;

            Log.LogMessage(MessageImportance.Normal, $"Compiling {XamlFiles.Length} XAML file(s)...");
            Log.LogMessage(MessageImportance.Low, $"Assembly: {AssemblyPath}");
            Log.LogMessage(MessageImportance.Low, $"Output: {outputPath}");

            // Load reference assemblies
            var refs = File.ReadAllLines(ReferencesFile)
                .Where(r => !string.IsNullOrWhiteSpace(r) && File.Exists(r))
                .Concat([AssemblyPath])
                .Distinct()
                .ToArray();

            Log.LogMessage(MessageImportance.Low, $"Loaded {refs.Length} reference assemblies.");

            var typeSystem = new CecilTypeSystem(refs, AssemblyPath);
            var assembly = typeSystem.FindAssembly(Path.GetFileNameWithoutExtension(AssemblyPath))
                           ?? throw new InvalidOperationException($"Assembly not found: {Path.GetFileNameWithoutExtension(AssemblyPath)}");

            var asm = typeSystem.GetAssembly(assembly);

            // Create XamlX configuration with our type mappings
            var typeMappings = XamlHelpers.CreateTypeMappings(typeSystem);
            var diagnosticsHandler = new XamlDiagnosticsHandler
            {
                HandleDiagnostic = diagnostic =>
                {
                    _diagnostics.Add(diagnostic);
                    if (diagnostic.Severity == XamlDiagnosticSeverity.Error)
                        Log.LogError($"XAML: {diagnostic.Code} - {diagnostic.Title}");
                    else if (diagnostic.Severity == XamlDiagnosticSeverity.Warning)
                        Log.LogWarning($"XAML: {diagnostic.Code} - {diagnostic.Title}");
                    else
                        Log.LogMessage(MessageImportance.Normal, $"XAML: {diagnostic.Code} - {diagnostic.Title}");
                    return diagnostic.Severity;
                }
            };
            var config = new TransformerConfiguration(typeSystem, assembly, typeMappings, diagnosticsHandler: diagnosticsHandler);

            // Find the XamlHotReload.Register method for runtime support
            var registerMethod = assembly
                .FindType("nfm_world.ui.yoga.xaml.XamlHotReload")
                ?.FindMethod(method => method.Name == "Register");
            
            if (registerMethod == null)
            {
                Log.LogMessage(MessageImportance.High, "XamlHotReload.Register method not found.");
            }

            // Set up emit mappings with a callback to inject hot reload registration code
            var emitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>()
            {
                ContextFactoryCallback = (context, codeGen) =>
                {
                    if (registerMethod != null)
                    {
                        codeGen
                            .Ldarg(1) // load element parameter
                            .Ldstr(context.BaseUrl) // load base URI
                            .EmitCall(registerMethod); // Call XamlHotReload.Register(element, baseUri)
                    }
                }
            };
            var compiler = new XamlILCompiler(config, emitMappings, true)
            {
                EnableIlVerification = false // Disable for now, can enable for debugging
            };

            // Add our custom transformer to remove x:Class and other directives before emit
            compiler.Transformers.Add(new RemoveXamlDirectivesTransformer());

            // Create a single shared context type
            var contextType = new TypeDefinition(
                "__XamlRuntime__",
                "XamlContext",
                TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Sealed,
                asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextType);
            var contextBuilder = typeSystem.CreateTypeBuilder(contextType);
            var contextTypeDef = compiler.CreateContextType(contextBuilder);

            var successCount = 0;
            foreach (var xamlItem in XamlFiles)
            {
                var xamlPath = xamlItem.GetMetadata("FullPath");
                if (string.IsNullOrEmpty(xamlPath))
                    xamlPath = xamlItem.ItemSpec;

                if (!File.Exists(xamlPath))
                {
                    Log.LogWarning($"XAML file not found: {xamlPath}");
                    continue;
                }

                try
                {
                    CompileXamlFile(xamlPath, typeSystem, asm, compiler, contextTypeDef);
                    successCount++;
                    Log.LogMessage(MessageImportance.Normal, $"  Compiled: {Path.GetFileName(xamlPath)}");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Error compiling {Path.GetFileName(xamlPath)}: {ex.Message}");
                    Log.LogMessage(MessageImportance.Low, ex.StackTrace ?? "");
                }
            }

            // Finalize context type
            contextBuilder.CreateType();

            // Write modified assembly
            if (successCount > 0)
            {
                var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
                var tempDllPath = outputPath + ".tmp";
                var tempPdbPath = pdbPath + ".tmp";

                var writerParams = new WriterParameters();
                // Check if symbols were loaded with the assembly
                if (asm.MainModule.HasSymbols)
                {
                    writerParams.WriteSymbols = true;
                    // Use portable PDB writer as it's more reliable
                    writerParams.SymbolWriterProvider = new PortablePdbWriterProvider();
                    writerParams.SymbolStream = new FileStream(tempPdbPath, FileMode.Create, FileAccess.Write);
                    Log.LogMessage(MessageImportance.Normal, $"Writing assembly with symbols: {outputPath}");
                }
                else if (File.Exists(pdbPath))
                {
                    Log.LogMessage(MessageImportance.Normal, $"PDB exists but symbols not loaded, will not write symbols");
                }

                // Write to temp file first
                using (var dllStream = new FileStream(tempDllPath, FileMode.Create, FileAccess.Write))
                {
                    asm.Write(dllStream, writerParams);
                }

                // Close the symbol stream if it was created
                writerParams.SymbolStream?.Close();
                writerParams.SymbolStream?.Dispose();

                // Replace original files
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                File.Move(tempDllPath, outputPath);

                if (File.Exists(tempPdbPath))
                {
                    if (File.Exists(pdbPath))
                        File.Delete(pdbPath);
                    File.Move(tempPdbPath, pdbPath);
                }

                Log.LogMessage(MessageImportance.High, $"Successfully compiled {successCount} XAML file(s).");
            }

            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogError($"XAML compilation failed: {ex.Message}");
            Log.LogMessage(MessageImportance.Low, ex.StackTrace ?? "");
            return false;
        }
    }

    private void CompileXamlFile(
        string xamlPath,
        CecilTypeSystem typeSystem,
        AssemblyDefinition asm,
        XamlILCompiler compiler,
        IXamlType contextTypeDef)
    {
        var xml = File.ReadAllText(xamlPath);
        var doc = XDocumentXamlParser.Parse(xml);

        // Extract x:Class from the root element
        var className = ExtractClassName(xml);
        if (string.IsNullOrEmpty(className))
        {
            Log.LogWarning($"XAML file {Path.GetFileName(xamlPath)} does not have x:Class attribute. Skipping.");
            return;
        }

        Log.LogMessage(MessageImportance.Normal, $"  Target class: {className}");

        // Find the target type in the assembly
        Log.LogMessage(MessageImportance.Low, $"  Searching for type in {asm.MainModule.Types.Count} types...");

        var targetType = asm.MainModule.Types.FirstOrDefault(t => t.FullName == className);
        if (targetType == null)
        {
            // Try nested types
            foreach (var type in asm.MainModule.Types)
            {
                targetType = FindNestedType(type, className);
                if (targetType != null) break;
            }
        }

        if (targetType == null)
        {
            // Log available types for debugging
            Log.LogMessage(MessageImportance.Low, "  Available types in assembly:");
            foreach (var t in asm.MainModule.Types.Take(20))
            {
                Log.LogMessage(MessageImportance.Low, $"    - {t.FullName}");
            }
            Log.LogError($"Type '{className}' not found in assembly. Ensure the code-behind class exists.");
            return;
        }

        Log.LogMessage(MessageImportance.Low, $"  Found type: {targetType.FullName}");

        // Transform the XAML AST
        compiler.Transform(doc);

        // Create a type builder for the target type
        var typeBuilder = typeSystem.CreateTypeBuilder(targetType);

        // Compile and emit Populate/Build methods
        compiler.Compile(
            doc,
            typeBuilder,
            contextTypeDef,
            populateMethodName: "Populate",
            createMethodName: "Build",
            namespaceInfoClassName: "XamlNamespaceInfo",
            baseUri: xamlPath,
            fileSource: new XamlFileSource(xamlPath, xml));
    }

    private static TypeDefinition? FindNestedType(TypeDefinition parent, string fullName)
    {
        foreach (var nested in parent.NestedTypes)
        {
            if (nested.FullName == fullName || nested.FullName.Replace('/', '.') == fullName)
                return nested;

            var found = FindNestedType(nested, fullName);
            if (found != null) return found;
        }
        return null;
    }

    private static string? ExtractClassName(string xaml)
    {
        try
        {
            var xdoc = XDocument.Parse(xaml);
            var root = xdoc.Root;
            if (root == null) return null;

            // Look for x:Class attribute in various namespace formats
            // First check all attributes for any that end with "Class" and have a prefix that starts with x: or similar
            foreach (var attr in root.Attributes())
            {
                // Check for x:Class with various namespace URIs
                if (attr.Name.LocalName == "Class")
                {
                    var ns = attr.Name.NamespaceName;
                    // Accept clr-namespace:System, standard XAML namespaces, or empty
                    if (ns.StartsWith("clr-namespace:System") ||
                        ns == "http://schemas.microsoft.com/winfx/2006/xaml" ||
                        ns == "http://schemas.microsoft.com/winfx/2009/xaml" ||
                        string.IsNullOrEmpty(ns))
                    {
                        return attr.Value;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
