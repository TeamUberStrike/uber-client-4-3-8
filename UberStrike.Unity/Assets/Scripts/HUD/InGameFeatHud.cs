using UnityEngine;

class InGameFeatHud : Singleton<InGameFeatHud>, IUpdatable
{
    public float TextHeight
    {
        get
        {
            return Screen.height * 0.08f;
        }
    }

    public Vector2 AnchorPoint
    {
        get
        {
            return new Vector2(Screen.width / 2, Screen.height * 0.26f);
        }
    }

    public AnimationSchedulerNew AnimationScheduler { get { return _animScheduler; } }

    public void Update()
    {
        _animScheduler.Update();
    }

    private InGameFeatHud()
    {
        _animScheduler = new AnimationSchedulerNew();
        _animScheduler.ScheduleAnimation = ScheduleStrategy.ScheduleOverlap;
    }

    private AnimationSchedulerNew _animScheduler;
}
