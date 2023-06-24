namespace DevAssist
{
    [ExportCompletionProvider(nameof(MoqCompletionProvider), LanguageNames.CSharp)]
    internal class MoqCompletionProvider : CompletionProvider
    {
        public override Task ProvideCompletionsAsync(CompletionContext context)
        {
            return Task.CompletedTask;
        }
    }
}