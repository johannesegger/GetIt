namespace global

open System.Runtime.CompilerServices

[<assembly: InternalsVisibleTo("GetIt.Controller")>]
[<assembly: InternalsVisibleTo("GetIt.Test")>]
do ()
