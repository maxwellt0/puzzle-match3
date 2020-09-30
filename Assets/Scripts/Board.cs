using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour
{
    public int width;
    public int height;

    public int borderSize;

    public GameObject tilePrefab;
    public GamePiece[] gamePiecePrefabs;

    public float swapTime = 0.5f;

    private Tile[,] _allTiles;
    private GamePiece[,] _allGamePieces;

    private Tile _clickedTile;
    private Tile _targetTile;

    private void Start()
    {
        _allTiles = new Tile[width, height];
        _allGamePieces = new GamePiece[width, height];
        
        SetupTiles();
        SetupCamera();
        FillRandom();
    }

    private void Update()
    {
    }

    private void SetupTiles()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(i, j, 0), Quaternion.identity);
                tile.name = "Tile (" + i + "," + j + ")";
                tile.transform.parent = transform;

                _allTiles[i, j] = tile.GetComponent<Tile>();
                _allTiles[i, j].Init(i,j, this);
            }
        }
    }

    private void SetupCamera()
    {
        Camera.main.transform.position = new Vector3((width - 1) / 2f, (height - 1) / 2f, -10f);

        float aspectRatio = Screen.width / (float) Screen.height;
        float verticalSize = height / 2f + borderSize;
        float horizontalSize = (width / 2f + borderSize) / aspectRatio;

        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
    }

    private GamePiece GetRandomGamePiece()
    {
        int randomIndex = Random.Range(0, gamePiecePrefabs.Length);

        if (gamePiecePrefabs[randomIndex] == null)
        {
            Debug.Log("Board: [" + randomIndex + "] doesn't contain a valid GamePiece prefab!");
        }

        return gamePiecePrefabs[randomIndex];
    }

    public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
    {
        gamePiece.transform.position = new Vector3(x, y, 0);
        gamePiece.transform.rotation = Quaternion.identity;
        if (IsWithinBounds(x, y))
        {
            _allGamePieces[x, y] = gamePiece;
        }
        gamePiece.SetCoord(x, y);
    }

    private bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    private void FillRandom()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GamePiece randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity);

                if (randomPiece != null)
                {
                    var gamePieceComponent = randomPiece.GetComponent<GamePiece>();
                    gamePieceComponent.Init(this);
                    PlaceGamePiece(gamePieceComponent, i, j);
                    
                    randomPiece.transform.parent = transform;
                }
            }
        }
    }

    public void ClickTile(Tile tile)
    {
        if (_clickedTile == null)
        {
            _clickedTile = tile;
        }
    }

    public void DragToTile(Tile tile)
    {
        if (_clickedTile != null && IsNextTo(_clickedTile, tile))
        {
            _targetTile = tile;
        }
    }

    public void ReleaseTile()
    {
        if (_clickedTile != null && _targetTile != null)
        {
            SwitchTiles(_clickedTile, _targetTile);
        }
        
        _clickedTile = null;
        _targetTile = null;
    }

    private void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        GamePiece clickedPiece = _allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
        GamePiece targetPiece = _allGamePieces[targetTile.xIndex, targetTile.yIndex];
        
        clickedPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
        targetPiece.Move(clickedPiece.xIndex, clickedPiece.yIndex, swapTime);
    }

    private bool IsNextTo(Tile start, Tile end)
    {
        if (Math.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
        {
            return true;
        }

        if (Math.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex)
        {
            return true;
        }

        return false;
    }

    private List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minlength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();
        GamePiece startPiece = IsWithinBounds(startX, startY) ? _allGamePieces[startX, startY] : null;

        if (startPiece == null)
        {
            return null;
        }

        matches.Add(startPiece);

        int maxValue = (width > height) ? width : height;

        for (int i = 1; i < maxValue; i++)
        {
            var nextX = startX + (int) Mathf.Clamp(searchDirection.x, -1, 1) * i;
            var nextY = startY + (int) Mathf.Clamp(searchDirection.y, -1, 1) * i;

            if (!IsWithinBounds(nextX, nextY))
            {
                break;
            }

            GamePiece nextPiece = _allGamePieces[nextX, nextY];

            if (nextPiece.matchValue != startPiece.matchValue || matches.Contains(nextPiece))
            {
                break;
            }
            
            matches.Add(nextPiece);
        }

        return matches.Count >= minlength ? matches : null;
    }
}