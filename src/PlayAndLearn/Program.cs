using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using Avalonia.Threading;
using Elmish.Net;
using Elmish.Net.VDom;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using PlayAndLearn.Models;
using PlayAndLearn.Utils;
using static LanguageExt.Prelude;
using Unit = System.Reactive.Unit;

namespace PlayAndLearn
{
    public class App : Application
    {
        public override void Initialize()
        {
            var baseUri = new Uri("resm:PlayAndLearn.App.xaml?assembly=PlayAndLearn");
            new[]
            {
                new Uri("resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default"),
                new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default")
            }
            .Select(source => new StyleInclude(baseUri) { Source = source })
            .ForEach(Styles.Add);
        }
    }

    public class MainWindow : Window
    {
        public MainWindow()
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var iconStream = asm.GetManifestResourceStream(asm.GetName().Name + ".Icon.ico"))
            {
                Icon = new WindowIcon(iconStream);
            }
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var appBuilder = AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .SetupWithoutStarting();

            var proxy = new Proxy();
            var renderLoop = AvaloniaLocator.Current.GetService<IRenderLoop>();
            var requestAnimationFrame = Observable
                .FromEventPattern<EventArgs>(
                    h => renderLoop.Tick += h,
                    h => renderLoop.Tick -= h)
                .Select(_ => Unit.Default);
            ElmishApp.Run(
                requestAnimationFrame,
                Init(),
                Update,
                View,
                Subscribe,
                AvaloniaScheduler.Instance,
                () => proxy.Window);

            var cts = new CancellationTokenSource();
            proxy.WindowChanged.Subscribe(window =>
            {
                window.Show();
                window.Closed += (s, e) => cts.Cancel();
            });
            appBuilder.Instance.Run(cts.Token);
        }

        private class Proxy
        {
            private readonly Subject<Window> windowChanged = new Subject<Window>();
            private Window _window;

            public IObservable<Window> WindowChanged => windowChanged.AsObservable();

            public Window Window
            {
                get => _window;
                set
                {
                    _window = value;
                    windowChanged.OnNext(value);
                }
            }
        }

        private static (State, Cmd<Message>) Init()
        {
            var options = ScriptOptions.Default
                .AddReferences(MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location))
                .AddImports(typeof(PlayerExtensions).Namespace);
//             var code = @"Player.Position = new Position(0, 0);
// Player.Pen = new Pen(1.5, new RGB(0x00, 0xFF, 0xFF));
// var n = 5;
// while (n < 800)
// {
//     Player.Go(n);
//     Player.Rotate(89.5);

//     Player.Pen = Player.Pen.WithHueShift(10.0 / 360);
//     n++;

//     Player.SleepMilliseconds(10);
// }";
            var code = @"Player.SetPenColor(new RGB(0xFF, 0x00, 0xFF));
