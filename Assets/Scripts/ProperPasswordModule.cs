using System;
using PuzzleSolvers;
using ProperPassword;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;
using System.Threading;

/// <summary>
/// On the Subject of Proper Password
/// Created by Timwi
/// </summary>
public class ProperPasswordModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public GameObject[] Screens;
    public Mesh Quad;
    public Material SymbolMaterial;
    public Texture[] Symbols;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private const float _z = -.0001f;

    private const int min = 1;  // inclusive
    private const int max = 26; // inclusive
    private const int numLetters = 6;   // negotiable

    private readonly List<GameObject>[] symbols = new List<GameObject>[7];

    private PConstraint[] puzzle = null;
    private bool threadReady = false;

    void Awake()
    {
        _moduleId = _moduleIdCounter++;
        for (var i = 0; i < symbols.Length; i++)
            symbols[i] = new List<GameObject>();
        new Thread(GeneratePuzzle).Start();
        StartCoroutine(waitForThread());
    }

    private IEnumerator waitForThread()
    {
        yield return new WaitUntil(() => threadReady);
        Debug.LogFormat(@"<> Thread ready");
        for (var i = 0; i < puzzle.Length; i++)
            Debug.LogFormat(@"<> {0} ({1})", puzzle[i].Group, puzzle[i].Constraint.GetType().Name);
    }

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command == "m")
        {
            foreach (var del in symbols[1])
                Destroy(del);
            var g = new GameObject("Symbol");
            g.transform.parent = Screens[1].transform;
            g.transform.localPosition = new Vector3(0, 0, _z);
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = new Vector3(.7f, .7f, .7f);
            var mf = g.AddComponent<MeshFilter>();
            mf.sharedMesh = Quad;
            var mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterial = SymbolMaterial;
            mr.material.mainTexture = Symbols[0];
        }

        yield break;
    }

    T[] newArray<T>(params T[] array) { return array; }

    void GeneratePuzzle()
    {
        var words = Data.AllWords;
        var seed = Rnd.Range(0, int.MaxValue);
        Debug.LogFormat(@"<Proper Password #{0}> Puzzle seed: {1}", _moduleId, seed);
        var rnd = new System.Random(seed);

        var privilegedGroups = newArray(
            ConstraintGroup.Between, ConstraintGroup.Outside, ConstraintGroup.NotDivisible, ConstraintGroup.LessThanConstant, ConstraintGroup.GreaterThanConstant,
            ConstraintGroup.NotPresent, ConstraintGroup.NotLargest, ConstraintGroup.NotPrime, ConstraintGroup.NotSmallest, ConstraintGroup.NotSquare,
            ConstraintGroup.ConcatenationNotDivisible, ConstraintGroup.LessThan, ConstraintGroup.Prime);

        var startTime = DateTime.UtcNow;
        var solution = words[rnd.Next(0, words.Length)].Select(ch => ch - 'A' + 1).ToArray();
        var n = solution.Length;
        var numAttempts = 0;
        tryAgain:
        numAttempts++;
        var allConstraints = new List<PConstraint>();

        // Relations between two numbers (symmetric)
        for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
            {
                for (var m = 2; m < max / 2; m++)
                    if (solution[i] % m == solution[j] % m)
                        allConstraints.Add(PConstraint.ModuloDiff2(i, j, m));

                allConstraints.Add(PConstraint.Sum2(i, j, solution[i] + solution[j]));
                allConstraints.Add(PConstraint.Difference2(i, j, Math.Abs(solution[i] - solution[j])));
                if (solution[j] != 0 && solution[i] % solution[j] == 0 && solution[i] / solution[j] < 4)
                    allConstraints.Add(PConstraint.Quotient2(i, j, solution[i] / solution[j]));
                if (solution[i] != 0 && solution[j] % solution[i] == 0 && solution[j] / solution[i] < 4)
                    allConstraints.Add(PConstraint.Quotient2(i, j, solution[j] / solution[i]));

                for (var value = Math.Min(solution[i], solution[j]) + 1; value < Math.Max(solution[i], solution[j]); value++)
                    allConstraints.Add(PConstraint.Between2(i, j, value));
            }

        // Relations between two numbers (asymmetric)
        var concatenationModulos = new[] { 3, 4, 6, 7, 8, 9, 11 };
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                if (i != j)
                {
                    if (solution[i] < solution[j])
                        allConstraints.Add(PConstraint.LessThan(i, j));
                    var concat = int.Parse(solution[i].ToString() + solution[j].ToString());
                    foreach (var m in concatenationModulos)   // beware lambdas
                        allConstraints.Add(concat % m == 0 ? PConstraint.ConcatenationDivisible(i, j, m) : PConstraint.ConcatenationNotDivisible(i, j, m));
                    if (solution[j] != 0)
                        allConstraints.Add(PConstraint.Modulo2(i, j, solution[i] % solution[j]));
                }

        // Relations between three numbers
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                if (j != i)
                    for (var k = 0; k < n; k++)
                        if (k != i && k != j)
                        {
                            if (j > i && solution[i] + solution[j] == solution[k])
                                allConstraints.Add(PConstraint.Sum3(i, j, k));
                            if (j > i && solution[i] * solution[j] == solution[k])
                                allConstraints.Add(PConstraint.Product3(i, j, k));
                            if (solution[j] != 0 && solution[i] % solution[j] == solution[k])
                                allConstraints.Add(PConstraint.Modulo3(i, j, k));
                        }

        var minVal = solution.Min();
        var maxVal = solution.Max();

        // Value relation constraints
        for (var i = 0; i < n; i++)
        {
            foreach (var v in Enumerable.Range(min, max - min + 1)) // don’t use ‘for’ loop because the variable is captured by lambdas
                if (solution[i] < v)
                    allConstraints.Add(PConstraint.LessThanConstant(i, v));
                else if (solution[i] > v)
                    allConstraints.Add(PConstraint.GreaterThanConstant(i, v));
            allConstraints.Add(solution[i] == minVal ? PConstraint.Smallest(i) : PConstraint.NotSmallest(i));
            allConstraints.Add(solution[i] == maxVal ? PConstraint.Largest(i) : PConstraint.NotLargest(i));
        }

        // Position constraints
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                if (i < j)
                    allConstraints.Add(PConstraint.LeftOfPosition(solution[i], j));
                else if (i > j)
                    allConstraints.Add(PConstraint.RightOfPosition(solution[i], j));

        // Numerical properties of a single value
        for (var i = 0; i < n; i++)
        {
            allConstraints.Add(Data.Primes.Contains(solution[i]) ? PConstraint.Prime(i) : PConstraint.NotPrime(i));
            allConstraints.Add(Data.Squares.Contains(solution[i]) ? PConstraint.Square(i) : PConstraint.NotSquare(i));
            for (var m = 2; m <= 5; m++)
                allConstraints.Add(solution[i] % m == 0 ? PConstraint.Divisible(i, m) : PConstraint.NotDivisible(i, m));
        }

        // Presence and absence of values
        foreach (var v in Enumerable.Range(min, max - min + 1)) // don’t use ‘for’ loop because the value is captured by lambdas
            if (!solution.Contains(v))
                allConstraints.Add(PConstraint.NotPresent(v));

        for (var low = min; low <= max; low++)
            for (var high = low + 1; high <= max; high++)
            {
                if (solution.Any(v => v > low && v < high))
                    allConstraints.Add(PConstraint.Between(low, high));
                if (solution.Any(v => v < low || v > high))
                    allConstraints.Add(PConstraint.Outside(low, high));
            }

        for (var v1 = min; v1 <= max; v1++)
            for (var v2 = v1 + 1; v2 <= max; v2++)
            {
                if ((solution.Contains(v1) && !solution.Contains(v2)) || (solution.Contains(v2) && !solution.Contains(v1)))
                    allConstraints.Add(PConstraint.HasXor(v1, v2));
                // Unused — too slow
                //if ((solution.Contains(v1) && solution.Contains(v2)) || (!solution.Contains(v1) && !solution.Contains(v2)))
                //    allConstraints.Add(PConstraint.HasXnor(v1, v2));
            }

        // Group the constraints
        var constraintGroups = allConstraints.GroupBy(c => c.Group).Select(gr => gr.ToList()).ToList();

        // Choose one constraint from each group
        var constraints = new List<PConstraint>();
        foreach (var gr in constraintGroups)
        {
            var ix = rnd.Next(0, gr.Count);
            constraints.Add(gr[ix]);
            gr.RemoveAt(ix);
        }
        var constraintDic = constraintGroups.Where(gr => gr.Count > 0 && privilegedGroups.Contains(gr[0].Group)).ToDictionary(gr => gr[0].Group);

        // Add more constraints if this is not unique
        var addedCount = 0;
        int solutionCount;
        while ((solutionCount = judgeConstraints(constraints)) > 1)
        {
            foreach (var kvp in constraintDic)
            {
                if (kvp.Value.Count == 0)
                    continue;
                addedCount++;
                var ix = rnd.Next(0, kvp.Value.Count);
                constraints.Add(kvp.Value[ix]);
                kvp.Value.RemoveAt(ix);
            }
        }

        if (solutionCount == 0) // No solution: pretty bad bug
        {
            Debug.LogFormat(@"[Proper Password #{0}] Fatal error: no solution!");
            Module.HandlePass();
            throw new InvalidOperationException();
        }

        // Reduce the set of constraints again
        var req = Ut.ReduceRequiredSet(
             constraints.Shuffle(rnd),
             set => judgeConstraints(set.SetToTest) < 2).ToArray();

        if (req.Length > 16)
        {
            Debug.LogFormat(@"<Proper Password #{0}> Trying again...", _moduleId);
            goto tryAgain;
        }

        puzzle = req;
        threadReady = true;
    }

    private static int judgeConstraints(IEnumerable<PConstraint> constraints)
    {
        return new Puzzle(numLetters, min, max, constraints.Select(c => c.Constraint)).Solve().Take(2).Count();
    }
}
