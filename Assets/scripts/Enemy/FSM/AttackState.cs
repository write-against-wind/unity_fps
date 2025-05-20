using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : EnemyBaseState
{
    public override void EnemyState(Enemy enemy)
    {
        Debug.Log("巡逻");
    }

    public override void OnUpdate(Enemy enemy)
    {
        Debug.Log("巡逻");
    }
}
