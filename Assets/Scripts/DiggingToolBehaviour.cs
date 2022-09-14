using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using CursorBehavior;
using DataStorage;

public class DiggingToolBehaviour : MonoBehaviour
{
    private GameObject[] layers;

    private GameObject[] artifacts;

    DataStorageClass dataStorage;
    GameObject dataStorageObject;

    private int[][] pattern = new int[5][];

    private bool zoomArtifactActive = false;

    private float manpower;

    CursorBehaviour cursorBehaviour;

    AudioSource audioSource;

    void Awake()
    {
        cursorBehaviour = GameObject.Find("CursorBehaviourSkript").GetComponent<CursorBehaviour>();
        layers = GameObject.FindGameObjectsWithTag("GroundLayer");
        artifacts = GameObject.FindGameObjectsWithTag("Artifact");
        audioSource = GameObject.Find("AudioDigging").GetComponent<AudioSource>();
    }

    void Start()
    {
        activeToolZoomArtifact();
        cursorBehaviour = GameObject.Find("CursorBehaviourSkript").GetComponent<CursorBehaviour>();
        dataStorageObject = GameObject.Find("DataStorageObject");
        dataStorage = dataStorageObject.GetComponent<DataStorageClass>();
        setManpowerCounter(dataStorage.manpower);
        manpower = dataStorage.manpower;
        setExpCounter(dataStorage.exp);
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            mousePosition.z = 0; //Die Tilemaps sind auf z = 0 und die Kamera bei z = -10 --> Tilemap.HasTile(mousePosition) muss bei z = 0 haben um true zu sein
            if (zoomArtifactActive)
            {
                zoomArtifact(mousePosition);
            }
            else
            {
                if (manpower > 0)
                {
                    digGround(mousePosition);
                }
            }
        }

