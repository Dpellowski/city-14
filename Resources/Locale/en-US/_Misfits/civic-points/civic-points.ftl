civic-priority-1-title = Priority 1 Citizen
civic-priority-2-title = Priority 2 Citizen
civic-priority-3-title = Priority 3 Citizen
civic-priority-4-title = Priority 4 Citizen
civic-priority-5-title = Priority 5 Citizen
civic-priority-6-title = Priority 6 Citizen
civic-priority-7-title = Priority 7 Citizen

civic-priority-id-examine = Civic Priority: [bold]{$priority}[/bold]

cmd-civicpoints_get-desc = Shows Civic Points for one persistent character.
cmd-civicpoints_get-help = Usage: civicpoints_get <account-name-or-guid> <slot>
cmd-civicpoints_add-desc = Adds Civic Points to one persistent character.
cmd-civicpoints_add-help = Usage: civicpoints_add <account-name-or-guid> <slot> <amount> <reason>
cmd-civicpoints_remove-desc = Removes Civic Points from one persistent character. The balance may become negative.
cmd-civicpoints_remove-help = Usage: civicpoints_remove <account-name-or-guid> <slot> <amount> <reason>
cmd-civicpoints_set-desc = Sets the Civic Points balance for one persistent character.
cmd-civicpoints_set-help = Usage: civicpoints_set <account-name-or-guid> <slot> <amount> <reason>

cmd-civicpoints-player-not-found = Unable to find account '{$player}'.
cmd-civicpoints-invalid-slot = '{$slot}' is not a valid character slot.
cmd-civicpoints-character-not-found = Account '{$player}' has no character in slot {$slot}.
cmd-civicpoints-invalid-amount = '{$amount}' is not a valid Civic Points amount.
cmd-civicpoints-reason-required = A reason is required.
cmd-civicpoints-character-removed = The character no longer exists.
cmd-civicpoints-overflow = That operation would exceed the Civic Points storage range.
cmd-civicpoints-value = {$character} (#{$characterId}, slot {$slot}): {$points} points -- {$priority}
cmd-civicpoints-change-success = {$operation} Civic Points for {$character} (#{$characterId}): {$oldPoints} -> {$newPoints}; {$priority}.
cmd-civicpoints-hint-player = <account name or GUID>
cmd-civicpoints-hint-slot = <character slot>
cmd-civicpoints-hint-amount = <point amount>
cmd-civicpoints-hint-reason = <reason>
