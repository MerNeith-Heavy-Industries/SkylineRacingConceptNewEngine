using System.Collections;
using System.Collections.Specialized;
using System.Reactive.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.DriverInterface.UI;

public partial class ItemsRepeater : FlexPanel
{
    private IDisposable? _collectionSubscription;

    public ItemsRepeater()
    {
        FlexDirection = YgFlexDirection.Column;
    }

    [Property(OnChangedMethod = nameof(OnItemsSourceChanged))]
    public partial IEnumerable? ItemsSource { get; set; }

    [Property(OnChangedMethod = nameof(OnItemTemplateChanged))]
    public partial ControlTemplate? ItemTemplate { get; set; }

    private partial void OnItemsSourceChanged(IEnumerable? newSource)
    {
        Rebuild();
        if (newSource is INotifyCollectionChanged ncc)
            _collectionSubscription = Observable.Create<NotifyCollectionChangedEventArgs>(obs =>
                {
                    ncc.CollectionChanged += Handler;
                    return () => ncc.CollectionChanged -= Handler;
                    void Handler(object? sender, NotifyCollectionChangedEventArgs e) => obs.OnNext(e);
                })
                .Subscribe(_ => Rebuild());
    }

    private partial void OnItemTemplateChanged(ControlTemplate? newTemplate)
    {
        Rebuild();
    }

    private void Rebuild()
    {
        Children.Clear();

        if (ItemsSource is null || ItemTemplate is null)
            return;

        // ControlTemplate.Build needs an IServiceProvider — pass null for simple cases
        // (the XamlX-compiled factory closure doesn't actually use it for basic templates)
        var sp = AvaloniaXamlLoader.CreateDefaultServiceProvider(this);
        foreach (var item in ItemsSource)
        {
            var built = ItemTemplate.Build(sp);
            if (built is not BindableObject bindable)
                continue;

            // DataContext auto-inherits, but we override it per-item
            bindable.DataContext = item;

            if (built is Visual visual)
                Children.Add(visual);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _collectionSubscription?.Dispose();
    }
}