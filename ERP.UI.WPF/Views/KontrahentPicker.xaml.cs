using System.Windows;
using System.Windows.Controls;
using ERP.Application.Repositories;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// UserControl do wyboru kontrahenta (kontrahenci_v).
/// </summary>
public partial class KontrahentPicker : UserControl
{
    private KontrahentPickerViewModel? _vm;
    private bool _isUpdatingSelection;

    public KontrahentPicker()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public static readonly DependencyProperty CompanyIdProperty =
        DependencyProperty.Register(nameof(CompanyId), typeof(int?), typeof(KontrahentPicker),
            new PropertyMetadata(null, OnCompanyIdChanged));

    public static readonly DependencyProperty SelectedKontrahentIdProperty =
        DependencyProperty.Register(nameof(SelectedKontrahentId), typeof(int?), typeof(KontrahentPicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIdChanged));

    public static readonly DependencyProperty SelectedKontrahentNazwaProperty =
        DependencyProperty.Register(nameof(SelectedKontrahentNazwa), typeof(string), typeof(KontrahentPicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty SelectedKontrahentEmailProperty =
        DependencyProperty.Register(nameof(SelectedKontrahentEmail), typeof(string), typeof(KontrahentPicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty SelectedKontrahentWalutaProperty =
        DependencyProperty.Register(nameof(SelectedKontrahentWaluta), typeof(string), typeof(KontrahentPicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public int? CompanyId
    {
        get => (int?)GetValue(CompanyIdProperty);
        set => SetValue(CompanyIdProperty, value);
    }

    public int? SelectedKontrahentId
    {
        get => (int?)GetValue(SelectedKontrahentIdProperty);
        set => SetValue(SelectedKontrahentIdProperty, value);
    }

    public string? SelectedKontrahentNazwa
    {
        get => (string?)GetValue(SelectedKontrahentNazwaProperty);
        set => SetValue(SelectedKontrahentNazwaProperty, value);
    }

    public string? SelectedKontrahentEmail
    {
        get => (string?)GetValue(SelectedKontrahentEmailProperty);
        set => SetValue(SelectedKontrahentEmailProperty, value);
    }

    public string? SelectedKontrahentWaluta
    {
        get => (string?)GetValue(SelectedKontrahentWalutaProperty);
        set => SetValue(SelectedKontrahentWalutaProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null) return;
        if (System.Windows.Application.Current is not App app) return;
        var repo = app.GetService<IKontrahenciQueryRepository>();
        var userContext = app.GetService<ERP.UI.WPF.Services.IUserContext>();
        _vm = new KontrahentPickerViewModel(repo, userContext);
        DataContext = _vm;
        _vm.SelectedKontrahentChanged += (_, _) =>
        {
            if (_isUpdatingSelection) return;
            _isUpdatingSelection = true;
            SelectedKontrahentId = _vm.SelectedKontrahent?.Id;
            SelectedKontrahentNazwa = _vm.SelectedKontrahent?.Nazwa;
            SelectedKontrahentEmail = _vm.SelectedKontrahent?.Email;
            SelectedKontrahentWaluta = _vm.SelectedKontrahent?.Waluta;
            _isUpdatingSelection = false;
        };
        _vm.CompanyId = CompanyId;
        _ = _vm.LoadAsync();
        if (SelectedKontrahentId.HasValue)
            _vm.SetSelectionById(SelectedKontrahentId);
    }

    private static void OnCompanyIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KontrahentPicker picker && picker._vm != null)
        {
            picker._vm.CompanyId = ToNullableInt(e.NewValue);
        }
    }

    private static void OnSelectedIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KontrahentPicker picker && picker._vm != null && !picker._isUpdatingSelection)
        {
            picker._vm.SetSelectionById(ToNullableInt(e.NewValue));
        }
    }

    private static int? ToNullableInt(object? value)
    {
        if (value is int intValue)
            return intValue;
        return null;
    }
}
