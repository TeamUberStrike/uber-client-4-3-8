using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectRecycler
{
    public ObjectRecycler(GameObject gameObject, int initialCapacity, GameObject parentObject = null)
    {
        _objectList = new List<GameObject>(initialCapacity);
        _objectToRecycle = gameObject;
        _parentObject = parentObject;

        for (int i = 0; i < initialCapacity; i++)
        {
            GameObject newObject = Object.Instantiate(_objectToRecycle) as GameObject;
            newObject.gameObject.active = false;
            if (parentObject != null)
            {
                newObject.transform.parent = _parentObject.transform;
            }

            _objectList.Add(newObject);
        }
    }

    public GameObject GetNextFree()
    {
        var freeObject = (from item in _objectList
                          where item.active == false
                          select item).FirstOrDefault();

        if (freeObject == null)
        {
            freeObject = Object.Instantiate(_objectToRecycle) as GameObject;
            if (_parentObject != null)
            {
                freeObject.transform.parent = _parentObject.transform;
            }
            _objectList.Add(freeObject);
        }

        freeObject.active = true;

        return freeObject;
    }

    public void FreeObject(GameObject objectToFree)
    {
        objectToFree.gameObject.active = false;
    }

    private List<GameObject> _objectList;
    private GameObject _objectToRecycle;
    private GameObject _parentObject;
}
