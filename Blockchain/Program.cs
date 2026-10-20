using Blockchain.Menus;
using Blockchain.Services;
using Blockchain.UI;

var context = new MenuContext(showMetrics: false);
var renderer = new ConsoleRenderer(context);

var blockchainService = new BlockchainService(2);
var explorerService = new ExplorerService(blockchainService);
var transactionService = new TransactionService();
var walletService = new WalletService();
var balanceService = new BalanceService();
var hashService = new HashService();
var displayService = new DisplayService(balanceService, blockchainService, walletService, hashService);

// ########################################
var networkService = new NetworkService(
    port: int.Parse(args[0]),
    peers: args.Skip(1).Select(int.Parse).ToList());
// ########################################

var blockchainMenu = new BlockchainMenu(
    blockchainService,
    transactionService,
    walletService,
    displayService,
    networkService,
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
    networkService,
    renderer,
    context,
    blockchainMenu,
    explorerMenu,
    walletMenu);

mainMenu.Run();