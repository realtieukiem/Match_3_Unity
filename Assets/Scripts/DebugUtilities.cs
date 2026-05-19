using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;


public static class DebugUtilities
{

    public static string[,] FillShapesArrayFromResourcesData()
    {
        TextAsset txt = Resources.Load("level") as TextAsset;
        if (txt == null)
            throw new Exception("Cannot find Resources/level.txt");

        string level = txt.text;
        string[] lines = level.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw new Exception("Premade level is empty");

        int columns = lines[0].Split('|').Length;
        string[,] shapes = new string[lines.Length, columns];

        for (int row = 0; row < lines.Length; row++)
        {
            string[] items = lines[row].Split('|');
            if (items.Length != columns)
                throw new Exception("Premade level row " + (row + 1) + " has " + items.Length + " columns, expected " + columns);

            for (int column = 0; column < columns; column++)
            {
                shapes[row, column] = items[column];
            }
        }
        return shapes;

    }

    public static void DebugRotate(GameObject go)
    {
        go.transform.Rotate(0, 0, 80f);
    }

    public static void DebugAlpha(GameObject go)
    {
        Color c = go.GetComponent<SpriteRenderer>().color;
        c.a = 0.6f;
        go.GetComponent<SpriteRenderer>().color = c;
    }

    public static void DebugPositions(GameObject hitGo, GameObject hitGo2)
    {
        string lala =
                        hitGo.GetComponent<Shape>().Row + "-"
                        + hitGo.GetComponent<Shape>().Column + "-"
                         + hitGo2.GetComponent<Shape>().Row + "-"
                         + hitGo2.GetComponent<Shape>().Column;
        Debug.Log(lala);

    }

    public static void ShowArray(ShapesArray shapes)
    {

        Debug.Log(GetArrayContents(shapes));
    }

    /// <summary>
    /// Creates a string with the contents of the shapes array
    /// </summary>
    /// <param name="shapes"></param>
    /// <returns></returns>
    public static string GetArrayContents(ShapesArray shapes)
    {
        string x = string.Empty;
        for (int row = Constants.Rows - 1; row >= 0; row--)
        {

            for (int column = 0; column < Constants.Columns; column++)
            {
                if (shapes[row, column] == null)
                    x += "NULL  |";
                else
                {
                    var shape = shapes[row, column].GetComponent<Shape>();
                    x += shape.Row.ToString("D2")
                        + "-" + shape.Column.ToString("D2");

                    x += shape.Type.Substring(5, 2);

                    if (BonusTypeUtilities.ContainsDestroyWholeRowColumn(shape.Bonus))
                        x += "B";
                    else
                        x += " ";

                    x += " | ";
                }
            }
            x += Environment.NewLine;
        }
        return x;
    }
}

