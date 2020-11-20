using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace GetIt.UI
{
    // see https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/march/foundations-writing-more-efficient-itemscontrols
    internal class PenLines : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource",
                typeof(ObservableCollection<PenLineViewModel>),
                typeof(PenLines),
                new PropertyMetadata(OnItemsSourceChanged));

        public ObservableCollection<PenLineViewModel> ItemsSource
        {
            set { SetValue(ItemsSourceProperty, value); }
            get { return (ObservableCollection<PenLineViewModel>)GetValue(ItemsSourceProperty); }
        }

        static void OnItemsSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as PenLines).OnItemsSourceChanged(args);
        }

        void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
        {
            if (args.OldValue is ObservableCollection<PenLineViewModel> oldCollection)
            {
                oldCollection.CollectionChanged -= OnCollectionChanged;
            }

            if (args.NewValue is ObservableCollection<PenLineViewModel> newCollection)
            {
                newCollection.CollectionChanged += OnCollectionChanged;
            }

            InvalidateVisual();
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            InvalidateVisual();
        }

        private static readonly System.Windows.Media.Pen pen = new System.Windows.Media.Pen(Brushes.YellowGreen, 2);
        static PenLines()
        {
            pen.Freeze();
        }
        protected override void OnRender(DrawingContext dc)
        {
            if (ItemsSource == null)
            {
                return;
            }
            foreach (PenLineViewModel penLine in ItemsSource)
            {
                dc.DrawLine(
                    pen,
                    new Point(penLine.X1, penLine.Y1),
                    new Point(penLine.X2, penLine.Y2)
                );
            }
        }
    }
}
