using Blockchain.Menus;
using Blockchain.Services;
using Blockchain.UI;

var context = new MenuContext(showMetrics: false);
var renderer = new ConsoleRenderer(context);

var blockchainService = new BlockchainService(2);
var displayService = new DisplayService();
var explorerService = new ExplorerService(blockchainService);
var transactionService = new TransactionService();
var walletService = new WalletService();

var blockchainMenu = new BlockchainMenu(
    blockchainService,
    transactionService,
    walletService,
    displayService,
    renderer,
    context);

var explorerMenu = new ExplorerMenu(
    explorerService,
    displayService,
    renderer,
    context);

var walletMenu = new WalletMenu(
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