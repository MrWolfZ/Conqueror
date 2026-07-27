# Recipe migration: old `recipes/` → module-nested recipes

This document is the working guide for migrating the completed old-world (CQS) recipes to the
new-world API (Messages/Signals/Iterators). It lives in the old `recipes/` directory and is deleted
together with it once migration is complete. Agents performing a migration MUST read this document,
the conventions in `src/core/recipes/CLAUDE.md` (authoritative for structure/process), and the two
already-migrated reference recipes before starting.

## Status tracker

Update this table as work progresses.

| # | Recipe | Target location | Status |
|---|--------|-----------------|--------|
| 1 | messaging/getting-started | `src/core/recipes/messaging/getting-started` | done |
| 2 | messaging/testing-handlers | `src/core/recipes/messaging/testing-handlers` | done |
| 3 | messaging/solving-cross-cutting-concerns (PILOT) | `src/core/recipes/messaging/solving-cross-cutting-concerns` | done |
| 4 | messaging/testing-handlers-with-pipelines | `src/core/recipes/messaging/testing-handlers-with-pipelines` | done |
| 5 | messaging/testing-middlewares | `src/core/recipes/messaging/testing-middlewares` | done |
| 6 | messaging/exposing-via-http | `src/transports/http/recipes/messaging/exposing-via-http` | done |
| 7 | messaging/testing-http | `src/transports/http/recipes/messaging/testing-http` | done |
| 8 | messaging/calling-http | `src/transports/http/recipes/messaging/calling-http` | done |
| 9 | messaging/testing-calling-http | `src/transports/http/recipes/messaging/testing-calling-http` | done |
| 10 | messaging/clean-architecture | `src/core/recipes/messaging/clean-architecture` | done |
| 11 | messaging/monolith-to-distributed | `src/core/recipes/messaging/monolith-to-distributed` | done |
| 12 | signalling/getting-started (NEW, no old source) | `src/core/recipes/signalling/getting-started` | done |
| 13 | signalling/testing-handlers (NEW, no old source) | `src/core/recipes/signalling/testing-handlers` | done |
| 14 | iterating/getting-started (NEW, no old source) | `src/core/recipes/iterating/getting-started` | done |

Out of scope: all old recipes that were stubs or listed-only (everything under `recipes/eventing/`,
`recipes/streaming*/`, cross-cutting-concerns lists, expert stubs). They stay unwritten for now.

## Placement rules

Recipes live with the code they teach about; the learning path is expressed by ordering in the root
README, not by the file system.

- Core messaging/signalling/iterating recipes → `src/core/recipes/<feature>/<recipe-name>/`
- HTTP transport recipes → `src/transports/http/recipes/<feature>/<recipe-name>/`
- Middleware recipes (future) → `src/middlewares/<middleware>/recipes/...`
- Recipe directory names: kebab-case. Project names: `Conqueror.Recipes.<Feature>.<RecipeName>`
  (e.g. `Conqueror.Recipes.Messaging.ExposingViaHttp`), same pattern regardless of module.

Solution wiring:

- Core recipes: standalone `.sln` + wire into `src/core/core.sln` and root `Conqueror.sln`
  (nested solution folders mirroring directory structure; see `src/core/recipes/CLAUDE.md`).
- HTTP recipes: standalone `.sln` + wire into `src/transports/http/Conqueror.Transport.Http.sln`
  and root `Conqueror.sln`, mirroring how core recipes are wired into their solutions.

## Reference material (read before migrating)

- `src/core/recipes/CLAUDE.md` — conventions, csproj conditional-reference pattern, README
  structure, testing checklist. Authoritative.
- Reference pair for calibration: old `recipes/cqs/basics/testing-handlers` vs new
  `src/core/recipes/messaging/testing-handlers` (shows both mechanical renames and the
  judgment-area rewrites), plus `src/core/recipes/messaging/getting-started`.
- Copy `Taskfile.yml` shape from an existing new recipe (`build`, `test`, `run:completed`).

## Mechanical transformation rules

