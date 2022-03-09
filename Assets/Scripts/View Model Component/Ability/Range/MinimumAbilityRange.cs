using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MinimumAbilityRange : AbilityRange
{

    public int blindSpace;

    public override List<Tile> GetTilesInRange(Board board)
    {
        List<Tile> tileList = board.Search(unit.tile, ExpandSearch);
        List<Tile> blindSpots = board.Search(unit.tile, BlindSearch);

        foreach (Tile t in blindSpots)
        {
            tileList.Remove(t);
        }
        return tileList;
    }

    bool ExpandSearch(Tile from, Tile to)
    {
        return (from.distance + 1) <= horizontal && Mathf.Abs(to.height - unit.tile.height) <= vertical;
    }

    bool BlindSearch(Tile from, Tile to)
    {
        return (from.distance + 1) <= blindSpace && Mathf.Abs(to.height - unit.tile.height) <= vertical;
    }
}