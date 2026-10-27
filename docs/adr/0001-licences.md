# 0001 · OSI licences only, and no commercial or heavyweight dependencies

- Status: accepted
- Date: 27 Oct 2026

## Context

The template is copied into client projects. Whatever it depends on, every one of them depends on, and a licence
change upstream (as happened to MediatR, AutoMapper and MassTransit, now commercial) lands in all of them.

## Decision

Every package has an OSI licence, written next to its pin in `Directory.Packages.props`. Postgres for data. No
MediatR (ADR 0002), no AutoMapper (records are mapped by hand, in the store), no MassTransit (no bus is needed).

## Consequences

A few dozen lines of mapping are written by hand. A project that needs a queue picks one under the same rule.
