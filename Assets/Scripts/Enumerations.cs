// It's nice to dream. There are only melee units in this version, apologies!
public enum UnitType
{
    Melee,
    Elite,
    Archer,
    Cavalry
}

public enum GameFlow
{
    Paused,
    Play
}

public enum UnitState
{
    Idle,
    Moving,
    Seeking,
    Attacking
}

public enum SoldierState
{
    Idle,
    Moving,
    Attacking
}

// UI
public enum ButtonAction
{
    OnDrag,
    OnRelease
}
