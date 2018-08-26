using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using MirrorSharp.Internal;
using MirrorSharp.Internal.Reflection;

namespace MirrorSharp.Extensions
{
    public static class CSharpScriptSession
    {
        public static GenericLanguageSession<SyntaxTree> Create(
            string text,
            IEnumerable<MetadataReference> metadataReferences,
            CSharpCompilationOptions compilationOptions,
            Type globalsType)
        {
            var tree = CSharpSyntaxTree.ParseText(
                text,
                CSharpParseOptions.Default.WithKind(SourceCodeKind.Script));

            return new GenericLanguageSession<SyntaxTree>(
                getCompletionChange: (completionSpan, item, cancellationToken) =>
                {
                    throw new NotImplementedException("Code Completion not implemented yet");
                },
                getCompletions: (cursorPosition, trigger, cancellationToken) =>
                {
                    throw new NotImplementedException("Code Completion not implemented yet");
                },
                getDiagnostics: cancellationToken =>
                {
                    var compilation = CSharpCompilation.CreateScriptCompilation(
                        "CSharpScriptSessionTemp",
                        tree,
                        metadataReferences,
                        compilationOptions,
                        globalsType: globalsType
                    );
                    return Task.FromResult(compilation.GetDiagnostics());
                },
                getText: () => tree.ToString(),
                replaceText: (newText, start, length) =>
                {
                    System.Console.WriteLine($"Replace text with {newText}, from: {start}, length: {length}");
                    var sourceText = tree.GetText();
                    var finalLength = length ?? sourceText.Length - start;
                    var change = new TextChange(new TextSpan(start, finalLength), newText);
                    tree = tree.WithChangedText(sourceText.WithChanges(change));
                },
                shouldTriggerCompletion: (cursorPosition, trigger) =>
                {
                    return false;
                },
                getData: () => tree,
                dispose: () => {}
            );
        }
    }
}