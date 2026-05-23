# Middleware architecture findings

## Scope and files read

- `src/core/Conqueror.Abstractions/Messaging/IMessageMiddleware.cs`
- `src/core/Conqueror.Abstractions/Messaging/MessageMiddlewareContext.cs`
- `src/core/Conqueror.Abstractions/Signalling/ISignalMiddleware.cs`
- `src/core/Conqueror.Abstractions/Signalling/SignalMiddlewareContext.cs`
- `src/core/Conqueror.Abstractions/Iterating/IIteratorMiddleware.cs`
- `src/core/Conqueror.Abstractions/Iterating/IteratorMiddlewareContext.cs`
- `src/middlewares/authorization/Conqueror.Middleware.Authorization/**`
- `src/middlewares/logging/Conqueror.Middleware.Logging/**`
- `src/middlewares/polly/Conqueror.Middleware.Polly/**`

## Middleware integration model

Conqueror uses an onion-style chain-of-responsibility pipeline. A middleware receives a strongly typed context and calls `ctx.Next(...)` to continue to the next middleware or final handler/transport. Contexts carry the current payload, service provider, `ConquerorContext`, transport metadata, and cancellation token.

Pipeline extension methods follow the `UseX`, `ConfigureX`, `WithoutX` convention. Middleware configuration is usually mutable and supplied through fluent closures.

## Authorization

Authorization currently targets messages. `AuthorizationMessageMiddlewareConfiguration<TMessage,TResponse>` stores named sync or async checks. Each check receives `MessageAuthorizationContext<TMessage,TResponse>` with message, principal, service provider, context, and cancellation token.

Checks execute sequentially. The first failure short-circuits and throws `MessageAuthorizationFailedException`, carrying the authorization result, message payload, and transport type. The middleware reads the principal from `ConquerorContext.CurrentPrincipal`.

## Logging

Logging supports message and signal pipelines. It logs pre-execution, post-execution, and exceptions with trace/operation IDs, transport metadata, payload/response payload depending on strategy, elapsed time, and exception details.

Payload strategies include omit, raw, minimal JSON, and indented JSON. Configuration supports per-phase log levels, payload strategy factories, logger category factories, hooks, and optional stack-trace capture disabling. It uses `Microsoft.Extensions.Logging` and `System.Text.Json` contexts for AOT-aware serialization.

Logging intentionally prevents hook/logging failures from failing the business operation.

## Polly/resilience

The Polly middleware is a thin wrapper around Polly v8 `ResiliencePipelineBuilder`. Messages use a typed builder over `TResponse`; signals use an untyped builder. If no resilience pipeline is configured, execution passes through directly. Otherwise, the built pipeline wraps `ctx.Next(...)`.

Middleware order determines what is wrapped by retries, timeouts, circuit breakers, and other resilience strategies.

## Design patterns extracted

- Chain-of-responsibility with explicit continuation.
- Strong generic typing per message/signal/iterator.
- Cross-cutting concerns configured per pipeline, not globally.
- Extension-method based middleware registration.
- Transport-aware context available to middleware.
- Fail-fast authorization, resilient observability, and policy-wrapped downstream execution.

## Rust portability assessment

Middleware maps well to `tower::Service` and `tower::Layer`, plus `tracing` for structured logs. Authorization can be modeled as policy traits over a request context. Polly equivalents can be composed from `tower` timeout/retry layers plus custom circuit breaker/rate-limit/fallback layers, or a Rust resilience crate if one is chosen.

An idiomatic Rust version should likely make context explicit in the service request type rather than depend heavily on task-local ambient state.

## Risks and open questions

- There is no direct `ClaimsPrincipal` equivalent; Rust needs a domain-owned identity/claims model.
- `Microsoft.Extensions.Logging` category/provider behavior maps imperfectly to `tracing`.
- Polly has broad strategy composition; Rust parity would require careful library selection or custom work.
- Iterator middleware should be treated as a first-class design target even though current middleware packages focus on messages/signals.
