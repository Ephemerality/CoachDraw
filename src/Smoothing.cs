using System;
using System.Collections.Generic;
using System.Drawing;

namespace CoachDraw
{
    static class Smoothing
    {
        public static int getLineLength(Point start, Point end)
        {
            return (int)Math.Sqrt(Math.Pow(Math.Abs(start.X - end.X), 2) + Math.Pow(Math.Abs(start.Y - end.Y), 2));
        }

        public static Point getMidPoint(int x1, int y1, int x2, int y2)
        {
            return new Point((x1 + x2) / 2, (y1 + y2) / 2);
        }

        public static Point getMidPoint(Point point1, Point point2)
        {
            return getMidPoint(point1.X, point1.Y, point2.X, point2.Y);
        }

        // Find the point on a line (from point1 to point2), x units away from the start
        public static Point getPointFromDistance(Point point1, Point point2, int x)
        {
            int d = getLineLength(point1, point2);
            double t = (double)x / (double)d;
            return new Point((int)(((1 - t) * point1.X) + (t * point2.X)), (int)(((1 - t) * point1.Y) + (t * point2.Y)));
        }

        public static float getAngle(Point point1, Point point2)
        {
            return (float)(Math.Atan2(point2.Y - point1.Y, point2.X - point1.X) * 180 / Math.PI);
        }

        public static List<Point> BasicReduction(List<Point> points, int interval)
        {
            int count = 0;
            List<Point> returnVal = new List<Point>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (count++ % interval == 0) returnVal.Add(points[i]);
            }
            returnVal.Add(points[points.Count - 1]);
            return returnVal;
        }

        public static List<Point> McMasters(List<Point> points)
        {
            if (points == null || points.Count < 5) return points;
            List<Point> returnPoints = new List<Point> { points[0], points[1] };
            for (int i = 2; i < points.Count - 2; i++)
            {
                double x = (points[i - 2].X + points[i - 1].X + points[i].X + points[i + 1].X + points[i + 2].X) / 5.0;
                double y = (points[i - 2].Y + points[i - 1].Y + points[i].Y + points[i + 1].Y + points[i + 2].Y) / 5.0;
                Point midpoint = getMidPoint(points[i].X, points[i].Y, (int)Math.Round(x, MidpointRounding.AwayFromZero), (int)Math.Round(y, MidpointRounding.AwayFromZero));
                returnPoints.Add(midpoint);
            }
            returnPoints.Add(points[points.Count - 2]);
            returnPoints.Add(points[points.Count - 1]);
            return returnPoints;
        }


        /*Provided by: http://www.codeproject.com/KB/cs/Douglas-Peucker_Algorithm.aspx */
        public static List<Point> DouglasPeuckerReduction(List<Point> Points, Double Tolerance)
        {
            if (Points == null || Points.Count < 3)
                return Points;

            Int32 firstPoint = 0;
            Int32 lastPoint = Points.Count - 1;
            //Add the first and last index to the keepers
            List<Int32> pointIndexsToKeep = new List<Int32>() { firstPoint, lastPoint };

            //The first and the last point cannot be the same
            while (Points[firstPoint].Equals(Points[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(Points, firstPoint, lastPoint,
            Tolerance, ref pointIndexsToKeep);

            List<Point> returnPoints = new List<Point>();
            pointIndexsToKeep.Sort();
            foreach (Int32 index in pointIndexsToKeep)
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
        private static void DouglasPeuckerReduction(List<Point> points, Int32 firstPoint, Int32 lastPoint, Double tolerance, ref List<Int32> pointIndexsToKeep)
        {
            Double maxDistance = 0;
            Int32 indexFarthest = 0;

            for (Int32 index = firstPoint; index < lastPoint; index++)
            {
                Double distance = PerpendicularDistance
                    (points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexFarthest);

                DouglasPeuckerReduction(points, firstPoint,
                indexFarthest, tolerance, ref pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexFarthest,
                lastPoint, tolerance, ref pointIndexsToKeep);
            }
        }

        /// <summary>
        /// The distance of a point from a line made from point1 and point2.
        /// </summary>
        /// <param name="pt1">The PT1.</param>
        /// <param name="pt2">The PT2.</param>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public static Double PerpendicularDistance
            (Point Point1, Point Point2, Point Point)
        {
            //Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
            //Base = v((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
            //Area = .5*Base*H                                          *Solve for height
            //Height = Area/.5/Base

            Double area = Math.Abs(.5 * (Point1.X * Point2.Y + Point2.X *
            Point.Y + Point.X * Point1.Y - Point2.X * Point1.Y - Point.X *
            Point2.Y - Point1.X * Point.Y));
            Double bottom = Math.Sqrt(Math.Pow(Point1.X - Point2.X, 2) +
            Math.Pow(Point1.Y - Point2.Y, 2));
            Double height = area / bottom * 2;

            return height;
        }
    }
}
