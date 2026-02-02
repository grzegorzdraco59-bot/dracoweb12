using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using ERP.Application.DTOs;
using ERP.Application.Helpers;
using ERP.Application.Repositories;
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
    private readonly IOfferToFpfConversionService _offerToFpfService;
    private readonly IOfferPdfService _offerPdfService;
    private readonly ICompanyRepository _companyRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOfferTotalsService _offerTotalsService;
    private readonly IEmailService _emailService;
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
        IOfferToFpfConversionService offerToFpfService,
        IOfferPdfService offerPdfService,
        ICompanyRepository companyRepository,
        IInvoiceRepository invoiceRepository,
        IOfferTotalsService offerTotalsService,
        IEmailService emailService,
        ProductRepository productRepository, 
        ICustomerRepository customerRepository,
        IUserContext userContext)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _orderMainService = orderMainService ?? throw new ArgumentNullException(nameof(orderMainService));
        _offerToFpfService = offerToFpfService ?? throw new ArgumentNullException(nameof(offerToFpfService));
        _offerPdfService = offerPdfService ?? throw new ArgumentNullException(nameof(offerPdfService));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _offerTotalsService = offerTotalsService ?? throw new ArgumentNullException(nameof(offerTotalsService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        
        Offers = new ObservableCollection<OfferDto>();
        OfferPositions = new ObservableCollection<OfferPositionDto>();
        DocumentTreeItems = new ObservableCollection<DocumentTreeNode>();
        
        _offersViewSource = new CollectionViewSource { Source = Offers };
        _offersViewSource.View.Filter = FilterOffers;
        
        LoadOffersCommand = new RelayCommand(async () => await LoadOffersAsync());
        AddOfferCommand = new RelayCommand(async () => await AddOfferAsync());
        EditOfferCommand = new RelayCommand(() => EditOffer(), () => SelectedOffer != null);
        DeleteOfferCommand = new RelayCommand(async () => await DeleteOfferAsync(), () => SelectedOffer != null);
        ChangeStatusCommand = new RelayCommand(async () => await ChangeStatusAsync(), () => SelectedOffer != null);
        CreateOrderFromOfferCommand = new RelayCommand(async () => await CreateOrderFromOfferAsync(), () => SelectedOffer != null);
        AddPositionCommand = new RelayCommand(AddPositionAsync, () => SelectedOffer != null);
        EditPositionCommand = new RelayCommand(() => EditPosition(), () => SelectedOfferPosition != null);
        DeletePositionCommand = new RelayCommand(async () => await DeletePositionAsync(), () => SelectedOfferPosition != null);
        
        // Drukuj PDF – tylko gdy zaznaczona oferta i Id > 0
        PrintOfferPdfCommand = new RelayCommand(async () => await ExportOfferToPdfAsync(), () => SelectedOffer != null && SelectedOffer.Id > 0);
        // Oferta mail – enabled gdy oferta ma Id i odbiorca_mail (CustomerEmail) nie jest pusty
        SendEmailCommand = new RelayCommand(async () => await SendOfferEmailAsync(), () => SelectedOffer != null && SelectedOffer.Id > 0 && !string.IsNullOrWhiteSpace(SelectedOffer.CustomerEmail));
        CopyToNewOfferCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do nowej oferty - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        CopyToFpfCommand = new RelayCommand(async () => await CopyToFpfAsync(), () => SelectedOffer != null);
        CopyToFpfZalCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do FPFzal. - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        CopyToOrderCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do zlecenia - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        CopyToFvCommand = new RelayCommand(() => System.Windows.MessageBox.Show("Kopiuj do FV - w przygotowaniu", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), () => SelectedOffer != null);
        
        // Automatyczne ładowanie przy starcie
        _ = LoadOffersAsync();
    }

    public ObservableCollection<OfferDto> Offers { get; }
    public ObservableCollection<OfferPositionDto> OfferPositions { get; }
    /// <summary>Drzewko dokumentów: korzeń = oferta (sum_brutto z oferty), dzieci = dokumenty (faktury.sum_brutto).</summary>
    public ObservableCollection<DocumentTreeNode> DocumentTreeItems { get; }
    
    public ICollectionView FilteredOffers => _offersViewSource.View;

    /// <summary>True gdy wybrano ofertę – panele po prawej są aktywne.</summary>
    public bool HasSelectedOffer => SelectedOffer != null;

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
                OnPropertyChanged(nameof(HasSelectedOffer));
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
                if (PrintOfferPdfCommand is RelayCommand printPdfCmd)
                    printPdfCmd.RaiseCanExecuteChanged();
                if (SendEmailCommand is RelayCommand sendEmailCmd)
                    sendEmailCmd.RaiseCanExecuteChanged();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
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
                    _ = LoadDocumentTreeAsync(value);
                }
                else
                {
                    OfferPositions.Clear();
                    DocumentTreeItems.Clear();
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
    public ICommand PrintOfferPdfCommand { get; }
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

    private async Task ReloadOffersAndReselectAsync(int? offerId)
    {
        await LoadOffersAsync();
        SelectedOffer = offerId.HasValue ? Offers.FirstOrDefault(o => o.Id == offerId.Value) : null;
    }

    /// <summary>Odświeża pozycje oferty i DTO oferty (sum_netto, sum_vat, sum_brutto z nagłówka) w UI bez przeładowania całej listy.</summary>
    private async Task RefreshPositionsAndSumBruttoAsync(int offerId)
    {
        await LoadOfferPositionsAsync(offerId);
        if (!_userContext.CompanyId.HasValue) return;
        try
        {
            var offer = await _offerService.GetByIdAsync(offerId, _userContext.CompanyId.Value);
            if (offer == null) return;
            var offerDto = MapToDto(offer);
            var idx = -1;
            for (var i = 0; i < Offers.Count; i++)
            {
                if (Offers[i].Id == offerId) { idx = i; break; }
            }
            if (idx >= 0)
            {
                Offers[idx] = offerDto;
                if (SelectedOffer?.Id == offerId)
                    SelectedOffer = offerDto;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "Błąd odświeżania oferty", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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

    /// <summary>Drzewko dokumentów: oferta (TotalBrutto) + dokumenty (faktury.sum_brutto). 1 zapytanie po nagłówki.</summary>
    /// <summary>Ładuje drzewko dokumentów. Gdy selectFirstDocumentNode=true (np. po Kopiuj do FPF), zaznacza pierwszy węzeł dokumentu i zgłasza RequestBringIntoView.</summary>
    private async Task LoadDocumentTreeAsync(OfferDto offer, bool selectFirstDocumentNode = false)
    {
        DocumentTreeItems.Clear();
        if (!_userContext.CompanyId.HasValue) return;
        try
        {
            var dateStr = offer.FormattedOfferDateYyyyMmDd;
            var root = new DocumentTreeNode
            {
                DisplayText = $"OFERTA {offer.FullNo} | {dateStr} | {(offer.SumBrutto ?? offer.TotalBrutto ?? 0m):N2}",
                IsExpanded = true
            };
            var documents = (await _invoiceRepository.GetDocumentsByOfferIdAsync(offer.Id, _userContext.CompanyId.Value)).ToList();
            if (documents.Count == 0)
            {
                root.Children.Add(new DocumentTreeNode { DisplayText = "Brak proformy (FPF)" });
            }
            else
            {
                DocumentTreeNode? firstDocNode = null;
                foreach (var doc in documents)
                {
                    var label = DocumentTreeNode.GetLabelForDocType(doc.DocType);
                    var docNo = doc.DocFullNo ?? "";
                    var data = doc.FormattedDataFaktury;
                    var brutto = (doc.SumBrutto ?? 0m).ToString("N2");
                    var displayText = $"{label} {docNo} | {data} | {brutto}";
                    if (string.Equals(doc.DocType, "FV", StringComparison.OrdinalIgnoreCase))
                        displayText += $" | DO ZAPŁATY: {(doc.DoZaplatyBrutto ?? 0m):N2}";
                    var node = new DocumentTreeNode
                    {
                        DisplayText = displayText,
                        IsExpanded = string.Equals(doc.DocType, "FPF", StringComparison.OrdinalIgnoreCase)
                    };
                    if (firstDocNode == null) firstDocNode = node;
                    root.Children.Add(node);
                }
                if (selectFirstDocumentNode && firstDocNode != null)
                {
                    firstDocNode.IsSelected = true;
                    RequestBringIntoView?.Invoke(this, EventArgs.Empty);
                }
            }
            DocumentTreeItems.Add(root);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd podczas ładowania drzewka dokumentów: {ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    /// <summary>Wywołane po ustawieniu IsSelected na węźle – View może przewinąć do zaznaczonego (BringIntoView).</summary>
    public event EventHandler? RequestBringIntoView;

    private static DateTime? ClarionToDateTime(int? clarionDate)
    {
        if (!clarionDate.HasValue || clarionDate.Value <= 0) return null;
        try
        {
            return new DateTime(1800, 12, 28).AddDays(clarionDate.Value);
        }
        catch { return null; }
    }

    /// <summary>Data oferty do numeru/pliku – DataOferty lub konwersja z Clarion (OfferDate).</summary>
    private static DateTime GetOfferDateForNumber(OfferDto offer)
    {
        if (offer.DataOferty.HasValue) return offer.DataOferty.Value;
        if (offer.OfferDate.HasValue && offer.OfferDate.Value > 0)
            return new DateTime(1800, 12, 28).AddDays(offer.OfferDate.Value);
        return DateTime.Today;
    }

    /// <summary>Folder na PDF-y ofert – zawsze C:\wydruki\oferty.</summary>
    private const string PdfOutputFolder = "C:\\wydruki\\oferty";

    private static string EnsurePdfFolder()
    {
        try { Directory.CreateDirectory(PdfOutputFolder); } catch { /* ignoruj */ }
        return PdfOutputFolder;
    }

    private static string GetUniqueFilePath(string desiredPath)
    {
        if (!File.Exists(desiredPath)) return desiredPath;
        var dir = Path.GetDirectoryName(desiredPath) ?? "";
        var nameWithoutExt = Path.GetFileNameWithoutExtension(desiredPath);
        var ext = Path.GetExtension(desiredPath);
        for (var i = 2; i < 1000; i++)
        {
            var candidate = Path.Combine(dir, $"{nameWithoutExt} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
        return Path.Combine(dir, $"{nameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{ext}");
    }

    /// <summary>Eksport oferty do PDF – zapis zawsze do C:\wydruki\oferty (bez dialogu), nazwa OF_yyyy-MM-dd-nr.pdf, opcja otwarcia.</summary>
    private async Task ExportOfferToPdfAsync()
    {
        if (SelectedOffer == null || SelectedOffer.Id <= 0 || !_userContext.CompanyId.HasValue)
            return;
        var companyId = _userContext.CompanyId.Value;
        var offerId = SelectedOffer.Id;
        try
        {
            await _offerTotalsService.RecalcOfferTotalsAsync(offerId);
            var offer = await _offerService.GetByIdAsync(offerId, companyId);
            if (offer == null)
            {
                System.Windows.MessageBox.Show("Nie znaleziono oferty.", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            var positions = (await _offerService.GetPositionsByOfferIdAsync(offerId)).ToList();
            var offerDto = MapToDto(offer);
            var positionsDto = positions.Select(MapToDto).ToList();

            CompanyDto? companyDto = null;
            var company = await _companyRepository.GetByIdAsync(offerDto.CompanyId);
            if (company != null)
            {
                companyDto = new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Street = company.Street,
                    PostalCode = company.PostalCode,
                    City = company.City,
                    Country = company.Country,
                    Nip = company.Nip,
                    Phone1 = company.Phone1,
                    Email = company.Email
                };
            }

            var dataOferty = GetOfferDateForNumber(offerDto);
            var nrOferty = offerDto.NrOferty ?? 0;
            var defaultFileName = OfferNumberHelper.BuildOfferFileName(dataOferty, nrOferty);
            var folder = EnsurePdfFolder();
            var finalPath = GetUniqueFilePath(Path.Combine(folder, defaultFileName));

            var tmpPath = finalPath + ".tmp";
            await using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await _offerPdfService.GeneratePdfAsync(offerDto, positionsDto, fs, companyDto);
            }
            File.Move(tmpPath, finalPath, overwrite: false);

            var result = System.Windows.MessageBox.Show(
                $"Zapisano ofertę PDF do: {finalPath}\n\nOtworzyć teraz?",
                "Zapisano",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo(finalPath) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "Błąd zapisu PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>Otwiera okno "Wyślij ofertę", generuje PDF do byte[], wysyła e-mail SMTP z tabeli firmy (firmy.id), opcjonalnie ustawia status na wyslane.</summary>
    private async Task SendOfferEmailAsync()
    {
        if (SelectedOffer == null || SelectedOffer.Id <= 0)
            return;
        if (!_userContext.CompanyId.HasValue)
        {
            System.Windows.MessageBox.Show("Brak wybranej firmy.", "Oferta mail", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        // Walidacja odbiorcy: oferty.odbiorca_mail
        var customerEmail = (SelectedOffer.CustomerEmail ?? "").Trim();
        if (string.IsNullOrEmpty(customerEmail))
        {
            System.Windows.MessageBox.Show("Brak odbiorca_mail w ofercie", "Oferta mail", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        var companyId = _userContext.CompanyId.Value;
        System.Windows.MessageBox.Show($"MAIL: companyId={companyId}", "Oferta mail", System.Windows.MessageBoxButton.OK);

        // SMTP z DB: SELECT ... FROM firmy WHERE id = @companyId (CompanyRepository.GetByIdAsync)
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
        {
            System.Windows.MessageBox.Show("Nie znaleziono firmy.", "Oferta mail", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        var host = (company.SmtpHost ?? "").Trim();
        var user = (company.SmtpUser ?? "").Trim();
        var fromEmail = (company.SmtpFromEmail ?? "").Trim();
        var port = company.SmtpPort ?? 25;
        var ssl = company.SmtpSsl ?? false;
        var pass = company.SmtpPass ?? "";

        System.Windows.MessageBox.Show($"MAIL SMTP: host=[{host}] port={port} ssl={ssl} from=[{fromEmail}]", "Oferta mail", System.Windows.MessageBoxButton.OK);

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(fromEmail))
        {
            System.Windows.MessageBox.Show("Brak konfiguracji SMTP dla tej firmy (uzupełnij w tabeli firmy)", "Oferta mail", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var defaultSubject = "Oferta " + SelectedOffer.FullNo;
        var defaultBody = "Dzień dobry,\n\nW załączeniu przesyłamy ofertę.\n\nPozdrawiamy";
        var vm = new SendOfferEmailViewModel(customerEmail, defaultSubject, defaultBody);

        var window = new SendOfferEmailWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (window.ShowDialog() != true)
            return;

        var to = (vm.To ?? "").Trim();
        var cc = string.IsNullOrWhiteSpace(vm.DwUdw) ? null : vm.DwUdw.Trim();
        var subject = (vm.Subject ?? "").Trim();
        var body = (vm.Body ?? "").Trim();
        var changeStatus = vm.ChangeStatusAfterSend;

        var offerId = SelectedOffer.Id;

        try
        {
            await _offerTotalsService.RecalcOfferTotalsAsync(offerId);
            var offer = await _offerService.GetByIdAsync(offerId, companyId);
            if (offer == null)
            {
                System.Windows.MessageBox.Show("Nie znaleziono oferty.", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            var positions = (await _offerService.GetPositionsByOfferIdAsync(offerId)).ToList();
            var offerDto = MapToDto(offer);
            var positionsDto = positions.Select(MapToDto).ToList();

            CompanyDto? companyDto = new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Street = company.Street,
                PostalCode = company.PostalCode,
                City = company.City,
                Country = company.Country,
                Nip = company.Nip,
                Phone1 = company.Phone1,
                Email = company.Email
            };

            byte[] pdfBytes;
            await using (var ms = new MemoryStream())
            {
                await _offerPdfService.GeneratePdfAsync(offerDto, positionsDto, ms, companyDto);
                pdfBytes = ms.ToArray();
            }

            var dataOferty = GetOfferDateForNumber(offerDto);
            var nrOferty = offerDto.NrOferty ?? 0;
            var attachmentFileName = OfferNumberHelper.BuildOfferFileName(dataOferty, nrOferty);

            var smtpDto = new SmtpSettingsDto
            {
                Host = host,
                Port = port,
                User = user,
                Pass = pass,
                Ssl = ssl,
                FromEmail = fromEmail,
                FromName = company.SmtpFromName
            };
            await _emailService.SendWithSmtpSettingsAsync(smtpDto, to, cc, subject, body, pdfBytes, attachmentFileName);
            if (changeStatus)
            {
                await _offerService.SetStatusAsync(offerId, companyId, OfferStatus.Sent);
                await ReloadOffersAndReselectAsync(offerId);
            }
            System.Windows.MessageBox.Show(changeStatus ? "Wysłano ofertę. Status: wyslane" : "Wysłano ofertę.", "Oferta mail", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "Błąd wysyłki", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }
    }

    /// <summary>Mapowanie Offer → OfferDto (odbiorca_mail w DB = CustomerEmail w DTO).</summary>
    private static OfferDto MapToDto(Offer offer)
    {
        var dataOferty = ClarionToDateTime(offer.OfferDate);
        string? formattedDate = dataOferty?.ToString("dd/MM/yyyy");
        if (offer.OfferDate.HasValue && string.IsNullOrEmpty(formattedDate))
            formattedDate = $"Błąd: {offer.OfferDate.Value}";
        
        return new OfferDto
        {
            Id = offer.Id,
            CompanyId = offer.CompanyId,
            ForProforma = offer.ForProforma,
            ForOrder = offer.ForOrder,
            OfferDate = offer.OfferDate,
            DataOferty = dataOferty,
            NrOferty = offer.OfferNumber,
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
            SumBrutto = offer.SumBrutto,
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
               (offer.NrOferty?.ToString().Contains(searchTextLower) ?? false) ||
               (offer.FormattedOfferDate?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (offer.DataOferty?.ToString("yyyy-MM-dd").Contains(searchTextLower) ?? false) ||
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
            Ilosc = position.Ilosc,
            CenaNetto = position.CenaNetto,
            Discount = position.Discount,
            PriceAfterDiscount = position.PriceAfterDiscount,
            NettoPoz = position.NettoPoz,
            VatRate = position.VatRate,
            VatPoz = position.VatPoz,
            BruttoPoz = position.BruttoPoz,
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
                // Odświeżamy pozycje i sumę brutto oferty bez przeładowania całej listy
                if (SelectedOffer != null)
                {
                    var id = SelectedOffer.Id;
                    _ = RefreshPositionsAndSumBruttoAsync(id);
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
                var id = SelectedOffer?.Id;
                _ = ReloadOffersAndReselectAsync(id);
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

    private async Task CopyToFpfAsync()
    {
        if (SelectedOffer == null || !_userContext.CompanyId.HasValue) return;

        try
        {
            var (invoiceId, createdNew) = await _offerToFpfService.CopyOfferToFpfAsync(
                SelectedOffer.Id, _userContext.CompanyId.Value);

            if (createdNew)
            {
                System.Windows.MessageBox.Show(
                    "Skopiowano do FPF",
                    "Sukces",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                var offerId = SelectedOffer!.Id;
                await LoadOffersAsync();
                SelectedOffer = Offers.FirstOrDefault(o => o.Id == offerId);
                if (SelectedOffer != null)
                    await LoadDocumentTreeAsync(SelectedOffer, selectFirstDocumentNode: true);

                var viewModel = new FakturaEditViewModel(invoiceId);
                var window = new FakturaEditWindow(viewModel)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                window.Show();
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "Oferta była już kopiowana",
                    "Informacja",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd: {ex.Message}", "Kopiuj do FPF",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeleteOfferAsync()
    {
        if (SelectedOffer == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Czy na pewno chcesz usunąć ofertę #{SelectedOffer.NrOferty} ({SelectedOffer.FullNo}) z dnia {SelectedOffer.FormattedOfferDate}?\n\n" +
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

    private void AddPositionAsync()
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

            // Nowa pozycja – DTO z Id=0; INSERT nastąpi dopiero po "Zapisz" w oknie edycji
            var newPositionDto = new OfferPositionDto
            {
                Id = 0,
                OfferId = SelectedOffer.Id,
                CompanyId = companyId,
                Unit = "szt",
                Name = null,
                Ilosc = 1,
                CenaNetto = 0,
                Discount = 0,
                VatRate = "23",
                NettoPoz = 0,
                VatPoz = 0,
                BruttoPoz = 0
            };

            var editViewModel = new OfferPositionEditViewModel(
                _offerService,
                _productRepository,
                newPositionDto,
                _userContext);
            var editWindow = new OfferPositionEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true && SelectedOffer != null)
            {
                _ = RefreshPositionsAndSumBruttoAsync(SelectedOffer.Id);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna pozycji: {ex.Message}",
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
            $"Ilość: {SelectedOfferPosition.Ilosc}\n" +
            $"Cena netto: {SelectedOfferPosition.CenaNetto}",
            "Potwierdzenie usunięcia",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var offerId = SelectedOffer?.Id;
            await _offerService.DeletePositionAsync((int)SelectedOfferPosition.Id);

            // Odświeżamy pozycje i sumę brutto bez przeładowania całej listy ofert
            if (offerId.HasValue)
            {
                await RefreshPositionsAndSumBruttoAsync(offerId.Value);
            }
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
