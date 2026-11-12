using MoneyTracker.Data;
using MoneyTracker.Services;
using MoneyTracker.UI;

// Entry point: create all objects, connect them and start the app.
// Every class gets the objects it needs through its constructor (dependency injection)
MoneyFileStorage storage = new("moneyTracker.json");
TransactionService transactionService = new();
CategoryService categoryService = new();
MoneyDataService dataService = new(storage, transactionService, categoryService);
TablePrinter tablePrinter = new(categoryService);

ConsoleMessages messages = new(transactionService, dataService);
TransactionMenu transactionMenu = new(transactionService, tablePrinter, messages);
CategoryMenu categoryMenu = new(transactionService, categoryService, tablePrinter, messages);

ConsoleUI console = new(transactionService, dataService, messages, transactionMenu, categoryMenu);
console.Run();
