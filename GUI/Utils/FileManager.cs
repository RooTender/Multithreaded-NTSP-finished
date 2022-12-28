using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using Bridge;

namespace GUI.Utils;

public static class FileManager
{
    public static List<Point> ReadPoints(string filePath)
    {
        return (from line in File.ReadLines(filePath) where !char.IsLetter(line[0])
            where line != "EOF"
            select ReadPoint(line)).ToList();
    }

    private static Point ReadPoint(string line)
    {
        var data = line.Split(' ')[1..3];

        try
        {
            var x = double.Parse(data[0], CultureInfo.InvariantCulture);
            var y = double.Parse(data[1], CultureInfo.InvariantCulture);

            return new Point(x, y);
        }
        catch (Exception)
        {
            throw new InvalidDataException("Line from file has invalid format.");
        }
    }
}
