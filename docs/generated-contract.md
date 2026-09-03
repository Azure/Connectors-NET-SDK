# Generated Connector Contract

This document defines the language-neutral contract that the Codeful SDK
generator and generated connector SDKs must preserve. It applies to every
language target. Language-specific naming and idioms remain governed by that
language's Azure SDK guidelines.

## Source of Truth

The connector Swagger document is the backend contract. A generated client may
offer an idiomatic API surface, but its request and response payloads must use
the JSON property names, primitive and collection shapes, required properties,
and referenced schemas defined by the Swagger operation.

There is no language-specific choice in the request wire format. If two SDKs
serialize different payloads for the same Swagger operation, at least one SDK is
incorrect. For `string/binary` Swagger request bodies, the wire contract is raw
bytes with the declared content type, such as `application/octet-stream`, rather
than a JSON object.

### Model and JSON Property Names

A model's customer-facing property name may follow its target language's naming
conventions and use clearer connector metadata, while its JSON property name
must remain the exact name defined by Swagger. For example, the .NET Azure Queues
model intentionally contains:

```csharp
[JsonPropertyName("TimeNextVisible")]
public string NextVisibleTime { get; set; }
```

`NextVisibleTime` is the public .NET API name, based on the customer-facing
`x-ms-summary` value `Next Visible Time`. `TimeNextVisible` is the Swagger wire
name. `[JsonPropertyName]` overrides serializer naming policies, so
`System.Text.Json` still reads and writes `TimeNextVisible` on the wire.

