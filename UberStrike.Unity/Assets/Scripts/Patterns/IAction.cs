using UnityEngine;
using System.Collections;

public interface IAction
{
    void Execute();
}

public interface IAction<T>
{
    void Execute(T o);
}
