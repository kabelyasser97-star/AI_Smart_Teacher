# Phase 6 — AI Tutor and Conversations (Implemented foundation)

## Status
Implemented and verified by build + isolated local smoke test. Cloud AI requests are disabled by default and require explicit user-provided settings/key.

## AI provider layer
- Contracts in `AI_Smart_Teacher.Application/AI/AiContracts.cs`: provider info, chat request/result, runtime/settings DTOs, `IAiProvider`, settings and conversation services.
- Pluggable providers in `AI_Smart_Teacher.AI`:
  - `NotConfiguredProvider`: clear local message, no external call.
  - `OfflineStudyProvider`: local rule-based study guidance, no external call.
  - `OpenAICompatibleProvider`: optional cloud HTTP provider (OpenAI-compatible chat completions shape). Handles missing key/endpoint, cloud-disabled, timeout/network/provider errors without exposing sensitive details.
- Provider catalog/registration: `AiProviderCatalog`, `AiRegistration.CreateDefaultProviders()`.

## Settings and key handling
- `IAiSettingsService` / `AiSettingsService` store AI settings in `SystemSettings` (`ai.*`).
- Cloud API key is not stored in source code. When provided, it is stored as `ai.provider.apiKey.protected` with `IsSensitive=true` and protected using Windows DPAPI `ProtectedData` (`CurrentUser` scope). Infrastructure is marked Windows-supported.
- Runtime resolves active provider/model/endpoint, `AllowCloudRequests`, daily limit, and attempts to unprotect key for cloud calls.

## Conversation persistence
- `ConversationService` saves user messages and assistant responses to existing `Conversations`/`ChatMessages` tables, applies a per-user daily saved user-message limit, and returns conversation history/messages.
- Uses `DbContext` (scoped) safely and local SQLite storage.

## Desktop UI
- Added AI Tutor tab: conversation messages, new conversation, input/send, and settings panel (provider/model/endpoint/key/cloud toggle/daily limit/save).
- AI settings management is permission-gated (`ai.settings.manage`); AI usage is permission-gated (`ai.use`).
- UI discloses that cloud AI is optional and needs internet + valid API key.

## Actual verification
- `dotnet build AI_Smart_Teacher.slnx -c Debug` succeeded: 0 warnings, 0 errors.
- Isolated smoke test (`tests/AI_Smart_Teacher.DbSmoke`, temp DB cleaned after run):
  - `AI_DEFAULT=NotConfigured;HAS_KEY=False;CLOUD=False`
  - Not-configured send stored a conversation/messages with clear local response: `MSG1=False:ai.not_configured:NotConfigured:True`, `CONV_COUNT=1`, `CHAT_COUNT=2`
  - Settings save protected key: `AI_SAVED=OpenAICompatible;HAS_KEY=True;CLOUD=True`, `KEY_PROTECTED=True;KEY_NOT_PLAIN=True`
  - Cloud request blocked when disabled: `CLOUD_BLOCKED=False:ai.cloud_disabled`

## Limits / not claimed
- No real cloud provider call was executed in tests (no valid external API key/network dependency in automated test).
- OpenAI-compatible provider currently parses the common `choices[0].message.content` shape; other provider schemas may need adapter work.
- No image upload/OCR/voice input in AI chat yet (phase 7).
- No per-conversation moderation/policy engine, token accounting from provider, or full admin settings UI.
- Desktop manual UI interaction was not executed in this session.
