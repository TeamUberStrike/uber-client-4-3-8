using System.Collections.Generic;
using UnityEngine;
using Cmune.Util;

class HudDrawFlagGroup : Singleton<HudDrawFlagGroup>
{
    public HudDrawFlags BaseDrawFlag
    {
        get { return _baseDrawFlag; }
        set
        {
            _baseDrawFlag = value;
            UpdateDrawFlags();
        }
    }

    public bool IsScreenshotMode
    {
        get { return _isScreenShotMode; }
        set
        {
            _isScreenShotMode = value;
            if (value == true)
            {
                _drawFlagTunings.Add(screenshotDrawFlagTuning);
            }
            else
            {
                _drawFlagTunings.Remove(screenshotDrawFlagTuning);
            }
            UpdateDrawFlags();
        }
    }

    public bool TuningTabScreen
    {
        set
        {
            if (value == true)
            {
                _drawFlagTunings.Add(tabScreenDrawFlagsTuning);
            }
            else
            {
                _drawFlagTunings.Remove(tabScreenDrawFlagsTuning);
            }
            UpdateDrawFlags();
        }
    }

    public void AddFlag(HudDrawFlags drawFlag)
    {
        _drawFlagTunings.Add(drawFlag);
        UpdateDrawFlags();
    }

    public void RemoveFlag(HudDrawFlags drawFlag)
    {
        if (_drawFlagTunings.Contains(drawFlag))
        {
            _drawFlagTunings.Remove(drawFlag);
            UpdateDrawFlags();
        }
    }

    public void ClearFlags()
    {
        _drawFlagTunings.Clear();
        UpdateDrawFlags();
    }

    public HudDrawFlags GetConsolidatedFlag()
    {
        HudDrawFlags drawFlag = ~HudDrawFlags.None;
        drawFlag &= BaseDrawFlag;
        foreach (var drawFlagTuning in _drawFlagTunings)
        {
            drawFlag &= drawFlagTuning;
        }
        return drawFlag;
    }

    #region Private methods

    private HudDrawFlagGroup()
    {
        _drawFlagTunings = new HashSet<HudDrawFlags>();
    }

    private void UpdateDrawFlags()
    {
        HudController.Instance.DrawFlags = GetConsolidatedFlag();
        //Debug.Log(HudController.Instance.DrawFlagString);
    }

    #endregion

    #region Private fields

    private const HudDrawFlags screenshotDrawFlagTuning = HudDrawFlags.None;
    private const HudDrawFlags tabScreenDrawFlagsTuning = HudDrawFlags.Reticle;

    private HudDrawFlags _baseDrawFlag;
    private HashSet<HudDrawFlags> _drawFlagTunings;
    private bool _isScreenShotMode = false;

    #endregion
}