# Command: add-api-endpoint

## Purpose
Add a new API endpoint that delegates to an existing Command or Query.

## Expected inputs
- Controller name
- Route/action
- Command or Query type
- HTTP method
- Authorization policy
- Expected response code

## Workflow
1. Read `ai-rules/04-api-contract.md` and `ai-rules/03-security-tenancy.md`.
2. Locate target Controller.
3. Add action method with API version route conventions.
4. Delegate to MediatR.
5. Return correct HTTP status code and ProblemDetails metadata.
6. Propagate CancellationToken.

## Rules to apply
- `ai-rules/04-api-contract.md`
- `ai-rules/03-security-tenancy.md`

## Output
- Controller action
- ProducesResponseType attributes
- Authorization attribute

## Verification
- Route starts with `/api/v{version:apiVersion}/`
- No business logic in Controller
- `CancellationToken` passed to MediatR
