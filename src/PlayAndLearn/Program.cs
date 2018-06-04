using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Elmish.Net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using PlayAndLearn.Models;
using PlayAndLearn.Utils;

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
            var proxy = new Proxy { Window = new MainWindow() };
            ElmishApp.Run(Init(), Update, View, AvaloniaScheduler.Instance, () => proxy.Window);
            proxy.Window.Show();
            appBuilder.Instance.Run(proxy.Window);
        }

        private class Proxy
        {
            public Window Window { get; set; }
        }

        private static (State, Cmd<Message>) Init()
        {
            var options = ScriptOptions.Default
                .AddReferences(MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location))
                .AddImports(typeof(PlayerExtensions).Namespace);
            var code = @"Player.Position = new Position(0, 0);
Player.Pen = new Pen(1.5, new RGB(0x00, 0xFF, 0xFF));
var n = 5;
while (n < 800)
{
    Player.Go(n);
    Player.Rotate(89.5);

    Player.Pen = Player.Pen.WithHueShift(10.0 / 360);
    n++;

    Player.SleepMilliseconds(10);
}";
            var script = CSharpScript.Create(code, options);
            var state = new State(new Models.Size(400, 400), new Code(script, canExecute: false), Turtle.CreateDefault());
            return (state, Cmd.None<Message>());
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
                    Debug.WriteLine("Change code");
                    var newScript = CSharpScript.Create(m.Code, state.Code.Script.Options, typeof(State));
                    var newState = state.With(p => p.Code, new Code(newScript, canExecute: false));
                    var cmd = Cmd.OfAsync(
                        ct => Task.Run(() => newScript.Compile(ct)),
                        diagnostics =>
                        {
                            var errors = diagnostics
                                .Where(p => p.Severity == DiagnosticSeverity.Error)
                                .ToList();
                            return errors.Count > 0
                                ? (Message)new Message.CompilationError(errors)
                                : new Message.EnableCodeExecution();
                        },
                        e => new Message.CompilationException(e)
                    );
                    return (newState, cmd);
                },
                (Message.EnableCodeExecution _) =>
                {
                    Debug.WriteLine("Enable code execution");
                    var newState = state.With(p => p.Code.CanExecute, true);
                    return (newState, Cmd.None<Message>());
                },
                (Message.CompilationError m) =>
                {
                    Debug.WriteLine("Compilation error: ");
                    m.Errors.ForEach(e => Debug.WriteLine(e));
                    var newState = state.With(p => p.Code.CanExecute, false);
                    return (newState, Cmd.None<Message>());
                },
                (Message.CompilationException m) =>
                {
                    if (m.Exception is OperationCanceledException)
                    {
                        Debug.WriteLine("Compilation canceled");
                        return (state, Cmd.None<Message>());
                    }
                    else
                    {
                        Debug.WriteLine("Compilation exception");
                        var newState = state.With(p => p.Code.CanExecute, false);
                        return (newState, Cmd.None<Message>());
                    }
                },
                (Message.ExecuteCode _) =>
                {
                    Debug.WriteLine("Execute code");
                    // TODO create new (mutable) type for globals
                    // und pass this to the script
                    var result = state.Code.Script.RunAsync(state).Result;
                    var newState = state.With(p => p.Player, (Player)result.ReturnValue);
                    return (newState, Cmd.None<Message>());
                }
            );
        }

        private static IVNode<MainWindow> View(State state, Dispatch<Message> dispatch)
        {
            var center = new Position(state.SceneSize.Width / 2, state.SceneSize.Height / 2);
            return VNode.Create<MainWindow>()
                .Set(p => p.Title, "Play and Learn")
                .Set(
                    p => p.Content,
                    VNode.Create<Grid>()
                        .SetCollection(
                            p => p.ColumnDefinitions,
                            VNode.Create<ColumnDefinition>()
                                .Set(p => p.Width, new GridLength(state.SceneSize.Width, GridUnitType.Pixel)),
                            VNode.Create<ColumnDefinition>()
                                .Set(p => p.Width, GridLength.Auto),
                            VNode.Create<ColumnDefinition>()
                                .Set(p => p.Width, new GridLength(1, GridUnitType.Star))
                        )
                        .SetCollection(
                            p => p.Children,
                            VNode.Create<Canvas>()
                                .Set(p => p.Background, new SolidColorBrush(Colors.WhiteSmoke))
                                .Set(p => p.MinWidth, 100)
                                .SetCollection(
                                    p => p.Children,
                                    VNode.Create<Image>()
                                        .Set(p => p.ZIndex, 10)
                                        .Set(p => p.Source, new Bitmap(new MemoryStream(state.Player.IdleCostume)))
                                        .Set(p => p.Width, state.Player.Size.Width)
                                        .Set(p => p.Height, state.Player.Size.Height)
                                        .Set(p => p.RenderTransform, new RotateTransform(360 - state.Player.Direction))
                                        .Attach(Canvas.LeftProperty, center.X + state.Player.Position.X - state.Player.Size.Width / 2)
                                        .Attach(Canvas.BottomProperty, center.Y + state.Player.Position.Y - state.Player.Size.Height / 2)
                                )
                                .Subscribe(p => Observable
                                    .FromEventPattern(
                                        h => p.LayoutUpdated += h,
                                        h => p.LayoutUpdated -= h
                                    )
                                    .Subscribe(e =>
                                    {
                                        var newSize = new Models.Size(
                                            (int)Math.Round(p.Bounds.Size.Width),
                                            (int)Math.Round(p.Bounds.Size.Height)
                                        );
                                        dispatch(new Message.ChangeSceneSize(newSize));
                                    })
                                ),
                            VNode.Create<GridSplitter>()
                                .Attach(Grid.ColumnProperty, 1),
                            VNode.Create<DockPanel>()
                                .Attach(Grid.ColumnProperty, 2)
                                .SetCollection(
                                    p => p.Children,
                                    VNode.Create<Button>()
                                        .Set(p => p.Content, "Run ▶")
                                        .Set(p => p.IsEnabled, state.Code.CanExecute)
                                        .Set(p => p.Foreground, new SolidColorBrush(Colors.GreenYellow))
                                        .Set(p => p.Margin, new Thickness(10, 5))
                                        .Set(p => p.HorizontalAlignment, HorizontalAlignment.Left)
                                        .Subscribe(p => Observable
                                            .FromEventPattern<RoutedEventArgs>(
                                                h => p.Click += h,
                                                h => p.Click -= h
                                            )
                                            .Subscribe(_ => dispatch(new Message.ExecuteCode()))
                                        )
                                        .Attach(DockPanel.DockProperty, Dock.Top),
                                    VNode.Create<TextBox>()
                                        .Set(p => p.Text, state.Code.Script.Code)
                                        .Set(p => p.TextWrapping, TextWrapping.Wrap)
                                        .Set(p => p.AcceptsReturn, true)
                                        .Subscribe(p => TextBox.TextProperty.Changed
                                            .Where(e => ReferenceEquals(e.Sender, p))
                                            .Subscribe(e => dispatch(new Message.ChangeCode((string)e.NewValue)))
                                        )
                                )
                            // VNode.Create<RoslynCodeEditor>()
                            //     .Set(p => p.MinWidth, 100)
                            //     .Set(p => p.Background, new SolidColorBrush(Colors.Azure))
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
                );
        }
    }
}
