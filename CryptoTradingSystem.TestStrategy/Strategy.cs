using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Strategy;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoTradingSystem.TestStrategy;

public class Strategy : IStrategy
{
	private readonly List<Tuple<Enums.TimeFrames, Enums.Assets, Type>> assets =
		new List<Tuple<Enums.TimeFrames, Enums.Assets, Type>>
		{
			Tuple.Create(Enums.TimeFrames.M15, Enums.Assets.Btcusdt, typeof(SMA)),
			Tuple.Create(Enums.TimeFrames.D1, Enums.Assets.Btcusdt, typeof(SMA))
		};

	private StrategyReturnParameter strategyReturnParameter = new();

	public StrategyParameter SetupStrategyParameter()
	{
		#region logging

		IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
		var loggingfilePath = config.GetValue<string>("LoggingLocation");

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.Console(
				restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
				outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.WriteTo.File(
				loggingfilePath ?? "logs/Strategy.txt",
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
				MinimalValidationDuration = TimeSpan.FromHours(10),
				ProfitLoss = 100
			});
	}

	public StrategyReturnParameter ExecuteStrategy(List<Indicator> indicators, decimal price)
	{
		foreach (var indicator in indicators)
		{
			if (assets.All(x => x.Item2.GetStringValue()?.ToLower() != indicator.AssetName?.ToLower()))
			{
				throw new ArgumentException($"There is no requested asset with this name {indicator.AssetName}");
			}

			if (assets.All(x => x.Item1.GetStringValue() != indicator.Interval))
			{
				throw new ArgumentException($"There is no requested asset with this interval {indicator.Interval}");
			}
		}

		var fifteenminute = (SMA) indicators[0];
		var oneHour = (SMA) indicators[1];

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
			};
		}
		else if (fifteenminute.SMA5 < oneHour.SMA75)
		{
			strategyReturnParameter = new StrategyReturnParameter
			{
				TradeType = Enums.TradeType.Sell,
				TradeStatus = Enums.TradeStatus.Closed,
			};
		}

		return strategyReturnParameter;
	}
}