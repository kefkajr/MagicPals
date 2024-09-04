using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;

public class SpawnMarker : MonoBehaviour {
    const float stepHeight = 0.5f;

    public SpawnData spawnData;
    public TextMeshProUGUI recipeNameLabel;
    public int height;
    public Transform directionMarker;
    public MeshRenderer meshRenderer;
    public Material heroMaterial;
    public Material enemyMaterial;
    public Material defaultMaterial;

	public void Set(SpawnData spawnData, int height) {
		this.spawnData = spawnData;
        this.height = height;
	}

    public void Match () {
        recipeNameLabel.text = spawnData.name;
		transform.localPosition = new Vector3( spawnData.position.x, height * stepHeight / 2f, spawnData.position.y );
		transform.localScale = new Vector3(1, 0.25f, 1);

        int directionValue = (int)spawnData.direction;
        float rotationY = 0 + (directionValue * 90);
        directionMarker.localEulerAngles = spawnData.direction.ToEuler();
        switch (spawnData.alliance) {
            case Alliances.Hero:
                meshRenderer.material = heroMaterial;
                break;
            case Alliances.Enemy:
                meshRenderer.material = enemyMaterial;
                break;
            default:
                meshRenderer.material = defaultMaterial;
                break;
        }
	}
}

[System.Serializable]
public class SpawnData {
    public string name;
    public string model;
	public string abilityCatalog;
	public string strategy;
	public Locomotions locomotion;
	public Alliances alliance;
	public PerceptionRecipe perceptionRecipe;
	public StatsTemplate statsTemplate;
    public Point position;
    public Direction direction;
    public int turnInitiativeOffset;

    public SpawnData(SpawnRecipe spawnRecipe, Point position, Direction direction, int turnInitiativeOffset) {
        name = spawnRecipe.name;
        model = spawnRecipe.model;
        abilityCatalog = spawnRecipe.abilityCatalog;
        strategy = spawnRecipe.strategy;
        locomotion = spawnRecipe.locomotion;
        alliance = spawnRecipe.alliance;
        perceptionRecipe = spawnRecipe.perceptionRecipe;
        statsTemplate = spawnRecipe.statsTemplate;
        this.position = position;
        this.direction = direction;
        this.turnInitiativeOffset = turnInitiativeOffset;
    }
	public SpawnData(string name, string model, string abilityCatalog, string strategy, Locomotions locomotion, Alliances alliance, PerceptionRecipe perceptionRecipe, StatsTemplate statsTemplate, Point position, Direction direction, int turnInitiativeOffset) {
		this.name = name;
        this.model = model;
        this.abilityCatalog = abilityCatalog;
        this.strategy = strategy;
        this.locomotion = locomotion;
        this.alliance = alliance;
        this.perceptionRecipe = perceptionRecipe;
        this.statsTemplate = statsTemplate;
		this.position = position;
        this.direction = direction;
        this.turnInitiativeOffset = turnInitiativeOffset;
	}
}

[CustomEditor(typeof(SpawnMarker))]
public class SpawnMarkerEditor : Editor {
	public string recipeName = "";
	
	public SpawnMarker current { get { return (SpawnMarker)target; } }

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
        if (GUILayout.Button("Match Spawn Data"))
			current.Match();
    }
}
