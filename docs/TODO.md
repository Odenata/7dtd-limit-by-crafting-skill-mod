# Follow-ups (low priority)

See [DESIGN.md](DESIGN.md) § **Roadmap / known gaps** and § **Optional (document only, no implementation)**. Hook and API coverage: [GAME_API_NOTES.md](GAME_API_NOTES.md).

- Server support: mod enforcement may all be done on the client, but for ease of distribution to players and consistency among players on the same server, all clients should use the config provided by the server.

- Potential exploit: player can allocate skill points to a specific crafting skill, equip those items, then reset their skill points and allocate them elsewhere. Since we only apply the restriction when equipping some items, they would remain equipped even when the player loses the crafting skill levels. This is a potential bypass, though not an easy one.
