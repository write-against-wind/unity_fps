using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// // 武器音效内部类
// [System.Serializable]
// public class SoundClips{
//     public AudioClip shootSound;
//     public AudioClip silencerShootSound;
//     public AudioClip reloadSoundAmmotLeft;
//     public AudioClip reloadSoundOutOfAmmo;
//     public AudioClip aimSound;
// }

public class Weapon_AutomaticGun : weapon
{
    [Header("武器部件位置")]
    [Tooltip("射击的位置")]public Transform shootPoint;
    [Tooltip("子弹的特效打出位置")]public Transform bulletShootPoint;
    [Tooltip("弹壳的抛出位置")]public Transform CasingBulletSpawnPoint;

    [Header("枪械属性")]
    [Tooltip("武器射程")]public float range;
    [Tooltip("武器射速")]public float fireRate;
    [Tooltip("原始射速")]private float originRate;
    [Tooltip("射击的一点偏移量")]private float SpreadFactor;
    [Tooltip("计时器控制武器射速")]private float fireTimer;
    [Tooltip("子弹发射的力")]private float bulletForce;

    [Tooltip("当前武器的每个弹夹子弹数")]public int bulletMag;
    [Tooltip("当前子弹数")]public int currentBullets;
    [Tooltip("备弹")]public int bulletLeft;
    
    [Header("键位设置")]
    [Tooltip("查看武器按键")]private KeyCode lookWeaponKey = KeyCode.F;

    [Header("特效")]
    public Light muzzleflashLight;
    private float lightDuration;
    public ParticleSystem muzzlePatic;
    public ParticleSystem sparkPatic;
    public int minSparkEmission = 1;
    public int maxSparkEmission = 7;
    
    [Header("音源")]
    public AudioSource mainAudioSource;

    [Header("枪械音效")]
    public AudioClip shootSound;
    public AudioClip silencerShootSound;
    public AudioClip reloadSoundAmmotLeft;
    public AudioClip reloadSoundOutOfAmmo;
    public AudioClip aimSound;
    
    private Animator anim;

    private void Awake()
    {
        mainAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        muzzleflashLight.enabled = false;
        range = 300f;
        lightDuration = 0.02f;
        bulletLeft = bulletMag *5;
        currentBullets =bulletMag;
        anim = GetComponent<Animator>();
    }
    private void Update()
    {
        
        if(Input.GetMouseButton(0))
        {
            GunFire();
        }
        if(Input.GetKeyDown(lookWeaponKey))
        {
            anim.SetTrigger("Inspect");
        }
        //计时器
        if(fireTimer < fireRate )
        {
            fireTimer += Time.deltaTime;
        }
    }
    public override void GunFire()
    {
        if(fireTimer < fireRate || currentBullets <= 0)
        {
            return;
        }
        StartCoroutine(MuzzleFlashLight());//开火灯光
        muzzlePatic.Emit(1);//开火特效
        sparkPatic.Emit(Random.Range(minSparkEmission,maxSparkEmission));//火花特效
        RaycastHit hit;
        Vector3 shootDirection = shootPoint.forward;//射击向前方射击
        shootDirection = shootDirection + shootPoint.TransformDirection(new Vector3(Random.Range(-SpreadFactor,SpreadFactor),Random.Range(-SpreadFactor,SpreadFactor)));
        if (Physics.Raycast(shootPoint.position,shootDirection,out hit,range))
        {
            Debug.Log(hit.transform.gameObject.name+"打到了");
        }
        mainAudioSource.clip = shootSound;
        mainAudioSource.Play(); //播放射击音效
        fireTimer = 0f;
        currentBullets--;
    }

    public IEnumerator MuzzleFlashLight(){
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleflashLight.enabled = false;
    }

    public override void Reload()
    {
        throw new System.NotImplementedException();
    }

    public override void AimIn()
    {
        throw new System.NotImplementedException();
    }

    public override void AimOut()
    {
        throw new System.NotImplementedException();
    }

    public override void ExpaningCrossUpdate(float expanDegree)
    {
        throw new System.NotImplementedException();
    }

    public override void DoReloadAnimation()
    {
        throw new System.NotImplementedException();
    }   
}
