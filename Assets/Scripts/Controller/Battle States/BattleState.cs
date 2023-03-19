﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BattleState : State 
{
	protected BattleController owner;
	protected Driver driver;
	public CameraRig cameraRig { get { return owner.cameraRig; }}
	public Board board { get { return owner.board; }}
	public LevelData levelData { get { return owner.levelData; }}
	public Transform tileSelectionIndicator { get { return owner.tileSelectionIndicator; }}
	public Point pos { get { return owner.pos; } set { owner.pos = value; }}
	public Tile currentTile { get { return owner.currentTile; }}
	public AbilityMenuPanelController abilityMenuPanelController { get { return owner.abilityMenuPanelController; }}
	public DescriptionPanelController descriptionPanelController { get { return owner.descriptionPanelController; } }
	public StatPanelController statPanelController { get { return owner.statPanelController; }}
	public HitSuccessIndicator hitSuccessIndicator { get { return owner.hitSuccessIndicator; }}
	public Turn turn { get { return owner.turn; }}
	public List<Unit> units { get { return owner.units; }}

	protected virtual void Awake ()
	{
		owner = GetComponent<BattleController>();
	}

	protected override void AddListeners ()
	{
		if (driver == null || driver.Current == DriverType.Human)
		{
			InputController.moveEvent += OnMove;
			InputController.submitEvent += OnSubmit;
		}
	}
	
	protected override void RemoveListeners ()
	{
		InputController.moveEvent -= OnMove;
		InputController.submitEvent -= OnSubmit;
	}

	public override void Enter ()
	{
		driver = (turn.actor != null) ? turn.actor.GetComponent<Driver>() : null;
		base.Enter ();
	}

	void OnMove(object sender, InfoEventArgs<Point> e)
	{
		// Translate which direction up/down/left/right go dending on the current camera direction
		Point point = e.info;
		Point translatedPoint;
		switch (cameraRig.currentDirection)
		{
			case Direction.North:
				translatedPoint = point;
				break;
			case Direction.South:
				translatedPoint = new Point(-point.x, -point.y);
				break;
			case Direction.West:
				translatedPoint = new Point(-point.y, point.x);
				break;
			default: // East
				translatedPoint = new Point(point.y, -point.x);
				break;
		}
		MoveEventData moveEventData = new MoveEventData(point, translatedPoint);

		OnMove(sender, moveEventData);
	}

	protected virtual void OnMove (object sender, MoveEventData d)
	{
		
	}
	
	protected virtual void OnSubmit ()
	{
		
	}

	protected virtual void OnCancel ()
	{
		
	}

	protected virtual void SelectTile (Point p)
	{
		if (pos == p || !board.tiles.ContainsKey(p))
			return;

		pos = p;
		tileSelectionIndicator.localPosition = board.tiles[p].center;
	}

	protected virtual Unit GetUnit (Point p)
	{
		Tile t = board.GetTile(p);
		GameObject content = t != null ? t.occupant : null;
		return content != null ? content.GetComponent<Unit>() : null;
	}

	protected virtual void RefreshPrimaryStatPanel (Point p)
	{
		Unit target = GetUnit(p);
		if (target != null)
			statPanelController.ShowPrimary(target.gameObject);
		else
			statPanelController.HidePrimary();
	}

	protected virtual void RefreshSecondaryStatPanel (Point p)
	{
		Unit target = GetUnit(p);
		if (target != null)
			statPanelController.ShowSecondary(target.gameObject);
		else
			statPanelController.HideSecondary();
	}

	protected virtual bool DidPlayerWin ()
	{
		return owner.GetComponent<BaseVictoryCondition>().Victor == Alliances.Hero;
	}
	
	protected virtual bool IsBattleOver ()
	{
		BaseVictoryCondition vc = owner.GetComponent<BaseVictoryCondition>();
		return vc.Victor != Alliances.None;
	}

	public struct MoveEventData
    {
		public Point point;
		public Point pointTranslatedByCameraDirection;

		public MoveEventData(Point point, Point pointTranslatedByCameraDirection)
		{
			this.point = point;
			this.pointTranslatedByCameraDirection = pointTranslatedByCameraDirection;
		}
	}
}