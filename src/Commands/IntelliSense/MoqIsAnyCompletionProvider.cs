using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DevAssist
{
    [ExportCompletionProvider(nameof(MoqIsAnyCompletionProvider), LanguageNames.CSharp)]
    internal class MoqIsAnyCompletionProvider : CompletionProvider
    {
        private static Regex setupMethodNamePattern = new("^Moq\\.Mock<.*>\\.Setup\\.*");

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                if (!context.Document.SupportsSemanticModel)
                    return;

                var syntaxRoot = await context.Document.GetSyntaxRootAsync();
                var semanticModel = await context.Document.GetSemanticModelAsync();

                var tokenAtCursor = GetCurrentArgumentListSyntaxToken(syntaxRoot, context.Position);
                if (tokenAtCursor.Kind() == SyntaxKind.None)
                    return;

                var mockedMethodArgumentList = tokenAtCursor.Parent as ArgumentListSyntax;
                var mockedMethodInvocation = mockedMethodArgumentList?.Parent as InvocationExpressionSyntax;
                var setupMethodLambda = mockedMethodInvocation?.Parent as LambdaExpressionSyntax;
                var setupMethodArgument = setupMethodLambda?.Parent as ArgumentSyntax;
                var setupMethodArgumentList = setupMethodArgument?.Parent as ArgumentListSyntax;
                var setupMethodInvocation = setupMethodArgumentList?.Parent as InvocationExpressionSyntax;

                if (IsMoqSetupMethod(semanticModel, setupMethodInvocation))
                {
                    var matchingMockedMethods = GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(semanticModel, setupMethodInvocation);

                    foreach (IMethodSymbol matchingMockedMethodSymbol in matchingMockedMethods.Where(m => m.Parameters.Any()))
                    {
                        if (tokenAtCursor.IsKind(SyntaxKind.OpenParenToken))
                        {
                            var fullMethodHelper = string.Join(", ", matchingMockedMethodSymbol.Parameters.Select(p => "It.IsAny<" + p.Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()"));
                            context.AddItem(CompletionItem.Create(fullMethodHelper));
                            if (matchingMockedMethodSymbol.Parameters.Length > 1)
                            {
                                var oneArgumentHelper = "It.IsAny<" + matchingMockedMethodSymbol.Parameters[0].Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()";
                                context.AddItem(CompletionItem.Create(oneArgumentHelper));
                            }
                        }
                        else
                        {
                            var allCommaTokens = mockedMethodArgumentList.ChildTokens().Where(t => t.IsKind(SyntaxKind.CommaToken)).ToList();
                            int paramIdx = allCommaTokens.IndexOf(tokenAtCursor) + 1;
                            if (matchingMockedMethodSymbol.Parameters.Length > paramIdx)
                            {
                                var oneArgumentHelper = "It.IsAny<" + matchingMockedMethodSymbol.Parameters[paramIdx].Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()";
                                context.AddItem(CompletionItem.Create(oneArgumentHelper));
                            }
                        }
                    }
                }
            }
            catch { }
        }

        internal static SyntaxToken GetCurrentArgumentListSyntaxToken(SyntaxNode node, int currentPosition)
        {
            var allArgumentLists = node.DescendantNodes(n => n.FullSpan.Contains(currentPosition - 1)).OfType<ArgumentListSyntax>().OrderBy(n => n.FullSpan.Length);
            return allArgumentLists.SelectMany(n => n.ChildTokens()
                .Where(t => t.IsKind(SyntaxKind.OpenParenToken) || t.IsKind(SyntaxKind.CommaToken))
                .Where(t => t.FullSpan.Contains(currentPosition - 1))).FirstOrDefault();
        }

        internal static bool IsMoqSetupMethod(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
        {
            var method = invocation.Expression as MemberAccessExpressionSyntax;
            return IsMoqSetupMethod(semanticModel, method);
        }

        internal static bool IsMoqSetupMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
        {
            var methodName = method?.Name.ToString();
            if (methodName != "Setup") return false;

            var symbolInfo = semanticModel.GetSymbolInfo(method);
            if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
            {
                return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Any(s => setupMethodNamePattern.IsMatch(s.ToString()));
            }
            else if (symbolInfo.CandidateReason == CandidateReason.None)
            {
                return symbolInfo.Symbol is IMethodSymbol && setupMethodNamePattern.IsMatch(symbolInfo.Symbol.ToString());
            }
            return false;
        }

        internal static IEnumerable<IMethodSymbol> GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(SemanticModel semanticModel, InvocationExpressionSyntax setupMethodInvocation)
        {
            var setupLambdaArgument = setupMethodInvocation?.ArgumentList.Arguments[0]?.Expression as LambdaExpressionSyntax;
            var mockedMethodInvocation = setupLambdaArgument?.Body as InvocationExpressionSyntax;

            return GetAllMatchingSymbols<IMethodSymbol>(semanticModel, mockedMethodInvocation);
        }

        internal static IEnumerable<T> GetAllMatchingSymbols<T>(SemanticModel semanticModel, ExpressionSyntax expression) where T : class
        {
            var matchingSymbols = new List<T>();
            if (expression != null)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(expression);
                if (symbolInfo.CandidateReason == CandidateReason.None && symbolInfo.Symbol is T)
                {
                    matchingSymbols.Add(symbolInfo.Symbol as T);
                }
                else if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                {
                    matchingSymbols.AddRange(symbolInfo.CandidateSymbols.OfType<T>());
                }
            }
            return matchingSymbols;
        }
    }
}