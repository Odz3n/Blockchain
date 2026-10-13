using Blockchain.Menus;
using Blockchain.Services;
using Blockchain.UI;
using Blockchain.Models;

var context = new MenuContext(showMetrics: false);
var renderer = new ConsoleRenderer(context);

var blockchainService = new BlockchainService(2);
var explorerService = new ExplorerService(blockchainService);
var transactionService = new TransactionService();
var walletService = new WalletService();
var balanceService = new BalanceService();
var displayService = new DisplayService(balanceService, blockchainService, walletService);

var blockchainMenu = new BlockchainMenu(
    blockchainService,
    transactionService,
    walletService,
    displayService,
    renderer,
    context);

var explorerMenu = new ExplorerMenu(
    walletService,
    explorerService,
    displayService,
    renderer,
    context);

var walletMenu = new WalletMenu(
    blockchainService,
    walletService,
    displayService,
    renderer,
    context);

var mainMenu = new MainMenu(
    blockchainService,
    displayService,
    renderer,
    context,
    blockchainMenu,
    explorerMenu,
    walletMenu);

mainMenu.Run();