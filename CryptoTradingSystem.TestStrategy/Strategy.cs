using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Strategy;
using Microsoft.Extensions.Configuration;

using Serilog;

namespace CryptoTradingSystem.TestStrategy
{
    public class Strategy : IStrategy
    {
        private List<Tuple<Enums.TimeFrames, Enums.Assets, Type>> assets =
            new List<Tuple<Enums.TimeFrames, Enums.Assets, Type>>
            {
                Tuple.Create(Enums.TimeFrames.M15, Enums.Assets.Btcusdt, typeof(SMA)),
                Tuple.Create(Enums.TimeFrames.D1, Enums.Assets.Btcusdt, typeof(SMA))
            };
        
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
                timeFrameEnd: null);
        }

        public Enums.TradeType ExecuteStrategy(List<Indicator> indicators, Enums.TradeStatus status)
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
                return Enums.TradeType.None;
            }

            return status switch
            {
                // If there is no open trade for this currency pair
                // If the 15 min SMA is above the 1 day SMA, buy
                Enums.TradeStatus.Closed when fifteenminute.SMA5 > oneHour.SMA75 => Enums.TradeType.Buy,
                // If there is an open trade for this currency pair
                // If the 15 min SMA is below the 1 day SMA, sell
                Enums.TradeStatus.Open when fifteenminute.SMA5 < oneHour.SMA75 => Enums.TradeType.Sell,
                _ => Enums.TradeType.None
            };
        }
    }
}
