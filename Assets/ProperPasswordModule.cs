using System;
using PuzzleSolvers;
using ProperPassword;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

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

    private readonly List<GameObject>[] symbols = new List<GameObject>[7];

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (var i = 0; i < symbols.Length; i++)
            symbols[i] = new List<GameObject>();
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

    void GeneratePuzzle()
    {
        const int min = 1;  // inclusive
        const int max = 26; // inclusive

        var words = Words.AllWords;
        var seed = Rnd.Range(0, int.MaxValue);
        var rnd = new System.Random(seed);

        var privilegedGroups = new[] { "# < ▲ < #", "# < ¬▲ < #", "#¬|", "< #", "> #", "¬#", "¬largest", "¬prime", "¬smallest", "¬square", "¬|concat", "< ▲", "prime" };

            var startTime = DateTime.UtcNow;
        var solution = words[rnd.Next(0, words.Length)].Select(ch => ch - 'A' + 1).ToArray();
            var n = solution.Length;
            var numAttempts = 0;
            tryAgain:
            numAttempts++;
            var allConstraints = new List<(Constraint constraint, string group, string name)>();

            Puzzle makePuzzle(IEnumerable<Constraint> cs) => new Puzzle(n, min, max, cs);

            static Constraint differenceConstraint(int cell1, int cell2, int diff) => new TwoCellLambdaConstraint(cell1, cell2, (a, b) => Math.Abs(a - b) == diff);
            static Constraint quotientConstraint(int cell1, int cell2, int quotient) => new TwoCellLambdaConstraint(cell1, cell2, (a, b) => a * quotient == b || b * quotient == a);
            static Constraint moduloDiffConstraint(int cell1, int cell2, int modulo) => new TwoCellLambdaConstraint(cell1, cell2, (a, b) => a % modulo == b % modulo);
            static Constraint moduloConstraint(int cell1, int cell2, int modulo) => new TwoCellLambdaConstraint(cell1, cell2, (a, b) => b != 0 && a % b == modulo);

            // Relations between two numbers (symmetric)
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    if (j != i)
                    {
                        if (j > i)
                        {
                            for (var m = 2; m < max / 2; m++)
                                if (solution[i] % m == solution[j] % m)
                                    allConstraints.Add((moduloDiffConstraint(i, j, m), "same %", $"{(char) (i + 'A')} is a multiple of {m} away from {(char) (j + 'A')}."));

                            allConstraints.Add((new SumConstraint(solution[i] + solution[j], new[] { i, j }), "+ #", $"The sum of {(char) (i + 'A')} and {(char) (j + 'A')} is {solution[i] + solution[j]}."));
                            allConstraints.Add((new ProductConstraint(solution[i] * solution[j], new[] { i, j }), "× #", $"The product of {(char) (i + 'A')} and {(char) (j + 'A')} is {solution[i] * solution[j]}."));
                            if (Math.Abs(solution[i] - solution[j]) < 6)
                                allConstraints.Add((differenceConstraint(i, j, Math.Abs(solution[i] - solution[j])), "− #", $"The absolute difference of {(char) (i + 'A')} and {(char) (j + 'A')} is {Math.Abs(solution[i] - solution[j])}."));
                            if (solution[j] != 0 && solution[i] % solution[j] == 0 && solution[i] / solution[j] < 4)
                                allConstraints.Add((quotientConstraint(i, j, solution[i] / solution[j]), "÷ #", $"Of {(char) (i + 'A')} and {(char) (j + 'A')}, one is {solution[i] / solution[j]} times the other."));
                            if (solution[i] != 0 && solution[j] % solution[i] == 0 && solution[j] / solution[i] < 4)
                                allConstraints.Add((quotientConstraint(i, j, solution[j] / solution[i]), "÷ #", $"Of {(char) (i + 'A')} and {(char) (j + 'A')}, one is {solution[j] / solution[i]} times the other."));

                            var minIj = Math.Min(solution[i], solution[j]);
                            var maxIj = Math.Max(solution[i], solution[j]);
                            if (maxIj - minIj > 1)
                                foreach (var k in Enumerable.Range(minIj + 1, maxIj - minIj - 1))
                                    allConstraints.Add((new TwoCellLambdaConstraint(i, j, (a, b) => (a < k && k < b) || (b < k && k < a)), "▲ < # < ▲", $"{k} is between {(char) (i + 'A')} and {(char) (j + 'A')}."));
                        }
                        if (solution[j] != 0)
                            allConstraints.Add((moduloConstraint(i, j, solution[i] % solution[j]), "% #", $"{(char) (i + 'A')} modulo {(char) (j + 'A')} is {solution[i] % solution[j]}."));
                    }

            // Relations between two numbers (asymmetric)
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                {
                    if (i == j)
                        continue;

                    if (solution[i] < solution[j])
                        allConstraints.Add((new TwoCellLambdaConstraint(i, j, (a, b) => a < b), "< ▲", $"{(char) (i + 'A')} is less than {(char) (j + 'A')}."));

                    var concat = int.Parse($"{solution[i]}{solution[j]}");
                    foreach (var m in new[] { 3, 4, 6, 7, 8, 9, 11 })   // beware lambdas
                        if (concat % m == 0)
                            allConstraints.Add((new TwoCellLambdaConstraint(i, j, (a, b) => int.Parse($"{a}{b}") % m == 0), "|concat", $"The concatenation of {(char) (i + 'A')}{(char) (j + 'A')} is divisible by {m}."));
                        else
                            allConstraints.Add((new TwoCellLambdaConstraint(i, j, (a, b) => int.Parse($"{a}{b}") % m != 0), "¬|concat", $"The concatenation of {(char) (i + 'A')}{(char) (j + 'A')} is not divisible by {m}."));
                }

            // Relations between three numbers
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    if (j != i)
                        for (var k = 0; k < n; k++)
                            if (k != i && k != j)
                            {
                                if (j > i && solution[i] + solution[j] == solution[k])
                                    allConstraints.Add((new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a + b == c), "sum ▲", $"{(char) (i + 'A')} + {(char) (j + 'A')} = {(char) (k + 'A')}"));
                                if (j > i && solution[i] * solution[j] == solution[k])
                                    allConstraints.Add((new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a * b == c), "product ▲", $"{(char) (i + 'A')} × {(char) (j + 'A')} = {(char) (k + 'A')}"));
                                if (solution[j] != 0 && solution[i] % solution[j] == solution[k])
                                    allConstraints.Add((new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a % b == c), "mod ▲", $"{(char) (i + 'A')} modulo {(char) (j + 'A')} = {(char) (k + 'A')}"));
                            }

            var minVal = solution.Min();
            var maxVal = solution.Max();

            // Value relation constraints
            for (var i = 0; i < n; i++)
            {
                foreach (var v in Enumerable.Range(min, max - min + 1)) // don’t use ‘for’ loop because the variable is captured by lambdas
                    if (solution[i] < v - 1)
                        allConstraints.Add((new OneCellLambdaConstraint(i, a => a < v), "< #", $"{(char) (i + 'A')} is less than {v}."));
                    else if (solution[i] > v + 1)
                        allConstraints.Add((new OneCellLambdaConstraint(i, a => a > v), "> #", $"{(char) (i + 'A')} is greater than {v}."));
                allConstraints.Add(solution[i] == minVal
                    ? ((Constraint) new MinMaxConstraint(i, MinMaxMode.Min), "smallest", $"{(char) (i + 'A')} has the smallest value.")
                    : (new NotMinMaxConstraint(i, MinMaxMode.Min), "¬smallest", $"{(char) (i + 'A')} does not have the smallest value."));
                allConstraints.Add(solution[i] == maxVal
                    ? ((Constraint) new MinMaxConstraint(i, MinMaxMode.Max), "largest", $"{(char) (i + 'A')} has the largest value.")
                    : (new NotMinMaxConstraint(i, MinMaxMode.Max), "¬largest", $"{(char) (i + 'A')} does not have the largest value."));
            }

            // Position constraints
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    if (i < j - 1)
                        allConstraints.Add((new LeftOfPositionConstraint(solution[i], j), "# ← ▲", $"There is a {solution[i]} further left than {(char) (j + 'A')}."));
                    else if (i > j + 1)
                        allConstraints.Add((new RightOfPositionConstraint(solution[i], j), "▲ → #", $"There is a {solution[i]} further right than {(char) (j + 'A')}."));

            // Numerical properties of a single value
            var primes = new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199 };
            var squares = Enumerable.Range(0, 100).Select(i => i * i).ToArray();
            for (var i = 0; i < n; i++)
            {
                allConstraints.Add(primes.Contains(solution[i])
                    ? (new OneCellLambdaConstraint(i, a => primes.Contains(a)), "prime", $"{(char) (i + 'A')} is a prime number.")
                    : (new OneCellLambdaConstraint(i, a => !primes.Contains(a)), "¬prime", $"{(char) (i + 'A')} is not a prime number."));
                allConstraints.Add(squares.Contains(solution[i])
                    ? (new OneCellLambdaConstraint(i, a => squares.Contains(a)), "square", $"{(char) (i + 'A')} is a square number.")
                    : (new OneCellLambdaConstraint(i, a => !squares.Contains(a)), "¬square", $"{(char) (i + 'A')} is not a square number."));
                foreach (var m in Enumerable.Range(2, 5))   // don’t use ‘for’ loop because the value is captured by lambdas
                    allConstraints.Add(solution[i] % m == 0
                         ? (new OneCellLambdaConstraint(i, a => a % m == 0), "#|", $"{(char) (i + 'A')} is divisible by {m}.")
                         : (new OneCellLambdaConstraint(i, a => a % m != 0), "#¬|", $"{(char) (i + 'A')} is not divisible by {m}."));
            }

            // Presence and absence of values
            foreach (var v in Enumerable.Range(min, max - min + 1)) // don’t use ‘for’ loop because the value is captured by lambdas
                if (!solution.Contains(v))
                    allConstraints.Add((new LambdaConstraint((taken, grid, ix, mv, mxv) =>
                    {
                        if (ix == null)
                            foreach (var arr in taken)
                                arr[v - mv] = true;
                        return null;
                    }), "¬#", $"There is no {v}."));

            static Constraint betweenConstraint(int low, int high, bool reversed) => new LambdaConstraint((taken, grid, ix, mv, mxv) =>
            {
                if (ix == null)
                    return null;
                int remainingCell = -1, numRemaining = 0;
                for (var i = 0; i < grid.Length; i++)
                    if (grid[i] == null)
                    {
                        numRemaining++;
                        remainingCell = i;
                    }
                    else if (reversed ? (grid[i].Value + mv < low || grid[i].Value + mv > high) : (grid[i].Value + mv > low && grid[i].Value + mv < high))
                        return Enumerable.Empty<Constraint>();
                if (numRemaining != 1)
                    return null;
                for (var v = 0; v < taken[remainingCell].Length; v++)
                    if (reversed ? (v + mv >= low && v + mv <= high) : (v + mv <= low || v + mv >= high))
                        taken[remainingCell][v] = true;
                return null;
            });

            for (var low = min; low <= max; low++)
                for (var high = low + 1; high <= max; high++)
                {
                    if (solution.Any(v => v > low && v < high))
                        allConstraints.Add((betweenConstraint(low, high, reversed: false), "# < ▲ < #", $"There is a value between {low} and {high}."));
                    if (solution.Any(v => v < low || v > high))
                        allConstraints.Add((betweenConstraint(low, high, reversed: true), "# < ¬▲ < #", $"There is a value outside of {low} to {high}."));
                }

            static Constraint hasXorConstraint(int has1, int has2) => new LambdaConstraint((taken, grid, ix, mv, mxv) =>
            {
                if (ix == null)
                    return null;
                if (grid[ix.Value].Value + mv == has1)
                {
                    for (var i = 0; i < taken.Length; i++)
                        taken[i][has2 - mv] = true;
                    return Enumerable.Empty<Constraint>();
                }
                if (grid[ix.Value].Value + mv == has2)
                {
                    for (var i = 0; i < taken.Length; i++)
                        taken[i][has1 - mv] = true;
                    return Enumerable.Empty<Constraint>();
                }
                int remainingCell = -1;
                for (var i = 0; i < grid.Length; i++)
                    if (grid[i] == null)
                    {
                        if (remainingCell == -1)
                            remainingCell = i;
                        else
                            return null;
                    }
                for (var v = 0; v < taken[remainingCell].Length; v++)
                    if (v + mv != has1 && v + mv != has2)
                        taken[remainingCell][v] = true;
                return Enumerable.Empty<Constraint>();
            });

            //// unused — too slow
            //static Constraint hasXnorConstraint(int v1, int v2) => new LambdaConstraint((taken, grid, ix, mv, mxv) =>
            //{
            //    if (ix == null)
            //        return null;
            //    bool found1 = false, found2 = false, possible1 = false, possible2 = false;
            //    int remainingCellCount = 0, remainingCell = -1;
            //    for (var i = 0; i < grid.Length; i++)
            //    {
            //        if (grid[i] == null)
            //        {
            //            if (!taken[i][v1 - mv])
            //                possible1 = true;
            //            if (!taken[i][v2 - mv])
            //                possible2 = true;
            //            remainingCellCount++;
            //            remainingCell = i;
            //        }
            //        else
            //        {
            //            if (grid[i].Value + mv == v1)
            //                found1 = true;
            //            if (grid[i].Value + mv == v2)
            //                found2 = true;
            //            if (found1 && found2)
            //                return Enumerable.Empty<Constraint>();
            //        }
            //    }
            //    possible1 |= found1;
            //    possible2 |= found2;
            //    if (remainingCellCount == 1)
            //    {
            //        for (var v = 0; v < taken[remainingCell].Length; v++)
            //            if ((found1 && v + mv != v2) || (found2 && v + mv != v1))
            //                taken[remainingCell][v] = true;
            //        return Enumerable.Empty<Constraint>();
            //    }
            //    if (!possible1 && !possible2)
            //        return Enumerable.Empty<Constraint>();
            //    if (!possible1 || !possible2)
            //        for (var i = 0; i < grid.Length; i++)
            //            for (var v = 0; v < taken[i].Length; v++)
            //                if ((!possible1 && v + mv == v2) || (!possible2 && v + mv == v1))
            //                    taken[i][v] = true;
            //    return null;
            //});

            for (var has1 = min; has1 <= max; has1++)
                for (var has2 = has1 + 1; has2 <= max; has2++)
                {
                    if ((solution.Contains(has1) && !solution.Contains(has2)) || (solution.Contains(has2) && !solution.Contains(has1)))
                        allConstraints.Add((hasXorConstraint(has1, has2), "#/¬#", $"There is a {has1} or a {has2}, but not both."));
                    //if ((solution.Contains(has1) && solution.Contains(has2)) || (!solution.Contains(has1) && !solution.Contains(has2)))
                    //    allConstraints.Add((hasXnorConstraint(has1, has2), "##/¬#¬#", $"There is a {has1} and a {has2}, or neither."));
                }

            // Group the constraints
            var constraintGroups = allConstraints.GroupBy(c => c.group).Select(gr => gr.ToList()).ToList();

            // Choose one constraint from each group
            var constraints = new List<(Constraint constraint, string group, string name)>();
            foreach (var gr in constraintGroups)
            {
                var ix = rnd.Next(0, gr.Count);
                constraints.Add(gr[ix]);
                gr.RemoveAt(ix);
            }
            var constraintDic = constraintGroups.Where(gr => gr.Count > 0 && privilegedGroups.Contains(gr[0].group)).ToDictionary(gr => gr[0].group);
            //Console.WriteLine($"Got {constraints.Count} constraints (one from each group).");

            // Add more constraints if this is not unique
            var addedCount = 0;
            int solutionCount;
            while ((solutionCount = makePuzzle(constraints.Select(c => c.constraint)).Solve().Take(2).Count()) > 1)
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
                System.Diagnostics.Debugger.Break();
            //Console.WriteLine($"Added {addedCount} extra constraints, now have {constraints.Count}.");
            Console.WriteLine($"Seed: {seed,10} - extra: {addedCount}");

            // Reduce the set of constraints again
            var req = Ut.ReduceRequiredSet(
                 constraints.Select((c, ix) => (c.constraint, c.group, c.name, ix)).ToArray().Shuffle(rnd),
                 set => !makePuzzle(set.SetToTest.Select(c => c.constraint)).Solve().Skip(1).Any()).ToArray();
            //Console.WriteLine($"Left with {req.Length} constraints after reduce.");

            if (req.Length > 16)
            {
                //Console.WriteLine("Trying again...");
                goto tryAgain;
            }

            lock (dic)
            {
                foreach (var group in req.Select(c => c.group).Distinct())
                    dic.IncSafe(group);

                timeCount.Count((DateTime.UtcNow - startTime).TotalSeconds);
                constraintsCount.Count(req.Length);
                attemptsCount.Count(numAttempts);
                var numTwoSymbolConstraints = req.Count(tup => new[] { "sum ▲", "product ▲", "mod ▲" }.Contains(tup.group));
                twoSymbolConstraintsCount.Count(numTwoSymbolConstraints);

                //    Console.WriteLine($"There are {n} positions (labeled A–{(char) ('A' + n - 1)}) containing digits {min}–{max - 1}.");
                //    foreach (var (constraint, group, name, ix) in req.OrderBy(t => t.name))
                //        Console.WriteLine($"{name}");
                //    Console.WriteLine();
                //    Console.ReadLine();
                //    foreach (var sol in makePuzzle(req.Select(c => c.constraint)).Solve())
                //        Console.WriteLine(sol.JoinString(", "));
            }
        });
    }
}
