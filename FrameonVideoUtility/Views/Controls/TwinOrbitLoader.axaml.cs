using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace FrameonVideoUtility.Views.Controls;

public partial class TwinOrbitLoader : UserControl
{
    private readonly DispatcherTimer _animationTimer;
    private readonly RotateTransform _outerTransform = new();
    private readonly RotateTransform _innerTransform = new();
    private readonly Stopwatch _animationClock = new();

    public TwinOrbitLoader()
    {
        InitializeComponent();

        OuterOrbit.RenderTransform = _outerTransform;
        InnerOrbit.RenderTransform = _innerTransform;

        _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        _animationTimer.Tick += (_, _) => Animate();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _animationClock.Restart();
        _animationTimer.Start();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _animationTimer.Stop();
        _animationClock.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void Animate()
    {
        double seconds = _animationClock.Elapsed.TotalSeconds;

        _outerTransform.Angle = (seconds * 150) % 360;
        _innerTransform.Angle = (360 - (seconds * 220) % 360) % 360;
    }
}
