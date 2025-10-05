
using Cmune.Util;

public class CircularInteger
{
    public CircularInteger(int lowerBound, int upperBound)
    {
        SetRange(lowerBound, upperBound);
    }

    public void SetRange(int lowerBound, int upperBound)
    {
        CmuneDebug.Assert(lowerBound < upperBound, "CircularInteger ctor failed because lowerBound greater than upperBound");

        _current = 0;
        _lower = lowerBound;
        _length = upperBound - lowerBound + 1;
    }

    public void Reset()
    {
        _current = 0;
    }

    #region PROPERTIES

    public int Current
    {
        get { return _current + _lower; }
        set
        {
            CmuneDebug.Assert(value < _lower + _length && value >= _lower, "CircularInteger: Assigned value not in range!");

            _current = value - _lower;
        }
    }

    public int Next
    {
        get
        {
            _current = (_current + 1) % _length;
            return Current;
        }
    }

    public int Prev
    {
        get
        {
            _current = (_current + _length - 1) % _length;
            return Current;
        }
    }

    public int First
    {
        get
        {
            _current = 0;
            return Current;
        }
    }

    public int Last
    {
        get
        {
            _current = _length - 1;
            return Current;
        }
    }

    public int Range
    {
        get
        {
            return _length;
        }
    }

    #endregion

    #region FIELDS
    private int _lower;
    private int _length;
    private int _current;
    #endregion
}
