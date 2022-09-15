using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ApplicationMenu : MonoBehaviour, IDataPersistence
{
    public double multi_1;
    public double multi_2;
    public double multi_3;
    public double multi_4;
    public double moneyMultiplikator;
    public int money;
    public TMP_Dropdown options_1;
    public TMP_Dropdown options_2;
    public TMP_Dropdown options_3;
    public TMP_Dropdown options_4;
    public TMP_Dropdown degree;
    public void LoadData(GameData data)
    {
        this.money = data.money;
    }

    public void SaveData(ref GameData data)
    {
        data.money = this.money;
    }

    void Start()
    {
        //dont ask me about that
        options_1.onValueChanged.AddListener(delegate
        {
            valueChanged_1(options_1);
        });

        options_2.onValueChanged.AddListener(delegate
        {
            valueChanged_2(options_2);
        });

        options_3.onValueChanged.AddListener(delegate
        {
            valueChanged_3(options_3);
        });

        options_4.onValueChanged.AddListener(delegate
        {
            valueChanged_4(options_4);
        });
    }

    void valueChanged_1(TMP_Dropdown option)
    {
        //or about this
        if (option.value == 1 || option.value == 2)
        {
            multi_1 = 0.5;
        }

        else if (option.value == 3)
        {
            multi_1 = 0.3;
        }

        else if (option.value == 4 || option.value == 6)
        {
            multi_1 = 0.1;
        }

        else
        {
            multi_1 = 0.2;
        }
    }

    void valueChanged_2(TMP_Dropdown option)
    {
        if (option.value == 1 || option.value == 2)
        {
            multi_2 = 0.5;
        }

        else if (option.value == 3)
        {
            multi_2 = 0.3;
        }

        else if (option.value == 4 || option.value == 6)
        {
            multi_2 = 0.3;
        }

        else
        {
            multi_2 = 0.2;
        }
    }

    void valueChanged_3(TMP_Dropdown option)
    {
        if (option.value == 1 || option.value == 2)
        {
            multi_3 = 0.5;
        }

        else if (option.value == 3)
        {
            multi_3 = 0.3;
        }

        else if (option.value == 4 || option.value == 6)
        {
            multi_3 = 0.3;
        }

        else
        {
            multi_3 = 0.2;
        }
    }

    void valueChanged_4(TMP_Dropdown option)
    {
        if (option.value == 1 || option.value == 2)
        {
            multi_4 = 0.5;
        }

        else if (option.value == 3)
        {
            multi_4 = 0.3;
        }

        else if (option.value == 4 || option.value == 6)
        {
            multi_4 = 0.3;
        }

        else
        {
            multi_4 = 0.2;
        }
    }

    public void Apply()
    {
        if (degree.value == 0 || degree.value == 1)
        {
            SceneManager.LoadScene("Game Over");
        }

        else
        {
            SceneManager.LoadScene("Management");
        }


        moneyMultiplikator = multi_1 + multi_2 + multi_3 + multi_4;
        money = (int)moneyMultiplikator * 60000;
        DataPersistenceManager.instance.SaveGame();
    }
}
