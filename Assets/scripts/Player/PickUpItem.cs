using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpltem : MonoBehaviour
{
    [Tooltip("武器旋转的速度")]private float rotateSpeed;
    [Tooltip("武器编号")]public int itemID;
    private GameObject weaponModel;

    void Start ()
    {
        rotateSpeed = 100f;
    }

    void Update ()
    {
        transform.eulerAngles += new Vector3(0, rotateSpeed*Time.deltaTime, 0);
    }
    private void OnTriggerEnter (Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerController player = other.GetComponent<PlayerController>();
            weaponModel = GameObject.Find("Player/Assult_Rife_Arm/Inventory/").gameObject.transform.GetChild(itemID).gameObject;
            player.PickUpWeapon(itemID,weaponModel);
            Destroy(gameObject);
        }
    }
}