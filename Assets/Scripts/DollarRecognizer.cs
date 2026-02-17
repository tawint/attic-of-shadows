using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// $1 Unistroke Recognizer - algoritm raspoznavaniya zhestov
/// Based on: http://faculty.washington.edu/wobbrock/pubs/uist-07.01.pdf
/// </summary>
public class DollarRecognizer
{
    public class Unistroke
    {
        public int ExampleIndex;
        public string Name { get; private set; }
        public Vector2[] Points { get; private set; }
        public float Angle { get; private set; }
        public List<float> Vector { get; private set; }

        public Unistroke(string name, IEnumerable<Vector2> points)
        {
            Name = string.Intern(name);
            Points = DollarRecognizer.resample(points, _kNormalizedPoints);
            Angle = DollarRecognizer.indicativeAngle(Points);
            DollarRecognizer.rotateBy(Points, -Angle);
            DollarRecognizer.scaleTo(Points, _kNormalizedSize);
            DollarRecognizer.translateTo(Points, Vector2.zero);
            Vector = DollarRecognizer.vectorize(Points);
        }

        public override string ToString()
        {
            return string.Format("{0} #{1}", Name, ExampleIndex);
        }
    }

    public struct Result
    {
        public Unistroke Match;
        public float Score;
        public float Angle;

        public Result(Unistroke match, float score, float angle)
        {
            Match = match;
            Score = score;
            Angle = angle;
        }

        public static Result None
        {
            get
            {
                return new Result(null, 0, 0);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} @{2} ({1})", Match, Score, Angle);
        }
    }

    public string[] EnumerateGestures()
    {
        List<string> result = new List<string>();

        for (int i = 0; i < _library.Count; i++)
        {
            if (!result.Contains(_library[i].Name))
                result.Add(_library[i].Name);
        }

        return result.ToArray();
    }

    protected const int _kNormalizedPoints = 64;
    protected const float _kNormalizedSize = 256.0f;
    protected const float _kAngleRange = 45.0f * Mathf.Deg2Rad;
    protected const float _kAnglePrecision = 2.0f * Mathf.Deg2Rad;
    protected static readonly float _kDiagonal = (Vector2.one * _kNormalizedSize).magnitude;
    protected static readonly float _kHalfDiagonal = _kDiagonal * 0.5f;

    protected List<Unistroke> _library;
    protected Dictionary<string, List<int>> _libraryIndex;

    public DollarRecognizer()
    {
        _library = new List<Unistroke>();
        _libraryIndex = new Dictionary<string, List<int>>();
    }

