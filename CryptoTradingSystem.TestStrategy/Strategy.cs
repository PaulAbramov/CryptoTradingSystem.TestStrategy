using System;
using System.Collections.Generic;

using CryptoTradingSystem.General.Data;
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
                .WriteTo.Console()
                .WriteTo.File(loggingfilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
            #endregion

            return new StrategyParameter(
                assets: new List<Tuple<Enums.TimeFrames, Enums.Assets, Enums.Indicators>>
                {
                    Tuple.Create(Enums.TimeFrames.M15, Enums.Assets.Btcusdt, Enums.Indicators.SMA),
                    Tuple.Create(Enums.TimeFrames.D1, Enums.Assets.Btcusdt, Enums.Indicators.SMA)
                },
                timeFrameStart: null,
                timeFrameEnd: null);
        }

        public string ExecuteStrategy()
        {
            return string.Empty;
        }
    }
}
