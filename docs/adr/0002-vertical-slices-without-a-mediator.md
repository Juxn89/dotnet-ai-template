# 0002 · Vertical slices over the layers, without a mediator

- Status: accepted
- Date: 27 Oct 2026

## Context

Layers keep the dependency rule testable (`Domain <- Application <- Infrastructure <- Api`), but grouping code by
technical type (`Services/`, `Dtos/`, `Validators/`) scatters one feature across the solution. A mediator is the
usual cure, and it adds an indirection and a dependency.

## Decision

Inside `Application`, code is grouped by feature (`Features/Notes/`). Each use case is a class with
`ExecuteAsync`, registered in DI and injected into its endpoint. Expected failures are `Error` values in a
`Result<T>`, mapped to HTTP in one place. The layers stay, and ArchUnitNET plus a reflection check enforce them.

## Consequences

"Go to definition" from an endpoint lands on the use case. Cross-cutting behaviour that a mediator's pipeline would
add (logging, validation) is done where it belongs: OpenTelemetry in ServiceDefaults, rules in the domain.
