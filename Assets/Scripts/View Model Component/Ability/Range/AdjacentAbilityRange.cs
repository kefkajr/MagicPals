using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AdjacentAbilityRange : AbilityRange {
    public override List<Tile> GetTilesInRange(Board board) {
        List<Tile> tileList = board.Search(unit.tile, delegate (Tile from, Tile to) {
            return ExpandSearch(board, from, to);
		});

        tileList.Remove(unit.tile);

        return tileList;
    }

    bool ExpandSearch(Board board, Tile from, Tile to) {
        // Skip if walls are blocking the way
        if (board.WallSeparatingTiles(from, to) != null)
            return false;

        return (from.distance + 1) <= horizontal && Mathf.Abs(to.height - unit.tile.height) <= vertical;
    }
}