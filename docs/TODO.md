# Follow-ups (low priority)

See [DESIGN.md](DESIGN.md) § **Roadmap / known gaps** and § **Optional (document only, no implementation)**. Hook and API coverage: [GAME_API_NOTES.md](GAME_API_NOTES.md).

- Server support: mod enforcement may all be done on the client, but for ease of distribution to players and consistency among players on the same server, all clients should use the config provided by the server.

- Potential exploit: player can allocate skill points to a specific crafting skill, equip those items, then reset their skill points and allocate them elsewhere. Since we only apply the restriction when equipping some items, they would remain equipped even when the player loses the crafting skill levels. This is a potential bypass, though not an easy one.

- CurseForge or other mod listing site main page: prepare the top-level README.md to be compatible with mod listing sites' requirements. It should be consumer-facing and provide a link to this repo for any developers to reference or contribute to. It should provide a compressed package of the mod files for distribution. Old versions may also be provided as packages, expecting not all users to be on the latest game software.
