using ERP.Application.Services;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("=== Test połączenia z bazą danych i odczytu odbiorców ===\n");

// Konfiguracja Dependency Injection
var services = new ServiceCollection();
services.AddSingleton<DatabaseContext>();
services.AddScoped<ICustomerRepository, CustomerRepository>();
services.AddScoped<ICustomerService, CustomerService>();
var serviceProvider = services.BuildServiceProvider();

try
{
    var customerService = serviceProvider.GetRequiredService<ICustomerService>();
    
    Console.WriteLine("1. Test połączenia z bazą danych...");
    var allCustomers = await customerService.GetAllAsync();
    Console.WriteLine($"   ✓ Połączenie OK! Znaleziono {allCustomers.Count()} odbiorców.\n");
    
    Console.WriteLine("2. Wyświetlanie pierwszych 5 odbiorców:");
    Console.WriteLine("   " + "=".PadRight(100, '='));
    int count = 0;
    foreach (var customer in allCustomers.Take(5))
    {
        count++;
        Console.WriteLine($"\n   Odbiorca #{count}:");
        Console.WriteLine($"   - ID: {customer.Id}");
        Console.WriteLine($"   - Nazwa: {customer.Name}");
        Console.WriteLine($"   - Email: {customer.Email1 ?? "(brak)"}");
        Console.WriteLine($"   - Telefon: {customer.Phone1 ?? "(brak)"}");
        Console.WriteLine($"   - Miasto: {customer.City ?? "(brak)"}");
        Console.WriteLine($"   - NIP: {customer.Nip ?? "(brak)"}");
        Console.WriteLine($"   - Status: {customer.Status ?? "(brak)"}");
        Console.WriteLine($"   - Waluta: {customer.Currency}");
    }
    Console.WriteLine("\n   " + "=".PadRight(100, '='));
    
    Console.WriteLine("\n3. Test odczytu pojedynczego odbiorcy...");
    if (allCustomers.Any())
    {
        var firstCustomer = allCustomers.First();
        var customerById = await customerService.GetByIdAsync(firstCustomer.Id);
        if (customerById != null)
        {
            Console.WriteLine($"   ✓ Odczytywanie po ID działa! Odbiorca: {customerById.Name}");
        }
        else
        {
            Console.WriteLine("   ✗ Błąd: Nie udało się odczytać odbiorcy po ID");
        }
    }
    
    Console.WriteLine("\n4. Test odczytu aktywnych odbiorców...");
    var activeCustomers = await customerService.GetActiveAsync();
    Console.WriteLine($"   ✓ Znaleziono {activeCustomers.Count()} aktywnych odbiorców.");
    
    Console.WriteLine("\n=== Wszystkie testy zakończone pomyślnie! ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ BŁĄD: {ex.Message}");
    Console.WriteLine($"\nStack trace:");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}

Console.WriteLine("\nNaciśnij Enter, aby zakończyć...");
Console.ReadLine();
