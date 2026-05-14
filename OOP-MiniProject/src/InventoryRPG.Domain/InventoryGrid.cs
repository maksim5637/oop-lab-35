namespace InventoryRPG.Domain;

// ── Одна клітинка — Composite leaf ──────────────────────────
public sealed class GridCell
{
    public bool  IsOccupied  { get; private set; }
    public Item? OccupiedBy  { get; private set; }

    public void Occupy(Item item)
    {
        IsOccupied = true;
        OccupiedBy = item;
    }

    public void Clear()
    {
        IsOccupied = false;
        OccupiedBy = null;
    }
}

// ── Сітка — Composite container ─────────────────────────────
/// <summary>
/// Двовимірна сітка інвентарю. Реалізує патерн Composite:
/// великі предмети (1×4, 2×3) займають кілька клітинок,
/// але поводяться як єдиний об'єкт.
/// </summary>
public sealed class InventoryGrid
{
    public int Rows { get; }
    public int Cols { get; }

    private readonly GridCell[,] _cells;

    public InventoryGrid(int rows = 8, int cols = 6)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));
        Rows   = rows;
        Cols   = cols;
        _cells = new GridCell[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                _cells[r, c] = new GridCell();
    }

    public bool TryPlace(Item item)
    {
        for (int r = 0; r <= Rows - item.GridHeight; r++)
            for (int c = 0; c <= Cols - item.GridWidth; c++)
                if (CanFit(r, c, item))
                {
                    Place(r, c, item);
                    item.GridX = c;
                    item.GridY = r;
                    return true;
                }
        return false;
    }

    public void Remove(Item item)
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (_cells[r, c].OccupiedBy == item)
                    _cells[r, c].Clear();
        item.GridX = item.GridY = -1;
    }

    public Item? GetItemAt(int row, int col) =>
        (row >= 0 && row < Rows && col >= 0 && col < Cols)
            ? _cells[row, col].OccupiedBy
            : null;

    private bool CanFit(int row, int col, Item item)
    {
        for (int r = row; r < row + item.GridHeight; r++)
            for (int c = col; c < col + item.GridWidth; c++)
                if (_cells[r, c].IsOccupied) return false;
        return true;
    }

    private void Place(int row, int col, Item item)
    {
        for (int r = row; r < row + item.GridHeight; r++)
            for (int c = col; c < col + item.GridWidth; c++)
                _cells[r, c].Occupy(item);
    }
}
