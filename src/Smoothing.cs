using System;
using System.Collections.Generic;
using System.Drawing;
// ReSharper disable ArrangeRedundantParentheses

namespace CoachDraw
{
    internal static class Smoothing
    {
        public static int GetLineLength(Point start, Point end)
        {
            return (int)Math.Sqrt(Math.Pow(Math.Abs(start.X - end.X), 2) + Math.Pow(Math.Abs(start.Y - end.Y), 2));
        }

        public static Point GetMidPoint(int x1, int y1, int x2, int y2)
        {
            return new Point((x1 + x2) / 2, (y1 + y2) / 2);
        }

        public static Point GetMidPoint(Point point1, Point point2)
        {
            return GetMidPoint(point1.X, point1.Y, point2.X, point2.Y);
        }

        // Find the point on a line (from point1 to point2), x units away from the start
        public static Point GetPointFromDistance(Point point1, Point point2, int x)
        {
            var d = GetLineLength(point1, point2);
            var t = (double) x / d;
            return new Point((int)(((1 - t) * point1.X) + (t * point2.X)), (int)(((1 - t) * point1.Y) + (t * point2.Y)));
        }

        public static float getAngle(Point point1, Point point2)
        {
            return (float)(Math.Atan2(point2.Y - point1.Y, point2.X - point1.X) * 180 / Math.PI);
        }

        public static List<Point> BasicReduction(List<Point> points, int interval)
        {
            var count = 0;
            var returnVal = new List<Point>();
            for (var i = 0; i < points.Count - 1; i++)
            {
                if (count++ % interval == 0) returnVal.Add(points[i]);
            }
            returnVal.Add(points[points.Count - 1]);
            return returnVal;
        }

        public static List<Point> McMasters(List<Point> points)
        {
            if (points == null || points.Count < 5) return points;
            var returnPoints = new List<Point> { points[0], points[1] };
            for (var i = 2; i < points.Count - 2; i++)
            {
                var x = (points[i - 2].X + points[i - 1].X + points[i].X + points[i + 1].X + points[i + 2].X) / 5.0;
                var y = (points[i - 2].Y + points[i - 1].Y + points[i].Y + points[i + 1].Y + points[i + 2].Y) / 5.0;
                var midpoint = GetMidPoint(points[i].X, points[i].Y, (int)Math.Round(x, MidpointRounding.AwayFromZero), (int)Math.Round(y, MidpointRounding.AwayFromZero));
                returnPoints.Add(midpoint);
            }
            returnPoints.Add(points[points.Count - 2]);
            returnPoints.Add(points[points.Count - 1]);
            return returnPoints;
        }


        /*Provided by: http://www.codeproject.com/KB/cs/Douglas-Peucker_Algorithm.aspx */
        public static List<Point> DouglasPeuckerReduction(List<Point> Points, double Tolerance)
        {
            if (Points == null || Points.Count < 3)
                return Points;

            var firstPoint = 0;
            var lastPoint = Points.Count - 1;
            //Add the first and last index to the keepers
            var pointIndexsToKeep = new List<int> { firstPoint, lastPoint };

            //The first and the last point cannot be the same
            while (Points[firstPoint].Equals(Points[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(Points, firstPoint, lastPoint,
            Tolerance, ref pointIndexsToKeep);

            var returnPoints = new List<Point>();
            pointIndexsToKeep.Sort();
            foreach (var index in pointIndexsToKeep)
            {
                returnPoints.Add(Points[index]);
            }

            return returnPoints;
        }

        /// <summary>
        /// Douglas-Peucker reduction.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="firstPoint">The first point.</param>
        /// <param name="lastPoint">The last point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="pointIndexsToKeep">The point index to keep.</param>
        private static void DouglasPeuckerReduction(List<Point> points, int firstPoint, int lastPoint, double tolerance, ref List<int> pointIndexsToKeep)
        {
            while (true)
            {
                double maxDistance = 0;
                var indexFarthest = 0;

                for (var index = firstPoint; index < lastPoint; index++)
                {
                    var distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
                    if (!(distance > maxDistance)) continue;
                    maxDistance = distance;
                    indexFarthest = index;
                }

                if (maxDistance > tolerance && indexFarthest != 0)
                {
                    //Add the largest point that exceeds the tolerance
                    pointIndexsToKeep.Add(indexFarthest);

                    DouglasPeuckerReduction(points, firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
                    firstPoint = indexFarthest;
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// The distance of a point from a line made from point1 and point2.
        /// </summary>
        public static double PerpendicularDistance (Point Point1, Point Point2, Point Point)
        {
            //Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
            //Base = v((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
            //Area = .5*Base*H                                          *Solve for height
            //Height = Area/.5/Base

            var area = Math.Abs(.5 * (Point1.X * Point2.Y + Point2.X *
                Point.Y + Point.X * Point1.Y - Point2.X * Point1.Y - Point.X *
                Point2.Y - Point1.X * Point.Y));
            var bottom = Math.Sqrt(Math.Pow(Point1.X - Point2.X, 2) + Math.Pow(Point1.Y - Point2.Y, 2));
            var height = area / bottom * 2;

            return height;
        }
    }
}
