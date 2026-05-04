# Command: write-integration-tests

## Purpose
Generate integration tests that use real infrastructure, especially PostgreSQL via Testcontainers.

## Expected inputs
- Target feature/API endpoint
- External dependencies involved
- Success and failure scenarios

## Workflow
1. Read `ai-rules/07-testing.md` and `ai-rules/08-efcore.md`.
2. Inspect existing integration test fixture style.
3. Use Testcontainers for PostgreSQL/Redis/RabbitMQ as needed.
4. Avoid EF Core InMemory provider.
5. Ensure test data cleanup by rollback or container reset.
6. Run integration tests when possible.

## Rules to apply
- `ai-rules/07-testing.md`
- `ai-rules/08-efcore.md`
- `ai-rules/13-background-jobs.md`

## Output
- Integration test class
- Testcontainer setup
- Cleanup logic

## Verification
- Real database/provider is used
- Tests are isolated
- No shared mutable state
