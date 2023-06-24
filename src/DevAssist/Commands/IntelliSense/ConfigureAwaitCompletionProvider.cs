using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevAssist
{
    [ExportCompletionProvider(nameof(ConfigureAwaitCompletionProvider), LanguageNames.CSharp)]
    public class ConfigureAwaitCompletionProvider : CompletionProvider
    {
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                if (!context.Document.SupportsSemanticModel)
                    return;

                var syntaxRoot = await context.Document.GetSyntaxRootAsync();
                var semanticModel = await context.Document.GetSemanticModelAsync();

                var currentNode = GetCurrentMemberAccess(syntaxRoot, context.Position);
                if (currentNode == null)
                    return;

                var typeOfExpression = semanticModel.GetTypeInfo(currentNode.Expression);
                if (typeOfExpression.Type.Name == "Task" && typeOfExpression.Type.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks")
                {
                    context.AddItem(CompletionItem.Create("ConfigureAwait(false)", tags: ImmutableArray.Create(new[] { "Method", "Public" })));
                }
            }
            catch { }
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var completionDescription = CompletionDescription.Create(
                ImmutableArray.Create(new TaggedText[] {
                    new TaggedText(TextTags.Text, "Just a shortcut for the "),
                    new TaggedText(TextTags.Class, "Task"),
                    new TaggedText(TextTags.Method, ".ConfigureAwait"),
                    new TaggedText(TextTags.Text, " method.")
            }));

            return Task.FromResult(completionDescription);
        }

        private static MemberAccessExpressionSyntax GetCurrentMemberAccess(SyntaxNode node, int currentPosition)
        {
            var nodes = node.DescendantNodes(n => n.FullSpan.Contains(currentPosition - 1));
            return nodes.OfType<MemberAccessExpressionSyntax>()
                .FirstOrDefault(m => m.OperatorToken.FullSpan.Contains(currentPosition - 1))
                ?? nodes.OfType<SimpleNameSyntax>().FirstOrDefault(m => m.Span.Contains(currentPosition - 1))?.Parent as MemberAccessExpressionSyntax;
        }
    }
}