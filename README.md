# ZeroSum Exchange

**A real-time trading exchange where money never appears or disappears, and the tests prove it.**

> Status: in development. Features below marked *planned* are not built yet.

ZeroSum Exchange is a simulated exchange with a real order-matching engine. People and bots trade a fictional asset (GOLD) for fictional money (USD). The order book and trades update live for everyone, and the total of all balances in the system stays the same after every single trade.

No real money, no crypto. It is a training ground and an engineering showcase.

- **Live demo:** *planned*
- **Stack:** C# / .NET 10, ASP.NET Core, SignalR, PostgreSQL, SvelteKit

## Where it is now

- `Order`: an order with side, type, price and quantities. Filling more than the remaining quantity is rejected.
- `OrderBook`: resting limit orders stored as price levels, each level a FIFO queue. Reports the best bid and best ask.
- Unit tests for `Order` with xUnit.
- **Next:** order matching, so that crossing orders turn into trades.

The walkthrough below describes how the finished exchange will behave.

---

## How it works: one trading session

### Who trades

Every new account starts with **10,000 USD and 10 GOLD**.

| Participant | What it does |
|---|---|
| **People** | Sign up and trade by hand in the browser. |
| **Market-maker bot** | Always keeps buy orders slightly below the fair price and sell orders slightly above it, and earns the spread. Thanks to it, a person always has someone to trade with. |
| **Noise-trader bot** | Places small market orders from time to time so that trades happen and the price chart moves. |
| **News bot** | Every 10 minutes publishes a fictional headline ("New gold deposit found") and shifts the fair price by 3–5%. The market maker re-quotes, and the market starts to move. |

### 1. The market opens

The market maker has placed its orders. The order book looks like this:

| Sell (ask) | Price | Buy (bid) |
|---|---|---|
| 10 GOLD | 101.00 | |
| 5 GOLD | 100.50 | |
| | 99.50 | 5 GOLD |
| | 99.00 | 10 GOLD |

### 2. Anna buys 3 GOLD with a limit order at 100.50

1. The system **reserves 301.50 USD** from Anna's balance first, so she cannot spend the same money twice.
2. The engine finds the matching sell order at 100.50 and executes **3 GOLD @ 100.50**. The 100.50 level now has 2 GOLD left.
3. The trade is settled with **double-entry bookkeeping** in one database transaction:
   - Anna: −301.50 USD, +3 GOLD
   - Market-maker bot: +301.50 USD, −3 GOLD
4. Within milliseconds every browser shows the new trade ("3 @ 100.50") and the updated order book.

### 3. Person 2 places a buy order for 2 GOLD at 100.00

No one is selling at that price, so Person 2's order **rests in the book** as the new best bid. 200 USD of their balance is reserved: their available balance goes down, but the money is still theirs.

### 4. Bad news hits the market

The news bot publishes "Gold demand is falling". The fair price drops, and Anna sells 4 GOLD with a market order. Her order **walks the book**:

- 2 GOLD @ 100.00 (Person 2's order, Person 2 now owns the GOLD)
- 2 GOLD @ 99.50 (the market-maker bot)

One order, filled across two price levels. A red candle appears on the chart.

### 5. Trying to break it

These are the cases the project is built to survive:

| Attempt | What happens |
|---|---|
| Person 2 has 200 USD free and sends two 150 USD orders at the same moment from two tabs | The first is accepted, the second is rejected: *insufficient funds*. No money is created. |
| The network drops and the browser resends the same order | Same idempotency key, so no duplicate order. |
| The server crashes in the middle of trading | On restart the engine rebuilds the order book from its journal, and every balance matches to the cent. |

### 6. Leaderboard *(planned)*

Each participant's portfolio is valued at the current price, so you can see who made the most money today: the people or the bots.

---

## Guarantees

These invariants are the target of the project. Each one will be checked by automated tests after every scenario:

1. **Zero-sum:** the total of all balances per asset never changes.
2. **No crossed book:** the best bid is always below the best ask.
3. **Reservations match:** reserved funds always equal the sum of open orders.
4. **Price-time priority:** better price first; at the same price, earlier order first.

*Planned:* property-based tests (FsCheck), a 100-client race test, a crash-recovery test and a BenchmarkDotNet report with hardware and conditions stated.

## Architecture

```
Browser (SvelteKit) / Bots
        │  REST
        ▼
  ASP.NET Core API ── validation, idempotency
        │
  Fund reservation ── PostgreSQL transaction
        │
  Per-pair queue ──── System.Threading.Channels, strict order
        │
  Matching engine ─── one thread per pair, order book in memory
        │
  Trade settlement ── double entry, one transaction
        │
  SignalR hub ─────── order book and trades pushed to every client
```

**Key decision:** each trading pair is processed by a single thread, so the order book needs no locks and has no races, while money moves only inside database transactions.

## Tech stack

| Layer | Technology |
|---|---|
| Engine | C#, .NET 10, System.Threading.Channels |
| API | ASP.NET Core, JWT |
| Real-time | SignalR |
| Database | PostgreSQL, EF Core |
| Frontend | SvelteKit (Svelte 5), TypeScript, Lightweight Charts |
| Tests | xUnit, FsCheck, Testcontainers, BenchmarkDotNet, k6 |
| Infrastructure | Docker Compose, GitHub Actions |

## Roadmap

- [x] Order model and order book with unit tests
- [ ] Order matching
- [ ] API, accounts, fund reservation, double-entry ledger
- [ ] Live UI: order book, trades, order form, balances
- [ ] Bots and public demo
- [ ] Race test, crash-recovery test, benchmarks
- [ ] Candlestick chart and leaderboard

## Getting started

*Coming soon:* `docker compose up` will start the API, the frontend and the database.

## License

MIT
