module UserScript

open System
open System.Text.RegularExpressions
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Scripting
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Scripting
open GameLib.Data.Global

module private Seq =
    let ofType<'TResult> items =
        System.Linq.Enumerable.OfType<'TResult>(items)

module private Async =
    let map fn a = async {
        let! result = a
        return fn result
    }

[<CLIMutable>]
type ScriptGlobals = {
    Player: Player
}

type private UserScriptRewriter() =
    inherit CSharpSyntaxRewriter()

    let mutable numberOfRewrites = 0
    member __.NumberOfRewrites with get() = numberOfRewrites

    override __.VisitExpressionStatement(node) =
        match node.Expression with
        | :? InvocationExpressionSyntax as n ->
            let rec actualIdentifier (expression: ExpressionSyntax) =
                expression.ChildNodes()
                |> Seq.tryHead
                |> Option.bind (function
                | :? IdentifierNameSyntax as n -> Some n.Identifier.Text
                | :? MemberAccessExpressionSyntax as n -> actualIdentifier n
                | _ -> None)
            match actualIdentifier n.Expression with
            | Some identifier when identifier = "Player" ->
                numberOfRewrites <- numberOfRewrites + 1
                SyntaxFactory
                    .YieldStatement(
                        SyntaxKind.YieldReturnStatement,
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(identifier),
                            n))
                    .WithSemicolonToken(node.SemicolonToken)
                :> SyntaxNode
            | _ -> node :> SyntaxNode
        | _ -> node :> SyntaxNode

let rec private getFullTypeName (t: Type) =
    let rec getFullTypeNameWithoutGenerics (t: Type) =
        if not <| isNull t.DeclaringType
        then
            sprintf "%s.%s"
                (getFullTypeNameWithoutGenerics t.DeclaringType)
                t.Name
        else
            sprintf "%s.%s" t.Namespace (Regex.Replace(t.Name, @"`\d+$", ""))

    if t.IsGenericParameter then t.Name
    elif not t.IsGenericType then getFullTypeNameWithoutGenerics t
    else
        let name = getFullTypeNameWithoutGenerics t
        let genericArguments =
            t.GetGenericArguments()
            |> Seq.map getFullTypeName
            |> String.concat ", "
        sprintf "%s<%s>" name genericArguments

let rewriteForExecution (syntaxTree: SyntaxTree) =
    let rewriter = UserScriptRewriter()
    let root = rewriter.Visit(syntaxTree.GetRoot()) :?> CompilationUnitSyntax
    let method =
        SyntaxFactory
            .MethodDeclaration(
                SyntaxFactory.ParseTypeName(
                    getFullTypeName typeof<Player seq>),
                SyntaxFactory.Identifier("Run")
            )
            .WithBody(
                SyntaxFactory.Block(
                    [
                        yield!
                            root.Members
                            |> Seq.ofType<GlobalStatementSyntax>
                            |> Seq.map (fun p -> p.Statement)
                        if rewriter.NumberOfRewrites = 0
                        then
                            yield
                                SyntaxFactory.YieldStatement(
                                    SyntaxKind.YieldBreakStatement)
                                :> StatementSyntax
                    ]
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

let run (metadataReferences: MetadataReference seq) (usingDirectives: string seq) (globals: ScriptGlobals) (tree: SyntaxTree) = async {
    let options =
        ScriptOptions.Default
            .WithReferences(metadataReferences)
            .WithImports(usingDirectives)
    let! ct = Async.CancellationToken
    let guid = Guid.NewGuid()
    printfn "Started %O" guid
    ct.Register(fun () -> printfn "Cancelled %O" guid) |> ignore
    let! getResult =
        CSharpScript.EvaluateAsync<Player seq>(tree.ToString(), options, globals, typeof<ScriptGlobals>, ct)
        |> Async.AwaitTask
        |> Async.map (Seq.toList >> Some)
        |> Async.StartChild
    let! timeout =
        Async.Sleep 10_000
        |> Async.map (fun () -> raise (OperationCanceledException()))
        |> Async.StartChild
    let! result =
        Async.Choice [getResult; timeout]
        |> Async.map Option.get
    printfn "Finished %O" guid    
    return result
}