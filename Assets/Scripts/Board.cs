using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private bool _playerInputEnabled = true;

    private void Start()
    {
        _allTiles = new Tile[width, height];
        _allGamePieces = new GamePiece[width, height];

        SetupTiles();
        SetupCamera();
        FillBoard(10, 0.5f);
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
                _allTiles[i, j].Init(i, j, this);
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

    private void FillBoard(int falseYOffset = 0, float moveTime = 0.1f)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (_allGamePieces[i, j] != null)
                {
                    continue;
                }

                ;

                const int maxIterations = 100;
                var iterations = 0;

                FillRandomAt(i, j, falseYOffset, moveTime);

                while (HasMatchOnFill(i, j))
                {
                    ClearPieceAt(i, j);
                    FillRandomAt(i, j, falseYOffset, moveTime);

                    iterations++;

                    if (iterations >= maxIterations)
                    {
                        Debug.Log("Max iterations reached!");
                        break;
                    }
                }
            }
        }
    }

    private bool HasMatchOnFill(int x, int y, int minLength = 3)
    {
        List<GamePiece> leftMatches = FindMatches(x, y, new Vector2(-1, 0), minLength);
        List<GamePiece> downwardMatches = FindMatches(x, y, new Vector2(0, -1), minLength);

        return CombineMatches(leftMatches, downwardMatches).Count > 0;
    }

    private GamePiece FillRandomAt(int x, int y, int falseYOffset = 0, float moveTime = 0.1f)
    {
        GamePiece randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity);

        if (randomPiece == null)
        {
            return null;
        }

        var gamePieceComponent = randomPiece.GetComponent<GamePiece>();

        gamePieceComponent.Init(this);
        PlaceGamePiece(gamePieceComponent, x, y);

        if (falseYOffset != 0)
        {
            randomPiece.transform.position = new Vector3(x, y + falseYOffset, 0);
            gamePieceComponent.Move(x, y, moveTime);
        }

        randomPiece.transform.parent = transform;

        return gamePieceComponent;
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
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    private IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        if (_playerInputEnabled)
        {
            GamePiece clickedPiece = _allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
            GamePiece targetPiece = _allGamePieces[targetTile.xIndex, targetTile.yIndex];

            if (targetPiece == null || clickedPiece == null)
            {
                yield break;
            }

            clickedPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
            targetPiece.Move(clickedPiece.xIndex, clickedPiece.yIndex, swapTime);

            yield return new WaitForSeconds(swapTime);

            List<GamePiece> clickedPieceMatches = FindMatchesAt(clickedPiece.xIndex, clickedPiece.yIndex);
            List<GamePiece> targetPieceMatches = FindMatchesAt(targetTile.xIndex, targetTile.yIndex);

            if (targetPieceMatches.Count == 0 && clickedPieceMatches.Count == 0)
            {
                clickedPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                targetPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);

                yield return new WaitForSeconds(swapTime);
            }
            else
            {
                ClearAndRefillBoard(clickedPieceMatches.Union(targetPieceMatches).ToList());
            }
        }
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

            if (nextPiece == null || nextPiece.matchValue != startPiece.matchValue || matches.Contains(nextPiece))
            {
                break;
            }

            matches.Add(nextPiece);
        }

        return matches.Count >= minlength ? matches : null;
    }

    private List<GamePiece> FindVerticalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);
        List<GamePiece> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);

        var combinedMatches = CombineMatches(upwardMatches, downwardMatches);

        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }

    private List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);
        List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);

        var combinedMatches = CombineMatches(rightMatches, leftMatches);

        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }

    private void HighlightMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                HighlightMatchesAt(i, j);
            }
        }
    }

    private void HighlightPieces(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    private void HighlightMatchesAt(int x, int y)
    {
        HighlightTileOff(x, y);

        var combineMatches = FindMatchesAt(x, y);

        if (combineMatches.Count <= 0)
        {
            return;
        }

        foreach (GamePiece piece in combineMatches)
        {
            HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
        }
    }

    private void HighlightTileOn(int x, int y, Color color)
    {
        SpriteRenderer spriteRenderer = _allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
    }

    private void HighlightTileOff(int i, int j)
    {
        SpriteRenderer spriteRenderer = _allTiles[i, j].GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
    }

    private List<GamePiece> FindMatchesAt(int i, int j, int maxLength = 3)
    {
        List<GamePiece> horizMatches = FindHorizontalMatches(i, j, maxLength);
        List<GamePiece> vertMatches = FindVerticalMatches(i, j, maxLength);

        return CombineMatches(horizMatches, vertMatches);
    }

    private List<GamePiece> FindMatchesAt(List<GamePiece> gamePieces, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();

        foreach (GamePiece piece in gamePieces)
        {
            matches = matches.Union(FindMatchesAt(piece.xIndex, piece.yIndex, minLength)).ToList();
        }

        return matches;
    }

    private List<GamePiece> FindAllMatches()
    {
        List<GamePiece> combinedMatches = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                List<GamePiece> matches = FindMatchesAt(i, j);
                combinedMatches = combinedMatches.Union(matches).ToList();
            }
        }

        return combinedMatches;
    }

    private List<GamePiece> CombineMatches(List<GamePiece> matches1, List<GamePiece> matches2)
    {
        if (matches1 == null)
        {
            matches1 = new List<GamePiece>();
        }

        if (matches2 == null)
        {
            matches2 = new List<GamePiece>();
        }

        return matches1.Union(matches2).ToList();
    }

    private void ClearPieceAt(int x, int y)
    {
        GamePiece pieceToClear = _allGamePieces[x, y];

        if (pieceToClear != null)
        {
            _allGamePieces[x, y] = null;
            Destroy(pieceToClear.gameObject);
        }

        HighlightTileOff(x, y);
    }

    private void ClearPieceAt(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                ClearPieceAt(piece.xIndex, piece.yIndex);
            }
        }
    }

    private void ClearBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                ClearPieceAt(i, j);
            }
        }
    }

    private List<GamePiece> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();

        for (int i = 0; i < height - 1; i++)
        {
            if (_allGamePieces[column, i] == null)
            {
                for (int j = i + 1; j < height; j++)
                {
                    if (_allGamePieces[column, j] != null)
                    {
                        _allGamePieces[column, j].Move(column, i, collapseTime * (j - 1));
                        _allGamePieces[column, i] = _allGamePieces[column, j];
                        _allGamePieces[column, i].SetCoord(column, i);

                        if (!movingPieces.Contains(_allGamePieces[column, i]))
                        {
                            movingPieces.Add(_allGamePieces[column, i]);
                        }

                        _allGamePieces[column, j] = null;

                        break;
                    }
                }
            }
        }

        return movingPieces;
    }

    private List<GamePiece> CollapseColumn(List<GamePiece> gamePieces)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();
        List<int> columnsToCollapse = GetColumns(gamePieces);

        foreach (int column in columnsToCollapse)
        {
            movingPieces = movingPieces.Union(CollapseColumn(column)).ToList();
        }

        return movingPieces;
    }

    private List<int> GetColumns(List<GamePiece> gamePieces)
    {
        List<int> columns = new List<int>();

        foreach (GamePiece piece in gamePieces)
        {
            if (!columns.Contains(piece.xIndex))
            {
                columns.Add(piece.xIndex);
            }
        }

        return columns;
    }

    private void ClearAndRefillBoard(List<GamePiece> gamePieces)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gamePieces));
    }

    private IEnumerator ClearAndRefillBoardRoutine(List<GamePiece> gamePieces)
    {
        _playerInputEnabled = false;
        List<GamePiece> matches = gamePieces;

        do
        {
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));

            yield return StartCoroutine(RefillRoutine());

            matches = FindAllMatches();
            
            yield return new WaitForSeconds(0.5f);
        } while (matches.Count != 0);
        
        _playerInputEnabled = true;
    }

    private IEnumerator RefillRoutine()
    {
        FillBoard(10, 0.5f);
        yield return null;
    }

    private IEnumerator ClearAndCollapseRoutine(List<GamePiece> gamePieces)
    {
        HighlightPieces(gamePieces);
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            ClearPieceAt(gamePieces);

            yield return new WaitForSeconds(0.25f);
            List<GamePiece> movingPieces = CollapseColumn(gamePieces);

            while (!IsCollapsed(movingPieces))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            List<GamePiece> matches = FindMatchesAt(movingPieces);

            if (matches.Count == 0)
            {
                break;
            }

            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
        }

        yield return null;
    }

    private bool IsCollapsed(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                if (piece.transform.position.y - piece.yIndex > 0.001f)
                {
                    return false;
                }
            }
        }

        return true;
    }
}