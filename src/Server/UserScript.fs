module UserScript

open System
open System.Text.RegularExpressions
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Scripting
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Scripting
open GameLib.Instruction

module private Seq =
    let ofType<'TResult> items =
        System.Linq.Enumerable.OfType<'TResult>(items)

type private CodeParser() =
    inherit CSharpSyntaxRewriter()

    override __.VisitExpressionStatement(node) =
        match node.Expression with
        | :? InvocationExpressionSyntax as n ->
            let actualIdentifier =
                n.Expression.ChildNodes()
                |> Seq.ofType<IdentifierNameSyntax>
                |> Seq.tryHead
                |> Option.map (fun n -> n.Identifier.Text)
            match actualIdentifier with
            | Some identifier when identifier = "Player" ->
                SyntaxFactory
                    .YieldStatement(SyntaxKind.YieldReturnStatement, n)
                    .WithSemicolonToken(node.SemicolonToken)
                :> SyntaxNode
            | _ -> node :> SyntaxNode
        | _ -> node :> SyntaxNode

let rec private getFullTypeName (t: Type) =
    if t.IsGenericParameter then t.Name
    elif not t.IsGenericType then t.FullName
    else
        let name = sprintf "%s.%s" t.Namespace (Regex.Replace(t.Name, @"`\d+$", ""))
        let genericArguments =
            t.GetGenericArguments()
            |> Seq.map getFullTypeName
            |> String.concat ", "
        sprintf "%s<%s>" name genericArguments

let rewriteForExecution (syntaxTree: SyntaxTree) =
    let root = CodeParser().Visit(syntaxTree.GetRoot()) :?> CompilationUnitSyntax
    let method =
        SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName(
                    getFullTypeName typeof<GameInstruction seq>),
                SyntaxFactory.Identifier("Run")
            )
            .WithBody(
                SyntaxFactory.Block(
                    root.Members
                    |> Seq.ofType<GlobalStatementSyntax>
                    |> Seq.map (fun p -> p.Statement)
                )
            )
    let newRoot =
        root
            .WithMembers(
                SyntaxFactory.List(seq {
                    yield!
                        root.Members
                        |> Seq.filter (fun p -> p :? GlobalStatementSyntax |> not)
                    yield method :> MemberDeclarationSyntax
                    yield 
                        SyntaxFactory.GlobalStatement(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Run")),
                                SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)))
                        :> MemberDeclarationSyntax
                })
            )
            .NormalizeWhitespace()

    syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options)
let run (metadataReferences: MetadataReference seq) (usingDirectives: string seq) (tree: SyntaxTree) = async {
    let options =
        ScriptOptions.Default
            .WithReferences(metadataReferences)
            .WithImports(usingDirectives)
    return! CSharpScript.EvaluateAsync<GameInstruction seq>(tree.ToString(), options) |> Async.AwaitTask
}