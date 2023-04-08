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
                timeFrameStart: null,
                timeFrameEnd: null);
        }

        public string ExecuteStrategy(List<List<Indicator>> indicators)
        {

            return string.Empty;
        }
    }
}
