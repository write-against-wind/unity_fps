using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolState : EnemyBaseState
{

    public override void EnemyState(Enemy enemy)
    {
        enemy.animState = 0;
        // enemy.LoadPath(enemy.wayPointObj[WayPointManager.Instance.usingIndex[enemy.nameIndex]]);
        enemy.LoadPath(enemy.wayPointObj[2]);
    }

    public override void OnUpdate(Enemy enemy)
    {
        //判断如果当前idle动画已经播放完以后，才能执行移动
        if(!enemy.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")){
            enemy.animState = 1;
            enemy.MoveToTarget();
        }

        //计算敌人和导航点的距离
        float distance = Vector3.Distance(enemy.transform.position, enemy.wayPoints[enemy.index]);
        
        // 检查NavMeshAgent是否已经到达目标点
        if (distance<=0.5f)
        {
            enemy.animator.Play("Idle");
            enemy.index++;
            enemy.index = Mathf.Clamp(enemy.index,0,enemy.wayPoints.Count-1);
            //这里再次判断敌人和巡逻路线上最后1个导航点的距离，如果距离很小，那么当前路线已经走完，就重置导航点下标，使其重新又走一遍
            if(Vector3.Distance(enemy.transform.position,enemy.wayPoints[enemy.wayPoints.Count -1])<=0.5f){
                enemy.index=0;
            }
            Debug.Log("到达路径点，下一个索引: " + enemy.index);
        }
        if(enemy.attackList.Count>0){
            enemy.TransitionToState(enemy.attackState);
        }
    }
}

