using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimationSchedulerNew : IUpdatable
{
    public delegate void ScheduleAnimationDelegate(AnimationSchedulerNew animScheduler);

    public ScheduleAnimationDelegate ScheduleAnimation { get; set; }

    public AnimationSchedulerNew()
    {
        _animGroup = new List<IAnim>();
        _standbyQueue = new Queue<IAnim>();
        _toBeRemoved = new List<IAnim>();
        ScheduleAnimation = ScheduleStrategy.ScheduleInSequence;
    }

    public void Update()
    {
        if (ScheduleAnimation != null)
        {
            ScheduleAnimation(this);
        }
        UpdateAnimation();
    }

    public void EnqueueAnim(IAnim anim)
    {
        _standbyQueue.Enqueue(anim);
    }

    public IAnim DequeueAnim()
    {
        if (_standbyQueue.Count != 0)
        {
            return _standbyQueue.Dequeue();
        }
        return null;
    }

    public int StandbyQueueCount
    {
        get { return _standbyQueue.Count; }
    }

    public void AddToAnimGroup(IAnim anim)
    {
        _animGroup.Add(anim);
    }

    public void RemoveFromAnimGroup(IAnim anim)
    {
        _animGroup.Remove(anim);
    }

    public IAnim GetFromAnimGroup(int index)
    {
        if (index >= 0 && index < _animGroup.Count)
        {
            return _animGroup[index];
        }
        return null;
    }

    public int AnimGroupCount
    {
        get { return _animGroup.Count; }
    }

    public void ClearAll()
    {
        _standbyQueue.Clear();
        ClearAnimGroup();
    }

    public void ClearAnimGroup()
    {
        _toBeRemoved.Clear();
        foreach (IAnim anim in _animGroup)
        {
            _toBeRemoved.Add(anim);
        }
        foreach (IAnim anim in _toBeRemoved)
        {
            anim.Stop();
            _animGroup.Remove(anim);
        }
    }

    #region Private

    private void UpdateAnimation()
    {
        _toBeRemoved.Clear();
        foreach (IAnim anim in _animGroup)
        {
            anim.Update();
            if (!anim.IsAnimating)
            {
                _toBeRemoved.Add(anim);
            }
        }
        foreach (IAnim anim in _toBeRemoved)
        {
            _animGroup.Remove(anim);
        }
    }

    private List<IAnim> _animGroup;
    private Queue<IAnim> _standbyQueue;
    private List<IAnim> _toBeRemoved;

    #endregion
}