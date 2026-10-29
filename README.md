# Rat Race Escape

Rat Race Escape is a personal-finance board game. You take a profession, collect a paycheck, pay the bills, and try to buy assets that pay you enough to leave the monthly track.

It is inspired by Robert Kiyosaki's board game. The name, rules text, numbers, and art in this project are original.

Johan Karjadi ([rimkahan888](https://github.com/rimkahan888)), a software engineer with two years of experience, is building it as a one-week portfolio project.

## Tech stack

- .NET 10 (current LTS): `src/RatRaceEscape.Api` hosts the game, `src/RatRaceEscape.Engine` holds the rules
- xUnit in `tests/RatRaceEscape.Engine.Tests`
- Vue 3, Vite, and TypeScript in `web/`, with Vitest and ESLint
- GitHub Actions builds and tests both sides on pull requests and on `main`

Money in the engine is an integer count of cents. Nothing in the rules uses floating point.

## Run the API

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download), then from the repository root:

```bash
dotnet run --project src/RatRaceEscape.Api --launch-profile http
```

`GET http://localhost:5213/health` returns the service status and the engine version. In Development, the OpenAPI document is at `/openapi/v1.json`.

## Run the web app

Install Node.js 22, then:

```bash
cd web
npm install
npm run dev
```

Vite serves the client at `http://localhost:5173` and proxies game requests to the API. The opening screen seats you against your clone, or two to four people, on the island board.

## Test

From the repository root:

```bash
dotnet test RatRaceEscape.slnx
```

Client checks, from `web/`:

```bash
npm run typecheck
npm run lint
npm test
npm run build
```

`dotnet format RatRaceEscape.slnx --verify-no-changes` checks C# formatting against `.editorconfig`. CI runs the same commands.

## Roadmap

The sprint is 25–31 October 2026.

| Day | Date | Work |
| --- | --- | --- |
| 1 | 25 Oct | Solution, Vue shell, CI, editor settings, an integer money type, and a catalog of original professions |
| 2 | 26 Oct | Player, balance sheet, income statement, cash-flow calculation, game state, and API endpoints to start a game and read it |
| 3 | 27 Oct | Dice, board spaces, movement, the turn engine, and space effects such as payday, charity, a new child, and being downsized |
| 4 | 28 Oct | Decks that shuffle and draw, small deals, big deals, the market, doodads, and buying stocks, property, and businesses |
| 5 | 29 Oct | Selling and market events, bank loans and repayment, bankruptcy, leaving the rat race, the fast track, and the win condition |
| 6 | 30 Oct | Vue UI in an isometric island style, using Johan's art |
| 7 | 31 Oct | Animations, intro video, save and load, a game log, a simple AI opponent, README screenshots, and a v1.0 tag |

## License

MIT. See [LICENSE](LICENSE).
