using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class Piece : NetworkBehaviour
{
    [Networked]
    public bool canMoveInY { get; set; }

    [Networked]
    public bool isLocked { get; set; }

    public PieceColor pieceColor;
    public AudioSource audioSource;
    public AudioClip audioClip;
    public NetworkTransform NetworkTransform;

    public override void Spawned()
    {
        base.Spawned();

        GetComponent<SpriteRenderer>().sprite = GridManager.Instance.turn.piece[GridManager.Instance.currentPlayerTurn];
        GetComponent<Piece>().pieceColor = (PieceColor)GridManager.Instance.currentPlayerTurn;
    }
    private void Update()
    {
        if (!isLocked)
        {
            if (canMoveInY)
            {
                MoveInY();
            }
        }
        else
        {
            if(audioSource != null)
            {
                audioSource.Pause();
                Destroy(audioSource.gameObject);
            }
        }
    }
    public void MoveInX(int direction)
    {
        if (!IsValidPositionX(direction)) return;

        transform.position += new Vector3(direction, 0, 0);
    }
    private void MoveInY()
    {
        if (!IsValidPositionY(-1))
        {
            return;
        }

        StartCoroutine(loopDelay());
        IEnumerator loopDelay()
        {
            transform.position += new Vector3(0, -1, 0);
            NetworkTransform.Teleport(transform.position);
            canMoveInY = false;
            yield return new WaitForSeconds(0.1f);
            if(audioSource != null)
            audioSource.PlayOneShot(audioClip);
            canMoveInY = true;
        }
    }
    private bool IsValidPositionX(int col)
    {
        var tilePosition = new Vector2(col, Mathf.RoundToInt(transform.position.y));
      
        if (tilePosition.x < GridManager.Instance.RectInt.xMin || tilePosition.x  >= GridManager.Instance.RectInt.xMax)
        {
            return false;
        }
        return true;
    }
    private bool IsValidPositionY(int direction)
    {
        var tilePosition = new Vector2(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y + direction));
        for (int i = 0; i < GridManager.Instance.RectInt.width; i++)
        {
            for (int j = 0; j < GridManager.Instance.RectInt.height; j++)
            {
                if (GridManager.Instance.Grid[i, j] != null)
                {
                    if ((Vector2)GridManager.Instance.Grid[i, j].position == tilePosition)
                    {
                        isLocked = true;
                        return false;
                    }
                }
            }
        }
        if (tilePosition.y < GridManager.Instance.RectInt.yMin)
        {
            isLocked = true;
            return false;
        }
        return true;
    }
    private bool IsValidPositionForGhostShape()
    {
        var tilePosition = new Vector2(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
        for (int i = 0; i < GridManager.Instance.RectInt.width; i++)
        {
            for (int j = 0; j < GridManager.Instance.RectInt.height; j++)
            {
                if (GridManager.Instance.Grid[i, j] != null)
                {
                    if ((Vector2)GridManager.Instance.Grid[i, j].position == tilePosition)
                    {
                        return false;
                    }
                }
            }
        }
        if (tilePosition.y <= GridManager.Instance.RectInt.yMin)
        {
            return false;
        }
        return true;
    }
    private void MoveGhostShape()
    {
        while (IsValidPositionForGhostShape())
        {
            transform.position += new Vector3(0, -1, 0);
        }
        if (!IsValidPositionForGhostShape())
        {
            transform.position += new Vector3(0, 1, 0);
        }
    }
}
public enum PieceColor
{
    Red = 0,
    Yellow = 1
}
