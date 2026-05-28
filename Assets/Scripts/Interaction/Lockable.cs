using UnityEngine;
using UnityEngine.Events;

public class Lockable : MonoBehaviour
{
    public string requiredKeyId = "brass-key";
    public bool isLocked = true;
    public UnityEvent onUnlock;

    public bool TryUnlock(Key key)
    {
        if (!isLocked || key == null) return false;
        if (key.keyId != requiredKeyId) return false;
        isLocked = false;
        onUnlock?.Invoke();
        return true;
    }
}