    /// <summary>
    /// Inicializaciya shablonov zhestov
    /// </summary>
    public void InitializeDefaultGestures()
    {
        _library.Clear();
        _libraryIndex.Clear();

        // === LINII (Lines) ===
        // Vertikalnaya liniya (s????? vniz i naoborot)
        AddLinePattern("VerticalLine", new Vector2(0, -100), new Vector2(0, 100));
        AddLinePattern("VerticalLine", new Vector2(0, 100), new Vector2(0, -100)); 
        AddLinePattern("VerticalLine", new Vector2(-5, -100), new Vector2(5, 100)); 

        // Gorizontalnaya liniya (sleva napravo i naoborot)
        AddLinePattern("HorizontalLine", new Vector2(-100, 0), new Vector2(100, 0));
        AddLinePattern("HorizontalLine", new Vector2(100, 0), new Vector2(-100, 0));
        AddLinePattern("HorizontalLine", new Vector2(-100, -5), new Vector2(100, 5));

        // === KRUZHOK (Circle) ===
        SavePattern("Circle", MakeCirclePoints(100, true));   // po chasovoy
        SavePattern("Circle", MakeCirclePoints(100, false));  // protiv chasovoy
        SavePattern("Circle", MakeCirclePoints(60, true)); 
        SavePattern("Circle", MakeCirclePoints(80, false));

        // === SPIRAL ===
        SavePattern("Spiral", MakeSpiralPoints(30, 100, 2.5f, true, 128));   // 2.5 vitka po chasovoy
        SavePattern("Spiral", MakeSpiralPoints(30, 100, 2.5f, false, 128));  // protiv chasovoy
        SavePattern("Spiral", MakeSpiralPoints(20, 80, 2.0f, true, 96));     // menshe vitkov
        SavePattern("Spiral", MakeSpiralPoints(25, 90, 3.0f, true, 128));    // bolshe vitkov
        SavePattern("Spiral", MakeSpiralPoints(100, 30, 2.5f, true, 128));   // spiral naruzhu

        // === ZVEZDA (5-konechnaya Star) ===
        SavePattern("Star", MakeStarPoints(100, true));    // po chasovoy
        SavePattern("Star", MakeStarPoints(100, false));   // protiv chasovoy
        SavePattern("Star", MakeStarPoints(80, true));     // menshiy razmer
        SavePattern("Star", MakeStarPoints(120, false));   // bolshiy razmer

        // === FIGURA "^" (strelka vverh / caret) ===
        SavePattern("^", new List<Vector2>
        {
            new Vector2(-80, -80),   // levyy niz
            new Vector2(0, 100),     // vershina (vverh)
            new Vector2(80, -80)     // pravyy niz
        });

        SavePattern("^", new List<Vector2>
        {
            new Vector2(-100, -60),
            new Vector2(0, 90),
            new Vector2(100, -60)
        });

        // Plavnyy variant ^ (64 tochki)
        var caretPoints = new List<Vector2>();
        for (int i = 0; i < 32; i++)
        {
            float t = i / 31f;
            caretPoints.Add(Vector2.Lerp(new Vector2(-100, -100), new Vector2(0, 100), t));
        }
        for (int i = 0; i < 32; i++)
        {
            float t = i / 31f;
            caretPoints.Add(Vector2.Lerp(new Vector2(0, 100), new Vector2(100, -100), t));
        }
        SavePattern("^", caretPoints);

        // === FIGURA "V" (galochka vniz) ===
        SavePattern("V", new List<Vector2>
        {
            new Vector2(-80, 80),    // levyy verh
            new Vector2(0, -100),    // niz
            new Vector2(80, 80)      // pravyy verh
        });

        SavePattern("V", new List<Vector2>
        {
            new Vector2(-100, 60),
            new Vector2(0, -90),
            new Vector2(100, 60)
        });

        // Plavnyy variant V
        var vPoints = new List<Vector2>();
        for (int i = 0; i < 32; i++)
        {
            float t = i / 31f;
            vPoints.Add(Vector2.Lerp(new Vector2(-100, 100), new Vector2(0, -100), t));
        }
        for (int i = 0; i < 32; i++)
        {
            float t = i / 31f;
            vPoints.Add(Vector2.Lerp(new Vector2(0, -100), new Vector2(100, 100), t));
        }
        SavePattern("V", vPoints);

        // === FIGURA "<" (ugol vlevo) ===
        SavePattern("<", new List<Vector2>
        {
            new Vector2(80, -80),    // verh-pravo
            new Vector2(-100, 0),    // levaya vershina
            new Vector2(80, 80)      // niz-pravo
        });

        SavePattern("<", new List<Vector2>
        {
            new Vector2(100, -60),
            new Vector2(-80, 0),
            new Vector2(100, 60)
        });

        // Plavnyy variant "<"
        var ltPoints = new List<Vector2>();
        for (int i = 0; i < 32; i++)
            ltPoints.Add(Vector2.Lerp(new Vector2(100, -100), new Vector2(-100, 0), i / 31f));
        for (int i = 0; i < 32; i++)
            ltPoints.Add(Vector2.Lerp(new Vector2(-100, 0), new Vector2(100, 100), i / 31f));
        SavePattern("<", ltPoints);

        // === FIGURA ">" (ugol vpravo) ===
        SavePattern(">", new List<Vector2>
        {
            new Vector2(-80, -80),   // verh-levo
            new Vector2(100, 0),     // pravaya vershina
            new Vector2(-80, 80)     // niz-levo
        });

        SavePattern(">", new List<Vector2>
        {
            new Vector2(-100, -60),
            new Vector2(80, 0),
            new Vector2(-100, 60)
        });

        // Plavnyy variant ">"
        var gtPoints = new List<Vector2>();
        for (int i = 0; i < 32; i++)
            gtPoints.Add(Vector2.Lerp(new Vector2(-100, -100), new Vector2(100, 0), i / 31f));
        for (int i = 0; i < 32; i++)
            gtPoints.Add(Vector2.Lerp(new Vector2(100, 0), new Vector2(-100, 100), i / 31f));
        SavePattern(">", gtPoints);
    }

    // === Vspomogatelnye metody dlya generacii patternov ===

    /// <summary>
    /// Sozdaet pattern linii mezhdu dvumya tochkami
    /// </summary>
    private void AddLinePattern(string name, Vector2 start, Vector2 end)
    {
        var points = new List<Vector2>();
        for (int i = 0; i < 64; i++)
        {
            float t = i / 63f;
            points.Add(Vector2.Lerp(start, end, t));
        }
        SavePattern(name, points);
    }

