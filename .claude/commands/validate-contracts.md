Validate MessagePack contracts in AIAdventChallenge against the reference contracts in AIAdventChallenge.MCPServer.Stdio.

## What this does

Reads `AIAdventChallenge/Contracts/MessagePackContracts.cs` and calls the `validate_contract` MCP tool (from the `ai-advent-stdio` server) for each contract. The tool checks binary compatibility: Key indices and field types must match; field names are ignored.

## Instructions

1. Read the file `AIAdventChallenge/Contracts/MessagePackContracts.cs`.

2. If `$ARGUMENTS` is provided, validate only that contract name. Otherwise validate all three: `OrderContract`, `UserProfileContract`, `SensorReadingContract`.

3. For each contract, call the `validate_contract` tool from the `ai-advent-stdio` MCP server, passing the full file content and the contract name.

4. Report results as a table:

| Contract | Status | Details |
|----------|--------|---------|
| OrderContract | ✅ Compatible / ❌ Mismatch | description from tool |
| ... | ... | ... |

If any contract fails, summarize all mismatches clearly so the developer knows exactly what to fix.

## Requirements

- The `ai-advent-stdio` MCP server must be connected (defined in `.mcp.json`).
- Contracts are defined in `AIAdventChallenge/Contracts/MessagePackContracts.cs`.
