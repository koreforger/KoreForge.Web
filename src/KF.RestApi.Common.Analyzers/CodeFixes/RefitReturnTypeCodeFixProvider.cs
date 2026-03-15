using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using KF.RestApi.Common.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KF.RestApi.Common.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RefitReturnTypeCodeFixProvider))]
[Shared]
public sealed class RefitReturnTypeCodeFixProvider : CodeFixProvider
{
    private static readonly TypeSyntax ReplacementType = SyntaxFactory.ParseTypeName("global::System.Threading.Tasks.Task<global::Refit.ApiResponse<object>>");

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.ExternalMethodsMustReturnApiResponse.Id);

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
            var methodDeclaration = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodDeclaration is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Return Task<ApiResponse<object>>",
                    createChangedDocument: c => ApplyAsync(context.Document, root, methodDeclaration, c),
                    equivalenceKey: "ReturnApiResponse"),
                diagnostic);
        }
    }

    private static Task<Document> ApplyAsync(Document document, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        var newReturnType = ReplacementType
            .WithLeadingTrivia(methodDeclaration.ReturnType.GetLeadingTrivia())
            .WithTrailingTrivia(methodDeclaration.ReturnType.GetTrailingTrivia());

        var updatedMethod = methodDeclaration.WithReturnType(newReturnType);
        var newRoot = root.ReplaceNode(methodDeclaration, updatedMethod);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
