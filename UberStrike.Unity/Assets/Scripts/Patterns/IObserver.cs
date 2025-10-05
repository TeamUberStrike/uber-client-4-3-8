using UnityEngine;
using System.Collections;

public interface IObserver
{
    void Notify();
}

public interface ISubject
{
    void Subscribe(IObserver observer);
    void Unsubscribe(IObserver observer);
}
