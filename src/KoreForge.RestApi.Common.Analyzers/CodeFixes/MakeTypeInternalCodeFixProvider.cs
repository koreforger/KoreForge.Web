using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KoreForge.RestApi.Common.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KoreForge.RestApi.Common.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeTypeInternalCodeFixProvider))]
[Shared]
public sealed class MakeTypeInternalCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.ExternalTypesMustBeInternal.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var typeDeclaration = node.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>();
            if (typeDeclaration is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make type internal",
                    createChangedDocument: c => ApplyAsync(context.Document, root, typeDeclaration, c),
                    equivalenceKey: "MakeTypeInternal"),
                diagnostic);
        }
    }

    private static Task<Document> ApplyAsync(Document document, SyntaxNode root, BaseTypeDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var publicModifier = declaration.Modifiers.FirstOrDefault(token => token.IsKind(SyntaxKind.PublicKeyword));
        if (publicModifier == default)
        {
            return Task.FromResult(document);
        }

        var internalModifier = SyntaxFactory.Token(publicModifier.LeadingTrivia, SyntaxKind.InternalKeyword, publicModifier.TrailingTrivia);
        var newModifiers = declaration.Modifiers.Replace(publicModifier, internalModifier);
        var newDeclaration = declaration.WithModifiers(newModifiers);
        var newRoot = root.ReplaceNode(declaration, newDeclaration);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
