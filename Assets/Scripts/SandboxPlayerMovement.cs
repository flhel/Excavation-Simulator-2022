using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using DataStorage;
using SandboxTileManagement;

public class SandboxPlayerMovement : MonoBehaviour, IDataPersistence
{
    public float moveSpeed;

    public Rigidbody2D rb;

    public Animator animator;

    DataStorageClass dataStorage;
    SandboxTileManagementScript sandboxTileManagementScript;

    GameObject tableMenu;
    GameObject pauseMenu;

    Vector2 movement;
    bool canStartDigging = false;
    bool firstFlagPlaced = false;
    Vector2 firstFlagPosition;
    Vector2 secondFlagPosition;

    TMPro.TextMeshProUGUI infoBox;
    TMPro.TextMeshProUGUI expCounter;

    private bool abraumMatrixInitialized;

    void Awake()
    {
        GameObject dataStorageObject = GameObject.Find("DataStorageObject");
        dataStorage = dataStorageObject.GetComponent<DataStorageClass>();
        GameObject sandboxTileManagement = GameObject.Find("SandboxTileManagement");
        sandboxTileManagementScript =
            sandboxTileManagement.GetComponent<SandboxTileManagementScript>();
        tableMenu = GameObject.Find("TableMenu");
        pauseMenu = GameObject.Find("PauseMenu");
    }

    void Start()
    {
        DataPersistenceManager.instance.LoadGame();

        if (!abraumMatrixInitialized)
        {
            dataStorage.initializeAbraumMatrix();
        }

        sandboxTileManagementScript.initSandboxTileIsShown(); //Abraum Feature  

        expCounter =
            GameObject.Find("ExpCounter").GetComponent<TMPro.TextMeshProUGUI>();
        expCounter.text = "EXP: " + dataStorage.exp;

        infoBox = GameObject.Find("InfoBox").GetComponent<TMPro.TextMeshProUGUI>();
        infoBox.text = "Go outside the Base and press SPACE to place Flags";
        tableMenu.SetActive(false);
        pauseMenu.SetActive(false);

        String[] tmp = gameObject.scene.name.Split(" ");
        dataStorage.LevelNumber = Int32.Parse(tmp[1]);
        Debug.Log("Level Number: " + dataStorage.LevelNumber);
    }

    // Update is called once per frame
    void Update()
    {
        if (movement.y == 0)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
        }

        if (movement.x == 0)
        {
            movement.y = Input.GetAxisRaw("Vertical");
        }