    /// <summary>
    /// Generiruet tochki kruga
    /// </summary>
    private List<Vector2> MakeCirclePoints(float radius, bool clockwise)
    {
        var points = new List<Vector2>();
        int steps = 64;
        for (int i = 0; i < steps; i++)
        {
            float angle = 2 * Mathf.PI * i / steps;
            if (!clockwise) angle = -angle;
            points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        return points;
    }

    /// <summary>
    /// Generiruet tochki spirali
    /// </summary>
    /// <param name="innerRadius">Nachalniy radius</param>
    /// <param name="outerRadius">Konechniy radius</param>
    /// <param name="turns">Kolichestvo vitkov</param>
    /// <param name="clockwise">Po chasovoy strelke</param>
    /// <param name="steps">Kolichestvo tochek</param>
    private List<Vector2> MakeSpiralPoints(float innerRadius, float outerRadius, float turns, bool clockwise, int steps)
    {
        var points = new List<Vector2>();
        float totalAngle = turns * 2 * Mathf.PI;
        
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);
            float angle = t * totalAngle;
            if (!clockwise) angle = -angle;
            
            // Lineynaya interpolyaciya radiusa
            float radius = Mathf.Lerp(innerRadius, outerRadius, t);
            
            points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        return points;
    }

    /// <summary>
    /// Generiruet tochki pyatikonechnoy zvezdy (risuetsya odnoy liniey)
    /// </summary>
    /// <param name="radius">Radius zvezdy</param>
    /// <param name="clockwise">Po chasovoy strelke</param>
    private List<Vector2> MakeStarPoints(float radius, bool clockwise)
    {
        var points = new List<Vector2>();
        
        // 5 vershin zvezdy, raspolozhennyh ravnomerno
        // Poryadok risovaniya: 0 -> 2 -> 4 -> 1 -> 3 -> 0 (propuskaya po odnoy vershine)
        int[] order = clockwise ? new int[] { 0, 2, 4, 1, 3, 0 } : new int[] { 0, 3, 1, 4, 2, 0 };
        
        Vector2[] vertices = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            // Nachinaem sverhu (-90 gradusov)
            float angle = -Mathf.PI / 2 + (2 * Mathf.PI * i / 5);
            vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        
        // Sozdaem plavnye linii mezhdu vershinami
        int pointsPerSegment = 16;
        for (int i = 0; i < order.Length - 1; i++)
        {
            Vector2 start = vertices[order[i]];
            Vector2 end = vertices[order[i + 1]];
            
            for (int j = 0; j < pointsPerSegment; j++)
            {
                float t = j / (float)pointsPerSegment;
                points.Add(Vector2.Lerp(start, end, t));
            }
        }
        
        return points;
    }

    /// <summary>
    /// Sohranit pattern v biblioteku
    /// </summary>
    public Unistroke SavePattern(string name, IEnumerable<Vector2> points)
    {
        Unistroke stroke = new Unistroke(name, points);

        int index = _library.Count;
        _library.Add(stroke);

        List<int> examples = null;
        if (_libraryIndex.ContainsKey(stroke.Name))
            examples = _libraryIndex[stroke.Name];
        if (examples == null)
        {
            examples = new List<int>();
            _libraryIndex[stroke.Name] = examples;
        }
        stroke.ExampleIndex = examples.Count;
        examples.Add(index);

        return stroke;
    }

