module GetIt.Sample.TSP.Main

open System
open GetIt

type City =
    {
        Id: int
        Position: Position
    }

type Individual =
    {
        Tour: City list
        Fitness: float
    }

[<EntryPoint>]
let main argv =
    Game.ShowSceneAndAddTurtle()

    Turtle.Say ("TSP solver", 1.)

    let problem = GetIt.Sample.TSP.Samples.ulysses16

    let rand = Random()

    let minX = Seq.minBy fst problem.Coordinates |> fst
    let maxX = Seq.maxBy fst problem.Coordinates |> fst
    let minY = Seq.minBy snd problem.Coordinates |> snd
    let maxY = Seq.maxBy snd problem.Coordinates |> snd
    let bounds =
        let border = 50.
        {
            Position = { X = Game.SceneBounds.Left + border; Y = Game.SceneBounds.Bottom + border }
            Size = { Width = Game.SceneBounds.Size.Width - 2. * border; Height = Game.SceneBounds.Size.Height - 2. * border }
        }
    let scenePositions =
        problem.Coordinates
        |> List.map (fun (x, y) ->
            let xPos = (x - minX) / (maxX - minX) * bounds.Size.Width + bounds.Left
            let yPos = (y - minY) / (maxY - minY) * bounds.Size.Height + bounds.Bottom
            { X = xPos; Y = yPos }
        )

    scenePositions
    |> List.iter (fun position ->
        let player = Game.AddPlayer(PlayerData.Create(SvgImage.CreateCircle(RGBAColors.darkRed, 5.)))
        player.MoveTo(position)
    )

    let sceneCityLookup =
        scenePositions
        |> List.mapi (fun index p -> index + 1, { Id = index + 1; Position = p })
        |> Map.ofList

    let cities =
        problem.Coordinates
        |> List.mapi (fun index (x, y) -> { Id = index + 1; Position = { X = x; Y = y } })

    let cityLookup =
        cities
        |> Seq.map (fun city -> city.Id, city)
        |> Map.ofSeq

    let mutable iterationDelay = TimeSpan.FromMilliseconds 500.
    use d1 = Turtle.OnKeyDown (KeyboardKey.Down, fun _ -> iterationDelay <- iterationDelay * 2.)
    use d2 = Turtle.OnKeyDown (KeyboardKey.Up, fun _ -> iterationDelay <- iterationDelay / 2.)
    
    let mutable drawGlobalOptimum = false
    use d3 = Turtle.OnKeyDown (KeyboardKey.G, fun _ -> drawGlobalOptimum <- not drawGlobalOptimum)

    let getDistance cityA cityB =
        let dx = cityA.Position.X - cityB.Position.X
        let dy = cityA.Position.Y - cityB.Position.Y
        Math.Sqrt(dx * dx + dy * dy)

    let closeTour tour =
        List.append tour [ List.head tour ]

    let calculateFitness individual =
        let distance =
            individual
            |> closeTour
            |> List.windowed 2
            |> List.sumBy (fun b -> getDistance (List.item 0 b) (List.item 1 b))
        -distance

    let normalizeTour tour =
        [
            yield! List.skipWhile (fun city -> city.Id <> 1) tour
            yield! List.takeWhile (fun city -> city.Id <> 1) tour
        ]

    let drawTour tour =
        Turtle.TurnOffPen ()
        tour
        |> normalizeTour
        |> List.map (fun city -> Map.find city.Id sceneCityLookup)
        |> closeTour
        |> List.iter (fun city ->
            Turtle.MoveTo city.Position
            Turtle.TurnOnPen ()
        )

    // http://fssnip.net/L
    let shuffle a =
        let swap (a: _[]) x y =
            let tmp = a.[x]
            a.[x] <- a.[y]
            a.[y] <- tmp
        Array.iteri (fun i _ -> swap a i (rand.Next(i, Array.length a))) a
        a

    let tournamentSelect individuals size =
        individuals
        |> List.toArray
        |> shuffle
        |> Array.take size
        |> Seq.maxBy (fun p -> p.Fitness)

    let orderCrossover p1 p2 =
        let startIdx = rand.Next(List.length p1.Tour)
        let endIdx = rand.Next(List.length p1.Tour)
        if startIdx <= endIdx then
            let p1Tour =
                p1.Tour
                |> List.skip startIdx
                |> List.take (endIdx - startIdx)
            let p2RemainingTour =
                p2.Tour
                |> List.except p1Tour
            let tour =
                p2RemainingTour
                |> List.take startIdx
                |> fun l -> List.append l p1Tour
                |> fun l -> List.append l (List.skip startIdx p2RemainingTour)
            { Tour = tour; Fitness = calculateFitness tour }
        else
            let p1Tour1 =
                p1.Tour
                |> List.take (endIdx + 1)
            let p1Tour2 =
                p1.Tour
                |> List.skip startIdx
            let p2RemainingTour =
                p2.Tour
                |> List.except p1Tour1
                |> List.except p1Tour2
            let tour =
                p1Tour1
                |> fun l -> List.append l p2RemainingTour
                |> fun l -> List.append l p1Tour2
            { Tour = tour; Fitness = calculateFitness tour }

    let replaceAt idx item list =
        [
            yield! List.take idx list
            yield item
            yield! List.skip (idx + 1) list
        ]

    let twoOptChange p probability =
        if rand.NextDouble() < probability then
            let idx1 = rand.Next(List.length p.Tour)
            let idx2 = (idx1 + 1) % (List.length p.Tour)
            let tour =
                p.Tour
                |> replaceAt idx1 (List.item idx2 p.Tour)
                |> replaceAt idx2 (List.item idx1 p.Tour)
            { Tour = tour; Fitness = calculateFitness tour }
        else p

    let optimalTour =
        problem.OptimalTour
        |> List.map (fun i -> Map.find i cityLookup)
    let globalOptimum = calculateFitness optimalTour

    let populationSize = 500
    let iterations = 1000
    let mutationProbability = 0.05

    let initialPopulation =
        List.init populationSize (fun _ ->
            let tour = cities |> List.toArray |> shuffle |> Array.toList
            { Tour = tour; Fitness = calculateFitness tour }
        )

    (initialPopulation, Seq.init iterations ((+) 1))
    ||> Seq.scan (fun population iteration ->
        List.init populationSize (fun _ ->
            let tournamentSize = 3
            let parent1 = tournamentSelect population tournamentSize
            let parent2 = tournamentSelect population tournamentSize
            let child = orderCrossover parent1 parent2
            twoOptChange child mutationProbability
        )
    )
    |> Seq.iteri (fun index population ->
        do
            use x = Game.BatchCommands()
            Game.ClearScene()
            let fittest =
                population
                |> Seq.maxBy(fun individual -> individual.Fitness)
            let text =
                [
                    sprintf "Iteration: %d" index
                    sprintf "Min distance: %f" (Math.Abs fittest.Fitness)
                    sprintf "Global optimum: %f" (Math.Abs globalOptimum)
                ]
                |> String.concat Environment.NewLine
            Turtle.Say text

            Turtle.SetPenColor RGBAColors.orange
            drawTour fittest.Tour
            if drawGlobalOptimum then
                Turtle.SetPenColor RGBAColors.green
                drawTour optimalTour

        Turtle.Sleep iterationDelay
    )

    0