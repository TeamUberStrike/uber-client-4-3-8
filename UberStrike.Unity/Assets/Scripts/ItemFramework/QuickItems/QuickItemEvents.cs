
using UberStrike.DataCenter.Common.Entities;

public class HealthIncreaseEvent
{
    public int Health { get; set; }
}

public class ArmorIncreaseEvent
{
    public int Armor { get; set; }
}

public class AddAmmoIncreaseEvent
{
    public int Amount { get; set; }
}

public class AmmoAddMaxEvent
{
    public int Percent { get; set; }
}

public class AmmoAddStartEvent
{
    public int Percent { get; set; }
}

public class QuickItemAmountChangedEvent
{
}