using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using IUserContext = ERP.UI.WPF.Services.IUserContext;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Repositories;
using ERP.Infrastructure.Services;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku ofert
/// </summary>
public class OffersViewModel : ViewModelBase
{
    private readonly IOfferService _offerService;
    private readonly IOrderMainService _orderMainService;
    private readonly ProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserContext _userContext;
    private OfferDto? _selectedOffer;
    private OfferPositionDto? _selectedOfferPosition;
    private string _searchText = string.Empty;
    private CollectionViewSource _offersViewSource;
    
    public OffersViewModel(
        IOfferService offerService,
        IOrderMainService orderMainService,
        ProductRepository productRepository, 
        ICustomerRepository customerRepository,
        IUserContext userContext)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _orderMainService = orderMainService ?? throw new ArgumentNullException(nameof(orderMainService));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        
        Offers = new ObservableCollection<OfferDto>();
        OfferPositions = new ObservableCollection<OfferPositionDto>();
        
        _offersViewSource = new CollectionViewSource { Source = Offers };
        _offersViewSource.View.Filter = FilterOffers;
        
        LoadOffersCommand = new RelayCommand(async () => await LoadOffersAsync());
        AddOfferCommand = new RelayCommand(async () => await AddOfferAsync());
        EditOfferCommand = new RelayCommand(() => EditOffer(), () => SelectedOffer != null);
        DeleteOfferCommand = new RelayCommand(async () => await DeleteOfferAsync(), () => SelectedOffer != null);
        ChangeStatusCommand = new RelayCommand(async () => await ChangeStatusAsync(), () => SelectedOffer != null);
        CreateOrderFromOfferCommand = new RelayCommand(async () => await CreateOrderFromOfferAsync(), () => SelectedOffer != null);
        AddPositionCommand = new RelayCommand(async () => await AddPositionAsync(), () => SelectedOffer != null);
        EditPositionCommand = new RelayCommand(() => EditPosition(), () => SelectedOfferPosition != null);
        DeletePositionCommand = new RelayCommand(async () => await DeletePositionAsync(), () => SelectedOfferPosition != null);
        
