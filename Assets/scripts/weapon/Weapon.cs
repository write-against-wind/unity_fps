using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class weapon : MonoBehaviour
{
    public abstract void GunFire();
    public abstract void Reload();
    public abstract void AimIn();
    public abstract void AimOut();
    public abstract void ExpaningCrossUpdate(float expanDegree);
    public abstract void DoReloadAnimation();

    
}
