using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using CursorBehavior;
using DataStorage;

public class ArtifactToolBehaviour : MonoBehaviour
{
    private GameObject[] groundLayers;

    private GameObject[] dustLayers;

    private GameObject artifact;

    private Texture2D cursorTexture;

    private Vector2 cursorHotspot;

    private DataStorageClass dataStorage;

    CursorBehaviour cursorBehaviour;

    private int[][] pattern = new int[5][];

    private bool toolCanDig;

    private int clickCounter;

    AudioSource audioSource;

    void Awake()
    {
        groundLayers = GameObject.FindGameObjectsWithTag("GroundLayer");
        dustLayers = GameObject.FindGameObjectsWithTag("DustLayer");
        dataStorage = GameObject.Find("DataStorageObject").GetComponent<DataStorageClass>();
        audioSource = GameObject.Find("AudioBrush").GetComponent<AudioSource>();
    }

    // Start is called before the first frame update
    void Start()
    {
        string artifactName = "Artifact (" + dataStorage.ArtifactNumber + ")";
        artifact = GameObject.Find(artifactName);
        deactivateArtifacts();

        cursorBehaviour = GameObject.Find("CursorBehaviourSkript").GetComponent<CursorBehaviour>();
        setExpCounter(artifact.GetComponent<ArtifactArtifact>().experiencePoints);
        clickCounter = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            mousePosition.z = 0; //Die Tilemaps sind auf z = 0 und die Kamera bei z = -10 --> Tilemap.HasTile(mousePosition) muss bei z = 0 haben um true zu sein

            useActiveTool(mousePosition);

            checkArtifactIsCleaned();
        }
    }

    public void activeToolBrush()
    {
        toolCanDig = false;
        cursorBehaviour.setCursorBrush();
        pattern[0] = new int[] { 0, 0, 0, 0, 0 };
        pattern[1] = new int[] { 0, 0, 1, 1, 0 };
        pattern[2] = new int[] { 0, 1, 1, 1, 0 };
        pattern[3] = new int[] { 0, 1, 1, 0, 0 };
        pattern[4] = new int[] { 0, 0, 0, 0, 0 };
    }

    public void activeToolDustpan()
    {
        toolCanDig = true;
        cursorBehaviour.setCursorDustpan();
        pattern[0] = new int[] { 0, 0, 1, 1, 1 };
        pattern[1] = new int[] { 0, 1, 1, 1, 0 };
        pattern[2] = new int[] { 0, 1, 1, 1, 0 };
        pattern[3] = new int[] { 0, 0, 1, 1, 0 };
        pattern[4] = new int[] { 0, 1, 1, 1, 0 };
    }

    private void deactivateArtifacts()
    {
        GameObject[] artifacts = GameObject.FindGameObjectsWithTag("Artifact");
        for (int i = 0; i < artifacts.Length; i++)
        {
            if (artifacts[i].name != artifact.name)
            {
                artifacts[i].SetActive(false);
            }
        }
    }

    private void checkArtifactIsCleaned()
    {
        //Quick and Dirty
        clickCounter++;
        if (clickCounter > 10)
        {
            GameObject.Find("ButtonQuit").GetComponent<Button>().interactable = true;
        }
    }

    private void useActiveTool(Vector3 mousePosition)
    {
        if (!checkTilesDust(mousePosition))
        {
            if (!checkTilesGround(mousePosition))
            {
                /*
                if (!checkTilesArtifact(mousePosition))
                {

                }
                */
            }
        }
    }

    /*
        bool checkTilesArtifact(Vector3 mousePosition)
        {
            Tilemap artifactTilemap = artifact.GetComponent<Tilemap>();

            Vector3Int gridPosition = artifactTilemap.WorldToCell(mousePosition);

            if (artifactTilemap.HasTile(gridPosition))
            {
                if (toolCanDamageArtifact)
                {
                    artefactDamaged(artifact);

                    //Load Damaged Tile Graphics???
                }
                return true;
            }
            return false;
        }
    */

    bool checkTilesGround(Vector3 mousePosition)
    {
        // Differenzierung zwischen Staub und "Erde"
        // Staub von Pinsel abtragbar
        // Erde von ...
        if (toolCanDig)
        {
            for (int i = 0; i < groundLayers.Length - 1; i++)
            {
                // Sortiert nach Name, größere Zahl = tiefer
                Tilemap groundLayer =
                    GameObject
                        .Find("Ground (" + i + ")")
                        .GetComponent<Tilemap>();

                Vector3Int gridPosition =
                    groundLayer.WorldToCell(mousePosition);

                if (groundLayer.HasTile(gridPosition))
                {
                    audioSource.Play();
                    deleteTilesAtPosition(gridPosition, groundLayer);
                    i = groundLayers.Length + 1;
                    return true;
                }
            }
        }
        return false;
    }

    bool checkTilesDust(Vector3 mousePosition)
    {
        if (!toolCanDig)
        {
            for (int i = 0; i < dustLayers.Length; i++)
            {
                // Sortiert nach Name, größere Zahl = tiefer
                Tilemap dustLayer =
                    GameObject.Find("Dust (" + i + ")").GetComponent<Tilemap>();

                Tilemap groundLayer =
                    GameObject.Find("Ground (" + i + ")").GetComponent<Tilemap>();

                Vector3Int gridPosition = dustLayer.WorldToCell(mousePosition);

                if (dustLayer.HasTile(gridPosition) && !groundLayer.HasTile(gridPosition))
                {
                    audioSource.Play();
                    deleteTilesAtPosition(gridPosition, dustLayer);
                    i = dustLayers.Length + 1;
                    return true;
                }
            }
        }
        return false;
    }

    void deleteTilesAtPosition(Vector3Int gridPosition, Tilemap groundLayer)
    {
        Vector3Int gridPosTmp = gridPosition;

        //pattern ios set by the tool -> activeToolShovel() 
        //note that they are visualy 180 degree reversed in code
        for (int i = 0; i < pattern.Length; i++)
        {
            for (int k = 0; k < pattern[i].Length; k++)
            {
                int ModifiedCounterI = i + 1;
                int ModifiedCounterK = k + 1;

                if (pattern[i][k] == 1)
                {
                    if (ModifiedCounterI < 3)
                    {
                        gridPosTmp.y = gridPosTmp.y + (ModifiedCounterI - 3);
                    }
                    if (ModifiedCounterK < 3)
                    {
                        gridPosTmp.x = gridPosTmp.x + (ModifiedCounterK - 3);
                    }
                    if (ModifiedCounterI > 3)
                    {
                        gridPosTmp.y = gridPosTmp.y + (ModifiedCounterI - 3);
                    }
                    if (ModifiedCounterK > 3)
                    {
                        gridPosTmp.x = gridPosTmp.x + (ModifiedCounterK - 3);
                    }
                    groundLayer.SetTile(gridPosTmp, null);

                }
                gridPosTmp = gridPosition;
            }
        }
    }

    public void setExpCounter(float exp)
    {
        //Set the Textfield in the UI
        TMPro.TextMeshProUGUI expCounter =
            GameObject.Find("ExpCounter").GetComponent<TMPro.TextMeshProUGUI>();
        expCounter.text = "EXP: " + exp;
    }

    public void quitScene()
    {
        dataStorage.exp = dataStorage.exp
                        + (artifact.GetComponent<ArtifactArtifact>().experiencePoints * dataStorage.expMultiplikator);
        SceneManager.UnloadSceneAsync("Artifact " + dataStorage.LevelNumber);
        //Reload Digging Scene
        dataStorage.activateAllObjectsInScene();
    }
}
