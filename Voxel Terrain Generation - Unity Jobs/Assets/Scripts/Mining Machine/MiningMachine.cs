using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningMachine : MonoBehaviour
{
    public bool inMenu = false;
    public GameObject menuUI;
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (inMenu == true)
        {
            Menu();
        }
        if (inMenu == false)
        {
            Time.timeScale = 1;
            menuUI.SetActive(false);
        }
    }

    void InputCheck()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Exit();
        }
    }

    void Menu()
    {
        Time.timeScale = 0;
        menuUI.SetActive(true);
    }

    public void Mine()
    {
        animator.SetBool("Mining", true);
    }

    public void ViewOreChance()
    {

    }

    public void Upgrade()
    {

    }

    public void Exit()
    {
        inMenu = false;
    }
}
