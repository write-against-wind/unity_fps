using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR;  // 添加XR命名空间
using UnityEngine.XR.Interaction.Toolkit;  // 添加XR Interaction Toolkit命名空间
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs;  // 添加输入相关命名空间
using InputDevice = UnityEngine.XR.InputDevice;  // 明确指定使用XR的InputDevice
using CommonUsages = UnityEngine.XR.CommonUsages;  // 明确指定使用XR的CommonUsages

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
    public PlayerController playerController;

    public bool IS_AUTORIFLE;//是否是自动式器
    public bool IS_SEMIGUN;//是否半自动式器

    private Camera mainCamera;
    public Camera gunCamera;

    [Header("武器部件位置")]
    [Tooltip("射击的位置")]public Transform shootPoint;
    [Tooltip("子弹的特效打出位置")]public Transform bulletShootPoint;
    [Tooltip("弹壳的抛出位置")]public Transform CasingBulletSpawnPoint;

    [Header("子弹预制体和特效")]
    public Transform bulletPrefab; // 子弹
    public Transform casingPrefab; // 弹壳

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
    public bool isSilencer; //是否装备消音器
    private int shotFragment = 8;
    public float minDamage;
    public float maxDamage;
    
    [Header("键位设置")]
    [SerializeField][Tooltip("查看武器按键")]private KeyCode lookWeaponKey = KeyCode.I;
    [SerializeField][Tooltip("填装子弹按键")] private KeyCode reloadInputName = KeyCode.R;
    [SerializeField][Tooltip("自动半自动切换按键")] private KeyCode GunShootModelInput=KeyCode.X;

    [Header("狙击镜设置")]
    [Tooltip("狙击镜材质")]public Material scopeRenderMaterial;
    [Tooltip("当没有进行瞄准时狙击镜的颜色")]public Color fadeColor;
    [Tooltip("当瞄准时狙击镜的颜色")]public Color defaultColor;


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
    

    [Header("UI")]
    public Image[] crossQuarterImgs; //准心
    public float currentExpanedDegree;//当前准心的开合度
    private float crossExpanedDegree;//每帧准心开合度
    private float maxCrossDegree;//最大开合度
    public TextMeshProUGUI ammoTextUI;
    public TextMeshProUGUI shootModeTextUI;

    public ShootMode shootingMode;
    private bool GunShootInput;//根据全自动和半自动 射击的键位输入发生改变
    private int modeNum;
    private string shootModeName;

    private Animator anim;
    
    public PlayerController.PlayerState state;
    public bool isReloading = false;
    public bool isAiming = false;

    private Vector3 sniperingFiflePosition;
    public Vector3 sniperingFifleOnPosition;

    [Header("VR控制器")]
    private XRBaseController leftController;
    private XRBaseController rightController;
    private XROrigin xrOrigin;
    private bool wasLeftTriggerPressed = false;
    
    [Header("VR按键映射")]
    [SerializeField]
    public InputActionReference pressAAction;  // 右手A键 - 换弹
    [SerializeField]
    public InputActionReference leftXAction;   // 左手X键 - 检视武器
    [SerializeField]
    public InputActionReference leftYAction;   // 左手Y键 - 切枪
    [SerializeField]
    public InputActionReference rightBAction;  // 右手B键 - 奔跑 (public以便PlayerController访问)

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        mainAudioSource = GetComponent<AudioSource>();
        mainCamera = Camera.main;
        
        // 首先找到XR Origin
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
        {
            // 在XR Origin的子物体中查找控制器
            var allControllers = xrOrigin.GetComponentsInChildren<XRBaseController>();
            foreach (var controller in allControllers)
            {
                if (controller.name.ToLower().Contains("left"))
                {
                    leftController = controller;
                    Debug.Log("找到左手控制器: " + controller.name);
                }
                else if (controller.name.ToLower().Contains("right"))
                {
                    rightController = controller;
                    Debug.Log("找到右手控制器: " + controller.name);
                }
            }
        }

        // 如果还是没有找到控制器，输出详细警告
        if (leftController == null)
        {
            Debug.LogWarning("未找到左手控制器！请确保场景中存在XR Origin并且包含Controller组件。");
        }
        if (rightController == null)
        {
            Debug.LogWarning("未找到右手控制器！请确保正确设置。");
        }
    }

    private void Start()
    {
        sniperingFiflePosition = transform.localPosition;
        muzzleflashLight.enabled = false;
        bulletForce = 100f;
        crossExpanedDegree = 50f;
        maxCrossDegree = 300f;
        range = 300f;
        lightDuration = 0.02f;
        bulletLeft = bulletMag *5;
        currentBullets =bulletMag;
        anim = GetComponent<Animator>();
        originRate = fireRate;
        UpdateAmmoUI();
        /*根据不同枪械,游戏刚开始时进行不同射击模式设置*/
        if (IS_AUTORIFLE)
        {
            modeNum = 1;
            shootModeName="Fully Automatic";
            shootingMode = ShootMode.AutoRifle;
            UpdateAmmoUI();
        }

        if (IS_SEMIGUN)
        {
            modeNum = 0;
            shootModeName = "Semi Automatic";
            shootingMode = ShootMode.SemiGun;
            UpdateAmmoUI();
        }
    }
    
    private void Update()
    {
        // 获取VR控制器输入
        bool leftTriggerPressed = false;
        bool rightTriggerPressed = false;
        bool rightButtonAPressed = false;
        bool leftXPressed = false;
        bool leftYPressed = false;
        bool rightBPressed = false;
        
        if (leftController != null)
        {
            var actionBasedController = leftController.GetComponent<ActionBasedController>();
            if (actionBasedController != null && actionBasedController.activateAction.action != null)
            {
                leftTriggerPressed = actionBasedController.activateAction.action.ReadValue<float>() > 0.5f;
            }
        }
        
        if (rightController != null)
        {
            var actionBasedController = rightController.GetComponent<ActionBasedController>();
            if (actionBasedController != null)
            {
                rightTriggerPressed = actionBasedController.activateAction.action.ReadValue<float>() > 0.5f;
            }
        }

        // 检测VR按键按压
        if (pressAAction != null && pressAAction.action != null)
        {
            rightButtonAPressed = pressAAction.action.WasPressedThisFrame();
        }
        
        if (leftXAction != null && leftXAction.action != null)
        {
            leftXPressed = leftXAction.action.WasPressedThisFrame();
        }
        
        if (leftYAction != null && leftYAction.action != null)
        {
            leftYPressed = leftYAction.action.WasPressedThisFrame();
        }
        
        if (rightBAction != null && rightBAction.action != null)
        {
            rightBPressed = rightBAction.action.WasPressedThisFrame();
        }

        //自动枪械VR输入方式
        if (IS_AUTORIFLE)
        {
            //切换射击模式(全自动和半自动)
            if (Input.GetKeyDown(GunShootModelInput) && modeNum != 1)
            {
                modeNum = 1;
                shootModeName = "Fully Automatic";
                shootingMode = ShootMode.AutoRifle;
                UpdateAmmoUI();
            }
            else if (Input.GetKeyDown(GunShootModelInput) && modeNum != 0)
            {
                modeNum = 0;
                shootModeName = "Semi Automatic";
                shootingMode = ShootMode.SemiGun;
                UpdateAmmoUI();
            }

            /*控制射击模式的转换*/
            switch (shootingMode)
            {
                case ShootMode.AutoRifle:
                    // 全自动模式：持续按住扳机即可射击
                    GunShootInput = leftTriggerPressed;
                    fireRate = originRate;
                    break;
                case ShootMode.SemiGun:
                    // 半自动模式：检测扳机按下的瞬间
                    bool triggerDown = leftTriggerPressed && !wasLeftTriggerPressed;
                    wasLeftTriggerPressed = leftTriggerPressed;
                    GunShootInput = triggerDown;
                    fireRate = 0.2f;
                    break;
            }
        }
        else
        {
            // 其他武器类型使用扳机按下瞬间
            bool triggerDown = leftTriggerPressed && !wasLeftTriggerPressed;
            wasLeftTriggerPressed = leftTriggerPressed;
            GunShootInput = triggerDown;
        }

        state = playerController.playerState;
        if (state == PlayerController.PlayerState.Walking && state != PlayerController.PlayerState.Running && state != PlayerController.PlayerState.Crouching)
        {
            //移动时的准心开合度
            ExpaningCrossUpdate(crossExpanedDegree);
        }
        else if(state != PlayerController.PlayerState.Walking && state == PlayerController.PlayerState.Running && state != PlayerController.PlayerState.Crouching){
            ExpaningCrossUpdate(crossExpanedDegree*2);
        }
        else{
            ExpaningCrossUpdate(0);
        }

        if(GunShootInput)
        {
            if(IS_SEMIGUN && gameObject.name == "4"){
                shotFragment = 8;
            }
            else{
                shotFragment = 1;
            }
            GunFire();
        }
        // 检视武器 - 左手X键
        if(leftXPressed || Input.GetKeyDown(lookWeaponKey))
        {
            anim.SetTrigger("Inspect");
        }
        
        // 切枪 - 左手Y键
        if(leftYPressed)
        {
            // 调用库存系统的切枪功能
            var inventory = GetComponentInParent<Inventory>();
            if(inventory != null)
            {
                int nextWeaponID = (inventory.currentWeaponID + 1) % inventory.weapons.Count;
                if(nextWeaponID != inventory.currentWeaponID)
                {
                    inventory.changeWeapon(nextWeaponID);
                    Debug.Log("VR手柄切换到武器: " + nextWeaponID);
                }
            }
        }
        anim.SetBool("Run",playerController.isRunning);
        anim.SetBool("Walk",playerController.isWalk);
        
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if(info.IsName("reload_ammo_left") || info.IsName("reload_out_of_ammo")
            || info.IsName("reload_open") || info.IsName("reload_close") || info.IsName("reload_insert")
            || info.IsName("reload_insert1")|| info.IsName("reload_insert2")|| info.IsName("reload_insert3")
            || info.IsName("reload_insert4")|| info.IsName("reload_insert5")){
            isReloading = true;
        }
        else{
            isReloading = false;
        }
        if((info.IsName("reload_insert1") ||
            info.IsName ("reload_insert2") ||
            info.IsName ("reload_insert3") ||
            info.IsName ("reload_insert4") ||
            info.IsName ("reload_insert5") ||
            info.IsName ("reload_insert") ) && currentBullets == bulletMag)
        {
            anim.Play("reload_close");
            isReloading = false;
        }

        // 修改换弹触发逻辑
        if((rightButtonAPressed || Input.GetKeyDown(reloadInputName)) 
            && currentBullets < bulletMag 
            && bulletLeft > 0 
            && !isReloading)
        {
            DoReloadAnimation();
        }

        // VR瞄准控制：右手扳机
        if(rightTriggerPressed && !isReloading && !playerController.isRunning){
            isAiming = true;
            anim.SetBool("Aim",isAiming);
            transform.localPosition = sniperingFifleOnPosition;
        }
        else{
            isAiming = false;
            anim.SetBool("Aim",isAiming);
            transform.localPosition = sniperingFiflePosition;
        }
        
        //计时器
        if(fireTimer < fireRate )
        {
            fireTimer += Time.deltaTime;
        }
        SpreadFactor = (isAiming)?0.01f:0.1f;
    }
    public override void GunFire()
    {
        if(fireTimer < fireRate || currentBullets <= 0 || anim.GetCurrentAnimatorStateInfo(0).IsName("take_out") || isReloading)
        {
            return;
        }
        StartCoroutine(MuzzleFlashLight());//开火灯光
        muzzlePatic.Emit(1);//开火特效
        sparkPatic.Emit(Random.Range(minSparkEmission,maxSparkEmission));//火花特效
        StartCoroutine(Shoot_Crss());

        
        if(!isAiming){
            anim.CrossFadeInFixedTime("fire",0.1f);
        }
        else{
            anim.Play("aim_fire",0,0);
        }

        for(int i = 0; i < shotFragment; i++){
            RaycastHit hit;
            Vector3 shootDirection = shootPoint.forward;//射击向前方射击
            shootDirection = shootDirection + shootPoint.TransformDirection(new Vector3(Random.Range(-SpreadFactor,SpreadFactor),Random.Range(-SpreadFactor,SpreadFactor)));
            if (Physics.Raycast(shootPoint.position,shootDirection,out hit,range))
            {
                Transform bullet;
                if(IS_AUTORIFLE || (IS_SEMIGUN && gameObject.name == "2")){
                    bullet = Instantiate(bulletPrefab,bulletShootPoint.transform.position,bulletShootPoint.transform.rotation);
                }
                else{
                    bullet = Instantiate(bulletPrefab,hit.point,Quaternion.FromToRotation(Vector3.forward,hit.normal));
                }
                bullet.GetComponent<Rigidbody>().velocity = (bullet.transform.forward + shootDirection) * bulletForce;
                //击中敌人时做的判断
                if(hit.transform.gameObject.transform.tag == "Enemy")
                {
                    hit.transform.gameObject.GetComponent<Enemy>().Health(Random.Range(minDamage,maxDamage));//随机伤害
                }
                Debug.Log(hit.transform.gameObject.name+"打到了");
            }
        }
        
        Instantiate(casingPrefab,CasingBulletSpawnPoint.transform.position,CasingBulletSpawnPoint.transform.rotation);
        mainAudioSource.clip = isSilencer? silencerShootSound:shootSound;
        mainAudioSource.Play(); //播放射击音效
        fireTimer = 0f;
        currentBullets--;
        UpdateAmmoUI();
    }

    public override void DoReloadAnimation()
    {
        if(!(IS_SEMIGUN &&(gameObject.name == "4" || gameObject.name == "5"))){
            if (currentBullets > 0 && bulletLeft > 0)
            {
                anim.Play("reload_ammo_left",0,0);
                Reload();
                mainAudioSource.clip = reloadSoundAmmotLeft;
                mainAudioSource.Play();
            }
            if (currentBullets == 0 && bulletLeft > 0)
            {
                anim.Play("reload_out_of_ammo",0,0);
                Reload();
                mainAudioSource.clip = reloadSoundOutOfAmmo;
                mainAudioSource.Play();
            }
        }
        else{
            if(currentBullets == bulletMag){
                return;
            }
            anim.SetTrigger("shotgun_reload");
        }
        
    }

    public void ShotgunReload(){
        if(currentBullets < bulletMag){
            currentBullets++;
            bulletLeft--;
            UpdateAmmoUI();
        }
        else{
            anim.Play("reload_close",0,0);
            return;
        }
        if(bulletLeft <= 0){
            return;
        }
    }

    public override void Reload()
    {
        if(bulletLeft <= 0) return;
        int bulletToLoad = bulletMag - currentBullets;
        int bulletToReduce = bulletLeft >= bulletToLoad?bulletToLoad:bulletLeft;
        bulletLeft -= bulletToReduce;
        currentBullets += bulletToReduce;
        UpdateAmmoUI();
    }

    public void UpdateAmmoUI()
    {
        ammoTextUI.text = currentBullets + "/" + bulletLeft;
        shootModeTextUI.text = shootModeName;
    }

    public IEnumerator MuzzleFlashLight(){
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleflashLight.enabled = false;
    }

    public override void AimIn()
    {
        float currentVelocity = 0f;
        for(int i = 0; i < crossQuarterImgs.Length; i++)
        {
            crossQuarterImgs[i].gameObject.SetActive(false);
        }

        if(IS_SEMIGUN &&gameObject.name == "5"){
            scopeRenderMaterial.color = defaultColor;
            gunCamera.fieldOfView = 15;
        }

        mainCamera.fieldOfView = Mathf.SmoothDamp(30,60,ref currentVelocity,0.1f);
        mainAudioSource.clip = aimSound;
        mainAudioSource.Play();
    }

    public override void AimOut()
    {
        float currentVelocity = 0f;
        for(int i = 0; i < crossQuarterImgs.Length; i++)
        {
            crossQuarterImgs[i].gameObject.SetActive(true);
        }
        if(IS_SEMIGUN &&gameObject.name == "5"){
            scopeRenderMaterial.color = fadeColor;
            gunCamera.fieldOfView = 35;
        }

        mainCamera.fieldOfView = Mathf.SmoothDamp(60,30,ref currentVelocity,0.1f);
        mainAudioSource.clip = aimSound;
        mainAudioSource.Play();
    }

    public override void ExpaningCrossUpdate(float expanDegree)
    {
        if(currentExpanedDegree < expanDegree - 5){
            ExpendCross(Time.deltaTime * 150);
        }
        else if(currentExpanedDegree > expanDegree + 5){
            ExpendCross(-Time.deltaTime * 300);
        }
    }

    public void ExpendCross(float add){
        crossQuarterImgs[0].transform.localPosition += new Vector3(-add,0,0);// 扩大左边准星
        crossQuarterImgs[1].transform.localPosition += new Vector3(add,0,0);// 扩大右边准星
        crossQuarterImgs[2].transform.localPosition += new Vector3(0,add,0);// 扩大上边准星
        crossQuarterImgs[3].transform.localPosition += new Vector3(0,-add,0);// 扩大下边准星
        currentExpanedDegree += add;//保存准星开合度
        currentExpanedDegree = Mathf.Clamp(currentExpanedDegree,0,maxCrossDegree);// 限制准星开合度大小
    }   
    public IEnumerator Shoot_Crss(){
        yield return null;
        for(int i = 0;i < 10;i++){
            ExpendCross(Time.deltaTime * 500);
        }
    }
    public enum ShootMode {
        AutoRifle,
        SemiGun
    }

}

