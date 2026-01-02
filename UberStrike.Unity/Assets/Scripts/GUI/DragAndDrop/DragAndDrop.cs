using System;
using System.Collections;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class DragAndDrop : Singleton<DragAndDrop>
{
    private static int _itemSlotButtonHash = "Button".GetHashCode();

    private float _zoomMultiplier = 1.0f;
    private float _alphaValue = 1.0f;
    private bool _isZooming = false;
    private bool _dragBegin = false;
    private Rect _draggedControlRect;
    private Vector2 _dragScalePivot;

    public int CurrentId { get; private set; }
    public bool IsDragging { get { return CurrentId > 0 && DraggedItem != null && DraggedItem.Item != null; } }
    public IDragSlot DraggedItem { get; private set; }

    private DragAndDrop()
    {
        UnityRuntime.Instance.OnGui += OnGui;
    }

    public event Action<IDragSlot> OnDragBegin;

    private bool _releaseDragItem;

    private void OnGui()
    {
        //GUI.Label(new Rect(0, 100, Screen.width, 20), "DragAndDrop: " + CurrentId + " " + (DraggedItem != null) + " " + 0);

        if (_releaseDragItem)
        {
            _releaseDragItem = false;
            CurrentId = 0;
            DraggedItem = null;
        }

        // If MouseUp anywhere, turn off the drag mode & reset the ActiveDragItem
        if (Event.current.type == EventType.MouseUp)
        {
            _releaseDragItem = true;
        }

        //If we are dragging, create an visual GUIItem to follow the mouse
        if (IsDragging)
        {
            //call this on the beginning of a drag movement
            if (_dragBegin)
            {
                _dragBegin = false;
                if (OnDragBegin != null) OnDragBegin(DraggedItem);
                MonoRoutine.Start(StartDragZoom(0.0f, 1, 1.25f, 0.1f, 0.8f));
            }
            else
            {
                if (!_isZooming)
                {
                    _dragScalePivot = GUIUtility.ScreenToGUIPoint(Event.current.mousePosition);
                }

                GUIUtility.ScaleAroundPivot(new Vector2(_zoomMultiplier, _zoomMultiplier), _dragScalePivot);
                GUI.backgroundColor = new Color(1, 1, 1, _alphaValue);
                GUI.matrix = Matrix4x4.identity;

                GUI.Label(new Rect(_dragScalePivot.x - 24, _dragScalePivot.y - 24, 48, 48), DraggedItem.Item.Icon, BlueStonez.item_slot_large);
            }
        }
    }

    private IEnumerator StartDragZoom(float time, float startZoom, float endZoom, float startAlpha, float endAlpha)
    {
        _isZooming = true;
        var startPivot = new Vector2(_draggedControlRect.xMin + 32, _draggedControlRect.yMin + 32);

        float timer = 0f;
        while (timer < time)
        {
            _alphaValue = Mathf.Lerp(startAlpha, endAlpha, timer / time);
            _zoomMultiplier = Mathfx.Berp(startZoom, endZoom, timer / time);
            _dragScalePivot = Vector2.Lerp(startPivot, Event.current.mousePosition, timer / time);
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        _dragScalePivot = Event.current.mousePosition;
        _alphaValue = endAlpha;
        _zoomMultiplier = endZoom;
        _isZooming = false;
    }

    public void DrawSlot<T>(Rect rect, T item, Action<int, T> onDropAction = null, Color? color = null, bool isItemList = false) where T : IDragSlot
    {
        int id = GUIUtility.GetControlID(_itemSlotButtonHash, FocusType.Native);

        if ((ApplicationDataManager.Channel == ChannelType.Android 
            || ApplicationDataManager.Channel == ChannelType.IPad 
            || ApplicationDataManager.Channel == ChannelType.IPhone)
            && Event.current.GetTypeForControl(id) == EventType.MouseDown
            && isItemList)
        {
            rect.width = 50;
        }

        switch (Event.current.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (Event.current.type != EventType.used && rect.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = id;
                    Event.current.Use();
                } break;

            case EventType.MouseUp:
                OnMouseUp(rect, id, item.Id, onDropAction);
                break;

            case EventType.MouseDrag:
                //Check if this control is active and if it's draggable
                if (GUIUtility.hotControl == id)
                {
                    Vector2 controlScreenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                    _draggedControlRect = new Rect(controlScreenPos.x, controlScreenPos.y, rect.width, rect.height);

                    _dragBegin = true;

                    DraggedItem = item;
                    CurrentId = GUIUtility.hotControl;

                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                } break;

            case EventType.Repaint:
                {
                    if (color.HasValue)
                    {
                        GUI.color = color.Value;
                        BlueStonez.loadoutdropslot_highlight.Draw(rect, GUIContent.none, id);
                        GUI.color = Color.white;
                    }
                } break;
        }
    }

    public void DrawSlot<T>(Rect rect, Action<int, T> onDropAction) where T : IDragSlot
    {
        int id = GUIUtility.GetControlID(_itemSlotButtonHash, FocusType.Native);
        if (Event.current.GetTypeForControl(id) == EventType.MouseUp)
        {
            OnMouseUp(rect, id, 0, onDropAction);
        }
    }

    private void OnMouseUp<T>(Rect rect, int id, int slotId, Action<int, T> onDropAction) where T : IDragSlot
    {
        if (GUIUtility.hotControl == id)
        {
            GUIUtility.hotControl = 0;
            Event.current.Use();
        }
        else if (onDropAction != null && DraggedItem != null && rect.Contains(Event.current.mousePosition))
        {

            //Debug.LogWarning("OnMouseUp 2 " + slot);
            onDropAction(slotId, (T)DraggedItem);
        }
    }
}