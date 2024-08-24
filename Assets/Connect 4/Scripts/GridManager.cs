using Fusion;
using Starter.Platformer;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
public class GridManager : NetworkBehaviour
{
    public float columnWidth = 1f;

    public RectInt RectInt = default;
    public Transform[,] Grid = new Transform[7, 6];
    public GameObject shape;
    public Piece currentShape;
    public static GridManager Instance;
    public GameObject highlight;
    public Turn turn;
    [Networked]
    public int currentPlayerTurn {  get; set; }
    public bool canSpawn;
    public bool isGameStarted;
    public Transform []playerPos;
    public NetworkTransform NetworkTransform;
    public GameManager GameManager;
    public UnityEvent<Player> OnLineConnected;
    private void Awake()
    {
        Instance = this;
    }
    public override void Spawned()
    {
        base.Spawned();
    }
    void Update()
    {
        if (isGameStarted)
        {
            if (!IsGridFull())
            {
                if (currentShape == null) return;

                if (currentShape.isLocked)
                {
                    UpdateGrid();
                    DetectConnectedLines();

                }
            }
            else
            {
                Debug.Log("Draw");
               // StartCoroutine(WaitToResetGame());
            }
        }
        else
        {
           // StartCoroutine(WaitToResetGame());
        }
    }
    IEnumerator WaitToResetGame()
    {
        isGameStarted = false;
        highlight.SetActive(false);
        yield return new WaitForSeconds(3);
        ResetGame();
    }
    public void UpdateGrid()
    {
        var tilePosition = new Vector2(Mathf.RoundToInt(currentShape.transform.position.x), Mathf.RoundToInt(currentShape.transform.position.y));
        if (tilePosition.y > RectInt.yMax - 1)
        {
            Destroy(currentShape.gameObject);
        }
        else
        {
            Grid[Mathf.RoundToInt(tilePosition.x), Mathf.RoundToInt(tilePosition.y)] = currentShape.transform;
        }
    }
    public void DetectConnectedLines()
    {
        int connectedPieces = 0;

        // Check horizontally
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Grid[j, i] != null)
                {
                    bool isConnected = true;
                    for (int k = 1; k < 4; k++)
                    {
                        if (Grid[j + k, i] == null || Grid[j + k, i].GetComponent<Piece>().pieceColor != Grid[j, i].GetComponent<Piece>().pieceColor)
                        {
                            isConnected = false;
                            break;
                        }
                    }
                    if (isConnected)
                    {
                        Debug.Log("Connected line found horizontally at row: " + i);
                        connectedPieces = 4;
                        break;
                    }
                }
            }
        }

        // Check vertically
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                if (Grid[j, i] != null)
                {
                    bool isConnected = true;
                    for (int k = 1; k < 4; k++)
                    {
                        if (Grid[j, i + k] == null || Grid[j, i + k].GetComponent<Piece>().pieceColor != Grid[j, i].GetComponent<Piece>().pieceColor)
                        {
                            isConnected = false;
                            break;
                        }
                    }
                    if (isConnected)
                    {
                        Debug.Log("Connected line found vertically at column: " + j);
                        connectedPieces = 4;
                        break;
                    }
                }
            }
        }

        // Check diagonally (left to right)
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Grid[j, i] != null)
                {
                    bool isConnected = true;
                    for (int k = 1; k < 4; k++)
                    {
                        if (Grid[j + k, i + k] == null || Grid[j + k, i + k].GetComponent<Piece>().pieceColor != Grid[j, i].GetComponent<Piece>().pieceColor)
                        {
                            isConnected = false;
                            break;
                        }
                    }
                    if (isConnected)
                    {
                        Debug.Log("Connected line found diagonally (left to right) starting at column: " + j + ", row: " + i);
                        connectedPieces = 4;
                        break;
                    }
                }
            }
        }

        // Check diagonally (right to left)
        for (int i = 0; i < 3; i++)
        {
            for (int j = 3; j < 7; j++)
            {
                if (Grid[j, i] != null)
                {
                    bool isConnected = true;
                    for (int k = 1; k < 4; k++)
                    {
                        if (Grid[j - k, i + k] == null || Grid[j - k, i + k].GetComponent<Piece>().pieceColor != Grid[j, i].GetComponent<Piece>().pieceColor)
                        {
                            isConnected = false;
                            break;
                        }
                    }
                    if (isConnected)
                    {
                        Debug.Log("Connected line found diagonally (right to left) starting at column: " + j + ", row: " + i);
                        connectedPieces = 4;
                        break;
                    }
                }
            }
        }

        if (connectedPieces < 4)
        {
            Debug.Log("No connected line found.");
        }
        else
        {
            Debug.Log("Winner is detected! " + currentShape.pieceColor);
            isGameStarted = false;

            var player = GameManager.players.Find(x => x.SlotIndex == (int)currentShape.pieceColor);
            OnLineConnected?.Invoke(player);
        }
    }

    public void SpawnShape(int col)
    {
        if(Runner.IsServer)
        currentShape = Runner.Spawn(shape, new Vector3(col, 7, 0), Quaternion.identity).GetComponent<Piece>();
    }
    public bool IsFullColumn(int col)
    {
        for (int j = 0; j < 6; j++)
        {
            if (Grid[col, j] == null)
            {
                return false;
            }
        }
        return true;
    }
    private bool IsGridFull()
    {
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                if (Grid[j, i] == null)
                {
                    return false;
                }
            }
        }
        return true;
    }
    private void ClearGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        currentShape = null;
    }
    public void ResetGame()
    {
        ClearGrid();
        isGameStarted = true;
        highlight.gameObject.SetActive(true);
    }
    public void ChangeTurn()
    {
        currentPlayerTurn = (currentPlayerTurn == 0) ? 1 : 0;
        highlight.gameObject.SetActive(true);   
    }
}
[Serializable]
public struct Turn
{
    public Sprite[] piece;
}