Keep the old recipe's pedagogical structure (problem statement → step-by-step file order →
summary → links) exactly. Migrate prose with terminology swapped; only rewrite where listed under
"Judgment areas". Carry over explanatory asides even if they read as stylistic; only omit prose
that references a dropped or out-of-scope concept.

Types and handlers:

- `sealed record XCommand(...)` / `XQuery(...)` → `[Message<TResponse>] public partial record X(...)`
  (drop Command/Query suffix; type MUST be `partial`).
- Command without response → `[Message]` (no type argument).
- Delete custom handler interfaces (`IXHandler : ICommandHandler<...>`) entirely; the source
  generator provides `X.IHandler`. Drop prose paragraphs explaining custom handler interfaces.
- Handler class: `class H : IXHandler` → `partial class H : X.IHandler` (handler class MUST be
  `partial`).
- `static void ConfigurePipeline(ICommandPipeline<C,R> pipeline)` →
  `static void ConfigurePipeline(X.IPipeline pipeline)`.
- Middlewares: `ICommandMiddleware`/`IQueryMiddleware` → `IMessageMiddleware`; context structs and
  `ctx.Next(...)` pattern carry over.
- Drop `sealed` and other noise keywords per recipe conventions.

Registration and invocation:

- `AddConquerorCommandHandler<H>()` / `AddConquerorQueryHandler<H>()` → `AddMessageHandler<H>()`.
- `AddConquerorCQSTypesFromExecutingAssembly()` →
  `AddMessageHandlersFromAssembly(typeof(Program).Assembly)`.
- `serviceProvider.GetRequiredService<IXHandler>().Handle(...)` → inject `IMessageSenders` and call
  `senders.For(X.T).Handle(...)`.
- Pipelines at call site: `senders.For(X.T).WithPipeline(p => ...)`.

HTTP:

- `[HttpCommand(Version = "v1")]` / `[HttpQuery(Version = "v1")]` →
  `[HttpMessage<TResponse>(HttpMethod = "POST"|"GET", Version = "v1")]`.
- Server: `AddControllers().AddConquerorCQSHttpControllers()` + `app.UseConqueror()` +
  `app.MapControllers()` → `AddConquerorHttpServerAspNetCore()` + `app.MapMessageEndpoints()`.
- Routes are flattened: `/api/v1/commands/x` and `/api/v1/queries/x` → `/api/v1/x`. Update all
  route strings, curl examples, and test assertions.
- Client: `AddConquerorCQSHttpClientServices()` + `AddConquerorCommandClient<I>(b => b.UseHttp(addr))`
  → no client registration; `senders.For(X.T).WithTransport(b => b.UseHttp(...)).Handle(...)`.

Packages and projects:

- `Conqueror.CQS` / `Conqueror.CQS.Abstractions` → `Conqueror` / `Conqueror.Abstractions`.
- `Conqueror.CQS.Middleware.Logging` → `Conqueror.Middleware.Logging`; retry/resilience →
  `Conqueror.Middleware.Polly`. `Conqueror.CQS.Middleware.DataAnnotationValidation` has NO
  new-world equivalent (see judgment areas).
