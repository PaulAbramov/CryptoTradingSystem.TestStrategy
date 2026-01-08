# CryptoTradingSystem.TestStrategy

The decision-making module where market data and technical indicators meet logic to produce Buy and Sell signals.
Will be executed by the Backtester.
The path to the strategy.dll has to be provided in the Backtester.

### 🧩 Role in the Suite
This repository contains the trading "brain." It evaluates the indicators produced by the Calculator against user-defined rules to automate the lifecycle of a trade.

### ✨ Key Features
* **Signal Generation:** Triggers buy orders when customized indicator thresholds are met.
* **Exit Logic:** Monitors active positions to identify sell signals based on profit targets or stop-losses.
* **Backtesting Foundation:** (In Development) Modular structure allows for testing strategies against historical data.

### 🛠 Tech Stack
* **Language:** C#
* **Concepts:** Algorithmic Trading, Event-Driven Architecture.
