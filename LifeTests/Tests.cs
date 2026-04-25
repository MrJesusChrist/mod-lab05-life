using System.Collections.Generic;
using System.IO;
using cli_life;

namespace LifeTests;

// ─── Cell ────────────────────────────────────────────────────────────────────

public class CellTests
{
    private static Cell MakeCell(bool alive, int liveNeighbors)
    {
        var cell = new Cell { IsAlive = alive };
        for (int i = 0; i < 8; i++)
            cell.neighbors.Add(new Cell { IsAlive = i < liveNeighbors });
        return cell;
    }

    private static void Step(Cell cell) { cell.DetermineNextLiveState(); cell.Advance(); }

    [Fact]
    public void DeadCell_With3Neighbors_BecomesAlive()
    {
        var cell = MakeCell(false, 3);
        Step(cell);
        Assert.True(cell.IsAlive);
    }

    [Fact]
    public void LiveCell_With2Neighbors_StaysAlive()
    {
        var cell = MakeCell(true, 2);
        Step(cell);
        Assert.True(cell.IsAlive);
    }

    [Fact]
    public void LiveCell_With3Neighbors_StaysAlive()
    {
        var cell = MakeCell(true, 3);
        Step(cell);
        Assert.True(cell.IsAlive);
    }

    [Fact]
    public void LiveCell_With1Neighbor_Dies()
    {
        var cell = MakeCell(true, 1);
        Step(cell);
        Assert.False(cell.IsAlive);
    }

    [Fact]
    public void LiveCell_With4Neighbors_Dies()
    {
        var cell = MakeCell(true, 4);
        Step(cell);
        Assert.False(cell.IsAlive);
    }

    [Fact]
    public void DeadCell_With2Neighbors_StaysDead()
    {
        var cell = MakeCell(false, 2);
        Step(cell);
        Assert.False(cell.IsAlive);
    }
}

// ─── Board ───────────────────────────────────────────────────────────────────

public class BoardTests
{
    [Fact]
    public void Board_Dimensions_AreCorrect()
    {
        var board = new Board(50, 20, 1, 0);
        Assert.Equal(50, board.Columns);
        Assert.Equal(20, board.Rows);
    }

    [Fact]
    public void Board_Randomize0_AllCellsDead()
    {
        var board = new Board(10, 10, 1, 0);
        Assert.Equal(0, board.CountAlive());
    }

    [Fact]
    public void Board_Randomize1_AllCellsAlive()
    {
        var board = new Board(10, 10, 1, 1.0);
        Assert.Equal(100, board.CountAlive());
    }

    [Fact]
    public void Board_CountAlive_MatchesManuallySetCells()
    {
        var board = new Board(10, 10, 1, 0);
        board.Cells[0, 0].IsAlive = true;
        board.Cells[5, 5].IsAlive = true;
        board.Cells[9, 9].IsAlive = true;
        Assert.Equal(3, board.CountAlive());
    }

    [Fact]
    public void Board_EachCell_Has8Neighbors()
    {
        var board = new Board(10, 10, 1, 0);
        Assert.Equal(8, board.Cells[0, 0].neighbors.Count);
        Assert.Equal(8, board.Cells[5, 5].neighbors.Count);
    }

