using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Strategy;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoTradingSystem.TestStrategy
{
    public class Strategy : IStrategy
    {
        public StrategyStatistics Statistics { get; set; } = new ();

        private readonly List<Tuple<Enums.TimeFrames, Enums.Assets, Type>> assets =
            new List<Tuple<Enums.TimeFrames, Enums.Assets, Type>>
            {
                Tuple.Create(Enums.TimeFrames.M15, Enums.Assets.Btcusdt, typeof(SMA)),
                Tuple.Create(Enums.TimeFrames.D1, Enums.Assets.Btcusdt, typeof(SMA))
            };

        private StrategyReturnParameter strategyReturnParameter = new();
        private Enums.StrategyState strategyState = Enums.StrategyState.Backtesting;
        private Dictionary<Enums.TradeType, decimal> openTrades = new ();

        public StrategyParameter SetupStrategyParameter()
        {
            #region logging

            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var loggingfilePath = config.GetValue<string>("LoggingLocation");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(loggingfilePath ?? "logs/Strategy.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            #endregion

            return new StrategyParameter(
                assets: assets,
                assetToBuy: Enums.Assets.Btcusdt,
                timeFrameStart: new DateTime(2017, 10, 30),
                timeFrameEnd: null,
                strategyApprovementStatistics: new StrategyApprovementStatistics
                {
                    MinimalValidationDuration = new DateTime(0, 0, 10),
                    ProfitLoss = 100,
                });
        }

        public StrategyReturnParameter ExecuteStrategy(List<Indicator> indicators, decimal price)
        {
            foreach (var indicator in indicators)
            {
                if (assets.All(x => x.Item2.GetStringValue()?.ToLower() != indicator.AssetName?.ToLower()))
                    throw new ArgumentException($"There is no requested asset with this name {indicator.AssetName}");
                if (assets.All(x => x.Item1.GetStringValue() != indicator.Interval))
                    throw new ArgumentException($"There is no requested asset with this interval {indicator.Interval}");
            }

            var fifteenminute = (SMA)indicators[0];
            var oneHour = (SMA)indicators[1];

            if (fifteenminute.SMA5 == null
                || oneHour.SMA75 == null)
            {
                return strategyReturnParameter;
            }

            if (fifteenminute.SMA5 > oneHour.SMA75)
            {
                strategyReturnParameter = new StrategyReturnParameter
                {
                    TradeType = Enums.TradeType.Buy,
                    TradeStatus = Enums.TradeStatus.Open,
                    StrategyState = strategyState
                };
                
                // TODO in every case of opening a trade, add trade to openTrades
                openTrades.Add(strategyReturnParameter.TradeType, price);
            }
            else if (fifteenminute.SMA5 < oneHour.SMA75)
            {
                strategyReturnParameter = new StrategyReturnParameter
                {
                    TradeType = Enums.TradeType.Sell,
                    TradeStatus = Enums.TradeStatus.Closed,
                    StrategyState = strategyState
                };
                
                // TODO in every case of closing a trade, calculate the statistics
                CalculateStatistics(price, strategyReturnParameter.TradeType);
            }

            return strategyReturnParameter;
        }

        /// <summary>
        /// Set statistics after closing the trade
        /// </summary>
        /// <param name="candleClose"></param>
        /// <param name="tradeType"></param>
        public void CalculateStatistics(decimal candleClose, Enums.TradeType tradeType)
        {
            var profitLoss = CalculateProfitLoss(candleClose, tradeType);

            Statistics.TradesAmount++;

            if (profitLoss != null)
            {
                Statistics.ProfitLoss += profitLoss.Value;
                _ = profitLoss > 0 ? Statistics.AmountOfWonTrades++ : Statistics.AmountOfLostTrades++;
            }

            Statistics.ReturnOnInvestment = Statistics.ProfitLoss / Statistics.InitialInvestment * 100;

            Statistics.WonTradesPercentage = Statistics.AmountOfWonTrades / Statistics.TradesAmount * 100;
            Statistics.LostTradesPercentage = 100m - Statistics.WonTradesPercentage;

            // TODO calculate RiskReward Ratio
            // !ratio between potential profit and potential loss

            // TODO calculate Sharpe Ratio
            // !risk-adjusted return - considers return and volatility of strategy
            // !(Return of Portfolio - Risk-Free Rate) / Portfolio Standard Deviation
            // !(ROI - 2%) / BTC standard deviation
            //  strategy.StrategyAnalytics.SharpeRatio = (strategy.StrategyAnalytics.ReturnOnInvestment - 2m) / ;

            // TODO calculate Max Drawdown in %

            // TODO calculate Average Trade Duration
            // !shows holding time and strategy efficiency

            // TODO calculate Trade Frequency 30D?

            // TODO calculate Slippage
            // !analyze difference between expected price and execute price of orders

            // TODO calculate Volatility
        }

        private decimal? CalculateProfitLoss(decimal candleClose, Enums.TradeType tradeType)
        {
            decimal? profitLoss = null;
            switch (tradeType)
            {
                case Enums.TradeType.Buy:
                    var buyTrade = openTrades.FirstOrDefault(x => x.Key == Enums.TradeType.Buy);
                    profitLoss = buyTrade.Value - candleClose;
                    openTrades.Remove(buyTrade.Key);
                    break;
                case Enums.TradeType.Sell:
                    var sellTrade = openTrades.FirstOrDefault(x => x.Key == Enums.TradeType.Sell);
                    profitLoss = candleClose - sellTrade.Value;
                    openTrades.Remove(sellTrade.Key);
                    break;
                case Enums.TradeType.None:
                default:
                    break;
            }

            return profitLoss;
        }
    }
}
