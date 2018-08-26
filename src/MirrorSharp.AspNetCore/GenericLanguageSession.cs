using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using MirrorSharp.Internal.Abstraction;

namespace MirrorSharp.Extensions
{
    public class GenericLanguageSession<TData> : ILanguageSessionInternal
    {
        private readonly Func<TextSpan, CompletionItem, CancellationToken, Task<CompletionChange>> getCompletionChange;
        private readonly Func<int, CompletionTrigger, CancellationToken, Task<CompletionList>> getCompletions;
        private readonly Func<CancellationToken, Task<ImmutableArray<Diagnostic>>> getDiagnostics;
        private readonly Func<string> getText;
        private readonly Action<string, int, int?> replaceText;
        private readonly Func<int, CompletionTrigger, bool> shouldTriggerCompletion;
        private readonly Func<TData> getData;
        private readonly Action dispose;

        public TData Data => getData();

        public GenericLanguageSession(
            Func<TextSpan, CompletionItem, CancellationToken, Task<CompletionChange>> getCompletionChange,
            Func<int, CompletionTrigger, CancellationToken, Task<CompletionList>> getCompletions,
            Func<CancellationToken, Task<ImmutableArray<Diagnostic>>> getDiagnostics,
            Func<string> getText,
            Action<string, int, int?> replaceText,
            Func<int, CompletionTrigger, bool> shouldTriggerCompletion,
            Func<TData> getData,
            Action dispose
        )
        {
            this.getCompletionChange = getCompletionChange;
            this.getCompletions = getCompletions;
            this.getDiagnostics = getDiagnostics;
            this.getText = getText;
            this.replaceText = replaceText;
            this.shouldTriggerCompletion = shouldTriggerCompletion;
            this.getData = getData;
            this.dispose = dispose;
        }

        public Task<CompletionChange> GetCompletionChangeAsync(TextSpan completionSpan, CompletionItem item, CancellationToken cancellationToken) =>
            getCompletionChange(completionSpan, item, cancellationToken);

        public Task<CompletionList> GetCompletionsAsync(int cursorPosition, CompletionTrigger trigger, CancellationToken cancellationToken) =>
            getCompletions(cursorPosition, trigger, cancellationToken);

        public Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken cancellationToken) =>
            getDiagnostics(cancellationToken);

        public string GetText() => getText();

        public void ReplaceText(string newText, int start = 0, int? length = null)
            => replaceText(newText, start, length);

        public bool ShouldTriggerCompletion(int cursorPosition, CompletionTrigger trigger) =>
            shouldTriggerCompletion(cursorPosition, trigger);

        public void Dispose() => dispose();
    }
}