        if (Input.GetKeyDown("space"))
        {
            if (!canStartDigging)
            {
                checkPlaceFlag();
            }
        }

        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown("f"))
        {
            checkPlayerAtDesk();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //Quick and Dirty Menu with "Game freeze"
            if (pauseMenu.activeSelf)
            {
                Time.timeScale = 1;
                pauseMenu.SetActive(false);
            }
            else
            {
                Time.timeScale = 0;
                pauseMenu.SetActive(true);
            }

        }
    }

    void FixedUpdate()
    {
        if (movement != Vector2.zero)
        {
            animator.SetFloat("XInput", movement.x);
            animator.SetFloat("YInput", movement.y);
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    void checkPlaceFlag()
    {
        Vector3 playerPosition = GameObject.Find("Player").transform.position;
        if (dataStorage.LevelNumber == 0)
        {
            if ((playerPosition.x > 22 && playerPosition.x < 32) && (playerPosition.y > 11 && playerPosition.y < 17))
            {
                infoBox.text = "Don't place the Flags into the Base!!!";
            }
            else
            {
                placeFlag(playerPosition);
            }
        }
        if (dataStorage.LevelNumber == 1)
        {
            if ((playerPosition.x > 30 && playerPosition.x < 37) && (playerPosition.y > 3 && playerPosition.y < 9))
            {
                infoBox.text = "Don't place the Flags into the Base!!!";
            }
            else
            {
                placeFlag(playerPosition);
            }
        }

    }

    //Platziert Fagge(Rechteck um den Schnitt, Schnitte werden an den Stellen vordefiniert)
    void placeFlag(Vector3 playerPosition)
    {
        Tilemap tilemap = GameObject.Find("Flags").GetComponent<Tilemap>();
        Sprite sprite = Resources.Load<Sprite>("Flag");

        if (firstFlagPlaced)
        {
            createFlagObject("flag2", playerPosition);
            infoBox.text = "Go back to the Base and press ENTER at the Table to start digging.";
        }
        else
        {
            createFlagObject("flag1", playerPosition);
            infoBox.text = "First Flag placed, go place a second one.";
        }

        if (firstFlagPlaced)
        {
            secondFlagPosition.x = playerPosition.x;
            secondFlagPosition.y = playerPosition.y;
            firstFlagPlaced = false;
            canStartDigging = true;
        }
        else
        {
            firstFlagPosition.x = playerPosition.x;
            firstFlagPosition.y = playerPosition.y;
            firstFlagPlaced = true;
            canStartDigging = false;
        }
    }

    void createFlagObject(string name, Vector3 position)
    {
        GameObject flag = new GameObject();
        flag.name = name;
        flag.AddComponent<SpriteRenderer>();
        flag.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Flag");
        flag.transform.position = position;
    }

    void checkPlayerAtDesk()
    {
        float distance = Vector3.Distance(GameObject.Find("Player").transform.position, GameObject.Find("Desk").transform.position);

        if (canStartDigging && distance < 1.5)
        {
            canStartDigging = false;

            saveSize();
            if (dataStorage.size.x > 3 || dataStorage.size.y > 3)
            {
                deleteAllFlags();
                infoBox.text = "Der Ausgrabungsschnitt ist zu Groß, die maximale Größe beträgt 3x3 Kacheln!!!";
            }
            else
            {
                deleteTiles(); //Loch anzeigen
                               //infoBox.text = "Der Ausgrabungsschnitt ist " + dataStorage.size.x + "x" + dataStorage.size.y + " 2m Kacheln groß";
                saveNumberOfArtifacts();
                //SceneManager.LoadScene("Digging 0");
                Time.timeScale = 0;
                tableMenu.SetActive(true);
            }
        }
    }

    //Creates the Hole in the Ground from Digging
    private void deleteTiles()
    {
        int posXInt;
        if (firstFlagPosition.x < secondFlagPosition.x)
        {
            posXInt = (int)Math.Floor(firstFlagPosition.x);
        }
        else
        {
            posXInt = (int)Math.Floor(secondFlagPosition.x);
        }
        int posYInt;
        if (firstFlagPosition.y < secondFlagPosition.y)
        {
            posYInt = (int)Math.Floor(firstFlagPosition.y);
        }
        else
        {
            posYInt = (int)Math.Floor(secondFlagPosition.y);
        }

        for (int i = posXInt; i < (posXInt + dataStorage.size.x); i++)
        {
            for (int j = posYInt; j < (posYInt + dataStorage.size.y); j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    sandboxTileManagementScript.setSandboxTileIsShown(i, j, k, false);
                }
            }
        }

    }

    private void deleteAllFlags()
    {
        //Clean DataStorage
        dataStorage.size.x = 0;
        dataStorage.size.y = 0;

        //Delete Flag Objects
        Destroy(GameObject.Find("flag1"));
        Destroy(GameObject.Find("flag2"));
    }

    private void saveSize()
    {
        dataStorage.size.x = (float)Math.Floor(secondFlagPosition.x - firstFlagPosition.x);
        dataStorage.size.y = (float)Math.Floor(secondFlagPosition.y - firstFlagPosition.y);
        if (dataStorage.size.x < 0)
        {
            dataStorage.size.x = dataStorage.size.x * -1;
        }
        if (dataStorage.size.y < 0)
        {
            dataStorage.size.y = dataStorage.size.y * -1;
        }
        dataStorage.size = dataStorage.size + new Vector2(1.0f, 1.0f); //Starts counting at 1 
    }

    private void saveNumberOfArtifacts()
    {
        // Set the Number of found Artifacts
        int overallTileValue = 0;
        int numberOfTiles = 0;
        for (int i = (int)firstFlagPosition.x; i < (int)firstFlagPosition.x + (int)dataStorage.size.x; i++)
        {
            for (int j = (int)firstFlagPosition.y; j < (int)firstFlagPosition.y + (int)dataStorage.size.y; j++)
            {
                overallTileValue = overallTileValue + sandboxTileManagementScript.sandboxTileValues[i][j];
                numberOfTiles++;
            }
        }

        if (numberOfTiles > 1)
        {
            if (((overallTileValue / numberOfTiles) * 2.5) >= 10)
            {
                dataStorage.artifactsEnabled = 10;
            }
            else
            {
                dataStorage.artifactsEnabled = overallTileValue;
            }
        }
        else
        {
            dataStorage.artifactsEnabled = overallTileValue;
        }

        if (dataStorage.artifactsEnabled < 1)
        {
            dataStorage.artifactsEnabled = 1;
        }
    }


    public void LoadData(GameData data)
    {
        dataStorage.exp = data.exp;
        dataStorage.expMultiplikator = data.expMultiplikator;
        dataStorage.manpower = data.manpower;
        this.abraumMatrixInitialized = data.abraumMatrixObjectsInitialized;

        if (dataStorage.abraumMatrix == null)
        {
            dataStorage.initializeAbraumMatrix();
        }
        if (data.abraumMatrixObjectsInitialized)
        {
            LoadAbraumMatrix(data);
        }
        Debug.Log("Data Loaded");
    }

    public void SaveData(ref GameData data)
    {
        data.exp = dataStorage.exp;
        data.expMultiplikator = dataStorage.expMultiplikator;
        data.manpower = dataStorage.manpower;
        SaveAbraumMatrix(ref data);
        Debug.Log("Data Saved");
    }

    private void LoadAbraumMatrix(GameData data)
    {
        for (int i = 0; i < data.abraumMatrixObjects.Length; i++)
        {
            int x = data.abraumMatrixObjects[i].x;
            int y = data.abraumMatrixObjects[i].y;
            int k = data.abraumMatrixObjects[i].k;
            int tileIsShown = data.abraumMatrixObjects[i].tileIsShown;
            dataStorage.abraumMatrix[x][y][k] = tileIsShown;
        }
    }

    private void SaveAbraumMatrix(ref GameData data)
    {
        int counter = 0;
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 50; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    AbraumMatrixObject o;
                    if (dataStorage.abraumMatrix[i][j][k] == 0)
                    {
                        o = new AbraumMatrixObject(i, j, k, 0);
                    }
                    else
                    {
                        o = new AbraumMatrixObject(i, j, k, 1);
                    }
                    if (counter >= 15000)
                    {
                        Debug.Log("here");
                    }
                    data.abraumMatrixObjects[counter] = o;
                    counter++;
                }
            }
        }

        data.abraumMatrixObjectsInitialized = true;
    }
}
