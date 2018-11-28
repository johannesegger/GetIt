using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Validation;

namespace GetIt.CodeGeneration
{
    public class StaticifyGenerator : ICodeGenerator
    {
        private readonly string className;
        private readonly string instanceName;

        public StaticifyGenerator(AttributeData attributeData)
        {
            Requires.NotNull(attributeData, nameof(attributeData));

            className = (string)attributeData.ConstructorArguments[0].Value;
            instanceName = (string)attributeData.ConstructorArguments[1].Value;
        }

        public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(
            TransformationContext context,
            IProgress<Diagnostic> progress,
            CancellationToken cancellationToken)
        {
            var applyToClass = (ClassDeclarationSyntax)context.ProcessingNode;

            var type = SyntaxFactory.ClassDeclaration(className)
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                        SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                .AddMembers(GetMembers(applyToClass).ToArray());

            return Task.FromResult<SyntaxList<MemberDeclarationSyntax>>(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(type));
        }

        private IEnumerable<MemberDeclarationSyntax> GetMembers(ClassDeclarationSyntax applyToClass)
        {
            var extensionMethods = applyToClass.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(token => token.IsKind(SyntaxKind.PublicKeyword)))
                .Where(m => m.Modifiers.Any(token => token.IsKind(SyntaxKind.StaticKeyword)))
                .Where(m => m.ParameterList.Parameters.Count > 0 && m.ParameterList.Parameters[0].Modifiers.Any(token => token.IsKind(SyntaxKind.ThisKeyword)));
            foreach (var member in extensionMethods)
            {
                yield return SyntaxFactory
                    .MethodDeclaration(member.ReturnType, member.Identifier)
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(member.ParameterList
                        .WithParameters(SyntaxFactory
                            .SeparatedList(member.ParameterList.Parameters.Skip(1))))
                    .WithExpressionBody(
                        SyntaxFactory.ArrowExpressionClause(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(this.instanceName),
                                    SyntaxFactory.IdentifierName(member.Identifier)),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        member.ParameterList.Parameters
                                            .Skip(1)
                                            .Select(p => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier)))
                                    ))
                            )
                        )
                    )
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [CodeGenerationAttribute(typeof(StaticifyGenerator))]
    [Conditional("CodeGeneration")]
    public class StaticifyAttribute : Attribute
    {
        public StaticifyAttribute(string className, string instanceName)
        {
            Requires.NotNullOrEmpty(className, nameof(className));
            Requires.NotNullOrEmpty(instanceName, nameof(instanceName));

            ClassName = className;
            InstanceName = instanceName;
        }

        public string ClassName { get; }
        public string InstanceName { get; }
    }
}
