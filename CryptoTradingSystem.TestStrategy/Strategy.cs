using System;
using System.Collections.Generic;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Strategy;
using Microsoft.Extensions.Configuration;

using Serilog;

namespace CryptoTradingSystem.TestStrategy
{
    public class Strategy : IStrategy
    {
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
                assets: new List<Tuple<Enums.TimeFrames, Enums.Assets, Type>>
                {
                    Tuple.Create(Enums.TimeFrames.M15, Enums.Assets.Btcusdt, typeof(SMA)),
                    Tuple.Create(Enums.TimeFrames.D1, Enums.Assets.Btcusdt, typeof(SMA))
                },
                assetToBuy: Enums.Assets.Btcusdt,
                timeFrameStart: null,
                timeFrameEnd: null);
        }

        public Enums.TradeType ExecuteStrategy(List<Indicator> indicators, Enums.TradeStatus status)
        {
            var fifteenMinTimeframe = Enums.TimeFrames.M15.GetStringValue().ToLower();
            var assetName = Enums.Assets.Btcusdt.GetStringValue().ToLower();

            foreach (var indicator in indicators)
            {
                if (indicator.AssetName?.ToLower() != assetName)
                    continue;
                if (indicator.Interval?.ToLower() != fifteenMinTimeframe)
                    continue;
                
                var indicatorSma = (SMA) indicator;
                
                if (indicatorSma.SMA5 == null || indicatorSma.SMA75 == null)
                {
                    Log.Warning("SMA5 or SMA75 is null");
                    return Enums.TradeType.None;
                }
                
                Log.Information("{Indicator} | {TimeFrame} | {Asset} | {CloseTime} | {SMA5} | {SMA75}",
                    indicator.AssetName,
                    indicator.Interval,
                    indicator.Asset?.AssetName,
                    indicator.CloseTime,
                    ((SMA) indicator).SMA5,
                    ((SMA) indicator).SMA75);
            }
            
            var fifteenMinSma = (SMA) indicators
                .Find(x => x.Interval?.ToLower() == fifteenMinTimeframe?.ToLower()
                           && x.Asset?.AssetName?.ToLower() == Enums.Assets.Btcusdt.GetStringValue()?.ToLower())!;
            
            var oneDaySma = (SMA) indicators
                .Find(x => x.Interval == Enums.TimeFrames.D1.GetStringValue()
                           && x.Asset?.AssetName == Enums.Assets.Btcusdt.GetStringValue())!;

            

            foreach (var indicator in indicators)
                Log.Information("{Indicator} | {TimeFrame} | {Asset} | {CloseTime} | {SMA5} | {SMA75}",
                    indicator.AssetName,
                    indicator.Interval,
                    indicator.Asset?.AssetName,
                    indicator.CloseTime,
                    ((SMA) indicator).SMA5,
                    ((SMA) indicator).SMA75);

            return status switch
            {
                // If there is no open trade for this currency pair
                // If the 15 min SMA is above the 1 day SMA, buy
                Enums.TradeStatus.Closed when fifteenMinSma.SMA5 > oneDaySma.SMA75 => Enums.TradeType.Buy,
                // If there is an open trade for this currency pair
                // If the 15 min SMA is below the 1 day SMA, sell
                Enums.TradeStatus.Open when fifteenMinSma.SMA5 < oneDaySma.SMA75 => Enums.TradeType.Sell,
                _ => Enums.TradeType.None
            };
        }
    }
}
