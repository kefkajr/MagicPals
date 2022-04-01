# MagicPals
This is where change summaries, work intentions, and related planning will be written.

#### 3/31
I created the Trap component and updated the MovementSequenceState to do something when a trap is found.

However, no Traps can be placed on Tiles yet. A Mine ability prefab exists that has the Trap component, but it's not treated differently than other abilities yet.

It's worth testing how the AbilityTargetState and ConfirmAbilityTargetState behave with this ablity. At the very least, the PerformAbilityState will need to updated to place the Trap Ability on the tile rather than activating the ability at that moment.

#### 3/25
Items can now be picked up from the board.
Also, a description appears when items are selected in the pick up menu.
It would be awesome if there were also descriptions for abilities!
##### Change ItemDescriptionPanelController to DescriptionPanelController.
##### Create a Describable script and attach it to items and abilities alike.
##### Update ItemOptionState, ItemPickupState, and ActionSelectionState to search for a Describable object on each selection, and display the ItemDescriptionPanel for each.

UPDATE: All the above has been done. There was some trouble getting the DescriptionPanel to start in the right position, since it was using a "preferred" size that was not certain by the first frame. I used a neat trick by calling Canvas.ForceUpdateCanvases() to get the size. Seems great, I wonder what the downside is?

Next question is: How would you create an ability to put a landmine onto a tile?

##### Try adding a trap property to Tile. Update the Traverse coroutine to accept a "trap check" method action. Pass the method through MoveSequenceState. In the Traverse coroutine, on each tile, check for the presence of traps. If there is a trap, call the trap check method from the Traverse coroutine and then exit the coroutine. The passed method should start a TrapTriggeredState that outputs a Debug line that says "trapped!!"

#### 3/20
BoardInventory now exists to track items by point, and to associate item indicators with their items.

There is also a method to remove them, but there isn't a battle command to pick items up yet.

##### Add a new "Pick up" command contingent upon items being present at the point, then create a "item pick up state" to list those items, then move them to the unit inventory when selected.

#### 3/19
How would you place an item on a tile that can be picked up by moving onto it?
I gave the Tile class an Items property which would store any number of items. When a character drops an item in the ItemOptionState class, the Tile class can move the item from their Inventory to the tile’s items list, as well as reparent the item from the Inventory to the tile. If the tile has any items on it, an item indicator is visible.
How about picking the item up again?

##### Try making an Inventory object for the board itself. Subclass Inventory and have it Add and Remove items in a different way, using an object pool to show and hide item indicators.
