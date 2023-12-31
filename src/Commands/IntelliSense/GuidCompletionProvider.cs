namespace DevAssist
{
    [ExportCompletionProvider(nameof(GuidCompletionProvider), LanguageNames.CSharp)]
    internal class GuidCompletionProvider : CompletionProvider
    {
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                if (context.Document.TryGetSyntaxTree(out SyntaxTree tree) && context.Document.SupportsSemanticModel)
                {
                    SyntaxNode root = await tree.GetRootAsync();
                    SemanticModel semanticModel = await context.Document.GetSemanticModelAsync();
                    SyntaxNode node = root.FindNode(context.CompletionListSpan);

                    InsertionType insertionType = GuidSyntax.IsGuid(semanticModel, node);
                    if (insertionType != InsertionType.None)
                        context.AddItem(CreateCompletionItem(insertionType));
                }
            }
            catch { }
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (item.Properties.TryGetValue("Guid", out string value))
                return Task.FromResult(CompletionDescription.FromText(value));

            return base.GetDescriptionAsync(document, item, cancellationToken);
        }

        private static CompletionItem CreateCompletionItem(InsertionType insertionType)
        {
            string value = Guid.NewGuid().ToString().ToUpper();
            var insertionText = insertionType switch
            {
                InsertionType.Constructor => $"Guid(\"{value}\")",
                InsertionType.Assignment => $"new Guid(\"{value}\")",
                InsertionType.ValueWithQuotes => $"\"{value}\"",
                InsertionType.Value => value,
                _ => throw new NotSupportedException($"Not supported value '{insertionType}'."),
            };
            var tags = ImmutableArray.Create(WellKnownTags.Structure);
            var properties = ImmutableDictionary.Create<string, string>().Add("Guid", value);
            var rules = CompletionItemRules.Create(matchPriority: MatchPriority.Preselect);

            return CompletionItem.Create(
                insertionText,
                insertionText,
                sortText: "0",
                properties: properties,
                tags: tags,
                rules: rules
            );
        }
    }

    internal static class GuidSyntax
    {
        internal static InsertionType IsGuid(SemanticModel semanticModel, SyntaxNode node)
        {
            if (node is ExpressionStatementSyntax expressionStatementSyntax)
            {
                if (expressionStatementSyntax.Expression is AssignmentExpressionSyntax assignmentExpression && assignmentExpression.Left is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                {
                    var typeOfExpression = semanticModel.GetTypeInfo(memberAccessExpressionSyntax);
                    if (IsGuid(typeOfExpression))
                        return InsertionType.Assignment;
                }
            }
            else if (node is GlobalStatementSyntax globalStatement)
            {
                if (globalStatement.Statement is ExpressionStatementSyntax expressionStatement)
                {
                    if (expressionStatement.Expression is AssignmentExpressionSyntax assignmentExpression && assignmentExpression.Left is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                    {
                        var typeOfExpression = semanticModel.GetTypeInfo(memberAccessExpressionSyntax);
                        if (IsGuid(typeOfExpression))
                            return InsertionType.Assignment;
                    }
                }
            }
            else if (node is AssignmentExpressionSyntax assignment)
            {
                if (node.Parent is InitializerExpressionSyntax)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(assignment.Left);
                    if (IsGuid(symbolInfo))
                        return InsertionType.Assignment;
                }
                else if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
                {
                    var typeOfExpression = semanticModel.GetTypeInfo(memberAccess);
                    if (IsGuid(typeOfExpression))
                        return InsertionType.Assignment;
                }
            }
            else if (node is ObjectCreationExpressionSyntax objectCreation)
            {
                if (objectCreation.Parent is EqualsValueClauseSyntax equals)
                {
                    if (equals.Parent is VariableDeclaratorSyntax variableDeclarator)
                    {
                        if (variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration)
                        {
                            if (IsGuid(variableDeclaration.Type))
                                return InsertionType.Constructor;
                        }
                    }
                    else if (equals.Parent is PropertyDeclarationSyntax propertyDeclaration)
                    {
                        if (IsGuid(propertyDeclaration.Type))
                            return InsertionType.Constructor;
                    }
                }
                else if (objectCreation.Parent is AssignmentExpressionSyntax assignment2)
                {
                    if (assignment2.Left is MemberAccessExpressionSyntax memberAccess2)
                    {
                        var typeOfExpression = semanticModel.GetTypeInfo(memberAccess2);
                        if (IsGuid(typeOfExpression))
                            return InsertionType.Constructor;
                    }
                }
            }
            else if (node is ArgumentListSyntax argumentList)
            {
                if (argumentList.Parent is ObjectCreationExpressionSyntax objectCreation1)
                {
                    if (IsGuid(objectCreation1.Type))
                        return InsertionType.ValueWithQuotes;
                }
            }
            else if (node is ArgumentSyntax argument)
            {
                if (argument.Expression is LiteralExpressionSyntax literalExpression)
                {
                    if (literalExpression.Kind() == SyntaxKind.StringLiteralExpression && literalExpression.Token.ValueText?.Length == 0)
                    {
                        if (argument.Parent is ArgumentListSyntax argumentListSyntax)
                        {
                            if (argumentListSyntax.Parent is ObjectCreationExpressionSyntax objectCreation2)
                            {
                                if (IsGuid(objectCreation2.Type))
                                    return InsertionType.Value;
                            }
                        }
                    }
                }
            }
            else if (node is InitializerExpressionSyntax initializerExpression)
            {
            }

            return InsertionType.None;
        }

        private static bool IsGuid(TypeSyntax type)
        {
            if (type is IdentifierNameSyntax identifierName)
            {
                string name = identifierName.Identifier.ValueText;
                if (name == nameof(Guid) || name == typeof(Guid).FullName)
                    return true;
            }

            return false;
        }

        private static bool IsGuid(SymbolInfo symbol)
        {
            if (symbol.Symbol is IPropertySymbol propertySymbol)
                return propertySymbol.Type.Name == nameof(Guid);

            return false;
        }

        private static bool IsGuid(TypeInfo typeInfo)
        {
            return typeInfo.Type.Name == nameof(Guid);
        }
    }

    internal enum InsertionType
    {
        None,
        Constructor,
        ValueWithQuotes,
        Value,
        Assignment
    }
}