using UnityEngine;

public enum TileType
{
    Normal,
    Obstacle
}

public class Tile : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    private Board _board;

    public TileType tileType = TileType.Normal;
    private void OnMouseDown()
    {
        if (_board != null)
        {
            _board.ClickTile(this);
        }
    }

    private void OnMouseEnter()
    {
        if (_board != null)
        {
            _board.DragToTile(this);
        }
    }

    private void OnMouseUp()
    {
        if (_board != null)
        {
            _board.ReleaseTile();
        }
    }

    public void Init(int x, int y, Board board)
    {
        xIndex = x;
        yIndex = y;
        _board = board;
    }
    
}