        setExpCounter(dataStorage.exp);
    }

    public void activeToolShovel()
    {
        zoomArtifactActive = false;
        cursorBehaviour.setCursorShovel();
        pattern[0] = new int[] { 0, 0, 0, 0, 0 };
        pattern[1] = new int[] { 0, 0, 1, 0, 0 };
        pattern[2] = new int[] { 0, 1, 1, 1, 0 };
        pattern[3] = new int[] { 0, 1, 1, 1, 0 };
        pattern[4] = new int[] { 0, 0, 0, 0, 0 };
    }

    public void activeToolPickaxe()
    {
        zoomArtifactActive = false;
        cursorBehaviour.setCursorPickaxe();
        pattern[0] = new int[] { 0, 0, 1, 0, 0 };
        pattern[1] = new int[] { 0, 0, 1, 1, 0 };
        pattern[2] = new int[] { 0, 1, 1, 1, 0 };
        pattern[3] = new int[] { 0, 1, 1, 1, 0 };
        pattern[4] = new int[] { 0, 1, 1, 0, 0 };
    }

    public void activeToolTrowel()
    {
        zoomArtifactActive = false;
        cursorBehaviour.setCursorTrowel();
        pattern[0] = new int[] { 0, 0, 0, 0, 0 };
        pattern[1] = new int[] { 0, 0, 0, 0, 0 };
        pattern[2] = new int[] { 0, 0, 1, 0, 0 };
        pattern[3] = new int[] { 0, 0, 0, 0, 0 };
        pattern[4] = new int[] { 0, 0, 0, 0, 0 };
    }

    public void activeToolZoomArtifact()
    {
        zoomArtifactActive = true;
        cursorBehaviour.setCursorZoomArtifact();
        //pattern = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
    }

    void zoomArtifact(Vector3 mousePosition)
    {
        GameObject[] artifacts = GameObject.FindGameObjectsWithTag("Artifact");
        for (int i = 0; i < artifacts.Length; i++)
        {
            // Sortiert nach Name, größere Zahl = tiefer
            Tilemap artifact = artifacts[i].GetComponent<Tilemap>();

            Vector3Int gridPosition = artifact.WorldToCell(mousePosition);

            //Check if Artifact Tile was klicked
            if (artifact.HasTile(gridPosition))
            {
                //Artefakt bei Seite legen
                artifact.transform.position = new Vector3(-10, -10, 0);
                //Alle GameObjecte deaktivieren
                deactivateAllObjectsInScene();
                dataStorageObject.SetActive(true);

                //tmp[0] ist die zuweisung der jeweiligen Artefakt Nummer
                String[] tmp = artifacts[i].name.Split("(");
                tmp = tmp[1].Split(")");
                dataStorage.ArtifactNumber = Int32.Parse(tmp[0]);

                //Raus aus der Schleife
                i = artifacts.Length;
                //Artefakt Szene laden
                SceneManager.LoadScene("Artifact " + dataStorage.LevelNumber, LoadSceneMode.Additive);
            }
        }
    }

    private void deactivateAllObjectsInScene()
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            allObjects[i].SetActive(false);
        }
        dataStorage.allSceneObjects = allObjects;
    }

    void digGround(Vector3 mousePosition)
    {
        //Nur die Anzahl der Layer -1, weil richtige Sortierung wichtig und FindGameObjectsWithTag diese nicht garantiert
        //-1 weil der letzte Layer nicht abgetragen werden kann
        for (int i = 0; i < layers.Length - 1; i++)
        {
            // Sortiert nach Name, größere Zahl = tiefer
            GameObject groundLayer = GameObject.Find("Ground (" + i + ")");
            Tilemap groundLayerTilemap = groundLayer.GetComponent<Tilemap>();
            TilemapRenderer groundLayerRenderer = groundLayer.GetComponent<TilemapRenderer>();

            Vector3Int gridPosition =
                groundLayerTilemap.WorldToCell(mousePosition);
            if (groundLayerTilemap.HasTile(gridPosition) && !cursorBehaviour.MouseHoversToolbox)
            {
                audioSource.Play();
                deleteTilesAtPosition(gridPosition, groundLayerTilemap, groundLayerRenderer.sortingOrder);
                depleteManpower();
                i = layers.Length + 1;
            }
        }
    }

    void deleteTilesAtPosition(Vector3Int gridPosition, Tilemap groundLayerTilemap, int sortingOrder)
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

                    if (groundLayerTilemap.HasTile(gridPosTmp))
                    {
                        groundLayerTilemap.SetTile(gridPosTmp, null);
                    }
                    checkArtifactsGotHit(sortingOrder, gridPosTmp);
                }
                gridPosTmp = gridPosition;
            }
        }
    }

    void checkArtifactsGotHit(int sortingOrderGround, Vector3Int gridPosTmp)
    {
        GameObject[] artifacts = GameObject.FindGameObjectsWithTag("Artifact");
        for (int i = 0; i < artifacts.Length; i++)
        {
            Tilemap artifactTilemap = artifacts[i].GetComponent<Tilemap>();
            TilemapRenderer artifactTilemapRenderer = artifacts[i].GetComponent<TilemapRenderer>();

            Vector3Int gridPosition =
                artifactTilemap.WorldToCell(gridPosTmp);

            //Check if Artifact was Hit
            if (artifactTilemapRenderer.sortingOrder == (sortingOrderGround + 1)
                && artifactTilemap.HasTile(gridPosition))
            {
                artifacts[i].transform.position = new Vector3(-10, -10, 0); // hide Artifact "destroyed"
            }
        }
    }

    void artefactDamaged(GameObject artifact)
    {
        //Get the Exp from the Artifact
        ArtifactArtifact artifactScript =
            artifact.GetComponent<ArtifactArtifact>();

        //Substract Damaged Penalty
        if (artifactScript.experiencePoints > 0)
        {
            artifactScript.experiencePoints =
                artifactScript.experiencePoints - 20;
        }

        setExpCounter(artifactScript.experiencePoints);
    }

    void depleteManpower()
    {
        if (manpower > 0)
        {
            manpower--;
        }

        setManpowerCounter(manpower);
    }

    public void setManpowerCounter(float manpower)
    {
        //Set the Textfield in the UI
        TMPro.TextMeshProUGUI manpowerCounter =
            GameObject.Find("ManpowerCounter").GetComponent<TMPro.TextMeshProUGUI>();
        manpowerCounter.text = "Manpower: " + manpower;
    }

    public void setExpCounter(float exp)
    {
        //Set the Textfield in the UI
        try
        {
            TMPro.TextMeshProUGUI expCounter =
            GameObject.Find("ExpCounter").GetComponent<TMPro.TextMeshProUGUI>();
            expCounter.text = "EXP: " + exp;
        }
        catch (System.Exception)
        {
            Debug.Log("Alles gut, der Counter is weg ich weiß.");
        }

    }

    public void quitScene()
    {
        DataPersistenceManager.instance.SaveGame();
        SceneManager.LoadScene("Sandbox " + dataStorage.LevelNumber);
    }
}