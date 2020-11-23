namespace global

// see https://stackoverflow.com/a/18275864
type Async =
    static member HandleCancellation(work, onCancel, ?cancellationToken) =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            let ccont e = onCancel e cont econt ccont
            Async.StartWithContinuations(work, cont, econt, ccont, ?cancellationToken=cancellationToken)
        )
