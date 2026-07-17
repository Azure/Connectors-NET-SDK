# Generated Connector Contract

This document defines the language-neutral contract that the Codeful SDK
generator and generated connector SDKs must preserve. It applies to every
language target. Language-specific naming and idioms remain governed by that
language's Azure SDK guidelines.

## Source Of Truth

The connector Swagger document is the backend contract. A generated client may
offer an idiomatic API surface, but it must always produce requests whose JSON
property names, primitive and collection shapes, required properties, and
referenced schemas comply with the Swagger operation definition.

There is no language-specific choice in the request wire format. If two SDKs
serialize different payloads for the same Swagger operation, at least one SDK is
incorrect.

## Action Contract

Important, non-deprecated connector actions are callable SDK methods. Their
request and response models are generated from the operation definition and use
the operation's declared HTTP method, path, parameters, and schemas.

The generator may exclude a route only through an explicit, documented policy,
such as unsupported multipart transport or a curated replacement route. Such a
policy must preserve the intended SDK contract and be validated against the
Swagger input.

## Trigger Contract

Routes marked `x-ms-trigger` are not ordinary data-plane invocations. They are
registered with the Connector Namespace service as trigger configurations. The
Connector Namespace monitors the connector and POSTs a callback payload to the
application's registered endpoint when the trigger fires.

Generated SDKs provide trigger operation identifiers, parameter metadata, and
typed callback payloads where the trigger response has a JSON model. They do
not expose a trigger route as a normal action method solely because it has an
HTTP verb in Swagger. See [Connector Triggers](triggers.md) and the
[trigger-registration skill](../.github/skills/trigger-registration/SKILL.md)
for the registration and callback flow.

## Discovery Contract

Routes marked `x-ms-visibility: internal` are discovery helpers, not ordinary
public SDK invocations. The generator may retain them when a public operation's
dynamic values or dynamic schema metadata references their operation ID. In that
case, they support generated metadata and must not be promoted as customer-facing
action methods.

## Deterministic Identity

Generated operation, model, and parameter names are deterministic transformations
of the connector definition and generator policy. They are not selected by
heuristics at runtime. Any rename or route substitution must be implemented in
the generator as an explicit, tested rule so every language target can apply the
same contract decision.

## Validation Requirements

Generated-client validation must use a pinned Swagger snapshot and pinned
generator revision. Live ARM Swagger is useful for refreshes but is not a stable
byte-for-byte test input because descriptions and metadata can change outside a
pull request.

For each generated connector and language target, validation must confirm:

1. Callable actions match the intended non-trigger Swagger operations after
   documented route-selection policy is applied.
2. Trigger operations are represented as registration and callback contracts,
   not ordinary invocations.
3. Internal discovery operations remain non-public unless explicitly required by
   a documented SDK metadata mechanism.
4. Outbound JSON keys and values match the Swagger request schema exactly.
5. Inbound JSON binds to the corresponding Swagger response or trigger schema.

Cross-language validation should emit a deterministic report for each connector
that records the pinned Swagger input, generator revision, public operation
surface, and request/response wire-contract comparison.
