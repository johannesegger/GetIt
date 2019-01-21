﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
using Avalonia.Controls.Primitives;
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
using GetIt.Internal;
using LanguageExt;
using static Elmish.Net.ElmishApp<GetIt.Internal.Message>;
using static LanguageExt.Prelude;
using Unit = System.Reactive.Unit;

namespace GetIt
{
    /// <summary>
    /// Defines methods to setup a game, add players, register globals events and more.
    /// </summary>
    public static class Game
    {
        private static readonly ISubject<Message> dispatchSubject = new Subject<Message>();
        internal static State State { get; private set; }

        /// <summary>
        /// The bounds of the scene.
        /// </summary>
        public static Rectangle SceneBounds => State.SceneBounds;

        /// <summary>
        /// The current position of the mouse.
        /// </summary>
        public static Position MousePosition => State.Mouse.Position;

        /// <summary>
        /// Initializes and shows an empty scene with no players on it.
        /// </summary>
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
                    var renderTimer = AvaloniaLocator.Current.GetService<IRenderTimer>();
                    var requestAnimationFrame = Observable
                        .FromEvent<TimeSpan>(
                            h => renderTimer.Tick += h,
                            h => renderTimer.Tick -= h)
                        .Select(_ => Unit.Default);

                    Observable
                        .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                            h => KeyboardDevice.Instance.PropertyChanged += h,
                            h => KeyboardDevice.Instance.PropertyChanged -= h
                        )
                        .Where(p => p.EventArgs.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
                        .Select(_ => KeyboardDevice.Instance.FocusedElement)
                        .Where(p => p == null)
                        .CombineLatest(proxy.WindowChanged.Where(p => p != null), (_, window) => window)
                        .Subscribe(window => FocusManager.Instance.Focus(window));

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
                uiThread.Name = "Avalonia UI";
                uiThread.IsBackground = false;
                uiThread.Start();
                signal.Wait();
            }
        }

        private static (State, Cmd<Message>) Init()
        {
            State = new State(
                new Rectangle(new Position(-300, -200), new Size(600, 400)),
                ImmutableList<Player>.Empty,
                ImmutableList<PenLine>.Empty,
                MouseState.Empty,
                KeyboardState.Empty,
                ImmutableList<EventHandler>.Empty);
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
                    var bounds = new Rectangle(new Position(-m.Size.Width / 2, -m.Size.Height / 2), m.Size);
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
                (Message.NextCostume m) =>
                {
                    var newState = updatePlayer(
                        state,
                        m.PlayerId,
                        player => player.With(
                            p => p.CostumeIndex,
                            (player.CostumeIndex + 1) % player.Costumes.Count));
                    return (newState, Cmd.None<Message>());
                },
                (Message.AddPlayer m) =>
                {
                    var newState = state.With(p => p.Players, state.Players.Add(m.Player));
                    return (newState, Cmd.None<Message>());
                },
                (Message.RemovePlayer m) =>
                {
                    var newState = state.With(p => p.Players, state.Players.Where(p => p.Id != m.PlayerId).ToImmutableList());
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
                    .ObserveEvent(InputElement.PointerReleasedEvent)
                    .Select(p =>
                    {
                        var eventData = new MouseClickEvent(getPositionOfDefaultDevice(window), p.MouseButton.ToMouseButton());
                        return new Message.TriggerEvent(new Event.ClickScene(eventData));
                    }))
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
                .Set(p => p.Content, VDomNode<DockPanel>()
                    .SetChildNodes(
                        p => p.Children,
                        VDomNode<ScrollViewer>()
                            .Attach(DockPanel.DockProperty, Dock.Bottom)
                            .Set(p => p.HorizontalScrollBarVisibility, ScrollBarVisibility.Auto)
                            .Set(p => p.Background, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.LightGray))
                            .Set(p => p.Content, VDomNode<StackPanel>()
                                .Set(p => p.Orientation, Orientation.Horizontal)
                                .SetChildNodes(p => p.Children, GetPlayerInfo(state))),
                        VDomNode<Canvas>()
                            .Set(p => p.ClipToBounds, true)
                            .SetChildNodes(p => p.Children, GetSceneChildren(state, dispatch))
                            .Subscribe(p => Observable
                                .FromEventPattern(
                                    h => p.LayoutUpdated += h,
                                    h => p.LayoutUpdated -= h
                                )
                                .Select(_ => new Message.SetSceneSize(new Size(p.Bounds.Width, p.Bounds.Height))))));
        }

        private static IEnumerable<IVDomNode<Message>> GetSceneChildren(State state, Dispatch<Message> dispatch)
        {
            yield return VDomNode<Canvas>()
                .Attach(Canvas.LeftProperty, 0)
                .Attach(Canvas.BottomProperty, 0)
                .Set(p => p.ZIndex, 10)
                .SetChildNodes(p => p.Children, GetPlayersView(state, dispatch));

            yield return VDomNode<Canvas>()
                .Set(p => p.ZIndex, 5)
                .SetChildNodes(
                    p => p.Children,
                    state.PenLines.Select(line =>
                        VDomNode<Line>()
                            .Set(p => p.StartPoint, GetScreenCoordinate(state, line.Start))
                            .Set(p => p.EndPoint, GetScreenCoordinate(state, line.End))
                            .Set(p => p.Stroke, VDomNode<SolidColorBrush>().Set(p => p.Color, line.Color.ToAvaloniaColor()))
                            .Set(p => p.StrokeThickness, line.Weight)));
        }

        private static IEnumerable<IVDomNode<Message>> GetPlayersView(State state, Dispatch<Message> dispatch)
        {
            foreach (var player in state.Players)
            {
                yield return VDomNode<Canvas>()
                    .Attach(Canvas.LeftProperty, 0)
                    .Attach(Canvas.BottomProperty, 0)
                    .SetChildNodes(p => p.Children, GetFullPlayerView(player, state, dispatch));
            }
        }

        private static IEnumerable<IVDomNode<Message>> GetFullPlayerView(Player player, State state, Dispatch<Message> dispatch)
        {
            yield return VDomNode<ContentControl>()
                // .Set(p => p.Background, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.LightCoral))
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
                    
            foreach (var speechBubble in player.SpeechBubble)
            {
                yield return GetSpeechBubbleContent(player, state, speechBubble);
            }
        }

        private static IVDomNode<Panel, Message> GetSpeechBubbleContent(Player player, State state, SpeechBubble speechBubble)
        {
            var textColor = VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.SteelBlue);
            var content = speechBubble.Match<IVDomNode<IControl, Message>>(
                (SpeechBubble.Say sayBubble) => VDomNode<TextBlock>()
                    .Set(p => p.MaxWidth, 300)
                    .Set(p => p.HorizontalAlignment, HorizontalAlignment.Center)
                    .Set(p => p.FontSize, 15)
                    .Set(p => p.Foreground, textColor)
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
                            .Set(p => p.Foreground, textColor)
                            .Attach(DockPanel.DockProperty, Dock.Bottom)
                            .Subscribe(element => Observable
                                .FromEventPattern<TextInputEventArgs>(
                                    h => element.TextInput += h,
                                    h => element.TextInput -= h)
                                .Select(_ => new Message.ApplyAnswer(player.Id, element.Text)))
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
                            .Set(p => p.Foreground, textColor)));

            return VDomNode<Grid>()
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
                        .Set(p => p.CornerRadius, new CornerRadius(5))
                        .Set(p => p.BorderThickness, new Thickness(5))
                        .Set(p => p.BorderBrush, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.Black))
                        .Set(p => p.Child, content),
                    VDomNode<Path>()
                        .Set(p => p.Fill, VDomNode<SolidColorBrush>().Set(p => p.Color, Colors.Black))
                        .Set(p => p.HorizontalAlignment, HorizontalAlignment.Center)
                        .Set(p => p.Data, new VStreamGeometry<Message>("M0,0 L15,0 0,15"))
                        .Attach(Grid.RowProperty, 1));
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
                            .Set(p => p.Color, path.FillColor.ToAvaloniaColor()))
                        .Set(p => p.Data, new VStreamGeometry<Message>(path.Data))));
        }

        private static IEnumerable<IVDomNode<Message>> GetPlayerInfo(State state)
        {
            foreach (var player in state.Players)
            {
                var boxSize = new Size(30, 30);
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

        internal static IDisposable AddEventHandler(EventHandler handler)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.AddEventHandler(handler));
            return Disposable.Create(() => Game.DispatchMessageAndWaitForUpdate(new Message.RemoveEventHandler(handler)));
        }

        /// <summary>
        /// Adds a player to the scene and calls a method to control the player.
        /// The method runs on a task pool thread so that multiple players can be controlled in parallel.
        /// </summary>
        /// <param name="player">The definition of the player that should be added.</param>
        /// <param name="run">The method that is used to control the player.</param>
        /// <returns>The added player.</returns>
        public static PlayerOnScene AddPlayer(Player player, Action<PlayerOnScene> run)
        {
            DispatchMessageAndWaitForUpdate(new Message.AddPlayer(player));
            var p = new PlayerOnScene(player.Id);
            TaskPoolScheduler.Default.Schedule(() => run(p));
            return p;
        }

        /// <summary>
        /// Adds a player to the scene.
        /// </summary>
        /// <param name="player">The definition of the player that should be added.</param>
        /// <returns>The added player.</returns>
        public static PlayerOnScene AddPlayer(Player player)
        {
            return AddPlayer(player, _ => {});
        }

        /// <summary>
        /// Initializes and shows an empty scene and adds the default player to it.
        /// </summary>
        public static void ShowSceneAndAddTurtle()
        {
            ShowScene();
            Turtle.Default = AddPlayer(Turtle.DefaultPlayer, _ => {});
        }

        /// <summary>
        /// Clears all drawings from the scene.
        /// </summary>
        public static void ClearScene()
        {
            DispatchMessageAndWaitForUpdate(new Message.ClearScene());
        }

        /// <summary>
        /// Pauses execution until the mouse clicks at the scene.
        /// </summary>
        /// <returns>The position of the mouse click.</returns>
        public static MouseClickEvent WaitForMouseClick()
        {
            using (var signal = new ManualResetEventSlim())
            {
                var result = default(MouseClickEvent);
                var handler = new EventHandler.ClickScene(r => { result = r; signal.Set(); });
                using (AddEventHandler(handler))
                {
                    signal.Wait();
                    return result;
                }
            }
        }

        private static KeyboardKey WaitForKeyDown(Option<KeyboardKey> key)
        {
            using (var signal = new ManualResetEventSlim())
            {
                var keyboardKey = (KeyboardKey)(-1);
                var handler = new EventHandler.KeyDown(key, p => { keyboardKey = p; signal.Set(); });
                using (AddEventHandler(handler))
                {
                    signal.Wait();
                    return keyboardKey;
                }
            }
        }

        /// <summary>
        /// Pauses execution until a specific keyboard key is pressed.
        /// </summary>
        /// <param name="key">The keyboard key to wait for.</param>
        public static void WaitForKeyDown(KeyboardKey key) => WaitForKeyDown(Some(key));

        /// <summary>
        /// Pauses execution until any keyboard key is pressed.
        /// </summary>
        /// <returns>The keyboard key that is pressed.</returns>
        public static KeyboardKey WaitForAnyKeyDown() => WaitForKeyDown(None);

        /// <summary>
        /// Checks whether a given keyboard key is pressed.
        /// </summary>
        /// <param name="key">The keyboard key.</param>
        /// <returns>True, if the keyboard key is pressed, otherwise false.</returns>
        public static bool IsKeyDown(KeyboardKey key)
        {
            return State.Keyboard.KeysPressed.Contains(key);
        }

        /// <summary>
        /// Checks whether any keyboard key is pressed.
        /// </summary>
        /// <returns>True, if any keyboard key is pressed, otherwise false.</returns>
        public static bool IsAnyKeyDown()
        {
            return !State.Keyboard.KeysPressed.IsEmpty();
        }
    }
}