- `Conqueror.CQS.Transport.Http.*` → `Conqueror.Transport.Http.*`.
- Namespaces/project names: `Conqueror.Recipes.CQS.Basics.X` → `Conqueror.Recipes.Messaging.X`.
- csproj: conditional references gated on `$(SolutionName)` == standalone solution name
  (PackageReference standalone, ProjectReference otherwise). When using project references you MUST
  add the `Conqueror.SourceGenerators` project as an Analyzer-only reference (copy the pattern from
  an existing new recipe csproj; adjust relative paths for the recipe's depth/module).
- Do NOT copy `.editorconfig` / `global.json` / `*.sln.DotSettings` from the old recipes or
  `recipes/.template/` — new recipes rely on ambient repo tooling.
- Add a per-recipe `Taskfile.yml` (`build`, `test`, `run:completed` as applicable).
- HTTP recipes: ensure `src/transports/http/recipes/Directory.Packages.props` (disables central
  package management) covers them — it already exists.

Tests:

- Test infrastructure: composition-based `TestHost` (private ctor + static `Create()`,
  `IAsyncDisposable`, `await using` per test) — NEVER a `TestBase` base class. Copy the pattern from
  `src/core/recipes/messaging/testing-handlers/.completed/.../TestHost.cs`.
- Mocking library: NSubstitute, not Moq (`Received(1)`, `DidNotReceive()`, direct substitute
  objects). Test handlers black-box via `IMessageSenders`, never via handler classes directly.
- Given-when-then test naming as in the migrated testing-handlers recipe.

## Judgment areas (genuine re-authoring, not find/replace)

- Any prose framed around "commands vs queries" as distinct type-level concepts must be reworded:
  there is only Message now; the read/write distinction is conventional.
- `calling-http`: the old middle section teaches registration-time client factories and custom
  transport clients. The new world configures transport at the call site. This section must be
  re-authored around `.WithTransport(b => b.UseHttp(...))`, shared contracts projects carrying
  `[HttpMessage]`, and call-site/sender pipelines.
- `testing-calling-http`: mocking strategy changes accordingly (no client registration to replace;
  substitute the transport or use an in-process handler instead).
- `testing-middlewares`: `DataAnnotationValidationCommandMiddleware` and `RetryCommandMiddleware`
  don't exist. Use the hand-built middleware(s) from solving-cross-cutting-concerns and/or
  hand-built equivalents; `Conqueror.Middleware.Polly` exists if a shipped middleware is needed.
- Mocking-philosophy prose: follow the already-migrated testing-handlers recipe (prefer real
  dependencies, mock only where side effects demand it).

## Known new-world gaps (do not reference as available)

- No validation middleware package.
- No HTTP transport for iterators (in-process iterators work fully).
- No fire-and-forget signal broadcasting strategy (sequential/parallel exist).
- No authorization middleware for signals (by design).
- No shipped test-host/testing utility package — recipes hand-roll `TestHost`.

## Build environment and analyzer notes (learned in pilot)

- `.completed` sources MUST pass the repo root `.editorconfig` analyzers (StyleCop/Roslynator/
  Meziantou become build ERRORS in project-reference mode, i.e. `task run:completed` and the
  core/root solution builds). Frequent hits: SA1208 (System usings first), MA0003 (named args),
  RCS1236 (prefer exception filters), RCS1248/MA0141 (`is not null`), RCS1078 (`""` over
  `string.Empty`). Run `task fmt` and a project-reference-mode build before considering a recipe
  done.
- README snippets may use a simpler teaching style than the analyzer-clean on-disk sources (the
  existing recipes already do this, e.g. `!= null` in prose vs `is not null` on disk). "Snippets
  match sources" means semantically, not character-for-character.
- Conditional references: the new pattern gates a `ShouldReferencePackages` property on
  `$(SolutionName) == '<standalone recipe sln name>'` — PackageReference standalone, ProjectReference
  (+ SourceGenerators Analyzer) otherwise. This is INVERTED relative to the old recipes'
  `ShouldReferenceProjects`.
- Pin package versions to match the reference recipes: `Conqueror 0.8.2-beta.3`,
  `Microsoft.Extensions.DependencyInjection 8.0.1`.
- `dotnet new sln` fails in this container (read-only template engine) — copy a reference recipe's
  `.sln` and swap names/GUIDs (`cat /proc/sys/kernel/random/uuid`; `uuidgen` is absent).
  `dotnet sln <sln> add <csproj> --solution-folder <nested/path>` works and reuses existing folders.

## HTTP-module notes (learned in exposing-via-http)

- Packages standalone: `Conqueror.Transport.Http.Server.AspNetCore 0.8.2-beta.3` (nuget.org),
  Swashbuckle `8.1.4` (matches `examples/quickstart`).
- Project-reference paths from a starting project under
  `src/transports/http/recipes/messaging/<name>/` (one more `../` for `.completed`):
  core `../../../../../../core/Conqueror/Conqueror.csproj`, generators
  `../../../../../../core/Conqueror.SourceGenerators/Conqueror.SourceGenerators.csproj` (Analyzer,
  `ReferenceOutputAssembly=false`), server `../../../../Conqueror.Transport.Http.Server.AspNetCore/`.
  The HTTP transport has NO generators of its own — the core Analyzer reference suffices.
- `AddConquerorHttpServerAspNetCore()` already calls `AddEndpointsApiExplorer()`; only
  `AddSwaggerGen()` is needed for Swagger. `ApiGroupName` requires
  `DocInclusionPredicate((_, _) => true)` or the endpoint vanishes from the default swagger doc.
- Minimal-API endpoints created by the transport perform NO data-annotation model validation —
  don't port the old ASP.NET-validation demos; point at the solving-cross-cutting-concerns recipe.
- `IHttpCommandPathConvention` has no analog; `Path`/`PathPrefix`/`FullPath` attribute properties
  replace it. Custom controllers → custom minimal-API endpoints calling `IMessageSenders`
  (pattern: transport's `Tests.TopLevelProgram`). `SuccessStatusCode` attribute property exists.
- Default route: `api[/<version>]/<camelCaseTypeName>`; GET messages bind from query string;
  no-response messages return 204. Old `OperationId` → `Name`.
- `global using Conqueror;` in Usings.cs is an IDE0005 build ERROR in project-reference mode when
  all types sit in a `Conqueror.Recipes.*` namespace — put `using Conqueror;` only where needed.
- `task fmt` at the http module level fails on untracked files; run
  `dotnet jb cleanupcode <explicit .cs files> --settings=/home/dev/src/conqueror/.editorconfig`
  manually instead.
- Root `Conqueror.sln` contains empty legacy folders `transports/http/recipes/{messages,signals,
  iterators}/completed` — leave them; create the new `messaging/...` folders alongside
  (`dotnet sln add --solution-folder` reuses the existing chain).
- Client side: `AddConquerorHttpClient()` registers `IMessageSenders` + sender factory; call
  `senders.For(X.T).WithTransport(b => b.UseHttp(<Uri>).WithHttpClient(client))` — the `UseHttp`
  address is ignored when the supplied `HttpClient` has a `BaseAddress`. `UseHttp` is constrained
  to `IHttpMessage`-attributed types; custom endpoints need a plain `HttpClient`.
- Client failure type: `Conqueror.HttpMessageFailedOnClientException` (`Response`, `StatusCode`),
  thrown for any non-success status. Unhandled handler exceptions surface as a real 500 via
  `WebApplicationFactory` and do NOT pollute `dotnet test` output.
- Sonar S4457 (error in project-ref mode) only flags synchronous parameter-guard throws in async
  methods — domain exceptions thrown after an `await` are fine (pattern:
  `CounterNotFoundException` in getting-started).
- `Microsoft.AspNetCore.Mvc.Testing 8.0.19` works; test package versions mirror testing-handlers.

## Per-recipe process

1. Migration agent produces the recipe (README + projects + Taskfile + solutions wiring) following
   this doc and `src/core/recipes/CLAUDE.md`.
2. Review agent critically reviews against this doc, the reference recipes, and the old recipe
   (checking no pedagogical content was lost and no old-world API leaked through).
3. Validation agent builds and runs everything per the checklist below.
4. Update the status tracker above and the corresponding link in the root `README.md` recipe
   section (old `recipes/...` path → new path).

## Verification checklist (all must pass)

- Standalone: `cd <recipe-dir> && task build` (and `task test` / `task run:completed` as applicable).
- Module solution builds: `cd src/core && task build` (core recipes) or
  `cd src/transports/http && task build` (HTTP recipes).
- Root solution builds: `task build` from repo root.
- All `[view completed file](...)` links in the README resolve to existing files.
- Next-step links to not-yet-migrated sibling recipes are acceptable when the relative target path
  is where that recipe will live per the status tracker — do not flag as broken.
- README code snippets match the `.completed` sources.
- Root README link for the recipe updated.