    /// <summary>
    /// Raspoznat zhest po tochkam
    /// </summary>
    public Result Recognize(IEnumerable<Vector2> points)
    {
        Vector2[] working = resample(points, _kNormalizedPoints);
        float angle = indicativeAngle(working);
        rotateBy(working, -angle);
        scaleTo(working, _kNormalizedSize);
        translateTo(working, Vector2.zero);

        List<float> v = vectorize(working);

        float bestDist = float.PositiveInfinity;
        int bestIndex = -1;

        for (int i = 0; i < _library.Count; i++)
        {
            float dist = optimalCosineDistance(_library[i].Vector, v);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
            return Result.None;
        else
            return new Result(_library[bestIndex], 1.0f / bestDist, (_library[bestIndex].Angle - angle) * Mathf.Rad2Deg);
    }

    // === Algoritm $1 Recognizer ===

    protected static Vector2[] resample(IEnumerable<Vector2> points, int targetCount)
    {
        List<Vector2> result = new List<Vector2>();

        float interval = pathLength(points) / (targetCount - 1);
        float accumulator = 0;

        Vector2 previous = Vector2.zero;

        IEnumerator<Vector2> stepper = points.GetEnumerator();
        bool more = stepper.MoveNext();
        Vector2 point = stepper.Current;
        result.Add(point);
        previous = point;

        while (more)
        {
            Vector2 delta = point - previous;
            float dist = delta.magnitude;
            if ((accumulator + dist) >= interval)
            {
                float span = ((interval - accumulator) / dist);
                Vector2 q = previous + (span * delta);
                result.Add(q);
                previous = q;
                accumulator = 0;
            }
            else
            {
                accumulator += dist;
                previous = point;
                more = stepper.MoveNext();
                point = stepper.Current;
            }
        }

        if (result.Count < targetCount)
        {
            result.Add(previous);
        }

        return result.ToArray();
    }

    protected static Vector2 centroid(Vector2[] points)
    {
        Vector2 result = Vector2.zero;

        for (int i = 0; i < points.Length; i++)
        {
            result += points[i];
        }

        result = result / (float)points.Length;
        return result;
    }

    protected static float indicativeAngle(Vector2[] points)
    {
        Vector2 delta = centroid(points) - points[0];
        return Mathf.Atan2(delta.y, delta.x);
    }

    protected static void rotateBy(Vector2[] points, float angle)
    {
        Vector2 c = centroid(points);
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 delta = points[i] - c;
            points[i].x = (delta.x * cos) - (delta.y * sin);
            points[i].y = (delta.x * sin) + (delta.y * cos);
            points[i] += c;
        }
    }

    protected static Rect boundingBox(Vector2[] points)
    {
        Rect result = new Rect();
        result.xMin = float.PositiveInfinity;
        result.xMax = float.NegativeInfinity;
        result.yMin = float.PositiveInfinity;
        result.yMax = float.NegativeInfinity;

        for (int i = 0; i < points.Length; i++)
        {
            result.xMin = Mathf.Min(result.xMin, points[i].x);
            result.xMax = Mathf.Max(result.xMax, points[i].x);
            result.yMin = Mathf.Min(result.yMin, points[i].y);
            result.yMax = Mathf.Max(result.yMax, points[i].y);
        }

        return result;
    }

    protected static void scaleTo(Vector2[] points, float normalizedSize)
    {
        Rect bounds = boundingBox(points);
        Vector2 scale = new Vector2(bounds.width, bounds.height) * (1.0f / normalizedSize);
        for (int i = 0; i < points.Length; i++)
        {
            points[i].x = points[i].x * scale.x;
            points[i].y = points[i].y * scale.y;
        }
    }

    protected static void translateTo(Vector2[] points, Vector2 newCentroid)
    {
        Vector2 c = centroid(points);
        Vector2 delta = newCentroid - c;

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = points[i] + delta;
        }
    }

    protected static List<float> vectorize(Vector2[] points)
    {
        float sum = 0;
        List<float> result = new List<float>();

        for (int i = 0; i < points.Length; i++)
        {
            result.Add(points[i].x);
            result.Add(points[i].y);
            sum += points[i].sqrMagnitude;
        }

        float mag = Mathf.Sqrt(sum);
        for (int i = 0; i < result.Count; i++)
        {
            result[i] /= mag;
        }

        return result;
    }

    protected static float optimalCosineDistance(List<float> v1, List<float> v2)
    {
        if (v1.Count != v2.Count)
        {
            return float.NaN;
        }

        float a = 0;
        float b = 0;

        for (int i = 0; i < v1.Count; i += 2)
        {
            a += (v1[i] * v2[i]) + (v1[i + 1] * v2[i + 1]);
            b += (v1[i] * v2[i + 1]) - (v1[i + 1] * v2[i]);
        }

        float angle = Mathf.Atan(b / a);
        float result = Mathf.Acos((a * Mathf.Cos(angle)) + (b * Mathf.Sin(angle)));
        return result;
    }

    protected static float distanceAtAngle(Vector2[] points, Unistroke test, float angle)
    {
        Vector2[] rotated = new Vector2[points.Length];
        rotateBy(rotated, angle);
        return pathDistance(rotated, test.Points);
    }

    protected static float pathDistance(Vector2[] pts1, Vector2[] pts2)
    {
        if (pts1.Length != pts2.Length)
            return float.NaN;

        float result = 0;
        for (int i = 0; i < pts1.Length; i++)
        {
            result += (pts2[i] - pts1[i]).magnitude;
        }

        return result / (float)pts1.Length;
    }

    protected static float pathLength(IEnumerable<Vector2> points)
    {
        float result = 0;
        Vector2 previous = Vector2.zero;

        bool first = true;
        foreach (Vector2 point in points)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                result += (point - previous).magnitude;
            }
            previous = point;
        }

        return result;
    }
}