A difference between these names is not by itself a contract mismatch. Users
should use the model property exposed by their SDK. Contributors must preserve
the Swagger JSON name and follow [Deterministic Identity](#deterministic-identity)
when correcting a generated API name; generated models must not be edited by
hand.

### Wire Property Identity and Collisions

Every distinct reachable Swagger wire property must remain representable in the
generated model. Language normalization, summaries, capitalization, punctuation,
case differences, or connector-specific overrides must not cause one wire
property to silently replace or discard another.

When multiple wire properties project to the same language identifier, the
generator assigns deterministic unique identifiers and retains serializer
metadata for the original wire names. A natural projected identifier should keep
the unsuffixed name when possible; aliases use stable fallback names or suffixes
that do not collide with identifiers already present in the model.

Uniqueness must survive every generated naming layer. Synthetic members, such as
extension-data properties, and derived APIs, such as model-factory parameters,
can introduce collisions even after model property names are unique. Existing
capitalization rules remain unchanged for non-colliding names; disambiguation
applies only when a projected identifier is already claimed or reserved.

## Schema Reachability

The generated model contract is rooted in retained operations and the metadata
those operations reference. Request and response schemas, nested `$ref`
definitions, and discovery schemas are generated only when they are reachable
from that graph or retained by an explicit generator policy.

An unreferenced Swagger definition is not automatically part of a callable SDK
contract. Do not add its properties to an operation model unless the operation
or a documented metadata relationship reaches that definition. Conversely,
indirect reachability through nested schemas or supported discovery metadata
must not be discarded.

## Action Contract

Non-deprecated, non-internal, non-trigger operations are callable SDK methods.
Their request and response models are generated from the operation definition
and use the operation's declared HTTP method, path, parameters, and schemas.

### Operation Revision Families

Operation IDs that differ only by a leading or trailing version affix belong to
one connector revision family. First determine which revisions are supported for
the SDK, preferring public/production status over internal, preview, or deprecated
status. The generator then emits the latest eligible revision and gives it the
stable, version-free SDK name. Within that eligible sequence, an operation with
no affix is revision zero and explicit affixes provide revision order. A numeric
affix does not by itself override support status.

Older routes can remain in connector Swagger for existing Logic Apps workflows.
Their continued presence does not make them parallel SDK capabilities. Route,
parameter, or schema differences commonly explain why a new revision was needed;
those differences do not override the latest-revision rule. A new revision can,
for example, move from a preview route to a GA route, replace an indirect backend
query, or add explicit server and database selection.

When revision intent is unclear, inspect the owning connector source and history
in `AAPT-connectors`. Prefer, in order:

1. explicit family/revision or deprecation metadata;
2. public/production status over internal, preview, or deprecated status;
3. the most recently introduced revision and its owning change description.

Preserve multiple operations only when connector-owner evidence establishes that
they are independently supported actions rather than revisions of one action.
Different paths or payload shapes alone are not sufficient evidence. Do not infer
parallel support from generated output in another language; all targets must use
the same owner-backed revision decision.

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
connector actions. Outside an explicit connector curation policy, a target that
supports callable discovery methods retains a helper when an operation that
survives public route selection references its operation ID. Reachability is
transitive: a retained discovery helper can itself reference another helper.

DirectClient recognizes `x-ms-dynamic-values` on operation parameters and both
`x-ms-dynamic-schema` and its interchangeable `x-ms-dynamic-properties` alias on
parameters and schemas. Dynamic-schema pins can occur on request or response
schemas, trigger notification-content schemas, and referenced definitions at any
schema depth.

Deep traversal is schema-aware. It follows `schema`, `properties`, `items`,
`allOf`, schema-valued `additionalProperties`, and `#/definitions/` references,
starting from operation and path-item parameters (including shared `#/parameters/`
references), responses, and `x-ms-notification-content`. This deep pass collects
`x-ms-dynamic-schema` pins only. It does not treat `example`, `default`, `enum`,
`x-ms-examples`, or other instance data as schemas merely because they contain a
pin-shaped object. Definitions used only by operations removed during route or
version selection do not retain discovery helpers. Reachability starts from the
selected public and trigger surface and continues transitively through retained
discovery helpers.

Each language target declares or inherits whether it attaches callable discovery
methods. Outside explicit connector curation, public-operation collision resolution
runs first, and a target that opts out returns the resolved public surface before
discovery methods can participate in later collision or retained-type preparation.
C# and Python retain discovery methods; TypeScript currently opts out as an explicit
target policy. Swagger `x-ms-visibility: internal` describes the operation's connector
role; it does not prescribe a C# `internal`, TypeScript `protected`, or other language
access modifier. Retained helpers must remain callable by infrastructure consumers
without subclassing. A retaining target must preserve each unambiguous reachable
operation ID and its exact HTTP route. An opt-out target must still preserve the same
public action and trigger contract.

Explicit connector curation is a separate route-selection policy and takes
precedence over the per-target discovery policy. It may allow internal routes to
participate in selection and naming even for a target that normally omits
discovery methods. TypeScript's emitter continues to omit internal methods from
the callable surface after curated selection.

Discovery reachability begins only after public and trigger revision families are
resolved. A helper referenced solely by an omitted older revision is not retained.
When multiple revisions of a discovery helper are reachable from the selected
surface, the latest supported helper owns the version-free name unless explicit
connector-owner metadata identifies separate supported contracts.

Version selection is not a substitute for identifier-collision checking. Distinct
operation families can still normalize to the same language identifier. The
generator reports those residual collisions, and validation must adjudicate them
without reviving superseded revisions.

Callable discovery APIs exist for infrastructure consumers, such as the SDK LSP,
to enumerate dynamic values or schemas. Targets with a presentation mechanism must
distinguish them from customer-facing actions. C# emits a discovery-specific XML
remark and hides superseded revisions from IntelliSense. Python currently exposes
discovery methods without an equivalent marker; closing that presentation gap
remains a follow-up, not a difference in operation ID or route identity.

## Deterministic Identity

Generated operation, model, and parameter names are deterministic transformations
of the connector definition and generator policy. They are not selected by
heuristics at runtime. Any rename or route substitution must be implemented in
the generator as an explicit, tested rule so every language target can apply the
same contract decision.

Discovery methods use deterministic first-reference order. Across generator
revisions, growing the reachable set must preserve the relative order of helpers
that were already retained. A shipped method name rebinding to another route, or
a shipped route moving to a different method name, is an explicit identity change
and must never pass validation silently.

## Validation Requirements

Generated-client validation must use a pinned Swagger snapshot and pinned
generator revision. Live ARM Swagger is useful for refreshes but is not a stable
byte-for-byte test input because descriptions and metadata can change outside a
pull request.

For each generated connector and language target, pinned-contract validation must
confirm:

1. Callable actions match the intended non-trigger Swagger operations after
   documented route-selection policy is applied.
2. Trigger operations are represented as registration and callback contracts,
   not ordinary invocations.
3. Every discovery operation retained by a target maps to its exact Swagger route.
   An emitted dynamic-metadata operation ID that exists in the pinned Swagger must
   resolve to exactly one retained route; missing or ambiguous upstream references
   are input defects and must be reported.
4. Every distinct reachable wire property has one generated representation,
   including properties whose language-normalized identifiers collide.
5. Outbound JSON keys and values match the Swagger request schema exactly.
6. Inbound JSON binds to the corresponding Swagger response or trigger schema.

Validation must use techniques that can falsify those contract assertions:

- Compile generated code semantically after all naming layers are emitted.
  Syntax-only parsing is insufficient to detect duplicate members or parameters.
- For collision regressions, serialize and deserialize all affected wire names
  together rather than checking only for generated declarations.

When prior generated output exists, regression validation must additionally
confirm:

1. Previously retained discovery routes preserve relative order when new routes
   are added.
2. Existing method-name-to-route and route-to-method-name mappings do not change
   unexpectedly.
3. Intentional rebinds or renames are explicitly reviewed, recorded as breaking
   changes in the release notes, and isolated from unrelated output changes.

Cross-language validation should emit a deterministic report for each connector
that records the pinned Swagger input, generator revision, public operation
surface, reachable model surface, and request/response wire-contract comparison.
Changes to shared naming or reachability rules should also run a same-revision
catalog sweep to identify the complete additive, breaking, or output-neutral
blast radius before selecting SDK demonstration clients.
