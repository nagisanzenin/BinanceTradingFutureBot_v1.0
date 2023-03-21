using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using CryptoExchange.Net.Authentication;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using System.Collections.Generic;
using System.Diagnostics;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.CommonObjects;
using System.Threading;
using static System.Net.WebRequestMethods;

namespace BitcoinPriceViewer
{

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public static class BinanceApiCredentials
        {
            public static string ApiKey { get; } = "YrXGNMXWbevegk8lK2GphbQVxonn39Kvjecuso8SDDRG2XBKijB3tiw4eISq3mm5";
            public static string ApiSecret { get; } = "99xEzbamuweOTt7LqPtF4jf12oi1FkCX7Ngjd9bl78fXNBt5MlyMwFBlOc8BO8FF";

            public static BinanceClient GetClient()
            {
                var options = new BinanceClientOptions()
                {
                    ApiCredentials = new Binance.Net.Objects.BinanceApiCredentials(ApiKey, ApiSecret)
                };

                return new BinanceClient(options);
            }
        }

        //new client
        //var binanceClient = BinanceApiCredentials.GetClient();

        private decimal _futureBalance;
        private decimal _bitcoinPrice;
        private decimal _UpperBollinger;
        private decimal _LowerBollinger;
        private decimal _MovingAverage;


        public decimal FutureBalance
        {
            get { return _futureBalance; }
            set
            {
                _futureBalance = value;
                OnPropertyChanged(nameof(FutureBalance));
            }
        }
        public decimal BitcoinPrice
        {
            get { return _bitcoinPrice; }
            set { _bitcoinPrice = value; OnPropertyChanged(); }
        }

        public decimal UpperBollinger
        {
            get { return _UpperBollinger; }
            set { _UpperBollinger = value; OnPropertyChanged(); }
        }

        public decimal LowerBollinger
        {
            get { return _LowerBollinger; }
            set { _LowerBollinger = value; OnPropertyChanged(); }
        }

        public decimal MovingAverage
        {
            get { return _MovingAverage; }
            set { _MovingAverage = value; OnPropertyChanged(); }
        }

        public MainWindow()
        {
            InitializeComponent();         
            DataContext = this;
            FetchBitcoinPriceAsync();
            FetchBitcoinDataAsync();
            FetchUSDTBalance();
        }
        
        public async Task FetchUSDTBalance()
        {
            // Create an instance of the BinanceClient with your API key and secret
            //var apiKey = "YrXGNMXWbevegk8lK2GphbQVxonn39Kvjecuso8SDDRG2XBKijB3tiw4eISq3mm5";
            //var apiSecret = "99xEzbamuweOTt7LqPtF4jf12oi1FkCX7Ngjd9bl78fXNBt5MlyMwFBlOc8BO8FF";
            //var binanceClient = new BinanceClient(new Binance.Net.Objects.BinanceClientOptions()
            //{
            //    ApiCredentials = new Binance.Net.Objects.BinanceApiCredentials(apiKey, apiSecret)
            //});

            var binanceClient = BinanceApiCredentials.GetClient();
            var accountInfo = await binanceClient.UsdFuturesApi.Account.GetAccountInfoAsync();

            var asset = accountInfo.Data.Assets;

            var balanceInfo = await binanceClient.UsdFuturesApi.Account.GetBalancesAsync();

            var usdtAsset = balanceInfo.Data.FirstOrDefault(asset => asset.Asset.EndsWith("USDT"));
            decimal availableBalance = usdtAsset?.AvailableBalance ?? 0m;



            FutureBalance = availableBalance;


        //foreach (var balance in accountInfo.Data.Assets)
        //    if (balance.Asset == "USDT")
        //    {
        //        Console.WriteLine($"{balance.Asset}: {balance.WalletBalance} wallet balance, {balance.CrossWalletBalance} cross wallet balance");
        //    }
    }


