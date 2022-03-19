# MagicPals
This is where work intentions and related planning will be written.

#### How would you place an item on a tile that can be picked up by moving onto it?
I gave the Tile class an Items property which would store any number of items. When a character drops an item in the ItemOptionState class, the Tile class can move the item from their Inventory to the tile’s items list, as well as reparent the item from the Inventory to the tile. If the tile has any items on it, an item indicator is visible.
How about picking the item up again?

##### Try making an Inventory object for the board itself. Subclass Inventory and have it Add and Remove items in a different way, using an object pool to show and hide item indicators.
