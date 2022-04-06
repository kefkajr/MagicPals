# MagicPals
This is where change summaries, work intentions, and related planning will be written.

#### 4/6
I created the TrapSetState to place a Trap on the "current" tile, and updated the MovementSequenceState to halt the unit if a trap is found, find the Ability on the trap, and perform it at that moment. Currently, an ability being "performed" instantly calculated the change in state of the targeted units with no animation. MovementSequenceState does at least print the name of the Trap, though.

I'm surprised that simply keeping a reference to a component on an object (Trap) is enough to get another component from that same object, even though I don't know about that object's state. I suppose simply because the unit using the trap HAS that ability object, Unity is able to keep it as a reference via the Trap component. It will be good to beware of how this changes pending any refactors that alter the hierarchies of our prefabs.

Next question is: How would you put walls at the edges of tiles?

This is an exciting question for sure. I think the steps might involve:
- Creating a Wall class like the Tile class.
-- Give it ints: origin (0 is the border of the tile, -/+ is inside/outside), thickness, height.
- Give the Tile class a dictionary property that stores a Wall instance for each direction.
- Use the tile map editor again. See what updates can be made to include the creations of Walls


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