        private async Task FetchBitcoinPriceAsync()
        {
            string apiKey = "YrXGNMXWbevegk8lK2GphbQVxonn39Kvjecuso8SDDRG2XBKijB3tiw4eISq3mm5";
            string secretKey = "99xEzbamuweOTt7LqPtF4jf12oi1FkCX7Ngjd9bl78fXNBt5MlyMwFBlOc8BO8FF";
            string symbol = "BTCUSDT";
            //string url = $"https://fapi.binance.com/api/v3/ticker/price?symbol={symbol}";
            string url = $"https://fapi.binance.com/fapi/v1/ticker/price?symbol={symbol}";

            var client = new HttpClient();
           // client.DefaultRequestHeaders.Add("X-MBX-APIKEY", apiKey);

            while (true)
            {
                var response = await client.GetAsync(url);
                var jsonString = await response.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(jsonString);
                BitcoinPrice = (decimal)json["price"];
                Thread.Sleep(500);

            }

            
        }




        private async Task FetchBitcoinDataAsync()
        {

            var binanceClient = BinanceApiCredentials.GetClient();
            //string apiKey = "YrXGNMXWbevegk8lK2GphbQVxonn39Kvjecuso8SDDRG2XBKijB3tiw4eISq3mm5";
            string symbol = "BTCUSDT";
            //string url = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval=1m&limit=20";
            string url = $"https://fapi.binance.com/fapi/v1/klines?symbol={symbol}&interval=1m&limit=20";
            var client = new HttpClient();
            //client.DefaultRequestHeaders.Add("X-MBX-APIKEY", apiKey);

            while (true)
            {
            
                var response = await client.GetAsync(url);
                var jsonString = await response.Content.ReadAsStringAsync();

                JArray json = JArray.Parse(jsonString);

                var closes = json.Select(x => (decimal)x[4]).ToArray();
                var sma = closes.Average();
                var stdDev = Math.Sqrt(closes.Select(x => Math.Pow((double)(x - sma), 2)).Sum() / closes.Length);
                MovingAverage = sma;
                UpperBollinger = sma + (decimal)(2 * stdDev);
                LowerBollinger = sma - (decimal)(2 * stdDev);
                decimal upperPercentageDifference = (UpperBollinger / MovingAverage);
                decimal recommendedLev = Math.Floor(5 / (upperPercentageDifference));
                decimal lowerPercentageDifference = (LowerBollinger / MovingAverage);
                if (BitcoinPrice >= UpperBollinger)
                {
                    decimal sellQuantity = Math.Round((decimal)FutureBalance / BitcoinPrice, 3); // The quantity of BTC to sell short

                    var leverage = await binanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, (int)recommendedLev);       //.Account.ChangeInitialLeverageAsync(/* parameters */); ; // The recommended leverage
                    var placeSellOrder = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                        symbol,
                        Binance.Net.Enums.OrderSide.Sell,
                        Binance.Net.Enums.FuturesOrderType.Market,
                        sellQuantity);
                     
                }
                else if(BitcoinPrice <= LowerBollinger) 
                {
                    decimal sellQuantity = Math.Round((decimal)FutureBalance / BitcoinPrice, 3); // The quantity of BTC to sell short

                    var leverage = await binanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, (int)recommendedLev);       //.Account.ChangeInitialLeverageAsync(/* parameters */); ; // The recommended leverage
                    var placeBuyOrder = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                        symbol,
                        Binance.Net.Enums.OrderSide.Buy,
                        Binance.Net.Enums.FuturesOrderType.Market,
                        sellQuantity);
                }
                var position = await binanceClient.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
                decimal takeProfitQuantity = position.Data.FirstOrDefault(p => p.PositionSide == Binance.Net.Enums.PositionSide.Long)?.Quantity
                   ?? position.Data.FirstOrDefault(p => p.PositionSide == Binance.Net.Enums.PositionSide.Short)?.Quantity
                   ?? 0m;
                if (BitcoinPrice == MovingAverage)
                {
                     //Check if there is a long position and market sell if so
                    if (position.Data.Any(p => p.Symbol == symbol && p.PositionSide == PositionSide.Long))
                    {
                        var result = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Market, (decimal)PositionSide.Long, takeProfitQuantity);

                    }

                     //Check if there is a short position and market buy if so
                    if (position.Data.Any(p => p.Symbol == symbol && p.PositionSide == PositionSide.Short))
                    {
                        var result = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Market, (decimal)PositionSide.Short, takeProfitQuantity);

                    }
                }
                Thread.Sleep(500);
            }
           
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}   