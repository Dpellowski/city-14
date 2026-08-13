# Character Experience

Experience belongs to a persistent character profile, not an account or character name. `CharacterId` wraps the existing database `Profile.Id`, which remains stable when a character is renamed. An account can therefore own independent experience records for every profile slot. Deleting a profile cascades its experience rows; a replacement character receives a new ID and starts at zero.

Faction tracks are Industrial, Engineering, Infestation Control, Civil Patrol, Security Administration, and Resistance. Character tracks are Cooking, Medicine, Scavenging, and Crafting. Stable kebab-case IDs such as `infestation-control` are used in storage and commands.

Characters start at level 1. Level 1 to 2 costs 100 XP, and each subsequent level costs 25 XP more. Levels are derived from stored total XP and have no configured maximum; storage is limited only by PostgreSQL `BIGINT`.

Server code should inject `ICharacterExperienceManager`. It exposes set, add, and remove operations by `CharacterId`, `ICommonSession`, or player `EntityUid`. For example:

```csharp
await experience.AddExperienceAsync(player, ExperienceGroup.Engineering, 25);
```

Session/entity operations are pinned to the permanent profile ID used when the player spawned. Removal clamps at zero. Mutations return a `CharacterExperienceChange` containing old/new totals and levels.

Administrators with the `Admin` flag can use:

```text
characterxp_list <account>
characterxp_get <account> <slot> [group]
characterxp_set <account> <slot> <group> <amount> <reason>
characterxp_add <account> <slot> <group> <amount> <reason>
characterxp_remove <account> <slot> <group> <amount> <reason>
```

Mutation commands require a reason and write an admin audit log.