    [Fact]
    public void Board_SaveLoad_PreservesState()
    {
        var board = new Board(10, 10, 1, 0);
        board.Cells[0, 0].IsAlive = true;
        board.Cells[7, 3].IsAlive = true;

        string path = Path.GetTempFileName();
        try
        {
            board.Save(path);
            var loaded = Board.Load(path);
            Assert.True(loaded.Cells[0, 0].IsAlive);
            Assert.True(loaded.Cells[7, 3].IsAlive);
            Assert.False(loaded.Cells[1, 1].IsAlive);
            Assert.Equal(2, loaded.CountAlive());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Board_Advance_IsolatedCell_Dies()
    {
        var board = new Board(10, 10, 1, 0);
        board.Cells[5, 5].IsAlive = true;
        board.Advance();
        Assert.False(board.Cells[5, 5].IsAlive);
    }

    [Fact]
    public void Board_Advance_Block_IsStable()
    {
        var board = new Board(10, 10, 1, 0);
        board.Cells[3, 3].IsAlive = true;
        board.Cells[4, 3].IsAlive = true;
        board.Cells[3, 4].IsAlive = true;
        board.Cells[4, 4].IsAlive = true;
        board.Advance();
        Assert.True(board.Cells[3, 3].IsAlive);
        Assert.True(board.Cells[4, 3].IsAlive);
        Assert.True(board.Cells[3, 4].IsAlive);
        Assert.True(board.Cells[4, 4].IsAlive);
        Assert.Equal(4, board.CountAlive());
    }

    [Fact]
    public void Board_Advance_Blinker_Oscillates()
    {
        var board = new Board(10, 10, 1, 0);
        // horizontal blinker at row 5: cols 3,4,5
        board.Cells[3, 5].IsAlive = true;
        board.Cells[4, 5].IsAlive = true;
        board.Cells[5, 5].IsAlive = true;
        board.Advance();
        // should rotate to vertical blinker at col 4: rows 4,5,6
        Assert.True(board.Cells[4, 4].IsAlive);
        Assert.True(board.Cells[4, 5].IsAlive);
        Assert.True(board.Cells[4, 6].IsAlive);
        Assert.Equal(3, board.CountAlive());
    }
}

// ─── FigureClassifier ─────────────────────────────────────────────────────────

public class FigureClassifierTests
{
    [Fact]
    public void Classify_Block_ReturnsBlock()
    {
        var cells = new List<(int, int)> { (0, 0), (1, 0), (0, 1), (1, 1) };
        Assert.Equal("Block", FigureClassifier.Classify(cells));
    }

    [Fact]
    public void Classify_BlinkerHorizontal_ReturnsBlinker()
    {
        var cells = new List<(int, int)> { (0, 0), (1, 0), (2, 0) };
        Assert.Equal("Blinker", FigureClassifier.Classify(cells));
    }

    [Fact]
    public void Classify_BlinkerVertical_ReturnsBlinker()
    {
        var cells = new List<(int, int)> { (0, 0), (0, 1), (0, 2) };
        Assert.Equal("Blinker", FigureClassifier.Classify(cells));
    }

    [Fact]
    public void Classify_Beehive_ReturnsBeehive()
    {
        var cells = new List<(int, int)> { (1, 0), (2, 0), (0, 1), (3, 1), (1, 2), (2, 2) };
        Assert.Equal("Beehive", FigureClassifier.Classify(cells));
    }

    [Fact]
    public void Classify_Tub_ReturnsTub()
    {
        var cells = new List<(int, int)> { (1, 0), (0, 1), (2, 1), (1, 2) };
        Assert.Equal("Tub", FigureClassifier.Classify(cells));
    }

    [Fact]
    public void Classify_Loaf_ReturnsLoaf()
    {
        var cells = new List<(int, int)> { (1, 0), (2, 0), (0, 1), (3, 1), (1, 2), (3, 2), (2, 3) };
        Assert.Equal("Loaf", FigureClassifier.Classify(cells));
    }

    [Fact]
    public void Classify_UnknownShape_ReturnsUnknown()
    {
        // 4 cells in a row — not a known figure
        var cells = new List<(int, int)> { (0, 0), (1, 0), (2, 0), (3, 0) };
        Assert.Equal("Unknown", FigureClassifier.Classify(cells));
    }

    [Fact]
    public void Classify_BlockRotated_StillReturnsBlock()
    {
        // Block is symmetric, but test with offset coords to verify normalization
        var cells = new List<(int, int)> { (5, 5), (6, 5), (5, 6), (6, 6) };
        Assert.Equal("Block", FigureClassifier.Classify(cells));
    }
}

// ─── BoardAnalyzer ────────────────────────────────────────────────────────────

public class BoardAnalyzerTests
{
    [Fact]
    public void FindComponents_TwoIsolatedCells_Returns2Components()
    {
        var board = new Board(30, 30, 1, 0);
        board.Cells[2, 2].IsAlive = true;
        board.Cells[20, 20].IsAlive = true;
        var comps = BoardAnalyzer.FindComponents(board);
        Assert.Equal(2, comps.Count);
    }

    [Fact]
    public void FindComponents_ConnectedRow_Returns1Component()
    {
        var board = new Board(20, 20, 1, 0);
        board.Cells[5, 5].IsAlive = true;
        board.Cells[6, 5].IsAlive = true;
        board.Cells[7, 5].IsAlive = true;
        var comps = BoardAnalyzer.FindComponents(board);
        var comp = Assert.Single(comps);
        Assert.Equal(3, comp.Count);
    }

    [Fact]
    public void FindComponents_EmptyBoard_Returns0Components()
    {
        var board = new Board(10, 10, 1, 0);
        var comps = BoardAnalyzer.FindComponents(board);
        Assert.Empty(comps);
    }

    [Fact]
    public void ClassifyAll_BoardWithBlock_CountsBlock()
    {
        var board = new Board(20, 20, 1, 0);
        board.Cells[5, 5].IsAlive = true;
        board.Cells[6, 5].IsAlive = true;
        board.Cells[5, 6].IsAlive = true;
        board.Cells[6, 6].IsAlive = true;
        var counts = BoardAnalyzer.ClassifyAll(board);
        Assert.True(counts.ContainsKey("Block"));
        Assert.Equal(1, counts["Block"]);
        Assert.DoesNotContain("Unknown", counts.Keys);
    }

    [Fact]
    public void ClassifyAll_TwoBlocks_Counts2()
    {
        var board = new Board(20, 20, 1, 0);
        board.Cells[1, 1].IsAlive = true;
        board.Cells[2, 1].IsAlive = true;
        board.Cells[1, 2].IsAlive = true;
        board.Cells[2, 2].IsAlive = true;
        board.Cells[10, 10].IsAlive = true;
        board.Cells[11, 10].IsAlive = true;
        board.Cells[10, 11].IsAlive = true;
        board.Cells[11, 11].IsAlive = true;
        var counts = BoardAnalyzer.ClassifyAll(board);
        Assert.Equal(2, counts["Block"]);
    }

    [Fact]
    public void RunUntilStable_EmptyBoard_StabilizesImmediately()
    {
        var board = new Board(10, 10, 1, 0);
        int gen = BoardAnalyzer.RunUntilStable(board, stableWindow: 3);
        Assert.True(gen <= 10);
    }

    [Fact]
    public void RunUntilStable_BlockBoard_StabilizesQuickly()
    {
        var board = new Board(20, 20, 1, 0);
        board.Cells[5, 5].IsAlive = true;
        board.Cells[6, 5].IsAlive = true;
        board.Cells[5, 6].IsAlive = true;
        board.Cells[6, 6].IsAlive = true;
        int gen = BoardAnalyzer.RunUntilStable(board, stableWindow: 5);
        Assert.True(gen <= 15);
    }
}
