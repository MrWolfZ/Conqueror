using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators;

[Generator]
public sealed class MessageAbstractionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var messageTypesToGenerate = context.SyntaxProvider
                                            .CreateSyntaxProvider(
                                                //// in the first quick filter pass, select classes and records with base types
                                                static (s, _) => s is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } or RecordDeclarationSyntax { BaseList.Types.Count: > 0 },
                                                GetTypeToGenerate) // select classes with one of our message interfaces and extract details
                                            .WithTrackingName(TrackingNames.InitialExtraction)
                                            .Where(static m => m is not null) // Filter out errors that we don't care about
                                            .Select(static (m, _) => m!.Value)
                                            .WithTrackingName(TrackingNames.RemovingNulls);

        context.RegisterSourceOutput(messageTypesToGenerate,
                                     static (spc, messageTypeToGenerate) => Execute(in messageTypeToGenerate, spc));
    }

    private static void Execute(in MessageTypeToGenerate messageTypeToGenerate, SourceProductionContext context)
    {
        var (result, filename) = SourceGenerationHelper.GenerateMessageTypes(in messageTypeToGenerate);
        context.AddSource(filename, SourceText.From(result, Encoding.UTF8));
    }

    private static MessageTypeToGenerate? GetTypeToGenerate(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);

        if (symbol is not INamedTypeSymbol namedSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip message types that already declare a handler member
        // TODO: improve with adding explicit diagnostic and also detecting pipeline
        if (namedSymbol.MemberNames.Contains("IHandler"))
        {
            return null;
        }

        var baseListSyntax = (context.Node as TypeDeclarationSyntax)?.BaseList;
        INamedTypeSymbol? interfaceSymbol = null;

        foreach (var baseTypeSyntax in baseListSyntax?.Types ?? [])
        {
            if (baseTypeSyntax.Type is GenericNameSyntax { Identifier.Text: "IMessage" } or IdentifierNameSyntax { Identifier.Text: "IMessage" }
                && ModelExtensions.GetSymbolInfo(context.SemanticModel, baseTypeSyntax.Type).Symbol is INamedTypeSymbol s
                && s.ContainingAssembly.Name == "Conqueror.Abstractions")
            {
                interfaceSymbol = s;
            }
        }

        if (interfaceSymbol is null)
        {
            // no base type was one of our IMessage interfaces, so we skip this type
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GenerateTypeDescriptor(namedSymbol, interfaceSymbol, (TypeDeclarationSyntax)context.Node);
    }

    private static MessageTypeToGenerate GenerateTypeDescriptor(INamedTypeSymbol messageTypeSymbol,
                                                                INamedTypeSymbol interfaceSymbol,
                                                                TypeDeclarationSyntax messageTypeSyntax)
    {
        var responseTypeSymbol = interfaceSymbol.TypeArguments.Length > 0 ? interfaceSymbol.TypeArguments[0] : null;
        var responseTypeSyntaxNode = responseTypeSymbol?.DeclaringSyntaxReferences.Length > 0 ? responseTypeSymbol.DeclaringSyntaxReferences[0] : null;

        var messageTypeDescriptor = new TypeDescriptor(
            messageTypeSymbol.Name,
            FullyQualifiedName: messageTypeSymbol.ToString(),
            Namespace: messageTypeSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : messageTypeSymbol.ContainingNamespace.ToString(),
            IsRecord: messageTypeSyntax is RecordDeclarationSyntax,
            ParentClass: GetParentClasses(messageTypeSyntax));

        if (responseTypeSymbol is null || responseTypeSyntaxNode is null)
        {
            return new(messageTypeDescriptor, null);
        }

        var responseTypeDescriptor = new TypeDescriptor(
            responseTypeSymbol.Name,
            FullyQualifiedName: responseTypeSymbol.ToString(),
            Namespace: responseTypeSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : responseTypeSymbol.ContainingNamespace.ToString(),
            IsRecord: responseTypeSyntaxNode.GetSyntax() is RecordDeclarationSyntax,
            ParentClass: GetParentClasses(responseTypeSyntaxNode.GetSyntax()));

        return new(messageTypeDescriptor, responseTypeDescriptor);
    }

    private static ParentClass? GetParentClasses(SyntaxNode syntaxNode)
    {
        // Try and get the parent syntax. If it isn't a type like class/struct, this will be null
        var parentSyntax = syntaxNode.Parent as TypeDeclarationSyntax;
        ParentClass? parentClassInfo = null;

        // Keep looping while we're in a supported nested type
        while (parentSyntax != null && IsAllowedKind(parentSyntax.Kind()))
        {
            // Record the parent type keyword (class/struct etc), name, and constraints
            parentClassInfo = new(
                parentSyntax.Keyword.ValueText,
                parentSyntax.Identifier.ToString() + parentSyntax.TypeParameterList,
                parentSyntax.ConstraintClauses.ToString(),
                parentClassInfo); // set the child link (null initially)

            // Move to the next outer type
            parentSyntax = parentSyntax.Parent as TypeDeclarationSyntax;
        }

        // return a link to the outermost parent type
        return parentClassInfo;
    }

    // We can only be nested in class/struct/record
    private static bool IsAllowedKind(SyntaxKind kind) =>
        kind is SyntaxKind.ClassDeclaration
            or SyntaxKind.StructDeclaration
            or SyntaxKind.RecordDeclaration;
}
