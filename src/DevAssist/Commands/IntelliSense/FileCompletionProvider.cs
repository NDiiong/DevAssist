using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using System.Threading.Tasks;

namespace DevAssist
{
    [ExportCompletionProvider(nameof(FileCompletionProvider), LanguageNames.CSharp)]
    internal class FileCompletionProvider : CompletionProvider
    {
        public override Task ProvideCompletionsAsync(CompletionContext context)
        {
            //var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            //var directoryPath = (context.Document.Project.CompilationOptions.SourceReferenceResolver as SourceFileResolver)?.SearchPaths
            //    ?? ImmutableArray<string>.Empty;

            //var result = ArrayBuilder<CompletionItem>.GetInstance();
            //var pathKind = PathUtilities.GetPathKind(directoryPath);

            return Task.CompletedTask;
        }
    }
}