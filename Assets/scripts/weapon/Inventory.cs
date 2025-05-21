using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<GameObject> weapons = new List<GameObject>();
    public int currentWeaponID = -1;
    // Start is called before the first frame update
    void Start()
    {
        currentWeaponID = -1;
    }

    // Update is called once per frame
    void Update()
    {
        ChangeWeaponID();
    }

    public void ChangeWeaponID(){
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if(scroll > 0){
            changeWeapon((currentWeaponID - 1 + weapons.Count) % weapons.Count);
        }
        else if(scroll < 0){
           changeWeapon((currentWeaponID + 1) % weapons.Count);
        }
        else{
            return;
        }
        for(int i = 0; i < 10; i++){
            if(Input.GetKeyDown(KeyCode.Alpha0 + i)){
                int num = i - 1;
                if(num < weapons.Count){
                    changeWeapon(num);
                }
            }
        }
    }
    public void changeWeapon(int weaponID){
        if(weapons.Count == 0){
            return;
        }
        currentWeaponID =( weaponID + weapons.Count) % weapons.Count;
        for(int i = 0; i < weapons.Count; i++){
            if(i == currentWeaponID){
                weapons[i].gameObject.SetActive(true);
            }
            else{
                weapons[i].gameObject.SetActive(false);
            }
        }
    }

    public void AddWeapon(GameObject weapon){
        if(weapons.Contains(weapon)){
            print("武器已存在");
            return;
        }
        else{
            Debug.Log("武器不存在，添加武器。");
            if(weapons.Count < 7){
                weapons.Add(weapon);
                changeWeapon(weapons.Count - 1);
            }
        }
    }
    public void ThrowWeapon(GameObject weapon){
        if(!weapons.Contains(weapon) || weapons.Count == 0){
            print("武器不存在");
            return;
        }
        else{
            weapons.Remove(weapon);
            weapon.gameObject.SetActive(false);
            changeWeapon(currentWeaponID-1);
        }
    }
}
