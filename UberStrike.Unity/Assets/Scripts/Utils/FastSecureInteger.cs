
using Cmune.Realtime.Common.Security;
using UnityEngine;

class FastSecureInteger
{
    private const float syncFrequency = 1;
    private SecureMemory<int> secureValue = new SecureMemory<int>(0);
    private int deltaValue = 0;
    private float nextUpdate;

    public FastSecureInteger(int value)
    {
        secureValue.WriteData(value);
        deltaValue = 0;
    }

    public void Decrement(int value)
    {
        deltaValue -= value;
    }

    public void Increment(int value)
    {
        deltaValue += value;
    }

    public int Value
    {
        get
        {
            if (deltaValue != 0 && nextUpdate < Time.time)
            {
                Value = secureValue.ReadData(true) + deltaValue;
            }
            return secureValue.ReadData(false) + deltaValue;
        }
        set
        {
            secureValue.WriteData(value);
            deltaValue = 0;
            nextUpdate = Time.time + syncFrequency;
        }
    }

    public override string ToString()
    {
        return Value.ToString();// +" (" + secureValue.ReadData(false) + " + " + deltaValue + ")";
    }
}