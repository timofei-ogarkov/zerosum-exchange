# ZeroSum Exchange

**A real-time trading exchange where money never appears or disappears, and the tests prove it.**

> Status: in development. Features below marked *planned* are not built yet.

ZeroSum Exchange is a simulated exchange with a real order-matching engine. People and bots trade a fictional asset (GOLD) for fictional money (USD). The order book and trades update live for everyone, and the total of all balances in the system stays the same after every single trade.

No real money, no crypto. It is a training ground and an engineering showcase.

- **Live demo:** *planned*
- **Stack:** C# / .NET 10, ASP.NET Core, SignalR, PostgreSQL, SvelteKit

## Where it is now

- `Order`: an order with side, type, price and quantities. Filling more than the remaining quantity is rejected.
- `IOrderBook`: the contract every order book implements. All order book tests run against it, so another implementation can be swapped in and checked by the same tests.
- `SortedDictionaryOrderBook`: price levels stored in a sorted dictionary, each level a FIFO queue. Incoming limit orders are matched against the opposite side with price-time priority, can walk several price levels, and any unfilled remainder rests in the book. Market orders take the best available price level by level; whatever cannot be filled is dropped, because a market order never rests. Reports the best bid, the best ask and the total quantity at a price level (`QuantityAt`). A resting order can be cancelled by id (`Cancel`): each price level is a `LinkedList<Order>`, and the id index stores the list node, so a cancel is one dictionary lookup and one node removal, whatever the depth of the level.
- Input validation in `Add`: a duplicate order id, a non-positive quantity, a limit order without a price, a non-positive price or a price that is not on the 0.01 tick are all rejected before the book is touched.
- `Trade`: buyer order, seller order, price and quantity. Every `Add` returns the trades it produced.
- `BitmapOrderBook`: the same contract on a fixed price grid. A price becomes a cell index with one subtraction and one division (262,144 levels at a 0.01 tick, 0.00 to 2621.43; an order above the grid is rejected). Occupied levels live in a dictionary keyed by cell index. A three-level bitmap marks occupied cells: 4,096 `ulong` words, 64 summary words above them, one top word above those. The best bid or ask is three `TrailingZeroCount`/`LeadingZeroCount` calls, one per level, whatever the distance between occupied prices. Matching, market orders, `QuantityAt` and `Cancel` behave exactly as in the first book.
- One test suite, two books: `OrderBookTests` is abstract and only its `CreateBook()` differs between the two subclasses, so every order book test runs against both implementations. 54 tests with xUnit, all green: order fills, best prices, matching in both directions, multi-level matching, partial fills, first-in-first-out at the same price, quantity per level, cancellation, input validation, market orders, the grid boundary, and best prices across far-apart summary words.
- Benchmark (BenchmarkDotNet, Apple M5, .NET 10, Release). Three scenarios. *Narrow* and *Wide*: 1,000 resting limit orders on 100 price levels per side, then 1,000 crossing orders that produce 1,000 trades; levels 0.01 apart inside one summary word, or 5.00 apart across about 25 summary words. *Cancel*: 1,000 orders on one price level, then 500 cancels from the middle of that level.

  | Order book | Narrow | Wide | Cancel |
  |---|---:|---:|---:|
  | `BitmapOrderBook` | 374 µs | 375 µs | 109 µs |
  | `SortedDictionaryOrderBook` | 1,236 µs | 1,212 µs | 107 µs |

  The bitmap book is 3.3x faster on matching because each `Add` costs integer arithmetic and three bit instructions instead of `decimal` comparisons down a tree; spreading prices across summary words costs about 1 ns per order. The cancel scenario took 2,400 µs on both books while levels were `Queue<Order>` and every cancel rebuilt the queue; with linked lists it is 22x cheaper, and most of the remaining time is the 1,000 inserts. With many orders on a single price level the matching scenarios converge, since both books then walk the same list. Run it yourself: `dotnet run -c Release --project bench/ZeroSum.Benchmarks`.
- **Next:** the API, accounts with fund reservation, and the double-entry ledger.

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
- [x] Order matching for limit orders with price-time priority
- [x] Price tick rule, market orders, cancellation
- [x] Price-grid order book with bitmap search, benchmarked against `SortedDictionaryOrderBook`
- [ ] API, accounts, fund reservation, double-entry ledger
- [ ] Live UI: order book, trades, order form, balances
- [ ] Bots and public demo
- [ ] Race test, crash-recovery test, benchmarks
- [ ] Candlestick chart and leaderboard

## Getting started

*Coming soon:* `docker compose up` will start the API, the frontend and the database.

## License

MIT
