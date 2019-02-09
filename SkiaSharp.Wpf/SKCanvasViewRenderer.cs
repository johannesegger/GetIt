using System;
using System.ComponentModel;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms.Platform.WPF;
using SKFormsView = SkiaSharp.Views.Forms.SKCanvasView;
using SKNativeView = SkiaSharp.Views.WPF.SKElement;
using SKNativePaintSurfaceEventArgs = SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs;

namespace SkiaSharp.Wpf
{
    public abstract class SKCanvasViewRendererBase<TFormsView, TNativeView> : ViewRenderer<TFormsView, TNativeView>
        where TFormsView : SKFormsView
        where TNativeView : SKNativeView
    {

        protected SKCanvasViewRendererBase()
        {
            Initialize();
        }

        private void Initialize()
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<TFormsView> e)
        {
            if (e.OldElement != null)
            {
                var oldController = (ISKCanvasViewController)e.OldElement;

                // unsubscribe from events
                oldController.SurfaceInvalidated -= OnSurfaceInvalidated;
                oldController.GetCanvasSize -= OnGetCanvasSize;
            }

            if (e.NewElement != null)
            {
                var newController = (ISKCanvasViewController)e.NewElement;

                // create the native view
                if (Control == null)
                {
                    var view = CreateNativeControl();
                    view.PaintSurface += OnPaintSurface;
                    SetNativeControl(view);
                }

                // set the initial values
                Control.IgnorePixelScaling = e.NewElement.IgnorePixelScaling;

                // subscribe to events from the user
                newController.SurfaceInvalidated += OnSurfaceInvalidated;
                newController.GetCanvasSize += OnGetCanvasSize;

                // paint for the first time
                OnSurfaceInvalidated(newController, EventArgs.Empty);
            }

            base.OnElementChanged(e);
        }

        protected virtual TNativeView CreateNativeControl()
        {
            return (TNativeView)Activator.CreateInstance(typeof(TNativeView));
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == SKFormsView.IgnorePixelScalingProperty.PropertyName)
            {
                Control.IgnorePixelScaling = Element.IgnorePixelScaling;
            }
        }

        protected override void Dispose(bool disposing)
        {
            // detach all events before disposing
            var controller = (ISKCanvasViewController)Element;
            if (controller != null)
            {
                controller.SurfaceInvalidated -= OnSurfaceInvalidated;
                controller.GetCanvasSize -= OnGetCanvasSize;
            }

            var control = Control;
            if (control != null)
            {
                control.PaintSurface -= OnPaintSurface;
            }

            base.Dispose(disposing);
        }

        private void OnPaintSurface(object sender, SKNativePaintSurfaceEventArgs e)
        {
            var controller = Element as ISKCanvasViewController;

            // the control is being repainted, let the user know
            controller?.OnPaintSurface(new SKPaintSurfaceEventArgs(e.Surface, e.Info));
        }

        private void OnSurfaceInvalidated(object sender, EventArgs eventArgs)
        {
            // repaint the native control
            Control.InvalidateVisual();
        }

        // the user asked for the size
        private void OnGetCanvasSize(object sender, GetPropertyValueEventArgs<SKSize> e)
        {
            e.Value = Control?.CanvasSize ?? SKSize.Empty;
        }
    }

    public class SKCanvasViewRenderer : SKCanvasViewRendererBase<SKFormsView, SKNativeView>
    {
    }
}