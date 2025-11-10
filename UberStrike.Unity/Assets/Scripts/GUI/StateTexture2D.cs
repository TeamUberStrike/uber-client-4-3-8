using UnityEngine;

public class StateTexture2D
{
    private Texture[] _list;
    private int _index;
    private static readonly Texture _default = new Texture2D(1, 1);

    public StateTexture2D(params Texture[] states)
    {
        SetStates(states);
    }

    public void SetStates(params Texture[] states)
    {
        _list = states != null ? states : new Texture2D[0];
        _index = Mathf.Clamp(_index, 0, _list.Length - 1);
    }

    public Texture ChangeState(int stateId)
    {
        _index = Mathf.Clamp(stateId, 0, _list.Length - 1);

        return Current;
    }

    public Texture Current
    {
        get { return _list.Length > 0 ? _list[_index] : _default; }
    }
}