using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace IfStatementAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IfStatementAnalyzerCodeFixProvider)), Shared]
    public class IfStatementAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add braces to the bad if statement!";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IfStatementAnalyzerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: fix => AddBracesAsync(context.Document, declaration, fix),
                    equivalenceKey: Title),
                diagnostic);
        }

        /// <summary>
        /// This is where the magic happens returning a new document with the code fix inside.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="ifStatement"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Document> AddBracesAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            // Get our non block statement which we are going to recreate with a block and remove this
            var nonBlockStatement = ifStatement.Statement as ExpressionStatementSyntax;

            // New up a block statement from the existing non-block statement
            var newBlockStatement = SyntaxFactory.Block(nonBlockStatement)
                                                 .WithAdditionalAnnotations(Formatter.Annotation); // Handle formating of code

            // Swap out the nodes and return a new if statement
            var newIfStatement = ifStatement.ReplaceNode(nonBlockStatement, newBlockStatement);

            // Get entire document (compulation unit)
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(ifStatement, newIfStatement);

            // Create a new document and return the replacement
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}