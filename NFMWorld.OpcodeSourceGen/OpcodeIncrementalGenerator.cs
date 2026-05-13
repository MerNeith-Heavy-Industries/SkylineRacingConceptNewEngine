using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WorldXaml.Generator.Common;

namespace NFMWorld.OpcodeSourceGen;

[Generator(LanguageNames.CSharp)]
public class OpcodeIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var c2sPackets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NFMWorldLibrary.Multiplayer.Packets.PacketClientToServerAttribute",
            (node, ct) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
            (ctx, ct) =>
            {
                var opcode = ctx.Attributes.First().ConstructorArguments.FirstOrDefault().Value as sbyte?;
                var node = (TypeDeclarationSyntax)ctx.TargetNode;
                var semanticModel = ctx.SemanticModel;
                var typeSymbol = semanticModel.GetDeclaredSymbol(node) as ITypeSymbol;
                var @namespace = typeSymbol?.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Remove(0, "global::".Length);
                var name = typeSymbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                var isStruct = node is StructDeclarationSyntax;
                var isRecord = node is RecordDeclarationSyntax;

                return (@namespace, name, opcode, isStruct, isRecord);
            }
        );
        var s2cPackets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NFMWorldLibrary.Multiplayer.Packets.PacketServerToClientAttribute",
            (node, ct) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
            (ctx, ct) =>
            {
                var opcode = ctx.Attributes.First().ConstructorArguments.FirstOrDefault().Value as sbyte?;
                var node = (TypeDeclarationSyntax)ctx.TargetNode;
                var semanticModel = ctx.SemanticModel;
                var typeSymbol = semanticModel.GetDeclaredSymbol(node) as ITypeSymbol;
                var @namespace = typeSymbol?.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Remove(0, "global::".Length);
                var name = typeSymbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                var isStruct = node is StructDeclarationSyntax;
                var isRecord = node is RecordDeclarationSyntax;

                return (@namespace, name, opcode, isStruct, isRecord);
            }
        );
        
        context.RegisterSourceOutput(c2sPackets, GeneratePacket);
        context.RegisterSourceOutput(s2cPackets, GeneratePacket);
        
        context.RegisterSourceOutput(c2sPackets.Collect().Combine(s2cPackets.Collect()), (ctx, pair) =>
        {
            var (c2s, s2c) = pair;
            
            var sb = new IndentedStringBuilder();

            sb.AppendJoin(c2s.Concat(s2c)
                .Select(p => p.@namespace)
                .Where(ns => ns is not null)
                .Distinct(StringComparer.Ordinal)
                .Select(ns => $"using {ns};"), '\n');

            sb.AppendLine();

            sb.AppendLine(
                $$"""
                  using NFMWorldLibrary.Util;

                  namespace NFMWorldLibrary.Multiplayer;

                  public static class MultiplayerUtils
                  {
                  """);

            using (sb.Indent())
            {
                sb.AppendLine("private static T DeserializePacket<T>(ReadOnlyMemory<byte> data) where T : IReadableWritable<T> => T.Read(data);");
                sb.AppendLine("public static IPacketClientToServer? TryDeserializeC2SPacket(sbyte opcode, ReadOnlyMemory<byte> data)");
                sb.AppendLine("{");

                using (sb.Indent())
                {
                    sb.AppendLine("return opcode switch");
                    sb.AppendLine("{");

                    using (sb.Indent())
                    {
                        foreach (var (@namespace, name, opcode, isStruct, isRecord) in c2s)
                        {
                            if (@namespace is null || name is null || opcode is null) continue;
                            
                            sb.AppendLine($"{opcode} => DeserializePacket<{name}>(data),");
                        }

                        sb.AppendLine("_ => null");
                    }

                    sb.AppendLine("};");
                }

                sb.AppendLine("}");
                
                sb.AppendLine("public static IPacketServerToClient? TryDeserializeS2CPacket(sbyte opcode, ReadOnlyMemory<byte> data)");
                sb.AppendLine("{");

                using (sb.Indent())
                {
                    sb.AppendLine("return opcode switch");
                    sb.AppendLine("{");
                    
                    using (sb.Indent())
                    {
                        foreach (var (@namespace, name, opcode, isStruct, isRecord) in s2c)
                        {
                            if (@namespace is null || name is null || opcode is null) continue;
                            
                            sb.AppendLine($"{opcode} => DeserializePacket<{name}>(data),");
                        }

                        sb.AppendLine("_ => null");
                    }

                    sb.AppendLine("};");
                }

                sb.AppendLine("}");
            }
            
            sb.AppendLine("}");
            
            ctx.AddSource($"MultiplayerUtils.g.cs", sb.ToString());
        });
        return;

        static void GeneratePacket(SourceProductionContext ctx, (string? Namespace, string? Name, sbyte? Opcode, bool IsStruct, bool IsRecord) packetInfo)
        {
            var sb = new IndentedStringBuilder();

            sb.AppendLine(
                $$"""
                  using MessagePack;
                  
                  namespace {{packetInfo.Namespace}};

                  partial {{(packetInfo.IsStruct ? "struct" : packetInfo.IsRecord ? "record" : "class")}} {{packetInfo.Name}}
                  {
                      [IgnoreMember]
                      public {{(packetInfo.IsStruct ? "readonly " : "")}}sbyte Opcode => {{packetInfo.Opcode}};
                  }
                  """);
            
            ctx.AddSource($"{packetInfo.Name}.g.cs", sb.ToString());
        }
    }
}