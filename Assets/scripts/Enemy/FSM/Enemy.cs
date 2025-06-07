using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public UnityEngine.AI.NavMeshAgent agent;
    public Animator animator;
    private AudioSource audioSource;
    [Tooltip("敌人血量")]public float enemyHealth;
    [Tooltip("敌人血条")]public Slider slider;
    [Tooltip("敌人受到伤害的文字UI")]public Text getDamageText;
    [Tooltip("敌人死亡特效")]public GameObject dealEffect;

    public GameObject[] wayPointObj;//存放敌人不同路线
    public List<Vector3> wayPoints=new List<Vector3>();//存放巡逻路线的每个巡逻点
    public int index;//当前巡逻点索引
    public int nameIndex;//怪物id
    public int animState;//动画状态标识，0：idle，，1：run，，2:attack
    public Transform targetPoint;

    public EnemyBaseState currentState;
    public PatrolState patrolState;//定义敌人巡逻状态，声明对象
    public AttackState attackState;//定义敌人攻击状态，声明对象

    Vector3 targetPosition;
    //敌人的攻击目标，场景中有敌人（玩家）用列表存储
    public List<Transform> attackList=new List<Transform>();
    [Tooltip("攻击间隔，时间越长攻击频率越慢")]public float attackRate;
    private float nextAttack=0;//下次攻击时间
    [Tooltip("普通攻击距离")]public float attackRange;
    private bool isDead;

    public GameObject attackParticle01;
    public Transform attackParticle01Postion;
    public AudioClip attackSound;

    // Start is called before the first frame update
    private void Awake(){
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator =GetComponent<Animator>();
        audioSource=GetComponent<AudioSource>();
        patrolState =transform.gameObject.AddComponent<PatrolState>();
        attackState=transform.gameObject.AddComponent<AttackState>();
    }
    void Start ()
    {
        isDead = false;
        slider.minValue = 0;
        slider.maxValue = enemyHealth;
        slider.value = enemyHealth;
        index = 0;
        TransitionToState(patrolState);
    }

    // Update is called once per frame
    void Update ()
    {
        if (isDead) return;
        //这里是表示当前状态持续执行
        //敌人移动方法要一直执行
        currentState.OnUpdate(this);
        animator.SetInteger("state",animState);

    }
    public void MoveToTarget(){
        // agent.destination = wayPoints[index];
        if(attackList.Count == 0){
            targetPosition = Vector3.MoveTowards(transform.position,wayPoints[index],agent.speed*Time.deltaTime);
        }
        else{
            targetPosition = Vector3.MoveTowards(transform.position,attackList[0].position,agent.speed*Time.deltaTime);
            // 更新targetPoint为当前攻击目标
            targetPoint = attackList[0];
        }
        agent.destination = targetPosition;
    }
    public void LoadPath(GameObject go){
        wayPoints.Clear();
        foreach(Transform child in go.transform){
            wayPoints.Add(child.position);
        }
    }
    public void TransitionToState(EnemyBaseState State){
        currentState = State;
        currentState.EnemyState(this);

    }
    public void Health(float damage){
        if (isDead) return;
        getDamageText.text=Mathf.Round(damage).ToString();
        enemyHealth -= damage;
        slider.value = enemyHealth;
        if (slider.value<=0){
            isDead = true;
            animator.SetTrigger("dying");
            slider.gameObject.SetActive(false);
            Destroy(Instantiate(dealEffect,transform.position,Quaternion.identity),3f);
        }
    }
    public void AttackAction(){
        if(isDead) return;
        
        // 确保有攻击目标
        if(attackList.Count > 0 && attackList[0] != null){
            // 使用攻击列表中的第一个目标
            Transform currentTarget = attackList[0];
            targetPoint = currentTarget; // 更新targetPoint
            
            // 让敌人面向玩家
            Vector3 directionToPlayer = (currentTarget.position - transform.position).normalized;
            directionToPlayer.y = 0; // 只在水平面上旋转
            if(directionToPlayer != Vector3.zero){
                transform.rotation = Quaternion.LookRotation(directionToPlayer);
            }
            
            //当敌人和玩家距离很近的时候，触发攻击动画
            if (Vector3.Distance(transform.position, currentTarget.position) < attackRange){
                if(Time.time > nextAttack){
                    //触发攻击
                    animator.SetTrigger("attack");
                    //更新下次攻击时间
                    nextAttack = Time.time + attackRate;
                    
                    Debug.Log($"敌人 {gameObject.name} 攻击玩家！距离: {Vector3.Distance(transform.position, currentTarget.position):F2}");
                }
            }
        }
    }
    public void OnTriggerEnter(Collider other){
        if(!attackList.Contains(other.transform)&&!isDead &&!other.CompareTag("Bullect")){
            attackList.Add(other.transform);
            Debug.Log($"敌人 {gameObject.name} 检测到目标: {other.name}");
        }
    }
    public void OnTriggerExit(Collider other){
        if(attackList.Contains(other.transform)){
            attackList.Remove(other.transform);
            Debug.Log($"敌人 {gameObject.name} 失去目标: {other.name}");
        }
    }
}