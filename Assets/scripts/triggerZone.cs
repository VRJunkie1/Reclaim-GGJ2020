using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Register delegates of type 'void X(Collider other)' to receive trigger events
/// </summary>
public class triggerZone : MonoBehaviour
{
    public delegate void Collision(Collider other);

    /// <summary>
    /// Loops through all children (and self) and ignores these collisions. (For avoiding self collisions)
    /// </summary>
    public Transform ignoreChildCollisions;

    public Collision onTriggerEnter;
    public Collision onTriggerStay;
    public Collision onTriggerExit;


    private void OnTriggerStay(Collider other)
    {
        if (transformIsChild(other.transform, ignoreChildCollisions)) return;
        onTriggerStay?.Invoke(other);
    }
    //}
    private void OnTriggerEnter(Collider other)
    {
        if (transformIsChild(other.transform, ignoreChildCollisions)) return;
        onTriggerEnter?.Invoke(other);

    }
    private void OnTriggerExit(Collider other)
    {
        if (transformIsChild(other.transform, ignoreChildCollisions)) return;
        onTriggerExit?.Invoke(other);
    }

    bool transformIsChild(Transform check, Transform parent)
    {
        //print("parent check " + parent.name + check.name);
        if (!parent) return false;
        if (check == parent) return true;
        foreach (Transform tf in parent)
        {
            //print("tf " + tf);
            if (tf == check) return true;
        }
        return false;
    }
}
