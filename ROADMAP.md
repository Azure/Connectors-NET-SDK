# DirectClient SDK Roadmap

## Overview

This document tracks the progress of generating DirectClient SDK code for Logic Apps connectors, starting with high-value actions and expanding to triggers in later phases.

## Current Status

| Component | Status | Notes |
|-----------|--------|-------|
| BPM Generator | ⏳ Feature branch awaiting human review | Fixes for WhenWritingNull, nullable types, SharePoint fixes |
| SDK (Office365) | ✅ Complete | WhenWritingNull fix merged |
| SDK (SharePoint) | ✅ Complete | Validated end-to-end |
| POC | ✅ Complete | Office365 email + SharePoint lists validated |

---

## Prioritized Connector List

Consolidated from: Consumption usage data (30-day), partner team tier analysis (90-day), and agentic solution value assessment.

### Phase 1: M365 Core (Agentic Foundation)

These connectors form the backbone of modern agentic solutions with Teams as front-end, Graph as API backbone, and SharePoint/OneDrive for knowledge.

| Priority | Connector | Usage Rank | Partner Tier | 90-Day Executions | Agentic Value | Status |
|----------|-----------|------------|--------------|-------------------|---------------|--------|
| 1.0 | **Office 365** | #2/#10 | Tier 1 | 2.67B | Email/Calendar backend | ✅ Complete |
| 1.1 | **SharePoint Online** | #1/#3 | Tier 1 | 5.74B | Knowledge base, file/list operations | ✅ Complete |
| 1.2 | **Microsoft Graph** | — | Tier 2 | 820M | Unified M365 API backbone | ⬜ Not started |
| 1.3 | **Microsoft Teams** | — | Tier 2 | 550M | Preferred front-end, messaging | ⬜ Not started |
| 1.4 | **OneDrive for Business** | #6 | Tier 2 | 719M | User files, knowledge base | ⬜ Not started |
| 1.5 | **Office 365 Users** | #16 | Tier 1 | 2.92B | User directory, profile data | ⬜ Not started |

### Phase 2: Data & Integration (Enterprise Core)

High-volume connectors for enterprise data and integration scenarios.

| Priority | Connector | Usage Rank | Partner Tier | 90-Day Executions | Notes | Status |
|----------|-----------|------------|--------------|-------------------|-------|--------|
| 2.1 | **Dataverse (CDS)** | #12 | Tier 1 | 2.93B | Power Platform integration | ⬜ Not started |
| 2.2 | **Excel Online Business** | #9 | Tier 2 | 957M | Spreadsheet operations | ⬜ Not started |
| 2.3 | **Dynamics 365** | — | Tier 2 | 315M | CRM record operations | ⬜ Not started |
| 2.4 | **Salesforce** | — | Tier 2 | 127M | CRM operations | ⬜ Not started |

### Phase 3: Azure Services

Azure-native connectors (some may overlap with existing Functions bindings).

| Priority | Connector | Usage Rank | Partner Tier | Notes | Status |
|----------|-----------|------------|--------------|-------|--------|
| 3.1 | **Azure Blob Storage** | #8/#15 | — | May overlap with Functions binding | ⬜ Evaluate |
| 3.2 | **Azure Log Analytics** | #7 | — | Query/ingest operations | ⬜ Not started |
| 3.3 | **Event Hubs** | #14 | — | May overlap with Functions binding | ⬜ Evaluate |
| 3.4 | **Service Bus** | #4/#13 | — | Functions already supports | ⚠️ Skip (native) |

### Phase 4: Enterprise Systems

Enterprise messaging, databases, and LOB systems.

| Priority | Connector | Usage Rank | Partner Tier | 90-Day Executions | Status |
|----------|-----------|------------|--------------|-------------------|--------|
| 4.1 | **JDBC** | — | Tier 2 | 1.24B | Generic database connectivity | ⬜ Not started |
| 4.2 | **IBM MQ** | — | Tier 2 | 1.02B | Enterprise messaging | ⬜ Not started |
| 4.3 | **SMTP** | — | Tier 2 | 916M | Email sending | ⬜ Not started |
| 4.4 | **SFTP** | — | Tier 2 | 251M | File transfer | ⬜ Not started |
| 4.5 | **Oracle Database** | — | Tier 3 | 109M | Enterprise DB | ⬜ Not started |
| 4.6 | **ServiceNow** | — | Tier 3 | 41M | ITSM operations | ⬜ Not started |
| 4.7 | **Snowflake** | — | Tier 3 | 60M | Data warehouse | ⬜ Not started |
| 4.8 | **SAP** | — | Tier 3 | 25M | ERP integration | ⬜ Not started |
| 4.9 | **DocuSign** | — | Tier 3 | 27M | E-signature | ⬜ Not started |

