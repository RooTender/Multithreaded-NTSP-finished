using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using Bridge;

namespace GUI.Utils;

public static class FileManager
{
    private static readonly int _nrOfDummyLines = 7;

    public static List<Point> ReadPoints(string filePath = "C:\\Users\\grzeg\\source\\repos\\MultithreadedNTSP\\GUI\\PointSets\\wi29.tsp")
    {
        var points = new List<Point>();

        foreach (string line in File.ReadLines(filePath).Skip(_nrOfDummyLines))
        {
            if (line != "EOF")
            {
                points.Add(GetPointFromLine(line));
            }
        }

        return points;
        // append one more time last node to obtain cycle needed in NTSP
    }

    private static Point GetPointFromLine(string line)
    {
        var lineContent = line.Split(' ')[1..3];

        try
        {
            var x = double.Parse(lineContent[0], CultureInfo.InvariantCulture);
            var y = double.Parse(lineContent[1], CultureInfo.InvariantCulture);

            return new Point(x, y);
        }
        catch (Exception)
        {
            throw new InvalidDataException("Line from file has invalid format.");
        }
    }
}
