using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AdjacentAbilityRange : AbilityRange
{

    public override List<Tile> GetTilesInRange(Board board)
    {
        List<Tile> tileList = board.Search(unit.tile, ExpandSearch);

        tileList.Remove(unit.tile);

        return tileList;
    }

    bool ExpandSearch(Tile from, Tile to)
    {
        // Skip if walls are blocking the way
        if (Tile.DoesWallSeparateTiles(from, to))
            return false;

        return (from.distance + 1) <= horizontal && Mathf.Abs(to.height - unit.tile.height) <= vertical;
    }
}