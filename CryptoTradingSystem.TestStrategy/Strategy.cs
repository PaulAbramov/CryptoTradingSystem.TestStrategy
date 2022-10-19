using System;
using System.Linq;

using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Helper;
using CryptoTradingSystem.General.Strategy;
using Microsoft.Extensions.Configuration;

using Serilog;

namespace CryptoTradingSystem.TestStrategy
{
    public class Strategy : IStrategy
    {
        public string ExecuteStrategy(string _connectionString)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var loggingfilePath = config.GetValue<string>("LoggingLocation");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(loggingfilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var databaseHandler = new MySQLDatabaseHandler(_connectionString);

            var btcUsdt5minEma = Retry.Do(() => databaseHandler.GetIndicators<EMA>(Enums.Assets.Btcusdt, Enums.TimeFrames.M5, Enums.Indicators.EMA, DateTime.Now.AddMonths(-1)), TimeSpan.FromSeconds(1));

            Log.Information($"{btcUsdt5minEma.FirstOrDefault().AssetName}, {btcUsdt5minEma.FirstOrDefault().CloseTime}");
            
            return "test";
        }
    }
}
