# ECM Reservation System - Architecture Start

## Business Requirements (from 06-08-2025 document)
- Employee submits reservation request through `CASE Workflow`.
- Request data is transferred automatically to reservation system.
- System calculates total price (weekly rent + insurance + transportation if selected).
- Reservation is held temporarily for `24 hours` until manual payment confirmation.
- If payment is not confirmed within 24 hours, hold is released automatically.
- If capacity is full, request submission is blocked with clear message.
- Cancellation re-opens the same unit and same date range.

## Initial Clean Architecture Layout
- `ECM.ReservationSystem.Domain`
  - Core rules and constants (`ReservationRules.HoldDuration`).
- `ECM.ReservationSystem.Application`
  - Use-case contracts and orchestration interfaces.
  - `ICaseWorkflowClient` abstraction for external workflow system.
- `ECM.ReservationSystem.Infrastructure`
  - External implementation for CASE integration (`CaseWorkflowClient`).
  - Infrastructure registration (`AddInfrastructure`).
- `ECM.ReservationSystem` (Web)
  - MVC/UI, EF Core context, and current controllers/services.
  - Composition root (`Program.cs`) calls `AddApplication()` + `AddInfrastructure()`.

## CASE Workflow Integration (current scaffold)
- Config section: `CaseWorkflow` in `appsettings.json`.
- Client methods scaffolded:
  - Pull request by request id.
  - Confirm reservation in CASE by request id.
- Endpoint templates are configurable, so final CASE API contract can be plugged in quickly.

## Logging Baseline
- `Serilog` is configured in startup and reads from config.
- Console + rolling file sinks are enabled.
- Request logging middleware is enabled with `UseSerilogRequestLogging()`.
