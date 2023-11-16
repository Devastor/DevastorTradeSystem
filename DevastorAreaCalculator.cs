using System;
using System.Collections.Generic;

namespace DevastorTradeSystem
{
    public class DevastorCoverletPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int pos { get; set; }
        public int profit { get; set; }

        public DevastorCoverletPoint(double _x, double _y, int _pos, int _profit)
        {
            X = _x;
            Y = _y;
            pos = _pos;
            profit = _profit;
        }
    }

    public class Polygon
    {
        public List<DevastorCoverletPoint> Vertices { get; set; } = new List<DevastorCoverletPoint>();
    }

    public class DevastorAreaCalculator
    {
        public static List<List<DevastorCoverletPoint>> CoverletsArray = new List<List<DevastorCoverletPoint>>();
        public static List<double> CalculateAreas(List<DevastorCoverletPoint> coverletPoints)
        {
            List<DevastorCoverletPoint> greenPoints = new List<DevastorCoverletPoint>();
            List<DevastorCoverletPoint> redPoints = new List<DevastorCoverletPoint>();

            foreach (var point in coverletPoints)
            {
                if (point.pos == 1)
                    greenPoints.Add(point);
                if (point.pos == -1)
                    redPoints.Add(point);
            }

            List<DevastorCoverletPoint> intersectionPoints = FindIntersectionPoints(greenPoints, redPoints);

            List<DevastorCoverletPoint> CoverletPolygonPoints = new List<DevastorCoverletPoint>();
            List<DevastorCoverletPoint> intersectionPointsCOPY = new List<DevastorCoverletPoint>(intersectionPoints);
            DevastorCoverletPoint PolygonFirstPoint = new DevastorCoverletPoint(0, 0, 0, 0);
            DevastorCoverletPoint PolygonLastPoint = new DevastorCoverletPoint(0, 0, 0, 0);
            DevastorCoverletPoint PolygonNextGreenPoint = new DevastorCoverletPoint(0, 0, 0, 0);
            bool FIRST_POINT_SET = false;
            bool LAST_POINT_SET = false;
            bool FOUND_POLYGON_BORDER = false;
            bool FOUND_LAST_POLYGON_BORDER = false;
            redPoints.Reverse();                    // реверсируем массив красных точек для корректного порядка вершин в полигонах покрывал
            intersectionPoints.Reverse();           // реверсируем массив точек пересечения для корректного порядка вершин в полигонах покрывал
            foreach (DevastorCoverletPoint GR_POINT in greenPoints)
            {
                if (!FIRST_POINT_SET)
                {
                    PolygonFirstPoint = GR_POINT;
                    CoverletPolygonPoints.Add(PolygonFirstPoint);
                    FIRST_POINT_SET = true;
                }
                else CoverletPolygonPoints.Add(GR_POINT);
                DevastorCoverletPoint IntersectionPoint = new DevastorCoverletPoint(0, 0, 0, 0);
                if (intersectionPointsCOPY.Count > 0)
                {
                    foreach (DevastorCoverletPoint ISC_POINT in intersectionPointsCOPY)
                    {
                        try
                        {
                            // если абсцисса вершины Зеленой Ломаной больше абсциссы точки пересечения
                            if (ISC_POINT.X < GR_POINT.X)
                            {
                                CoverletPolygonPoints.Remove(GR_POINT);
                                PolygonNextGreenPoint = GR_POINT;
                                IntersectionPoint = ISC_POINT;
                                CoverletPolygonPoints.Add(IntersectionPoint);
                                intersectionPointsCOPY.Remove(ISC_POINT);
                                PolygonLastPoint = IntersectionPoint;
                                FOUND_POLYGON_BORDER = true;
                                break;
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    FOUND_POLYGON_BORDER = true;
                    FOUND_LAST_POLYGON_BORDER = true;
                    IntersectionPoint = GR_POINT;
                    PolygonLastPoint = IntersectionPoint;
                }
                if (FOUND_POLYGON_BORDER)
                {
                    // добавляем по очередной в обратном порядке в полигон точки из Красной Ломаной
                    foreach (DevastorCoverletPoint RD_POINT in redPoints)
                    {
                        if ( (RD_POINT.X <= PolygonLastPoint.X && RD_POINT.X >= PolygonFirstPoint.X) ||
                             (FOUND_LAST_POLYGON_BORDER && RD_POINT.X >= PolygonFirstPoint.X)
                             )
                            CoverletPolygonPoints.Add(RD_POINT);
                    }
                    if (!FOUND_POLYGON_BORDER)
                    {
                        CoverletPolygonPoints.Add(GR_POINT);
                    }
                    CoverletsArray.Add(CoverletPolygonPoints);
                    CoverletPolygonPoints = new List<DevastorCoverletPoint>();
                    // добавляем точку пересечения как стартовую точку нового полигона и PolygonNextGreenPoint
                    CoverletPolygonPoints.Add(IntersectionPoint);
                    CoverletPolygonPoints.Add(PolygonNextGreenPoint);
                    PolygonFirstPoint = IntersectionPoint;
                    FOUND_POLYGON_BORDER = false;
                }
            }
            redPoints.Reverse();                    // реверсируем обратно
            intersectionPoints.Reverse();           // реверсируем обратно

            // считаем площади покрывал
            double GreenCoverletsArea = 0;
            double RedCoverletsArea = 0;
            foreach (var Coverlet in CoverletsArray)
            {
                int coverlet_summ = CountCoverletSumm(Coverlet);
                //Console.WriteLine("Площадь покрывала: " + coverlet_summ);
                if (coverlet_summ > 0) GreenCoverletsArea += CalculatePolygonArea(Coverlet);
                if (coverlet_summ < 0) RedCoverletsArea += CalculatePolygonArea(Coverlet);
            }
            return new List<double>() { GreenCoverletsArea , RedCoverletsArea };
        }

        public static List<List<DevastorCoverletPoint>> DevastorReturnCoverletsArray()
        {
            return CoverletsArray;
        }

        public static int CountCoverletSumm(List<DevastorCoverletPoint> points)
        {
            int summ = 0;
            //Console.WriteLine("Покрывало: " + points.Count + " точек!");
            foreach (var _point in points)
            {
                //Console.WriteLine("Точка покрывала: [ " + _point.X + ", " + _point.Y + " ]  ( " +_point.pos + " ) { " + _point.profit + " }");
                summ += _point.profit;
            }
            return summ;
        }

        public static double CalculatePolygonArea(List<DevastorCoverletPoint> points)
        {
            int n = points.Count;
            double area = 0.0;

            for (int i = 0; i < n; i++)
            {
                DevastorCoverletPoint currentPoint = points[i];
                DevastorCoverletPoint nextPoint = points[(i + 1) % n];

                area += (currentPoint.X * nextPoint.Y) - (nextPoint.X * currentPoint.Y);
            }

            area /= 2.0;
            return Math.Abs(area);
        }

        public static List<DevastorCoverletPoint> FindIntersectionPoints(List<DevastorCoverletPoint> greenPoints, List<DevastorCoverletPoint> redPoints)
        {
            List<DevastorCoverletPoint> intersectionPoints = new List<DevastorCoverletPoint>();

            for (int i = 0; i < greenPoints.Count - 1; i++)
            {
                DevastorCoverletPoint greenStart = greenPoints[i];
                DevastorCoverletPoint greenEnd = greenPoints[i + 1];

                for (int j = 0; j < redPoints.Count - 1; j++)
                {
                    DevastorCoverletPoint redStart = redPoints[j];
                    DevastorCoverletPoint redEnd = redPoints[j + 1];

                    // Проверяем пересекаются ли отрезки greenStart-greenEnd и redStart-redEnd
                    if (DoSegmentsIntersect(greenStart, greenEnd, redStart, redEnd))
                    {
                        // Если отрезки пересекаются, находим точку пересечения и добавляем ее в список intersectionPoints
                        DevastorCoverletPoint intersectionPoint = FindIntersectionPoint(greenStart, greenEnd, redStart, redEnd);
                        try
                        {
                            intersectionPoints.Add(intersectionPoint);
                        }
                        catch { }
                    }
                }
            }
            Console.WriteLine("Пересечений ломаных:" + intersectionPoints.Count);
            // Устанавливаем поле Z для всех точек в списке intersectionPoints равным 0
            foreach (DevastorCoverletPoint point in intersectionPoints)
            {
                try
                { point.pos = 0; }
                catch { }
            }
            return intersectionPoints;
        }

        // Проверяет, пересекаются ли два отрезка
        private static bool DoSegmentsIntersect(DevastorCoverletPoint p1, DevastorCoverletPoint p2, DevastorCoverletPoint p3, DevastorCoverletPoint p4)
        {
            double d1 = Direction(p3, p4, p1);
            double d2 = Direction(p3, p4, p2);
            double d3 = Direction(p1, p2, p3);
            double d4 = Direction(p1, p2, p4);

            // Если знаки направлений разные, то отрезки пересекаются
            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            // Если один из отрезков состоит из одной точки, то они пересекаются, если эта точка лежит на другом отрезке
            if (d1 == 0 && IsPointOnSegment(p3, p4, p1))
                return true;

            if (d2 == 0 && IsPointOnSegment(p3, p4, p2))
                return true;

            if (d3 == 0 && IsPointOnSegment(p1, p2, p3))
                return true;

            if (d4 == 0 && IsPointOnSegment(p1, p2, p4))
                return true;

            return false;
        }

        // Вычисляет направление поворота для трех точек
        private static double Direction(DevastorCoverletPoint p1, DevastorCoverletPoint p2, DevastorCoverletPoint p3)
        {
            return (p3.X - p1.X) * (p2.Y - p1.Y) - (p2.X - p1.X) * (p3.Y - p1.Y);
        }

        // Проверяет, лежит ли точка p3 на отрезке p1-p2
        private static bool IsPointOnSegment(DevastorCoverletPoint p1, DevastorCoverletPoint p2, DevastorCoverletPoint p3)
        {
            return Math.Min(p1.X, p2.X) <= p3.X && p3.X <= Math.Max(p1.X, p2.X) &&
                   Math.Min(p1.Y, p2.Y) <= p3.Y && p3.Y <= Math.Max(p1.Y, p2.Y);
        }

        // Находит точку пересечения двух отрезков
        private static DevastorCoverletPoint FindIntersectionPoint(DevastorCoverletPoint p1, DevastorCoverletPoint p2, DevastorCoverletPoint p3, DevastorCoverletPoint p4)
        {
            double x1 = p1.X;
            double y1 = p1.Y;
            double x2 = p2.X;
            double y2 = p2.Y;
            double x3 = p3.X;
            double y3 = p3.Y;
            double x4 = p4.X;
            double y4 = p4.Y;

            double denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

            // Проверяем, что отрезки не параллельны
            if (denominator == 0)
            {
                return null; // Возвращаем null, если отрезки параллельны
            }
            double numeratorX = (x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4);
            double numeratorY = (x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4);

            double x = numeratorX / denominator;
            double y = numeratorY / denominator;

            return new DevastorCoverletPoint(x, y, 0, 0);
        }

    }

}
