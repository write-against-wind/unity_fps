using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : EnemyBaseState
{
    public override void EnemyState(Enemy enemy)
    {
        enemy.animState=2;
        enemy.targetPoint=enemy.attackList[0];
    }

    public override void OnUpdate(Enemy enemy)
    {
        //当前敌人没有目标，，此时敌人切换回巡逻状态
        if(enemy.attackList.Count==0){
            enemy.TransitionToState(enemy.patrolState);
        }
        //当前敌人有目标，，可能存在多个目标情况，，要找距离最近的攻击目标
        if(enemy.attackList.Count>1){
            for(int i=0;i<enemy.attackList.Count;i++){
                if(   Mathf.Abs(enemy.transform.position.x-enemy.attackList[i].position.x)
                    < Mathf.Abs(enemy.transform.position.x-enemy.targetPoint.position.x)
                  )
                {
                    enemy.targetPoint=enemy.attackList[i];
                }
            }
        }
        //当敌人只有1个攻击目标时，就只找list里第1个
        if(enemy.attackList.Count==1){
            enemy.targetPoint=enemy.attackList[0];
        }
        //如果目标点是玩家，则进行攻击
        if(enemy.targetPoint.tag=="Player"){
           enemy.AttackAction();
        }
        enemy.MoveToTarget();
    }
}
