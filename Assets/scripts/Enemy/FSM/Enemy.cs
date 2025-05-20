using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
private UnityEngine.AI.NavMeshAgent agent;
private Animator animator;

public GameObject[] wayPointObj;//存放敌人不同路线
public List<Vector3>wayPoints=new List<Vector3>();//存放巡逻路线的每个巡逻点


// Start is called before the first frame update
void Start ()
{
    agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    animator = GetComponent<Animator>();
}

// Update is called once per frame
void Update ()
{

}
}