### Phase 5: Triggers (Active Design → **Implementation Planning**)

Trigger support has moved from design to implementation planning. The BPM runtime trigger architecture has been analyzed, extraction paths identified, and work broken into 12 tasks across 4 sub-phases.

> **Note on architecture:** The trigger support plan proposes a **Timer+Blob+Queue polling architecture** as the **Phase 5a worker library POC**. This is a fast-iteration stepping stone to validate the model. The long-term **Phase 5b host extension** (described under "Implementation Approach" below) uses connector-infrastructure-managed webhooks where Functions does not poll. Both approaches coexist: the worker library POC proves the end-to-end developer experience; the host extension delivers the production webhook-push model. The trigger support plan's sub-phases (5A–5D) are task groupings within Phase 5a.

**Plans and analysis:** Internal design documents are maintained for trigger architecture, implementation planning, and reference implementation analysis.

#### Team Roles

| Team | Responsibility |
|------|----------------|
| **Connectors team** | SDK for calling actions + typed trigger data types. Connector-side trigger notification delivery to Functions. |
| **Functions team** | Implement the Functions extension(s) for triggers and bindings — both strongly-typed and generic approaches, across all supported languages (C#, Python, Node.js, Java). |
| **Functions PM** | Prioritization of connectors, GTM plan, productization. |

#### Connector Resource Access Strategy

The SDK currently uses `Microsoft.Web/connections` resources directly for both actions and trigger registration. A new **AI Gateway** resource (`Microsoft.Web/AiGateways`) is in development that exposes connections and triggers as data-plane resources without requiring Logic Apps. The AI Gateway is available in First Release / TIP regions today but is **not yet generally available**.

**Current approach:** Continue using `Microsoft.Web/connections` direct access for all SDK work (actions and triggers).
**Future migration:** Transition to AI Gateway API when it reaches GA and the API surface stabilizes. This is tracked as a backlog item (see [Execution Plan](#execution-plan)).

#### Architecture Decision: Webhook Model (Event Grid Analogy)

Connector triggers follow the **Event Grid webhook pattern**:

1. **Function app registers** a callback URL with the connector service at deployment time (cloud) or F5 time (local)
2. **Connector service monitors** for events on its own compute (Logic Apps multi-tenant infrastructure)
3. **Events push to the function** via HTTP callback — the function wakes up and processes the event
4. **No scale controller work** needed on Functions side (HTTP push triggers scaling naturally)

This means connector triggers do **not** run on Functions compute. The connector infrastructure handles all polling/monitoring (including any fallback polling if webhook subscribe fails) and only delivers an HTTP push when an event occurs; Azure Functions never performs polling on its own compute.

**Webhook expiry/refresh:** The connector infrastructure owns webhook lifecycle management, including automatic refresh of expiring webhook subscriptions. The Functions extension does **not** need to handle webhook renewal. (Settled 2026-03-10.)

#### Implementation Approach: Worker Library → Extension

| Phase | Approach | Scope | Languages |
|-------|----------|-------|-----------|
| **5a** | Worker library (C# NuGet) | Quick POC, end-to-end proof | C# only |
| **5b** | Host extension | Production, cross-language | All Functions languages |

> **Naming:** `5a`/`5b` refer to the two implementation phases (worker library → host extension). Within Phase 5a, the trigger support plan groups tasks into sub-phases `5A`–`5D`.

**Rationale**: Worker library is a 1-2 day POC effort. Host extension (like Durable Functions, MCP) requires deeper Functions runtime integration but works across all languages. Start with worker library, prove the model, then build the extension.

**Reference implementations**:
- Azure Functions Event Grid extension — similar webhook registration + callback pattern.
- [azure-functions-connectors-python](https://github.com/anthonychu/azure-functions-connectors-python) — Python sample showing connector trigger + action usage in Functions, including a Teams reply example. Must be studied in detail (see [Execution Plan](#execution-plan)).

#### Local Development (F5 Experience)

Local webhook triggers require:
1. **Dev tunnels / ngrok** to expose a local port as a public endpoint
2. **Webhook registration** at F5 time with the connector service pointing to the tunnel URL
3. **Separate dev/prod connectors** — configured via `local.settings.json` (dev) vs app settings (prod)

The Functions team is already improving local CLI tooling for tunnel integration.

#### Design Decisions from Brainstorm

| Decision | Rationale |
|----------|-----------|
| **No output bindings** | Use SDK action clients directly. Wait for customer feedback on whether input bindings (DI-injected clients) are needed. |
| **Strongly typed trigger payloads** | SDK code generation extends to triggers — each connector produces typed trigger output models. |
| **Generic + typed trigger attributes** | `[ConnectorTrigger(type, operation)]` for broad coverage + per-connector typed attributes (e.g., `[Office365OutlookTrigger]`) for popular connectors. |
| **Event notification, not content push** | For large files (SharePoint, Outlook attachments), trigger delivers metadata; user code fetches content separately via action client. |
| **No streaming initially** | Extension route has gRPC limitation for large payloads. Start with notification model; add streaming later. |
| **Deployment-time registration (cloud)** | Webhook relationships set up at deploy/provision time, not runtime. Locally, at F5 startup time. |
| **Webhook refresh owned by connector infra** | The connector platform handles webhook expiry and automatic re-registration. The Functions extension does not manage webhook lifecycle. |

#### Open Questions (as of 2026-03-13)

| Question | Owner | Context |
|----------|-------|---------|
| **VNet-locked Function Apps** — Pure webhooks break when inbound traffic is restricted (Private Endpoints, IP restrictions). Do we need pull delivery / polling from Functions side? | Functions + Connectors | Event Grid hybrid model (webhook + pull) suggested. May need Trigger Monitor / Scale Controller outbound polling as alternative. |
| **Local F5 tunnel setup** — How to automatically configure dev tunnels for webhook triggers across all languages? | Functions | Functions team improving local CLI tooling. We need to ensure our webhook registration works with tunnel URLs. |
| **RBAC for connector resources** — What Azure role(s) does the Function App identity need? New role required? | Connectors | Need to document minimum RBAC for `Microsoft.Web/connections`. |
| **Multi-language trigger data types** — Python decorators, Java annotations, Node.js methods for trigger payloads | Connectors + Functions | Our C# SDK generates typed trigger models. Need equivalent type generation strategy for Python/Node/Java. |

#### Known Risks

| Risk | Source | Impact | Mitigation |
|------|--------|--------|------------|
| **Webhook trigger 404 on subscribe** | [M365 Agent POC](https://github.com/coreai-microsoft/m365-agent-poc) found that `ApiConnectionNotification` (webhook-style) triggers return 404 on subscribe in LA Standard. Had to fall back to polling. | If this is a connector infrastructure issue (not LA-specific), our webhook model may not work for all connectors. | Validate webhook registration works from Functions extension using a different code path than LA's `ApiConnectionNotification` handler. If connector-level, design polling fallback. |
| **Teams connector gaps in LA Standard** | Same POC. LA Standard uses a different APIM gateway that restricts Graph API endpoints — chat/DM paths return 404. | Teams trigger feasibility may be limited to channel-level events. | Evaluate Graph API direct calls as alternative. Prioritize Office 365 email trigger (confirmed working) for Phase 5a POC. |
| **Catch-up storm on polling fallback** | If connector is down during polling, queued events process all at once on restart, potentially overwhelming the function. | Relevant only if we need polling fallback for connectors where webhook subscribe fails. | Batch-size limits in the connector service's polling loop; backpressure handling in the Functions extension. User function code should not need to throttle. |
| **VNet inbound restrictions** | Webhook-only model fails for Function Apps with locked-down inbound. | Enterprise customers with Private Endpoints cannot receive webhook callbacks. | Design a polling fallback where Functions Trigger Monitor performs outbound calls to connector service. Similar to Event Grid pull delivery model. |

#### Priority Triggers

| Connector | Trigger Type | 90-Day Executions | Status |
|-----------|--------------|-------------------|--------|
| Office 365 | Email arrival | 25.7B | ⬜ Design |
| SharePoint Online | File/list changes | 22.9B | ⬜ Design |
| Service Bus | Message trigger | 10.3B | ⬜ Evaluate overlap with native Functions binding |
| OneDrive for Business | File trigger | 5.3B | ⬜ Design |
| SQL | Row trigger | 2.6B | ⬜ Design |

**Most popular SaaS connectors for triggers**: Outlook and Teams for SaaS; Service Bus for PaaS.

---

## Execution Plan

### Immediate Next Steps

1. ~~**Generate SharePoint Online connector** (Priority 1.1)~~ ✅ Complete
   - [x] Run generator: `--directClient --connectors=sharepointonline`
   - [x] Add generated code to SDK repo
   - [x] Verify compilation (fixed 2 generator bugs)
   - [x] Create PR
   - [x] Validate end-to-end with POC (GetSharePointLists)

1. ~~**Study Python connector reference implementations**~~ ✅ Complete
   - [x] Clone [azure-functions-connectors-python](https://github.com/anthonychu/azure-functions-connectors-python) locally
   - [x] Analyze the Teams, Office 365, and SharePoint samples — trigger registration, callback handling, payload shapes
   - [x] Analyze [m365-agent-poc](https://github.com/coreai-microsoft/m365-agent-poc) — Brain-and-Nerves pattern, Logic App triggers
   - [x] Analyze Codeful Workflows SDK (`logicapps-sdk`) — code-to-workflow compiler approach, generated trigger classes, trigger type distinctions
   - [x] Document how trigger attributes are defined (Python decorators vs .NET attributes vs C# builder API)
   - [x] Identify patterns applicable to our C# worker library POC (Phase 5a)
   - [x] Identify gaps or differences from our current design assumptions
   - [x] Write up findings (internal analysis document)

1. **Generate Microsoft Graph connector** (Priority 1.2)
   - [ ] Run generator: `--directClient --connectors=graph` (or similar)
   - [ ] Add generated code to SDK repo
   - [ ] Verify compilation
   - [ ] Create PR

1. **SDK documentation**
   - [x] Add README with usage examples
   - [x] Document connection setup process ([docs/connection-setup.md](docs/connection-setup.md))
   - [ ] Add NuGet package instructions (future)

### Per-Connector Validation Checklist

For each new connector, complete these steps:

#### Code Generation
- [ ] Generate DirectClient code: `--directClient --connectors={connectorName}`
- [ ] Add to SDK repo as `Generated/{Connector}Extensions.cs`
- [ ] Fix any compilation errors (document generator bugs found)
- [ ] Create PR

#### Connection Setup (see [docs/connection-setup.md](docs/connection-setup.md))
- [ ] Create connection from Logic Apps Standard app
- [ ] Get connection runtime URL: `az resource show ... --query "properties.connectionRuntimeUrl"`
- [ ] Add access policy for your identity
- [ ] Wait for ACL propagation (1-5 minutes)
- [ ] Test via CLI: `az rest --method GET --uri "{runtimeUrl}/{path}" --resource "https://apihub.azure.com"`

#### POC Validation
- [ ] Add runtime URL to `local.settings.json`
- [ ] Create test endpoint in POC
- [ ] Validate end-to-end request

---

## Related Analysis

- **Azure Functions + Logic Apps Connector Integration** — Idea proposal from the Azure Functions team for bringing Logic Apps connectors into Functions as first-class triggers and action clients. Our SDK is the implementation of the "action clients" portion; the proposal validates our architecture and identifies future work (triggers, OBO, Python SDK).
- **Connector Trigger Support for Azure Functions (2026-02-20)** — Cross-team brainstorm establishing the webhook/Event Grid pattern for triggers, worker library → extension phased approach, and local dev requirements.
- **[Python connector samples](https://github.com/anthonychu/azure-functions-connectors-python)** — Reference implementation showing connector trigger + action usage in Functions (Python). Timer+Blob+Queue architecture with cursor-based polling. Analyzed internally.
- **[M365 Agent POC](https://github.com/coreai-microsoft/m365-agent-poc)** — Working POC using today's GA connectors with Functions (LA Connector as trigger layer, Functions as code runner). Found v1/v2/v3 connector version surprises and webhook subscribe 404 issues. Analyzed internally.
- **[Codeful Workflows SDK](https://github.com/AzureAD/logicapps-sdk)** (`logicapps-sdk`) — C# code-to-workflow compiler. Generates typed trigger classes for all 1000+ managed connectors. Compiles to workflow.json and delegates trigger lifecycle to Logic Apps runtime. Different approach from our direct-invocation SDK but shares swagger-to-trigger-model generation. Analyzed internally.
- **BPM Runtime Trigger Architecture** — Analyzed the Logic Apps runtime trigger implementation: three trigger strategies (polling, pure webhook, notification hybrid), dual-job provisioning, class hierarchy, coordination protocol. Extraction plan and trigger taxonomy documentation available internally.
- **AI Gateway API Design** — Control plane API design for `Microsoft.Web/AiGateways` (internal document).

### Backlog

1. **Transition to AI Gateway API** (`Microsoft.Web/AiGateways`)
   - The AI Gateway resource exposes connections and triggers as data-plane resources without requiring Logic Apps. Available in First Release / TIP regions (as of 2026-03-05) but not yet GA.
   - **Blocked on:** AI Gateway reaching GA with stable API surface.
   - [ ] Monitor AI Gateway API availability and documentation
   - [ ] Evaluate migration path from `Microsoft.Web/connections` direct access
   - [ ] Update SDK connection setup to support AI Gateway resource type
   - [ ] Update POC to validate AI Gateway–based connection flow
   - [ ] Update `docs/connection-setup.md` with AI Gateway instructions

1. **Multi-language trigger SDK** (Python, Node.js, Java)

1. **Connection setup automation** (VS Code extension / LSP)
   - AI agents can already automate the full connection lifecycle via the `connection-setup` skill (`.github/skills/connection-setup/SKILL.md`): AI Gateway creation, connection creation, OAuth consent link, access policies, and settings injection.
   - **Next:** Build native VS Code extension support (or LSP ConnectionsService) for in-editor connection creation with UI, following the building blocks documented in the skill file.
   - [ ] LSP ConnectionsService: detect existing connections, guided creation flow
   - [ ] VS Code command palette: "Add Connector Connection" with connector picker
   - [ ] Auto-inject connection settings into `local.settings.json` per connection settings schema
   - [ ] Auto-detect Function App MSI and offer one-click access policy grant
   - [ ] Support both Format A (`__aiGatewayName` + `__connectionName`) and Format B (`__connectionRuntimeUrl`)
   - The Functions team needs trigger data type definitions for Python decorators, Node.js methods, and Java annotations.
   - **Depends on:** Phase 5a C# POC proving the trigger model.
   - [ ] Design cross-language type generation strategy
   - [ ] Evaluate whether CodefulSdkGenerator can emit Python/TypeScript/Java models
   - [ ] Coordinate with Functions team on integration points

## Dependencies

| Dependency | Status | Impact |
|------------|--------|--------|
| BPM generator PR merged to master | ⏳ Awaiting review | Can work from feature branch, but need master for stability |
| OAuth connections for testing | ✅ Office365 works | Need connection setup for each new connector |
| AI Gateway GA | ⬜ Not yet GA | Using `Microsoft.Web/connections` direct access until then |
| Functions extension for triggers | ⬜ Functions team owns | We provide SDK + connector-side delivery; they build the extension |
| MWF sync cadence | ✅ Established 2026-03-13 | Cross-team alignment on Connectors for Functions |

---

## Success Metrics

| Metric | Target | Current |
|--------|--------|---------|
| Connectors with Actions | 10+ | 2 (Office365, SharePoint) |
| POC operations validated | 5+ | 3 (SendEmail, GetCategories, GetSharePointLists) |
| Partner adoption | 1+ team | 0 |

---

## Change Log

| Date | Change |
|------|--------|
| 2026-04-03 | Initial public release with Office365, SharePoint, and Teams connector clients. Trigger support in active design. |

---

## Lessons Learned

### Generator Issues Discovered

| Issue | Connector | Fix | BPM Commit |
|-------|-----------|-----|------------|
| Multi-line descriptions break XML doc syntax | SharePoint | `EscapeXml` now replaces `\r\n`, `\n`, `\r` with spaces | `5eb6bf0` |
| Property name same as class name (CS0542) | SharePoint | Append `Value` suffix when property name matches type name | `5eb6bf0` |
| `null` properties sent to APIs that reject them | Office365 | Added `JsonIgnoreCondition.WhenWritingNull` to JsonOptions | Earlier commit |

### Per-Connector Notes

| Connector | File Size | Operations | Issues Found | Notes |
|-----------|-----------|------------|--------------|-------|
| Office365 | ~170KB | ~50 | WhenWritingNull | Clean generation after fix |
| SharePoint | ~100KB | ~110 | 2 (newlines, property collision) | Validated with GetAllTablesAsync listing 8 libraries |
