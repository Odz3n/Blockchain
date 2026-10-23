using Blockchain.Menus;
using Blockchain.Services;
using Blockchain.UI;

var context = new MenuContext(showMetrics: false);
var renderer = new ConsoleRenderer(context);

// ########################################
var networkService = new NetworkService(
    port: int.Parse(args[0]),
    peers: args.Skip(1).Select(x =>
    {
        var parsed = x.Split(':');
        return new Blockchain.Models.Node(parsed[0], int.Parse(parsed[1]));
    }).ToList());
// ########################################

var blockchainService = new BlockchainService(networkService, context, difficulty: 2);
var explorerService = new ExplorerService(blockchainService);
var transactionService = new TransactionService();
var walletService = new WalletService();
var balanceService = new BalanceService();
var hashService = new HashService();
var displayService = new DisplayService(balanceService, blockchainService, walletService, hashService);

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
    walletService,
    renderer,
    context,
    blockchainMenu,
    explorerMenu,
    walletMenu);

await mainMenu.Run();