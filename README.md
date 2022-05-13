# MagicPals
This is where change summaries, work intentions, and related planning will be written.

#### 5/10
![Cone attack range restricted by walls](https://raw.githubusercontent.com/kefkajr/MagicPals/develop/Progress%20Pics/Screen%20Recording%202022-05-13%20at%2012.08.45%20PM.gif)

Walking movement and several ability ranges now take walls into account!

Deciding a logic for cone range was quite difficult. Take note of the range when the unit is facing downward. The ability range is able to reach around a corner, but the "wing" against the wall does not extend as far as the wing without a wall. It may be more fair to disallow the range from going around corners at all. Playtesting will tell if that feels fair or not.

A few more ranges need to be adjusted to deal with walls. Additionally, it may be time for some quality of life additions: skipping the walking animation on a button press, and being able to move the camera (while keeping directional control input consistent).

#### 5/10
![Some walls](https://raw.githubusercontent.com/kefkajr/MagicPals/develop/Progress%20Pics/Screen%20Shot%202022-05-10%20at%204.01.37%20PM.png)

LevelData now contains a list of TileData, which in turn contains a list of WallData. These can be saved, loaded, and displayed when the game runs!

However, they don't do anything yet. Before we even begin introducing the stealth element (the idea that enemies can't see units blocked by walls), we should start with updating the current targeting methods.

When calculating navigable paths for walking movement, walls should block the path and units should walk around them.

When selecting a target for a physical attack, a target should not be accessible if a wall divides them and the attacker. Start with the physical attack.

Should there be some exceptions? Some magical attacks should be able to target units on the other side of a wall. But the wall should block the area of effect, too. That is, a fireball dropped on the opposite side of a wall should not be able to burn back through the wall and cause damage on the near side of the wall.

#### 5/2
The Board Creator scene has been updated place to Walls on top of tiles, changing their height, thickness, and relative origin from the tile. Cool!!

The next goal would be to save and load these Walls within map data.


#### 4/6
I created the TrapSetState to place a Trap on the "current" tile, and updated the MovementSequenceState to halt the unit if a trap is found, find the Ability on the trap, and perform it at that moment. Currently, an ability being "performed" instantly calculates the result and changes the state of the targeted units with no animation – this was true before, as well. MovementSequenceState does at least print the name of the Trap, though.

I'm surprised that simply keeping a reference to a component on an object (Trap) is enough to get another component from that same object (Ability), even though the object itself is not tracked in these states. I suppose simply because the unit using the trap HAS that ability object in its hierarchy, Unity is able to keep it as a reference via the Trap component. It will be good to be aware of how this changes after any refactors that alter the hierarchies of our prefabs.

Next question is: How would you put walls at the edges of tiles?

This is an exciting question for sure. I think the steps might involve:
- Creating a Wall class like the Tile class.
  - Give it several `int` properties: origin (0 is the border of the tile, less is inside, more is outside), thickness, height.
- Give the Tile class a dictionary property that stores a Wall instance for each direction.
- Create a Wall prefab, probably just the Tile standing on its side.
- Use the tile map editor again. See what updates can be made to include the creations of Walls
  - In particular, see how a Tile's properties are used to buid and visualize each Tile when the board is constructed


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

#### Prior

This prototype is being built off of the [Tactics RPG tutorial by Liquid Fire](http://theliquidfire.com/category/projects/tactics-rpg/).
