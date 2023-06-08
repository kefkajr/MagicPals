using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour {
    public enum Origin { withinBorder, onBorder, withoutBorder}

    public const int thicknessMax = 8;
    public const float stepThickness = 0.10f;
    public const float stepHeight = 0.25f;

    public Tile tile;
    public Direction direction;
    public int thickness = 1;
    public int height = 1;
    public Origin origin = Origin.onBorder;

    public void Load(Tile tile, Direction direction) {
        this.tile = tile;
        this.direction = direction;
        Match();
    }

    public void Load(Tile tile, WallData wallData) {
        this.tile = tile;
        this.direction = wallData.direction;
        this.thickness = wallData.thickness;
        this.height = wallData.height;
        Match();
    }

    public void Grow() {
        height++;
        Match();
    }

    public void Shrink() {
        height--;
        Match();
    }

    public void Thicken() {
        if (thickness < thicknessMax) {
            thickness++;
            Match();
        }
    }

    public void Thin() {
        thickness--;
        Match();
    }

    public void MoveOrigin(int i) {
        switch (origin) {
            case Origin.withinBorder:
                origin = i > 0 ? Origin.onBorder : origin;
                break;
            case Origin.withoutBorder:
                origin = i < 0 ? Origin.onBorder : origin;
                break;
            default: // Origin.onBorder
                origin = i < 0 ? Origin.withinBorder : Origin.withoutBorder;
                break;
        }
        Match();
    }

    void Match() {
        float posX;
        float posZ;
        float scaleX;
        float scaleZ;
        switch (direction) {
            case Direction.North:
                scaleX = 1;
                scaleZ = thickness * stepThickness;
                posX = tile.pos.x;
                posZ = tile.pos.y + 0.5f + OriginOffSetFromScale(scaleZ);
                break;
            case Direction.East:
                scaleX = thickness * stepThickness;
                scaleZ = 1;
                posX = tile.pos.x + 0.5f + OriginOffSetFromScale(scaleX);
                posZ = tile.pos.y;
                break;
            case Direction.South:
                scaleX = 1;
                scaleZ = thickness * stepThickness;
                posX = tile.pos.x;
                posZ = tile.pos.y - 0.5f - OriginOffSetFromScale(scaleZ);
                break;
            default: // Direction.West:
                scaleX = thickness * stepThickness;
                scaleZ = 1;
                posX = tile.pos.x - 0.5f - OriginOffSetFromScale(scaleX);
                posZ = tile.pos.y;
                break;
        }
        float posY = (height * stepHeight) / 2f + (tile.height * Tile.stepHeight);

        transform.localPosition = new Vector3(posX, posY, posZ);
        transform.localScale = new Vector3(scaleX, height * stepHeight, scaleZ);
    }

    float OriginOffSetFromScale(float scale) {
        switch(origin) {
            case Origin.withinBorder:
                return -scale / 2;
            case Origin.withoutBorder:
                return scale / 2;
            default: // Origin.onBoarder
                return 0;
        }
    }
}

[System.Serializable]
public class WallData {
    public Direction direction;
    public Wall.Origin origin;
    public int thickness;
    public int height;

    public WallData(Wall wall) {
        this.direction = wall.direction;
        this.origin = wall.origin;
        this.thickness = wall.thickness;
        this.height = wall.height;
    }
}