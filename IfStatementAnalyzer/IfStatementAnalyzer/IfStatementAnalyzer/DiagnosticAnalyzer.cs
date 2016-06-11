using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IfStatementAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IfStatementAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IfStatementAnalyzer";

        // Setup the analyser feedback to the user.
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Syntax";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // Only look for if statements ignore everything else.
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IfStatement);
        }

        /// <summary>
        /// Analyses the if statement node for syntax errors.
        /// </summary>
        /// <param name="context"></param>
        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            // Cast our node context to an if statement
            var ifStatement = (IfStatementSyntax)context.Node;

            // Handle and incomplete if statement
            var nonBlockStatement = ifStatement.Statement as ExpressionStatementSyntax;
            if (nonBlockStatement == null) return;

            // Check the statement for a block
            if (ifStatement.Statement.IsKind(SyntaxKind.Block)) return;

            // Create a new diag.
            var diagnostic = Diagnostic.Create(descriptor: Rule, location: nonBlockStatement.GetLocation());

            // Report it back to the context
            context.ReportDiagnostic(diagnostic);
        }
    }
}
