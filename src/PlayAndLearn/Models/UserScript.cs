using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using OneOf;

namespace PlayAndLearn.Models
{
    public class UserScript : OneOfBase<
        UserScript.ParsedCode,
        UserScript.CompiledCode>
    {
        public bool CanExecute => IsT1 && !AsT1.HasErrors;

        public class ParsedCode : UserScript
        {
            public ParsedCode(SyntaxTree syntaxTree)
            {
                SyntaxTree = syntaxTree;
            }

            public SyntaxTree SyntaxTree { get; }
        }

        public class CompiledCode : UserScript
        {
            public CompiledCode(
                Compilation compilation,
                IImmutableList<Diagnostic> diagnostics)
            {
                Compilation = compilation;
                Diagnostics = diagnostics;
            }

            public Compilation Compilation { get; }
            public IImmutableList<Diagnostic> Diagnostics { get; }
            public bool HasErrors => Diagnostics
                .Any(p => p.Severity == DiagnosticSeverity.Error);
        }
    }
}