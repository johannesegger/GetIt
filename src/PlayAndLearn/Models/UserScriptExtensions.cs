using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PlayAndLearn.Utils;

namespace PlayAndLearn.Models
{
    public static class ScriptExtensions
    {
        public static Either<IImmutableList<Diagnostic>, T> Run<T>(
            this UserScript.CompiledCode code,
            object globals)
        {
            using (var stream = new MemoryStream())
            {
                var result = code.Compilation.Emit(stream);
                if (result.Success)
                {
                    var asm = Assembly.Load(stream.ToArray()); // TODO load in separate AppDomain?
                    var method = code.Compilation
                        .GetEntryPoint(CancellationToken.None)
                        .FindInAssembly(asm);
                    var fn = (Func<object[], Task<object>>)method
                        .CreateDelegate(typeof(Func<object[], Task<object>>));
                    var submission = new object[2]; // submission[1] is reserved
                    submission[0] = globals;
                    return (T)fn(submission).Result; // TODO await
                }
                else
                {
                    return result.Diagnostics;
                }
            }
        }

        public static UserScript.CompiledCode Compile(this UserScript.ParsedCode code)
        {
            Compilation CompileTree(SyntaxTree tree)
            {
                return CSharpCompilation.CreateScriptCompilation(
                    "PlayAndLearn.Game",
                    tree,
                    new[]
                    {
                        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
                        // MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location),
                        MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
                    },
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        usings: new[] { typeof(PlayerExtensions).Namespace }
                    ),
                    globalsType: typeof(UserScriptGlobals)
                );
            }

            var compilation = CompileTree(code.ModifiedTree);
            var diagnostics = CompileTree(code.OriginalTree).GetDiagnostics();
            return new UserScript.CompiledCode(compilation, diagnostics);
        }

        public static UserScript.ParsedCode ModifyForExecution(this SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)new CodeParser().Visit(tree.GetRoot());
            var method = SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.ParseTypeName(
                        typeof(System.Collections.Generic.IEnumerable<object>).GetFullName()),
                    SyntaxFactory.Identifier("Run")
                )
                .WithBody(
                    SyntaxFactory.Block(
                        root
                            .Members
                            .OfType<GlobalStatementSyntax>()
                            .Select(p => p.Statement)
                    )
                );
            var newRoot = root
                .WithMembers(
                    SyntaxFactory.List(
                        root
                            .Members
                            .Where(p => !(p is GlobalStatementSyntax))
                            .Concat(new MemberDeclarationSyntax[]
                            {
                                method,
                                SyntaxFactory.GlobalStatement(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Run")),
                                        SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)))
                            })))
                .NormalizeWhitespace();
            return new UserScript.ParsedCode(tree, tree.WithRootAndOptions(newRoot, tree.Options));
        }

        private class CodeParser : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                if (node.Expression is InvocationExpressionSyntax n)
                {
                    var actualIdentifier = n.Expression
                        .ChildNodes()
                        .OfType<IdentifierNameSyntax>()
                        .FirstOrDefault()
                        ?.Identifier
                        .Text;
                    if (Equals(actualIdentifier, nameof(State.Player)))
                    {
                        return SyntaxFactory
                            .YieldStatement(
                                SyntaxKind.YieldReturnStatement,
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(actualIdentifier),
                                    n))
                            .WithSemicolonToken(node.SemicolonToken);
                    }
                }
                return node;
            }
        }
    }
}