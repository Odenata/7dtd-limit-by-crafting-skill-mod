# Item Progression limited by Crafting Skill Mod

Project repo name: `7dtd-limit-by-crafting-skill-mod`
Colloquial name: `LimitByCraftingSkillMod`

This is an idea to slow progression down and make crafting skills more meaningful. Basically, a player can't equip or use any item for which they don't have the appropriate crafting level.

## Repo Philosophy and Sister Repo References

This repo should follow the same Bazel-first, strong hermeticity philosophy of its sister repos, `7dtd-mod-dev-tools` and `7dtd-auto-read-mod`. We can use those repos as a reference and guide for setting up this one. We should depend on and use the tools provided from `7dtd-mod-dev-tools` which also includes API names (pay special attention to the "Runtime API Workflow" documentation).

## Game Mechanic Context

The game locks crafting of specific types of items behind a player's crafting skill for that item. For example, there is a Crafting Skill named "Harvesting Tools" which controls the player's ability to craft Stone Axe, Stone Shovel, Iron Shovel, etc. As the skill level increases, players are able to craft better tiers (e.g. stone then iron then steel) and higher quality (quality values range from 1 to 5).

The game allows players to use items of any tier or quality regardless of whether the player has the relevant Crafting Skill. So if the player can at best craft a Quality 1 Stone Axe but they find a Quality 2 Stone Axe, they are allowed to used it.

Not all craftable items have a Quality attribute.

## Features

Players should not be able to equip or use in any way an item which has a higher level than they can craft.

### Wearable Items (Armor)

There is a Crafting Skill named Armor that controls crafting of armor items. This category of items is used by players by putting them in the player's armor slots where they passively provide benefits to the player. We can restrict the player's use of these items by simply preventing them from putting the items in their armor slots.

### Handheld Items (Weapons and Tools)

This is the broadest category. These are items that are used by putting the item in the player's "hand" (equipping it) and then activating one of the item's abilities; colloquially these are "left click" or "right click" abilities based on their default keybinds.

Restricting the use of these items may be difficult since the player can equip the item to their hand in a few ways. The item is in the player's hand when it is on their hotbar and they have set that as the active equipped slot. The active equipped slot can be changed by the player scrolling through their hotbar, by pressing a key (usually a number key) that is bound to that slot, or defaulting to the first slot when the player respawns.

#### Restriction Option: Prevent Adding to Hotbar

Because these items can be used from any hotbar slot, the simplest way to restrict their use is to prevent them from being added to a hotbar slot. This means we only need to check the player's Crafting Skill against the items when they are interacting with their inventory. This is a common action, but not as frequent as using an item as discussed in the "Allow Equipping but Prevent Abilities" option below. We'll need to handle the many different ways the items can be moved into the hotbar _or_ identify a method that captures all ways items can be moved to the hotbar or moved into specific inventory slots. The latter would be more resilient, so we should try that first.

This is the preferred option.

#### Restriction Option: Prevent Setting Active Slot

Preventing the player from selecting one of the hotbar slots would make the player's controls less predictible. We will discard that as an option to restrict the player. Let's simply document it as an alternative considered.

#### Restriction Option: Allow Equipping but Prevent Abilities

One reasonable option is to allow the player to hold the item in their hand, but prevent them from using the item's abilities. This would mean no need to restrict inventories, but it may cause confusion for the player when they attempt to use the item and nothing happens. It also means checking the player's crafting level every time they use an item, and we expect that to be a frequent action so it could be expensive. Let's document this as a considered alternative.

### Workstations

Workstations are placed in the world and can be interacted with by players opening the item's interface (default keybind 'E' when looking at the placed workstation). They are controlled by the "Workstations" Crafting Skill and include items like the Forge and the Workbench.

We can restrict the player's use of these items by checking against their crafting level when they attempt to open the placed item's interface. It should simply fail to open the interface. This should be relatively cheap since they aren't interacted with often and is usually done when the player is in a safe environment.

Players should still be able to perform other actions with the workstation such as picking it up.

Some of the items crafted using this skill are simply modifiers to the main workstation and need not be restricted; for example a Bellows requires a certain crafting skill but cannot be placed in world or interacted with, it is instead placed inside a Forge. Some of these modifiers could be progression-advancing such as the Crucible which allows the Forge to process Steel, but for our initial implementation we will not restrict these. We can document this as Future Work.

### Vehicles

Vehicles are placed in the world and can be interacted with by players opening the item's interface (default keybind 'E' when looking at the placed vehicle). Unlike workstations, there are many different options for interacting with vehicles, and we only want to restrict the "drive" option. Players without sufficient crafting skill should still be able to open the vehicle inventory, refill gas, pick up the vehicle, and even enter passenger seats.

The Crafting Skill for these items "Vehicles" but unlike the other crafting skills it only restricts crafting vehicle components. We'll need to provide special handling to determine whether a vehicle type should be restricted. The easy way to map a crafting component to the vehicle is by looking for the vehicle's name plus the word "Chassis". In the base game these are:
- "Bicycle Chassis" is required for "Bicycle"
- "Minibike Chassis" is required for "Minibike"
- "Motorcycle Chassis" is required for "Motorcycle"
- "4x4 Truck Chassis" is required for "4x4 Truck"
- "Gyrocopter Chassis" is required for "Gyrocopter"
We want to avoid hardcoding these names in case other vehicle types are added or the existing ones are renamed.

### Traps and Robotics

Some of the items in the "Traps" and the "Robotics" Crafting Skills are used by placing the items in the world. We can control their use in the same way we handle Handheld Items, since if they can't be held then they can't be placed in the world and used. Should not need special handling.

### Visualization

It should be clear to players what they can and can't use. When they attempt to use something that they aren't allowed to use, it should provide feedback on-screen to make it clear why they aren't allowed.

#### Inventory Indicator

It should be made obvious on the item's sprite in inventories that the player can't equip or use that item. This may be accomplished by modifying the sprite such as greying it out, or by adding an overlay such as an "X" on top of the sprite, or by recoloring the label for the item. Most likely recoloring the label is the easiest to implement and should be very obvious to the player, so that is chosen as the primary option. The others options are noted as potential Future Work in case visualization needs improvements.

#### Interaction Indicator

For in-world rather than in-inventory interactions, when a player attempts to interface or use restricted items there should be a text popup that informs them that they aren't allowed to use the item at their current crafting skill. The game already has a system for such text popups that we can hook into. The text should be red and include the following information:
- "You don't know how to use [item name]"
- "[Crafting Skill Name] [player's skill level]/[required skill level]"

### Prevent Acquisition (Optional)

Do not implement, just document.

Originally we discussed not being able to find, purchase, or be rewarded these items. This could be an interesting feature, but may be unnecessary if we can just limit usage of these items. It is also much more to implement since we would need to hook into loot generation, the trade system, and the reward system. Let's create a tentative design document for this as an optional idea, but not attempt to make it. For example, when looting if a level-based craftable item is generated for a loot container, it is automatically scrapped so the crafting component is preserved or it is removed from the loot table entirely.

### Level Restriction Offset (Optional)

Do not implement, just document.

In the default behavior of this mod, it checks whether the player has the crafting level for an item to decide whether to restrict it. It may be desirable to configure a range around the player's crafting level instead. For example, to reduce the restriction by allowing use of one tier higher than the player can craft.

## Configurability

Users should be able to configure the following:
- Which items are subject to the limit, for example to not restrict vehicle use. For maximal configurability, there should be one toggle per Crafting Skill.

### Server vs Client Config

For multiplayer games (aka Servers) the restrictions should respect the server's config, _not_ the player's config.