for (var i = 0; i < 20; i++)
{
    Player.TurnOffPen();
    Player.GoTo(0, -200 + 20 * i);
    Player.TurnOnPen();
    for (int j = 0; j < 60; j++)
    {
        Player.SetPenWeight((j % 5) + 1);
        Player.Rotate(6);
        Player.Go(5);
        Player.ShiftPenColor(0.01);
    }
}";
            var state = new State(
                sceneSize: new Models.Size(400, 400),
                code: code,
                script: None,
                executionState: new ExecutionState.Stopped(Enumerable.Empty<object>()),
                player: Turtle.CreateDefault(),
                previousDragPosition: None,
                lines: ImmutableList<VisualLine>.Empty);
            return Update(new Message.CompileCode(), state);
        }

        private static (State, Cmd<Message>) Update(Message message, State state)
        {
            return message.Match(
                (Message.ChangeSceneSize m) =>
                {
                    var newState = state.With(p => p.SceneSize, m.NewSize);
                    return (newState, Cmd.None<Message>());
                },
                (Message.ChangeCode m) =>
                {
                    var newState = state
                        .With(p => p.Code, m.Code)
                        .With(p => p.Script, None);
                    return Update(new Message.CompileCode(), newState);
                },
                (Message.CompileCode _) =>
                {
                    var parseOptions = CSharpParseOptions.Default
                        .WithKind(SourceCodeKind.Script);
                    var compiledCode = CSharpSyntaxTree
                        .ParseText(state.Code, parseOptions)
                        .ModifyForExecution()
                        .Compile();
                    var newState = state.With(p => p.Script, compiledCode);

                    return (newState, Cmd.None<Message>());
                },
                (Message.StartCodeExecution _) =>
                {
                    return state.ExecutionState
                        .Match(
                            stoppedState =>
                            {
                                return state.Script
                                    .Bind(p => Optional(p.AsT1))
                                    .Match(
                                        compiledCode =>
                                        {
                                            var globals = new UserScriptGlobals { Player = state.Player };
                                            return compiledCode
                                                .Run<IEnumerable<object>>(globals)
                                                .Match(
                                                    o =>
                                                    {
                                                        var newState = state.With(
                                                            p => p.ExecutionState,
                                                            new ExecutionState.Stopped(o));
                                                        return Update(new Message.ContinueCodeExecution(), newState);
                                                    },
                                                    diagnostics => (state, Cmd.None<Message>())); // TODO show errors
                                        },
                                        () => (state, Cmd.None<Message>()));

                            },
                            startedState =>
                            {
                                // That shouldn't happen, but just keep going
                                return (state, Cmd.None<Message>());
                            },
                            pausedState =>
                            {
                                var newState = state.With(
                                    p => p.ExecutionState,
                                    new ExecutionState.Started(pausedState.States, pausedState.State));
                                return Update(new Message.ContinueCodeExecution(), newState);
                            });

                },
                (Message.PauseCodeExecution _) =>
                {
                    var newState = state.ExecutionState
                        .Match(
                            stoppedState => state,
                            startedState => state.With(p => p.ExecutionState, new ExecutionState.Paused(startedState.States, startedState.State)),
                            pausedState => state);
                    return (newState, Cmd.None<Message>());
                },
                (Message.StopCodeExecution _) =>
                {
                    var newState = state.ExecutionState
                        .Match(
                            stoppedState => state,
                            startedState => state.With(p => p.ExecutionState, new ExecutionState.Stopped(startedState.States)),
                            pausedState => state.With(p => p.ExecutionState, new ExecutionState.Stopped(pausedState.States)));
                    return (newState, Cmd.None<Message>());
                },
                (Message.ContinueCodeExecution _) =>
                {
                    (State, Cmd<Message>) AdvanceExecution(IEnumerable<object> executionStates, Option<IEnumerator<object>> executionState)
                    {
                        return executionState
                            .Some(enumerator =>
                            {
                                if (enumerator.MoveNext())
                                {
                                    var newState = state
                                        .With(p => p.ExecutionState, new ExecutionState.Started(executionStates, enumerator))
                                        .With(p => p.Player, (Player)enumerator.Current);
                                    if (state.Player.Pen.IsOn && !state.Player.Position.Equals(newState.Player.Position))
                                    {
                                        var line = new VisualLine(
                                            state.Player.Position,
                                            newState.Player.Position,
                                            state.Player.Pen.Color,
                                            state.Player.Pen.Weight);
                                        newState = newState.With(p => p.Lines, newState.Lines.Add(line));
                                    }
                                    return (newState, Cmd.None<Message>());
                                }
                                else
                                {
                                    enumerator.Dispose();
                                    var newState = state
                                        .With(p => p.ExecutionState, new ExecutionState.Stopped(executionStates));
                                    return (newState, Cmd.None<Message>());
                                }
                            })
                            .None(() =>
                            {
                                var enumerator = executionStates.GetEnumerator();
                                var newState = state
                                    .With(p => p.ExecutionState, new ExecutionState.Started(executionStates, enumerator));
                                return (newState, Cmd.None<Message>());
                            });
                    }

                    return state.ExecutionState
                        .Match(
                            stoppedState => AdvanceExecution(stoppedState.States, None),
                            startedState => AdvanceExecution(startedState.States, Some(startedState.State)),
                            pausedState => AdvanceExecution(pausedState.States, Some(pausedState.State)));
                },
                (Message.ResetPlayerPosition _) =>
                {
                    var newState = state
                        .With(p => p.Player.Position, new Position(0, 0))
                        .With(p => p.Player.Direction, 0);
                    return (newState, Cmd.None<Message>());
                },
                (Message.StartDragPlayer m) =>
                {
                    var newState = state.With(p => p.PreviousDragPosition, m.Position);
                    return (newState, Cmd.None<Message>());
                },
                (Message.DragPlayer m) =>
                {
                    var newState = state.PreviousDragPosition
                        .Some(previousDragPosition => state
                            .With(
                                p => p.Player.Position,
                                state.Player.Position.Add(m.Position.Subtract(previousDragPosition)))
                            .With(p => p.PreviousDragPosition, m.Position)
                        )
                        .None(state);
                    return (newState, Cmd.None<Message>());
                },
                (Message.StopDragPlayer m) =>
                {
                    var newState = state.With(p => p.PreviousDragPosition, None);
                    return (newState, Cmd.None<Message>());
                });
        }

        private static FontFamily FiraCode =
            "resm:PlayAndLearn.Fonts.FiraCode.FiraCode-*.ttf?assembly=PlayAndLearn#Fira Code";

        private static IVDomNode<MainWindow> View(State state, Dispatch<Message> dispatch)
        {
            return VDomNode.Create<MainWindow>()
                .Set(p => p.FontFamily, "Segoe UI Symbol")
                .Set(p => p.Title, "Play and Learn")
                .Set(
                    p => p.Content,
                    VDomNode.Create<Grid>()
                        .SetChildNodes(
                            p => p.ColumnDefinitions,
                            VDomNode.Create<ColumnDefinition>()
                                .Set(p => p.Width, new GridLength(state.SceneSize.Width, GridUnitType.Pixel)),
                            VDomNode.Create<ColumnDefinition>()
                                .Set(p => p.Width, GridLength.Auto),
                            VDomNode.Create<ColumnDefinition>()
                                .Set(p => p.Width, new GridLength(1, GridUnitType.Star)))
                        .SetChildNodes(
                            p => p.Children,
                            VDomNode.Create<Canvas>()
                                .Set(p => p.Background, Brushes.WhiteSmoke)
                                .Set(p => p.MinWidth, 100)
                                .SetChildNodes(p => p.Children, GetCanvasItems(state, dispatch))
                                .Subscribe(p => Observable
                                    .FromEventPattern(
                                        h => p.LayoutUpdated += h,
                                        h => p.LayoutUpdated -= h)
                                    .Select(e => new Models.Size(
                                        (int)Math.Round(p.Bounds.Size.Width),
                                        (int)Math.Round(p.Bounds.Size.Height)))
                                    .Where(size => !size.Equals(state.SceneSize))
                                    .Subscribe(size => dispatch(new Message.ChangeSceneSize(size))))
                                .Subscribe(p => state.PreviousDragPosition
                                    .Some(previousDragPosition => Observable
                                        .FromEventPattern<PointerEventArgs>(
                                            h => p.PointerMoved += h,
                                            h => p.PointerMoved -= h)
                                        .Select(e => e.EventArgs.GetPosition(p.Parent).ToPosition(p.Parent))
                                        .Subscribe(position => dispatch(new Message.DragPlayer(position))))
                                    .None(Disposable.Empty))
                                .Subscribe(p => Observable
                                    .FromEventPattern<PointerReleasedEventArgs>(
                                        h => p.PointerReleased += h,
                                        h => p.PointerReleased -= h)
                                    .Subscribe(_ => dispatch(new Message.StopDragPlayer()))),
                            VDomNode.Create<GridSplitter>()
                                .Attach(Grid.ColumnProperty, 1),
                            VDomNode.Create<DockPanel>()
                                .Attach(Grid.ColumnProperty, 2)
                                .SetChildNodes(
                                    p => p.Children,
                                    VDomNode.Create<WrapPanel>()
                                        .SetChildNodes(p => p.Children, GetScriptButtons(state, dispatch))
                                        .Attach(DockPanel.DockProperty, Dock.Top),
                                    VDomNode.Create<TextBox>()
                                        .Set(p => p.Text, state.Code)
                                        .Set(p => p.FontFamily, FiraCode)
                                        .Set(p => p.TextWrapping, TextWrapping.Wrap)
                                        .Set(p => p.AcceptsReturn, true)
                                        .Subscribe(p => Observable
                                            .FromEventPattern<KeyEventArgs>(
                                                h => p.KeyUp += h,
                                                h => p.KeyUp -= h)
                                            .Subscribe(e => dispatch(new Message.ChangeCode(p.Text)))
                                        )
                                    // VDomNode.Create<TextEditor>()
                                    //     .Set(p => p.Text, state.Code)
                                    //     .Set(p => p.Background, Brushes.Transparent)
                                    //     .Set(p => p.ShowLineNumbers, true)
                                    //     .Set(p => p.TextArea.IndentationStrategy, new CSharpIndentationStrategy())
                                    //     .Set(p => p.FontSize, 30)
                                    //     .Set(
                                    //         p => p.SyntaxHighlighting,
                                    //         HighlightingManager.Instance.GetDefinition("C#"),
                                    //         EqualityComparer.Create((IHighlightingDefinition p) => p.Name))
                                    //     // .Set(p => p.FontFamily, FiraCode)
                                    //     .Subscribe(p => Observable
                                    //         .FromEventPattern<TextInputEventArgs>(
                                    //             h => p.TextArea.TextEntered += h,
                                    //             h => p.TextArea.TextEntered -= h)
                                    //         .Subscribe(e => dispatch(new Message.ChangeCode(p.Text)))
                                    //     )

                                    // VDomNode.Create<RoslynCodeEditor>()
                                    //     .Set(p => p.MinWidth, 100)
                                    //     .Set(p => p.Background, Brushes.Azure)
                                    //     .Attach(Grid.ColumnProperty, 2)
                                    //     .Subscribe(p => Observable
                                    //         // .FromEventPattern<EventHandler, EventArgs>(
                                    //         //     h => p.Initialized += h,
                                    //         //     h => p.Initialized -= h
                                    //         // )
                                    //         .Timer(TimeSpan.FromSeconds(2))
                                    //         .ObserveOn(AvaloniaScheduler.Instance)
                                    //         .Subscribe(_ =>
                                    //         {
                                    //             var host = new RoslynHost(additionalAssemblies: new[]
                                    //             {
                                    //                 Assembly.Load("RoslynPad.Roslyn.Avalonia"),
                                    //                 Assembly.Load("RoslynPad.Editor.Avalonia")
                                    //             });
                                    //             p.Initialize(
                                    //                 host,
                                    //                 new ClassificationHighlightColors(),
                                    //                 workingDirectory: Directory.GetCurrentDirectory(),
                                    //                 documentText: state.Code
                                    //             );
                                    //         })
                                    //     )
                                )
                        )
                );
        }

        private static IEnumerable<IVDomNode> GetCanvasItems(State state, Dispatch<Message> dispatch)
        {
            var center = new Position(state.SceneSize.Width / 2.0, state.SceneSize.Height / 2.0);
            yield return VDomNode.Create<Image>()
                .Set(p => p.ZIndex, 10)
                .Set(p => p.Source, new Bitmap(new MemoryStream(state.Player.IdleCostume)))
                .Set(p => p.Width, state.Player.Size.Width)
                .Set(p => p.Height, state.Player.Size.Height)
                .Set(
                    p => p.RenderTransform,
                    VDomNode.Create<RotateTransform>()
                        .Set(p => p.Angle, 360 - state.Player.Direction))
                .Subscribe(p => Observable
                    .FromEventPattern<PointerPressedEventArgs>(
                        h => p.PointerPressed += h,
                        h => p.PointerPressed -= h)
                    .Select(e => e.EventArgs.GetPosition(p.Parent).ToPosition(p.Parent))
                    .Subscribe(position => dispatch(new Message.StartDragPlayer(position))))
                .Attach(Canvas.LeftProperty, center.X + state.Player.Position.X - state.Player.Size.Width / 2)
                .Attach(Canvas.BottomProperty, center.Y + state.Player.Position.Y - state.Player.Size.Height / 2);

            yield return VDomNode.Create<TextBlock>()
                .Set(p => p.Text, $"X: {state.Player.Position.X:F2} | Y: {state.Player.Position.Y:F2} | ∠ {state.Player.Direction:F2}°")
                .Set(p => p.Foreground, Brushes.Gray)
                .Subscribe(p => Observable
                    .FromEventPattern<RoutedEventArgs>(
                        h => p.DoubleTapped += h,
                        h => p.DoubleTapped -= h)
                    .Subscribe(_ => dispatch(new Message.ResetPlayerPosition())))
                .Attach(Canvas.BottomProperty, 10)
                .Attach(Canvas.RightProperty, 10);

            foreach (var line in state.Lines)
            {
                yield return VDomNode.Create<Line>()
                    .Set(p => p.StartPoint, new Point(center.X + line.P1.X, state.SceneSize.Height - center.Y - line.P1.Y))
                    .Set(p => p.EndPoint, new Point(center.X + line.P2.X, state.SceneSize.Height - center.Y - line.P2.Y))
                    .Set(p => p.Stroke, VDomNode.Create<SolidColorBrush>().Set(p => p.Color, line.Color.ToColor()))
                    .Set(p => p.StrokeThickness, line.Weight)
                    .Set(p => p.ZIndex, 5);
            }
        }

        private static IEnumerable<IVDomNode> GetScriptButtons(State state, Dispatch<Message> dispatch)
        {
            yield return VDomNode.Create<Border>()
                .Set(p => p.BorderThickness, new Thickness(1))
                .Set(p => p.BorderBrush, Brushes.White)
                .Set(p => p.Margin, new Thickness(0, 5))
                .Attach(
                    ToolTip.TipProperty,
                    state.Script
                        .Bind(script => Optional(script.AsT1))
                        .Bind(compiledCode => compiledCode.HasErrors ? Some(compiledCode.Diagnostics) : None)
                        .MatchUnsafe(
                            list => string.Join(Environment.NewLine, list),
                            () => null))
                .Set(
                    p => p.Child,
                    VDomNode.Create<Button>()
                        .Set(p => p.Content, "Run ▶")
                        .Set(p => p.IsEnabled, state.CanExecuteScript() && !state.ExecutionState.IsStarted)
                        .Set(p => p.Foreground, Brushes.GreenYellow)
                        .Subscribe(p => Observable
                            .FromEventPattern<RoutedEventArgs>(
                                h => p.Click += h,
                                h => p.Click -= h
                            )
                            .Subscribe(_ => dispatch(new Message.StartCodeExecution()))));

            yield return VDomNode.Create<Button>()
                .Set(p => p.Content, "Pause ⏸")
                .Set(p => p.IsEnabled, state.ExecutionState.IsStarted)
                .Set(p => p.Foreground, Brushes.LightGoldenrodYellow)
                .Set(p => p.Margin, new Thickness(1, 6))
                .Subscribe(p => Observable
                    .FromEventPattern<RoutedEventArgs>(
                        h => p.Click += h,
                        h => p.Click -= h
                    )
                    .Subscribe(_ => dispatch(new Message.PauseCodeExecution())));

            yield return VDomNode.Create<Button>()
                .Set(p => p.Content, "Stop ■")
                .Set(p => p.IsEnabled, state.ExecutionState.IsStarted || state.ExecutionState.IsPaused)
                .Set(p => p.Foreground, Brushes.IndianRed)
                .Set(p => p.Margin, new Thickness(1, 6))
                .Subscribe(p => Observable
                    .FromEventPattern<RoutedEventArgs>(
                        h => p.Click += h,
                        h => p.Click -= h
                    )
                    .Subscribe(_ => dispatch(new Message.StopCodeExecution())));
        }

        private static Sub<Message> Subscribe(State state)
        {
            if (state.ExecutionState.IsStarted)
            {
                return Sub.Create(
                    "8994debe-794c-4e19-9276-abe669738280",
                    (string key, Dispatch<Message> dispatch) =>
                    {
                        return Observable
                            .Interval(TimeSpan.FromMilliseconds(20))
                            .Subscribe(_ => dispatch(new Message.ContinueCodeExecution()));
                    });
            }
            return Sub.None<Message>();
        }
    }
}
