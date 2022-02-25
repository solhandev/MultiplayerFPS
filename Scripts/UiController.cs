using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UiController : MonoBehaviour
{
    public static UiController instance;


    public TMP_Text overheatedMessage;
    public Slider weaponTempSlider;
    public GameObject deathScreen;
    public TMP_Text deathText;

    public TMP_Text gmKills;
    public GameObject winScreen;

    public TMP_Text winText;
    // Start is called before the first frame update

    private void Awake() {
       instance = this; 
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
}