        // Przyciski nad browseem ofert
        PrintOfferPlCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Print oferta PL - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        PrintOfferEngCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Print oferta ENG - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        SendEmailCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Send email - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        CopyToNewOfferCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do nowej oferty - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        CopyToFpfCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do FPF - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        CopyToFpfZalCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do FPFzal. - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        CopyToOrderCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do zlecenia - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        CopyToFvCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do FV - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        
        // Automatyczne ładowanie przy starcie
        _ = LoadOffersAsync();
    }

    public ObservableCollection<OfferDto> Offers { get; }
    public ObservableCollection<OfferPositionDto> OfferPositions { get; }
    
    public ICollectionView FilteredOffers => _offersViewSource.View;
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredOffers.Refresh();
            }
        }
    }

    public OfferDto? SelectedOffer
    {
        get => _selectedOffer;
        set
        {
            if (_selectedOffer != value)
            {
                _selectedOffer = value;
                OnPropertyChanged();
                if (EditOfferCommand is RelayCommand editCmd)
                    editCmd.RaiseCanExecuteChanged();
                if (DeleteOfferCommand is RelayCommand deleteCmd)
                    deleteCmd.RaiseCanExecuteChanged();
                if (ChangeStatusCommand is RelayCommand changeStatusCmd)
                    changeStatusCmd.RaiseCanExecuteChanged();
                if (CreateOrderFromOfferCommand is RelayCommand createOrderCmd)
                    createOrderCmd.RaiseCanExecuteChanged();
                if (AddPositionCommand is RelayCommand addPosCmd)
                    addPosCmd.RaiseCanExecuteChanged();
                if (PrintOfferPlCommand is RelayCommand printPlCmd)
                    printPlCmd.RaiseCanExecuteChanged();
                if (PrintOfferEngCommand is RelayCommand printEngCmd)
                    printEngCmd.RaiseCanExecuteChanged();
                if (SendEmailCommand is RelayCommand sendEmailCmd)
                    sendEmailCmd.RaiseCanExecuteChanged();
                if (CopyToNewOfferCommand is RelayCommand copyNewCmd)
                    copyNewCmd.RaiseCanExecuteChanged();
                if (CopyToFpfCommand is RelayCommand copyFpfCmd)
                    copyFpfCmd.RaiseCanExecuteChanged();
                if (CopyToFpfZalCommand is RelayCommand copyFpfZalCmd)
                    copyFpfZalCmd.RaiseCanExecuteChanged();
                if (CopyToOrderCommand is RelayCommand copyOrderCmd)
                    copyOrderCmd.RaiseCanExecuteChanged();
                if (CopyToFvCommand is RelayCommand copyFvCmd)
                    copyFvCmd.RaiseCanExecuteChanged();
                if (value != null)
                {
                    _ = LoadOfferPositionsAsync(value.Id);
                }
                else
                {
                    OfferPositions.Clear();
                }
            }
        }
    }

    public OfferPositionDto? SelectedOfferPosition
    {
        get => _selectedOfferPosition;
        set
        {
            if (_selectedOfferPosition != value)
            {
                _selectedOfferPosition = value;
                OnPropertyChanged();
                if (EditPositionCommand is RelayCommand editCmd)
                    editCmd.RaiseCanExecuteChanged();
                if (DeletePositionCommand is RelayCommand deleteCmd)
                    deleteCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand LoadOffersCommand { get; }
    public ICommand AddOfferCommand { get; }
    public ICommand EditOfferCommand { get; }
    public ICommand DeleteOfferCommand { get; }
    public ICommand ChangeStatusCommand { get; }
    public ICommand CreateOrderFromOfferCommand { get; }
    public ICommand AddPositionCommand { get; }
    public ICommand EditPositionCommand { get; }
    public ICommand DeletePositionCommand { get; }
    public ICommand PrintOfferPlCommand { get; }
    public ICommand PrintOfferEngCommand { get; }
    public ICommand SendEmailCommand { get; }
    public ICommand CopyToNewOfferCommand { get; }
    public ICommand CopyToFpfCommand { get; }
    public ICommand CopyToFpfZalCommand { get; }
    public ICommand CopyToOrderCommand { get; }
    public ICommand CopyToFvCommand { get; }

    private async Task LoadOffersAsync()
    {
        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                System.Windows.MessageBox.Show(
                    "Brak wybranej firmy. Wybierz firmę przed załadowaniem ofert.",
                    "Brak firmy",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var offers = await _offerService.GetByCompanyIdAsync(_userContext.CompanyId.Value);
            Offers.Clear();
            foreach (var offer in offers)
            {
                Offers.Add(MapToDto(offer));
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Błąd podczas ładowania ofert: {ex.Message}\n\n{ex.GetType().Name}\n\nStack trace:\n{ex.StackTrace}";
            System.Windows.MessageBox.Show(errorMessage, 
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadOfferPositionsAsync(int offerId)
    {
        try
        {
            var positions = await _offerService.GetPositionsByOfferIdAsync(offerId);
            OfferPositions.Clear();
            foreach (var position in positions)
            {
                OfferPositions.Add(MapToDto(position));
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd podczas ładowania pozycji oferty: {ex.Message}", 
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static OfferDto MapToDto(Offer offer)
    {
        // Konwersja daty Clarion (liczba dni od 28.12.1800) na format dd/MM/yyyy
        string? formattedDate = null;
        if (offer.OfferDate.HasValue)
        {
            try
            {
                // Data bazowa Clarion: 28 grudnia 1800
                var baseDate = new DateTime(1800, 12, 28);
                var offerDateTime = baseDate.AddDays(offer.OfferDate.Value);
                formattedDate = offerDateTime.ToString("dd/MM/yyyy");
            }
            catch
            {
                // Jeśli konwersja się nie powiedzie, zostawiamy datę jako pustą lub wartość numeryczną
                formattedDate = $"Błąd: {offer.OfferDate.Value}";
            }
        }
        
        return new OfferDto
        {
            Id = offer.Id,
            CompanyId = offer.CompanyId,
            ForProforma = offer.ForProforma,
            ForOrder = offer.ForOrder,
            OfferDate = offer.OfferDate,
            FormattedOfferDate = formattedDate,
            OfferNumber = offer.OfferNumber,
            CustomerId = offer.CustomerId,
            CustomerName = offer.CustomerName,
            CustomerStreet = offer.CustomerStreet,
            CustomerPostalCode = offer.CustomerPostalCode,
            CustomerCity = offer.CustomerCity,
            CustomerCountry = offer.CustomerCountry,
            CustomerNip = offer.CustomerNip,
            CustomerEmail = offer.CustomerEmail,
            RecipientName = offer.CustomerName, // Na razie używamy tego samego pola, można później rozdzielić
            Currency = offer.Currency,
            TotalPrice = offer.TotalPrice,
            VatRate = offer.VatRate,
            TotalVat = offer.TotalVat,
            TotalBrutto = offer.TotalBrutto,
            OfferNotes = offer.OfferNotes,
            AdditionalData = offer.AdditionalData,
            Operator = offer.Operator,
            TradeNotes = offer.TradeNotes,
            ForInvoice = offer.ForInvoice,
            History = offer.History,
            Status = offer.Status.ToString(),
            CreatedAt = offer.CreatedAt,
            UpdatedAt = offer.UpdatedAt
        };
    }
    
    private bool FilterOffers(object obj)
    {
        if (obj is not OfferDto offer)
            return false;
        
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;
        
        var searchTextLower = SearchText.ToLowerInvariant();
        
        // Wyszukiwanie po wszystkich kolumnach
        return (offer.Id.ToString().Contains(searchTextLower)) ||
               (offer.OfferNumber?.ToString().Contains(searchTextLower) ?? false) ||
               (offer.FormattedOfferDate?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (offer.CustomerName?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (offer.Currency?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (offer.TotalBrutto?.ToString().Contains(searchTextLower) ?? false) ||
               (offer.Operator?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (offer.CustomerStreet?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (offer.CustomerCity?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (offer.CustomerNip?.ToLowerInvariant().Contains(searchTextLower) ?? false);
    }

    private static OfferPositionDto MapToDto(OfferPosition position)
    {
        return new OfferPositionDto
        {
            Id = position.Id,
            CompanyId = position.CompanyId,
            OfferId = position.OfferId,
            ProductId = position.ProductId,
            SupplierId = position.SupplierId,
            ProductCode = position.ProductCode,
            Name = position.Name,
            NameEng = position.NameEng,
            Unit = position.Unit,
            UnitEng = position.UnitEng,
            Quantity = position.Quantity,
            Price = position.Price,
            Discount = position.Discount,
            PriceAfterDiscount = position.PriceAfterDiscount,
            PriceAfterDiscountAndQuantity = position.PriceAfterDiscountAndQuantity,
            VatRate = position.VatRate,
            Vat = position.Vat,
            PriceBrutto = position.PriceBrutto,
            OfferNotes = position.OfferNotes,
            InvoiceNotes = position.InvoiceNotes,
            Other1 = position.Other1,
            GroupNumber = position.GroupNumber,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt
        };
    }

    private void EditPosition()
    {
        if (SelectedOfferPosition == null) return;

        try
        {
            var editViewModel = new OfferPositionEditViewModel(
                _offerService, 
                _productRepository, 
                SelectedOfferPosition,
                _userContext);
            var editWindow = new OfferPositionEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę pozycji, aby pokazać zaktualizowane dane
                if (SelectedOffer != null)
                {
                    _ = LoadOfferPositionsAsync(SelectedOffer.Id);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji pozycji: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void EditOffer()
    {
        if (SelectedOffer == null) return;

        try
        {
            var editViewModel = new OfferEditViewModel(
                _offerService, 
                _customerRepository, 
                SelectedOffer,
                _userContext);
            var editWindow = new OfferEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę ofert, aby pokazać zaktualizowane dane
                _ = LoadOffersAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji oferty: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddOfferAsync()
    {
        try
        {
            var companyId = _userContext.CompanyId 
                ?? throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
            
            var operatorName = _userContext.Username 
                ?? "System";

            // Konwersja dzisiejszej daty na Clarion date
            var baseDate = new DateTime(1800, 12, 28);
            var today = DateTime.Now;
            var offerDate = (int)(today - baseDate).TotalDays;
            
            // Pobieramy kolejny numer oferty dla dzisiejszego dnia
            var nextOfferNumber = await _offerService.GetNextOfferNumberForDateAsync(offerDate, companyId);
            
            // Tworzymy nową ofertę
            var offer = new Offer(companyId, operatorName);
            offer.UpdateOfferInfo(offerDate, nextOfferNumber, "PLN");
            
            // Dodajemy ofertę do bazy
            var id = await _offerService.AddAsync(offer);
            
            // Pobieramy utworzoną ofertę z bazy
            var createdOffer = await _offerService.GetByIdAsync(id, companyId);
            if (createdOffer == null)
            {
                System.Windows.MessageBox.Show(
                    "Nie udało się utworzyć oferty.",
                    "Błąd",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            // Odświeżamy listę ofert
            await LoadOffersAsync();
            
            // Ustawiamy nowo utworzoną ofertę jako wybraną
            var offerDto = Offers.FirstOrDefault(o => o.Id == id);
            if (offerDto != null)
            {
                SelectedOffer = offerDto;
                
                // Otwieramy okno edycji nowo utworzonej oferty
                EditOffer();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas dodawania oferty: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task ChangeStatusAsync()
    {
        if (SelectedOffer == null || !_userContext.CompanyId.HasValue) return;

        var current = OfferStatusMapping.FromDb(SelectedOffer.Status);
        OfferStatus? newStatus = null;
        string? prompt = null;

        if (current == OfferStatus.Draft)
            prompt = "Ustaw status: Wysłana (Sent)?";
        else if (current == OfferStatus.Sent)
            prompt = "Ustaw status: Zaakceptowana (Accepted)?";

        if (string.IsNullOrEmpty(prompt))
        {
            System.Windows.MessageBox.Show(
                "Brak dozwolonego przejścia do testu (tylko Draft→Sent, Sent→Accepted).",
                "Zmień status",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var result = System.Windows.MessageBox.Show(prompt, "Zmień status",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        newStatus = current == OfferStatus.Draft ? OfferStatus.Sent : OfferStatus.Accepted;

        try
        {
            await _offerService.SetStatusAsync(SelectedOffer.Id, _userContext.CompanyId.Value, newStatus.Value);
            await LoadOffersAsync();
            System.Windows.MessageBox.Show($"Status zmieniony na {newStatus}.", "Zmień status",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd: {ex.Message}", "Błąd",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task CreateOrderFromOfferAsync()
    {
        if (SelectedOffer == null || !_userContext.CompanyId.HasValue) return;

        try
        {
            var orderId = await _orderMainService.CreateFromOfferAsync(SelectedOffer.Id);
            System.Windows.MessageBox.Show(
                $"Zamówienie utworzone z oferty. ID zamówienia: {orderId}.",
                "Utwórz zamówienie z oferty",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd: {ex.Message}", "Utwórz zamówienie z oferty",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeleteOfferAsync()
    {
        if (SelectedOffer == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Czy na pewno chcesz usunąć ofertę #{SelectedOffer.OfferNumber} z dnia {SelectedOffer.FormattedOfferDate}?\n\n" +
            $"Klient: {SelectedOffer.CustomerName ?? "Brak"}\n\n" +
            $"UWAGA: Zostaną również usunięte wszystkie pozycje tej oferty!",
            "Potwierdzenie usunięcia",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        if (!_userContext.CompanyId.HasValue)
        {
            System.Windows.MessageBox.Show("Brak wybranej firmy.", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        var offerToDelete = SelectedOffer;
        var companyId = _userContext.CompanyId.Value;

        try
        {
            await _offerService.DeleteAsync(offerToDelete.Id, companyId);

            Offers.Remove(offerToDelete);
            SelectedOffer = null;
            OfferPositions.Clear();

            System.Windows.MessageBox.Show(
                "Oferta została usunięta.",
                "Sukces",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania oferty: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddPositionAsync()
    {
        if (SelectedOffer == null)
        {
            System.Windows.MessageBox.Show(
                "Najpierw wybierz ofertę, do której chcesz dodać pozycję.",
                "Brak wybranej oferty",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var companyId = _userContext.CompanyId 
                ?? throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");

            // Tworzymy nową pozycję oferty
            var position = new OfferPosition(companyId, SelectedOffer.Id, "szt");
            
            // Ustawiamy domyślne wartości
            position.UpdateProductInfo(null, null, null, "Nowa pozycja", null);
            position.UpdatePricing(1, 0, 0, 0, 0);
            position.UpdateVatInfo("23", 0, 0);
            
            // Dodajemy pozycję do bazy
            var id = await _offerService.AddPositionAsync(position);
            
            // Odświeżamy listę pozycji
            await LoadOfferPositionsAsync(SelectedOffer.Id);
            
            // Ustawiamy nowo utworzoną pozycję jako wybraną
            var positionDto = OfferPositions.FirstOrDefault(p => p.Id == id);
            if (positionDto != null)
            {
                SelectedOfferPosition = positionDto;
                
                // Otwieramy okno edycji nowo utworzonej pozycji
                EditPosition();
            }
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas dodawania pozycji oferty: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeletePositionAsync()
    {
        if (SelectedOfferPosition == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Czy na pewno chcesz usunąć pozycję?\n\n" +
            $"Nazwa: {SelectedOfferPosition.Name ?? "Brak"}\n" +
            $"Ilość: {SelectedOfferPosition.Quantity}\n" +
            $"Cena: {SelectedOfferPosition.Price}",
            "Potwierdzenie usunięcia",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            await _offerService.DeletePositionAsync(SelectedOfferPosition.Id);
            
            // Usuwamy z listy
            var positionToRemove = SelectedOfferPosition;
            OfferPositions.Remove(positionToRemove);
            SelectedOfferPosition = null;
            
            System.Windows.MessageBox.Show(
                "Pozycja została usunięta.",
                "Sukces",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania pozycji: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
