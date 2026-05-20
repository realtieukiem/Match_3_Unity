using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public struct CandyMoveData
{
    public readonly GameObject Candy;
    public readonly int StartRow;
    public readonly int TargetRow;
    public readonly int Column;

    public CandyMoveData(GameObject candy, int startRow, int targetRow, int column)
    {
        Candy = candy;
        StartRow = startRow;
        TargetRow = targetRow;
        Column = column;
    }

    public int Distance
    {
        get { return Mathf.Abs(StartRow - TargetRow); }
    }
}

public class AlteredCandyInfo
{
    private List<GameObject> newCandy { get; set; }
    private List<CandyMoveData> moves { get; set; }
    public int MaxDistance { get; set; }

    /// <summary>
    /// Returns distinct list of altered candy
    /// </summary>
    public IEnumerable<GameObject> AlteredCandy
    {
        get
        {
            return newCandy.Distinct();
        }
    }

    public IEnumerable<CandyMoveData> Moves
    {
        get
        {
            return moves;
        }
    }

    public void AddCandy(GameObject go)
    {
        if (go == null)
            return;

        if (!newCandy.Contains(go))
            newCandy.Add(go);
    }

    public void AddCandy(GameObject go, int startRow, int targetRow, int column)
    {
        if (go == null)
            return;

        AddCandy(go);
        moves.Add(new CandyMoveData(go, startRow, targetRow, column));
    }

    public AlteredCandyInfo()
    {
        newCandy = new List<GameObject>();
        moves = new List<CandyMoveData>();
    }
}
