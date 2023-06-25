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
                    var tags = ImmutableArray.Create(WellKnownTags.Method, WellKnownTags.Public);
                    context.AddItem(CompletionItem.Create("ConfigureAwait(false)", tags: tags));
                }
            }
            catch { }
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