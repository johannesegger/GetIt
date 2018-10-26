using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Logging.Serilog;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Elmish.Net;
using Elmish.Net.VDom;
using GetIt.Models;
using GetIt.Utils;
using LanguageExt;
using static Elmish.Net.ElmishApp<GetIt.Models.Message>;
using static LanguageExt.Prelude;
using Unit = System.Reactive.Unit;

namespace GetIt
{
    public static class Game
    {
        private static readonly ISubject<Message> dispatchSubject = new Subject<Message>();
        public static State State { get; private set; }

        public static void ShowScene()
        {
            using (var signal = new ManualResetEventSlim())
            {
                var uiThread = new Thread(() =>
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
                        signal.Set();
                    });
                    appBuilder.Instance.Run(cts.Token);
                    Environment.Exit(0); // shut everything down when the UI thread exits
                });
                uiThread.IsBackground = false;
                uiThread.Start();
                signal.Wait();
            }
        }

        private static (State, Cmd<Message>) Init()
        {
            State = new State(
                new Models.Rectangle(new Position(-300, -200), new Models.Size(600, 400)),
                ImmutableList<Player>.Empty,
                ImmutableList<PenLine>.Empty,
                MouseState.Empty,
                KeyboardState.Empty,
                ImmutableList<Models.EventHandler>.Empty);
            var cmd = Cmd.None<Message>();
            return (State, cmd);
        }

        private static (State, Cmd<Message>) Update(Message message, State state)
        {
            var (newState, cmd) = UpdateCore(message, state);
            State = newState;
            return (State, cmd);
        }

        private static (State, Cmd<Message>) UpdateCore(Message message, State state)
        {
            State updatePlayer(State s, Guid playerId, Func<Player, Player> fn)
            {
                return s.With(
                    p => p.Players,
                    s.Players
                        .Select(player => player.Id == playerId ? fn(player) : player)
                        .ToImmutableList());
            }
            return message.Match(
                (Message.SetSceneSize m) =>
                {
                    var bounds = new Models.Rectangle(new Position(-m.Size.Width / 2, -m.Size.Height / 2), m.Size);
                    var newState = state.With(p => p.SceneBounds, bounds);
                    return (newState, Cmd.None<Message>());
                },
                (Message.SetMousePosition m) =>
                {
                    var newState = state.With(p => p.Mouse.Position, m.Position);
                    return (newState, Cmd.None<Message>());
                },
                (Message.SetKeyboardKeyPressed m) =>
                {
                    var newState = state.With(p => p.Keyboard.KeysPressed, state.Keyboard.KeysPressed.Add(m.Key));
                    return (newState, Cmd.None<Message>());
                },
                (Message.SetKeyboardKeyReleased m) =>
                {
                    var newState = state.With(p => p.Keyboard.KeysPressed, state.Keyboard.KeysPressed.Remove(m.Key));
                    return (newState, Cmd.None<Message>());
                },
                (Message.SetPosition m) =>
                {
                    var currentPlayer = state.Players.Single(p => p.Id == m.PlayerId);
                    var newState = updatePlayer(state, m.PlayerId, player => player.With(p => p.Position, m.Position));
                    if (currentPlayer.Pen.IsOn)
                    {
                        var line = new PenLine(currentPlayer.Position, m.Position, currentPlayer.Pen.Weight, currentPlayer.Pen.Color);
                        newState = newState.With(p => p.PenLines, state.PenLines.Add(line));
                    }
                    return (newState, Cmd.None<Message>());
                },
                (Message.SetDirection m) =>
                {
                    var newState = updatePlayer(state, m.PlayerId, player => player.With(p => p.Direction, m.Angle));
                    return (newState, Cmd.None<Message>());
                },
                (Message.SetSpeechBubble m) =>
                {
                    var newState = updatePlayer(state, m.PlayerId, player => player.With(p => p.SpeechBubble, m.SpeechBubble));
                    return (newState, Cmd.None<Message>());
                },
                (Message.UpdateAnswer m) =>
                {
                    var currentPlayer = state.Players.Single(p => p.Id == m.PlayerId);
                    var newSpeechBubble = currentPlayer.SpeechBubble
                        .Some(speechBubble => speechBubble
                            .Match<Option<SpeechBubble>>(
                                (SpeechBubble.Say say) => say,
                                (SpeechBubble.Ask ask) => ask.With(p => p.Answer, m.Answer)))
                        .None(currentPlayer.SpeechBubble);
                    var newState = updatePlayer(state, m.PlayerId, player => player.With(p => p.SpeechBubble, newSpeechBubble));
                    return (newState, Cmd.None<Message>());
                },
                (Message.ApplyAnswer m) =>
                {
                    var currentPlayer = state.Players.Single(p => p.Id == m.PlayerId);
                    var answerHandler = currentPlayer.SpeechBubble
                        .Some(speechBubble => speechBubble
                            .Match(
                                (SpeechBubble.Say say) => _ => {},
                                (SpeechBubble.Ask ask) => ask.AnswerHandler))
                        .None(_ => {});
                    var newState = updatePlayer(state, m.PlayerId, player => player.With(p => p.SpeechBubble, None));
                    // Have `Game.State` set before executing `answerHandler`
                    return Update(new Message.ExecuteAction(() => answerHandler(m.Answer)), newState);
                },
                (Message.SetPen m) =>
                {
                    var newState = updatePlayer(state, m.PlayerId, player => player.With(p => p.Pen, m.Pen));
                    return (newState, Cmd.None<Message>());
                },
                (Message.SetSizeFactor m) =>
                {
                    var newState = updatePlayer(state, m.PlayerId, player => player.With(p => p.SizeFactor, m.SizeFactor));
                    return (newState, Cmd.None<Message>());
                },
                (Message.AddPlayer m) =>
                {
                    var newState = state.With(p => p.Players, state.Players.Add(m.Player));
                    return (newState, Cmd.None<Message>());
                },
                (Message.RemovePlayer m) =>
                {
                    var newState = state.With(p => p.Players, state.Players.Where(p => p.Id != m.PlayerId));
                    return (newState, Cmd.None<Message>());
                },
                (Message.ClearScene m) =>
                {
                    var newState = state.With(p => p.PenLines, state.PenLines.Clear());
                    return (newState, Cmd.None<Message>());
                },
                (Message.AddEventHandler m) =>
                {
                    var newState = state.With(p => p.EventHandlers, state.EventHandlers.Add(m.Handler));
                    return (newState, Cmd.None<Message>());
                },
                (Message.RemoveEventHandler m) =>
                {
                    var newState = state.With(p => p.EventHandlers, state.EventHandlers.Remove(m.Handler));
                    return (newState, Cmd.None<Message>());
                },
                (Message.TriggerEvent m) =>
                {
                    var newState = state;
                    var cmd = Cmd.None<Message>();
                    if (m.Event is Event.KeyDown keyDownEvent)
                    {
                        var msg = new Message.SetKeyboardKeyPressed(keyDownEvent.Key);
                        var (keyDownState, keyDownCmd) = Update(msg, newState);
                        newState = keyDownState;
                        cmd = Cmd.Batch(cmd, keyDownCmd);
                    }
                    else if (m.Event is Event.KeyUp keyUpEvent)
                    {
                        var msg = new Message.SetKeyboardKeyReleased(keyUpEvent.Key);
                        var (keyUpState, keyUpCmd) = Update(msg, newState);
                        newState = keyUpState;
                        cmd = Cmd.Batch(cmd, keyUpCmd);
                    }
                    TaskPoolScheduler.Default
                        .Schedule(() =>
                            state.EventHandlers
                                .ForEach(p => p.Handle(m.Event)));
                    return (newState, Cmd.None<Message>());
                },
                (Message.ExecuteAction m) =>
                {
                    m.Action();
                    return (state, Cmd.None<Message>());
                });
        }

        private static Lazy<WindowIcon> Icon = new Lazy<WindowIcon>(() =>
        {
            using (var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GetIt.icon.png"))
            {
                return new WindowIcon(iconStream);
            }
        });

        private static Point GetScreenCoordinate(State state, Position position)
        {
            return new Point(
                position.X - state.SceneBounds.Left,
                state.SceneBounds.Top - position.Y);
        }

        private static IVDomNode<Window, Message> View(State state, Dispatch<Message> dispatch)
        {
            Position getDevicePosition(Window window, IPointerDevice pointerDevice)
            {
                var screenPoint = pointerDevice.GetPosition((IVisual)window.Content);
                return new Position(
                    state.SceneBounds.Left + screenPoint.X,
                    state.SceneBounds.Top - screenPoint.Y);
            }

            Position getPositionOfDefaultDevice(Window window)
            {
                return getDevicePosition(window, ((IInputRoot)window).MouseDevice);
            }

            return VDomNode<Window>()
                .Set(p => p.FontFamily, "Segoe UI Symbol")
                .Set(p => p.Title, "GetIt")
                .Set(p => p.Icon, Icon.Value, EqualityComparer.Create((WindowIcon icon) => 0))
                .Subscribe(window => window
                    .ObserveEvent(InputElement.TappedEvent)
                    .Select(p => new Message.TriggerEvent(new Event.ClickScene(getPositionOfDefaultDevice(window)))))
                .Subscribe(window => Observable
                    .Merge(
                        window
                            .ObserveEvent(InputElement.PointerMovedEvent)
                            .Select(p => getDevicePosition(window, p.Device)),
                        Observable
                            .FromEventPattern<VisualTreeAttachmentEventArgs>(
                                h => ((IVisual)window.Content).AttachedToVisualTree += h,
                                h => ((IVisual)window.Content).AttachedToVisualTree -= h)
                            .Select(p => getPositionOfDefaultDevice(window)))
                    .Select(p => new Message.SetMousePosition(p)))
                .Subscribe(window => window
                    .ObserveEvent(InputElement.KeyDownEvent)
                    .Choose(e => e.Key.TryGetKeyboardKey())
                    .Select(key => new Message.TriggerEvent(new Event.KeyDown(key))))
                .Subscribe(window => window
                    .ObserveEvent(InputElement.KeyUpEvent)
                    .Choose(e => e.Key.TryGetKeyboardKey())
                    .Select(key => new Message.TriggerEvent(new Event.KeyUp(key))))
                .Set(p => p.Content, VDomNode<Canvas>()
                    .SetChildNodes(p => p.Children, GetSceneChildren(state, dispatch))
                    .Subscribe(p => Observable
                        .FromEventPattern(
                            h => p.LayoutUpdated += h,
                            h => p.LayoutUpdated -= h
                        )
                        .Select(_ => new Message.SetSceneSize(new Models.Size(p.Bounds.Width, p.Bounds.Height)))));
        }

        private static IEnumerable<IVDomNode<Message>> GetSceneChildren(State state, Dispatch<Message> dispatch)
        {
            foreach (var player in state.Players)
            {
                yield return VDomNode<ContentControl>()
                    // .Set(p => p.Background, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.LightCoral))
                    .Set(p => p.ZIndex, 10)
                    .Set(p => p.Width, player.Size.Width)
                    .Set(p => p.Height, player.Size.Height)
                    .Set(p => p.RenderTransform, VDomNode<RotateTransform>()
                        .Set(p => p.Angle, 360 - player.Direction.Value))
                    .Set(p => p.Content, GetPlayerView(player)
                        .Set(p => p.RenderTransform, VDomNode<ScaleTransform>()
                            .Set(p => p.ScaleX, player.Size.Width / player.Costume.Size.Width)
                            .Set(p => p.ScaleY, player.Size.Height / player.Costume.Size.Height)))
                    .Attach(Canvas.LeftProperty, player.Bounds.Left - state.SceneBounds.Left)
                    .Attach(Canvas.BottomProperty, player.Bounds.Bottom - state.SceneBounds.Bottom);

                var speechBubbleContent =
                    player.SpeechBubble
                        .Some(speechBubble => speechBubble.Match<IVDomNode<IControl, Message>>(
                            (SpeechBubble.Say sayBubble) => VDomNode<TextBlock>()
                                .Set(p => p.MaxWidth, 300)
                                .Set(p => p.HorizontalAlignment, HorizontalAlignment.Center)
                                .Set(p => p.FontSize, 15)
                                .Set(p => p.TextWrapping, TextWrapping.Wrap)
                                .Set(p => p.Margin, new Thickness(10, 5))
                                .Set(p => p.Text, sayBubble.Text),
                            (SpeechBubble.Ask askBubble) => VDomNode<DockPanel>()
                                .Set(p => p.Width, 300)
                                .SetChildNodes(
                                    p => p.Children,
                                    VDomNode<TextBox>()
                                        .Set(p => p.Text, askBubble.Answer)
                                        .Set(p => p.FontSize, 15)
                                        .Set(p => p.Margin, new Thickness(10, 5))
                                        .Set(p => p.Watermark, "Answer")
                                        .Set(p => p.Foreground, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.SteelBlue))
                                        .Attach(DockPanel.DockProperty, Dock.Bottom)
                                        .Subscribe(element => Observable
                                            .FromEventPattern<TextInputEventArgs>(
                                                h => element.TextInput += h,
                                                h => element.TextInput -= h)
                                            .Select(_ => new Message.UpdateAnswer(player.Id, element.Text)))
                                        .Subscribe(element => Observable
                                            .FromEventPattern<KeyEventArgs>(
                                                h => element.KeyDown += h,
                                                h => element.KeyDown -= h)
                                            .Where(p => p.EventArgs.Key == Key.Enter)
                                            .Select(_ => new Message.ApplyAnswer(player.Id, element.Text))),
                                    VDomNode<TextBlock>()
                                        .Set(p => p.HorizontalAlignment, HorizontalAlignment.Center)
                                        .Set(p => p.FontSize, 15)
                                        .Set(p => p.TextWrapping, TextWrapping.Wrap)
                                        .Set(p => p.Margin, new Thickness(10, 5))
                                        .Set(p => p.Text, askBubble.Question)
                                        .Set(p => p.Foreground, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.SteelBlue)))))
                        .None(VDomNode<TextBlock>());

                yield return VDomNode<Grid>()
                    .Set(p => p.IsVisible, player.SpeechBubble.IsSome)
                    .Set(p => p.ZIndex, 7)
                    .Attach(Canvas.LeftProperty, player.Bounds.Right - state.SceneBounds.Left + 20)
                    .Attach(Canvas.BottomProperty, player.Bounds.Top - state.SceneBounds.Bottom)
                    .SetChildNodes(
                        p => p.RowDefinitions,
                        VDomNode<RowDefinition>().Set(p => p.Height, new GridLength(1, GridUnitType.Star)),
                        VDomNode<RowDefinition>().Set(p => p.Height, GridLength.Auto))
                    // TODO that's a bit ugly
                    .Subscribe(p => Observable
                        .FromEventPattern(
                            h => p.LayoutUpdated += h,
                            h => p.LayoutUpdated -= h
                        )
                        .Do(_ => p.RenderTransform = new TranslateTransform(-p.Bounds.Width / 2, 0))
                        .Select(_ => (Message)null)
                        .IgnoreElements()
                    )
                    .SetChildNodes(
                        p => p.Children,
                        VDomNode<Border>()
                            .Set(p => p.Background, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.WhiteSmoke))
                            .Set(p => p.CornerRadius, 5)
                            .Set(p => p.BorderThickness, 5)
                            .Set(p => p.BorderBrush, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.Black))
                            .Set(p => p.Child, speechBubbleContent),
                        VDomNode<Path>()
                            .Set(p => p.Fill, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.Black))
                            .Set(p => p.HorizontalAlignment, HorizontalAlignment.Center)
                            .Set(p => p.Data,
                                // TODO simplify?
                                new VDomNode<StreamGeometry, Message>(
                                    () => PathGeometry.Parse("M0,0 L15,0 0,15"),
                                    ImmutableList<IVDomNodeProperty<StreamGeometry, Message>>.Empty,
                                    _ => Sub.None<Message>()))
                            .Attach(Grid.RowProperty, 1));
            }

            foreach (var line in state.PenLines)
            {
                yield return VDomNode<Line>()
                    .Set(p => p.StartPoint, GetScreenCoordinate(state, line.Start))
                    .Set(p => p.EndPoint, GetScreenCoordinate(state, line.End))
                    .Set(p => p.Stroke, VDomNode<SolidColorBrush>().Set(p => p.Color, line.Color.ToAvaloniaColor()))
                    .Set(p => p.StrokeThickness, line.Weight)
                    .Set(p => p.ZIndex, 5);
            }

            yield return VDomNode<WrapPanel>()
                .Attach(Canvas.LeftProperty, 0)
                .Attach(Canvas.BottomProperty, 0)
                .SetChildNodes(p => p.Children, VDomNode<DockPanel>()
                    .SetChildNodes(p => p.Children, GetPlayerInfo(state)));
        }

        private static IVDomNode<Panel, Message> GetPlayerView(Player player)
        {
            return VDomNode<Canvas>()
                // .Set(p => p.Background, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.LightCoral))
                .Set(p => p.Width, player.Costume.Size.Width)
                .Set(p => p.Height, player.Costume.Size.Height)
                .Subscribe(p => p
                    .ObserveEvent(InputElement.TappedEvent)
                    .Select(_ => new Message.TriggerEvent(new Event.ClickPlayer(player.Id))))
                .Subscribe(p => p
                    .ObserveEvent(InputElement.PointerEnterEvent)
                    .Select(_ => new Message.TriggerEvent(new Event.MouseEnterPlayer(player.Id))))
                .SetChildNodes(p => p.Children, player.Costume.Paths
                    .Select(path => VDomNode<Path>()
                        .Set(p => p.Fill, VDomNode<SolidColorBrush>()
                            .Set(p => p.Color, path.Fill.ToAvaloniaColor()))
                        .Set(p => p.Data,
                            // TODO simplify?
                            new VDomNode<StreamGeometry, Message>(
                                () => PathGeometry.Parse(path.Data),
                                ImmutableList<IVDomNodeProperty<StreamGeometry, Message>>.Empty,
                                _ => Sub.None<Message>()))));
        }

        private static IEnumerable<IVDomNode<Message>> GetPlayerInfo(State state)
        {
            foreach (var player in state.Players)
            {
                var boxSize = new Models.Size(30, 30);
                var size = player.Costume.Size.Scale(boxSize);
                yield return VDomNode<DockPanel>()
                    .SetChildNodes(p => p.Children,
                        VDomNode<ContentControl>()
                            // .Set(p => p.Background, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.LightCoral))
                            .Set(p => p.Width, boxSize.Width)
                            .Set(p => p.Height, boxSize.Height)
                            .Set(p => p.Content, GetPlayerView(player)
                                .Set(p => p.RenderTransform, VDomNode<ScaleTransform>()
                                    .Set(p => p.ScaleX, size.Width / player.Costume.Size.Width)
                                    .Set(p => p.ScaleY, size.Height / player.Costume.Size.Height)))
                            .Set(p => p.Margin, new Thickness(10)),
                        VDomNode<TextBlock>()
                            .Set(p => p.VerticalAlignment, VerticalAlignment.Center)
                            .Set(p => p.Margin, new Thickness(10))
                            .Set(p => p.Text, $"X: {player.Position.X:F2} | Y: {player.Position.Y:F2} | ∠ {player.Direction.Value:F2}°"));
            }
        }

        private static Sub<Message> Subscribe(State state)
        {
            return new Sub<Message>(
                "8994debe-794c-4e19-9276-abe669738280",
                (scheduler, dispatch) => dispatchSubject.Subscribe(p => dispatch(p)));
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

        internal static void DispatchMessageAndWaitForUpdate(Message message)
        {
            dispatchSubject.OnNext(message);
        }

        internal static IDisposable AddEventHandler(Models.EventHandler handler)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.AddEventHandler(handler));
            return Disposable.Create(() => Game.DispatchMessageAndWaitForUpdate(new Message.RemoveEventHandler(handler)));
        }

        private static PlayerOnScene AddPlayer(Player player, Action<PlayerOnScene> fn)
        {
            DispatchMessageAndWaitForUpdate(new Message.AddPlayer(player));
            var p = new PlayerOnScene(player.Id);
            TaskPoolScheduler.Default.Schedule(() => fn(p));
            return p;
        }

        public static PlayerOnScene AddPlayer(Costume playerCostume)
        {
            return AddPlayer(Player.Create(playerCostume.Size, playerCostume), _ => {});
        }

        public static PlayerOnScene AddPlayer(Costume playerCostume, Action<PlayerOnScene> fn)
        {
            return AddPlayer(Player.Create(playerCostume.Size, playerCostume), fn);
        }

        public static PlayerOnScene AddPlayer(Models.Size playerSize, Costume playerCostume, Action<PlayerOnScene> fn)
        {
            return AddPlayer(Player.Create(playerSize, playerCostume), fn);
        }

        public static void ShowSceneAndAddTurtle()
        {
            ShowScene();
            Turtle.Default = AddPlayer(Turtle.DefaultPlayer, _ => {});
        }

        public static void Sleep(double durationInMilliseconds)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(durationInMilliseconds));
        }

        public static void ClearScene()
        {
            DispatchMessageAndWaitForUpdate(new Message.ClearScene());
        }

        public static Position WaitForMouseClick()
        {
            using (var signal = new ManualResetEventSlim())
            {
                var position = Position.Zero;
                var handler = new Models.EventHandler.ClickScene(p => { position = p; signal.Set(); });
                using (AddEventHandler(handler))
                {
                    signal.Wait();
                    return position;
                }
            }
        }

        private static KeyboardKey WaitForKeyDown(Option<KeyboardKey> key)
        {
            using (var signal = new ManualResetEventSlim())
            {
                var keyboardKey = (KeyboardKey)(-1);
                var handler = new Models.EventHandler.KeyDown(key, p => { keyboardKey = p; signal.Set(); });
                using (AddEventHandler(handler))
                {
                    signal.Wait();
                    return keyboardKey;
                }
            }
        }

        public static void WaitForKeyDown(KeyboardKey key) => WaitForKeyDown(Some(key));
        public static KeyboardKey WaitForAnyKeyDown() => WaitForKeyDown(None);

        public static bool IsKeyDown(KeyboardKey key)
        {
            return State.Keyboard.KeysPressed.Contains(key);
        }
        public static bool IsAnyKeyDown()
        {
            return !State.Keyboard.KeysPressed.IsEmpty();
        }
    }
}
