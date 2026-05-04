# Command: write-unit-tests

## Purpose
Generate or improve unit tests for Domain, Application validators, and pure application services.

## Expected inputs
- Target file/use case
- Test scenario(s)
- Expected behavior

## Workflow
1. Read `ai-rules/07-testing.md`.
2. Inspect existing test conventions.
3. Generate xUnit tests using FluentAssertions when available.
4. Prefer testing Domain invariants and Validator rules without database.
5. Run tests when possible.

## Rules to apply
- `ai-rules/07-testing.md`
- `ai-rules/02-cqrs-pattern.md`

## Output
- Unit test class
- Fact/Theory test methods

## Verification
- No database dependency in unit tests
- Tests do not share mutable state
- Test names express scenario and expected behavior
