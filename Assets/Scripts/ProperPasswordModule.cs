using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ProperPassword;
using PuzzleSolvers;
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
    public MeshRenderer[] ScreenBacks;
    public TextMesh[] InputLetters;
    public TextMesh WaitMessage;
    public Mesh Quad;
    public Material SymbolMaterial;
    public Material ScreenNormal;
    public Material ScreenInput;
    public Material ScreenWrong;
    public Material ScreenSolved;
    public Texture[] Symbols;
    public KMSelectable NextButton;
    public KMSelectable[] InputButtons;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private const float _z = -.0001f;

    private const int _min = 1;  // inclusive
    private const int _max = 26; // inclusive
    private const int _numLetters = 6;   // negotiable

    // Dynamically generated GameObjects (symbols and text objects)
    private readonly List<GameObject>[] dyn = new List<GameObject>[7];

    private Clue[] _puzzle = null;
    private string _solution = null;
    private bool _threadReady = false;
    private bool _isSolved = false;
    private int _curPage = 0;
    private Coroutine _solveSubmit;
    private readonly char?[] _curSubmission = new char?[6];
    private char _lastLetter = 'A';

    private static readonly Dictionary<LayoutType, ClueType[]> _layouts = new Dictionary<LayoutType, ClueType[]>
    {
        /* Full */ { LayoutType._1Constant, new[] { ClueType.NotPresent, ClueType.HasSum } },
        /* Room for 2 more */ { LayoutType._1Symbol_1Subsymbol, new[] { ClueType.Smallest, ClueType.NotSmallest, ClueType.Largest, ClueType.NotLargest, ClueType.Prime, ClueType.NotPrime, ClueType.Square, ClueType.NotSquare } },
        /* Full */ { LayoutType._1Symbol_1Constant, new[] { ClueType.LessThanConstant, ClueType.GreaterThanConstant, ClueType.LeftOfPosition, ClueType.RightOfPosition, ClueType.Divisible, ClueType.NotDivisible } },
        /* Full */ { LayoutType._2USymbols_1Constant, new[] { ClueType.Sum2, ClueType.Difference2, ClueType.Product2, ClueType.Between2, ClueType.Quotient2, ClueType.ModuloDiff2 } },
        /* Full */ { LayoutType._2OSymbols, new[] { ClueType.LessThan } },
        /* Room for 1 more */ { LayoutType._2OSymbols_1Constant, new[] { ClueType.Modulo2, ClueType.ConcatenationDivisible, ClueType.ConcatenationNotDivisible } },
        /* Full */ { LayoutType._2UConstants, new[] { ClueType.Between, ClueType.Outside, ClueType.HasXor, ClueType.HasXnor } },
        /* Full */ { LayoutType._2USymbols_1Symbol, new[] { ClueType.Sum3, ClueType.Product3 } },
        /* Room for 1 more */ { LayoutType._3OSymbols, new[] { ClueType.Modulo3 } }
    };

    static T[] newArray<T>(params T[] array) { return array; }

    void Awake()
    {
        _moduleId = _moduleIdCounter++;
        for (var i = 0; i < dyn.Length; i++)
            dyn[i] = new List<GameObject>();

        NextButton.OnInteract = NextButtonPress;
        for (var i = 0; i < InputButtons.Length; i++)
            InputButtons[i].OnInteract = InputButtonPress(i);

        ResetModule();
    }

    private void ResetModule()
    {
        foreach (var scr in ScreenBacks)
            scr.sharedMaterial = ScreenNormal;
        for (var i = 0; i < 6; i++)
            InputLetters[i].gameObject.SetActive(false);
        WaitMessage.text = "Stand by...,Working...,Initializing...,Please wait...,Booting up...,Calculating...,Processing...,Hang on...,Preparing...,Starting...,Loading...,Launching...".Split(',').PickRandom();
        WaitMessage.gameObject.SetActive(true);
        var seed = Rnd.Range(0, int.MaxValue);
        Debug.LogFormat(@"<Proper Password #{0}> Puzzle seed: {1}", _moduleId, seed);
        new Thread(() => GeneratePuzzle(seed)).Start();
        StartCoroutine(waitForThread());
    }

    private KMSelectable.OnInteractHandler InputButtonPress(int i)
    {
        return delegate
        {
            NextButton.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, NextButton.transform);
            if (!_threadReady || _isSolved)
                return false;
            if (_solveSubmit != null)
                StopCoroutine(_solveSubmit);
            if (_curSubmission[i] != _lastLetter)
                _curSubmission[i] = _lastLetter;
            else
                _curSubmission[i] = _lastLetter = (char) ((_lastLetter - 'A' + 1) % 26 + 'A');
            InputLetters[i].gameObject.SetActive(true);
            InputLetters[i].text = _curSubmission[i].ToString();
            ScreenBacks[i + 1].sharedMaterial = ScreenInput;
            foreach (var obj in dyn[i + 1])
                Destroy(obj);
            dyn[i + 1].Clear();
            _solveSubmit = StartCoroutine(delayedSubmit());
            return false;
        };
    }

    private IEnumerator delayedSubmit()
    {
        yield return new WaitForSeconds(5f);
        foreach (var list in dyn)
        {
            foreach (var obj in list)
                Destroy(obj);
            list.Clear();
        }
        var input = _curSubmission.Select(c => c ?? '?').Join("");
        if (input != _solution)
        {
            ScreenBacks[0].sharedMaterial = ScreenWrong;
            for (var i = 0; i < 6; i++)
            {
                ScreenBacks[i + 1].sharedMaterial = ScreenWrong;
                InputLetters[i].gameObject.SetActive(true);
                InputLetters[i].text = _solution[i].ToString();
            }

            Debug.LogFormat(@"[Proper Password #{0}] You entered: {1}. Strike!", _moduleId, input);
            _threadReady = false;
            Module.HandleStrike();
            StartCoroutine(delayedReset());
        }
        else
        {
            foreach (var scr in ScreenBacks)
                scr.sharedMaterial = ScreenSolved;
            Debug.LogFormat(@"[Proper Password #{0}] Module solved!", _moduleId);
            Module.HandlePass();
            _isSolved = true;
        }
    }

    private IEnumerator delayedReset()
    {
        yield return new WaitForSeconds(3f);
        ResetModule();
    }

    private bool NextButtonPress()
    {
        NextButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, NextButton.transform);
        if (!_threadReady || _isSolved)
            return false;
        setPage(_curPage + 1);
        return false;
    }

    private IEnumerator waitForThread()
    {
        yield return new WaitUntil(() => _threadReady);
        Debug.LogFormat(@"[Proper Password #{0}] Solution: {1}", _moduleId, _solution);
        setPage(0);
    }

    private void setPage(int page)
    {
        WaitMessage.gameObject.SetActive(false);
        if (_solveSubmit != null)
            StopCoroutine(_solveSubmit);
        foreach (var scr in ScreenBacks)
            scr.sharedMaterial = ScreenNormal;
        foreach (var txt in InputLetters)
            txt.gameObject.SetActive(false);
        for (var i = 0; i < 6; i++)
            _curSubmission[i] = null;
        _lastLetter = 'A';
        var smallScreenClues = new List<Clue>();
        var wideScreenClues = new List<Clue>();
        foreach (var clue in _puzzle)
            (clue.GetScreenType() == ScreenType.Wide ? wideScreenClues : smallScreenClues).Add(clue);
        var numPages = Math.Max((smallScreenClues.Count + 5) / 6, wideScreenClues.Count);

        _curPage = page % numPages;

        for (var screen = 0; screen < 7; screen++)
        {
            foreach (var obj in dyn[screen])
                Destroy(obj);
            if (screen == 0 && _curPage >= wideScreenClues.Count)
                continue;
            if (screen > 0 && 6 * _curPage + screen - 1 >= smallScreenClues.Count)
                continue;
            var clue = screen == 0 ? wideScreenClues[_curPage] : smallScreenClues[6 * _curPage + screen - 1];
            Debug.LogFormat("<Proper Password #{0}> Screen {1}: {2} ({3})", _moduleId, screen, clue.Type, clue.Values.Join(", "));
            var layout = Array.IndexOf(_layouts[clue.GetLayoutType()], clue.Type);
            switch (clue.Type.GetLayoutType())
            {
                // Wide screen
                case LayoutType._2USymbols_1Symbol:
                    assertValues(clue, 3);
                    showSymbolWide(screen, clue.Values[0], layout == 0 ? WideScreenPosition.Left : WideScreenPosition.Right);
                    showSymbolWide(screen, clue.Values[1], layout == 0 ? WideScreenPosition.Left : WideScreenPosition.Right);
                    showSymbolWide(screen, clue.Values[2], layout == 0 ? WideScreenPosition.Right : WideScreenPosition.Left);
                    break;
                case LayoutType._3OSymbols:
                    assertValues(clue, 3);
                    showSymbolWide(screen, clue.Values[0], layout == 0 ? WideScreenPosition.Left : WideScreenPosition.Right);
                    showSymbolWide(screen, clue.Values[1], layout == 0 ? WideScreenPosition.Left : WideScreenPosition.Right, small: true);
                    showSymbolWide(screen, clue.Values[2], layout == 0 ? WideScreenPosition.Right : WideScreenPosition.Left);
                    break;

                // Narrow screen
                case LayoutType._1Constant:
                    assertValues(clue, 1);
                    showConstant(screen, clue.Values[0], new[] { ConstantDisplay.InsideHoriz, ConstantDisplay.InsideVert }[layout]);
                    break;
                case LayoutType._2UConstants:
                    assertValues(clue, 2);
                    showConstant(screen, layout % 2 != 0 ? clue.Values.Max() : clue.Values.Min(), new[] { ConstantDisplay.InsideHorizUp, ConstantDisplay.InsideVertRight }[layout / 2]);
                    showConstant(screen, layout % 2 != 0 ? clue.Values.Min() : clue.Values.Max(), new[] { ConstantDisplay.InsideHorizDown, ConstantDisplay.InsideVertLeft }[layout / 2]);
                    break;
                case LayoutType._1Symbol_1Subsymbol:
                    assertValues(clue, 1);  // The value specifies the main symbol; the subsymbol is given by ‘layout’
                    showSymbol(screen, clue.Values[0]);
                    showSubsymbol(screen, layout);
                    break;
                case LayoutType._1Symbol_1Constant:
                    assertValues(clue, 2);
                    showSymbol(screen, clue.Values[0]);
                    showConstant(screen, clue.Values[1], new[] { ConstantDisplay.Above, ConstantDisplay.Right, ConstantDisplay.Below, ConstantDisplay.Left, ConstantDisplay.InsideHoriz, ConstantDisplay.InsideVert }[layout]);
                    break;
                case LayoutType._2OSymbols:
                    assertValues(clue, 2);
                    showSymbol(screen, clue.Values[0]);
                    showSymbol(screen, clue.Values[1], small: true);
                    break;
                case LayoutType._2USymbols_1Constant:
                    assertValues(clue, 3);
                    showSymbol(screen, clue.Values[0]);
                    showSymbol(screen, clue.Values[1]);
                    showConstant(screen, clue.Values[2], new[] { ConstantDisplay.Above, ConstantDisplay.Right, ConstantDisplay.Below, ConstantDisplay.Left, ConstantDisplay.InsideHoriz, ConstantDisplay.InsideVert }[layout]);
                    break;
                case LayoutType._2OSymbols_1Constant:
                    assertValues(clue, 3);
                    showSymbol(screen, clue.Values[0]);
                    showSymbol(screen, clue.Values[1], small: true);
                    showConstant(screen, clue.Values[2], new[] { ConstantDisplay.Above, ConstantDisplay.Right, ConstantDisplay.Below, ConstantDisplay.Left }[layout]);
                    break;
            }
        }
    }

    private void showSubsymbol(int screen, int subsymIx)
    {
        createGraphic(screen, "Symbol_" + subsymIx + "_3", Screens[screen], 0, 0, .5f, .5f);
    }

    private void showSymbol(int screen, int symIx, bool small = false)
    {
        createGraphic(screen, "Symbol_" + symIx + (small ? "_2" : "_1"), Screens[screen], 0, 0, small ? .375f : .75f, small ? .375f : .75f);
    }

    private void showSymbolWide(int screen, int symIx, WideScreenPosition position, bool small = false)
    {
        createGraphic(screen, "Symbol_" + symIx + (small ? "_2" : "_1"), Screens[screen], position == WideScreenPosition.Left ? -.35f : .35f, 0, small ? .375f : .75f, small ? .375f : .75f);
    }

    private void showConstant(int screen, int cnst, ConstantDisplay display)
    {
        var outer = createObject(screen, "Constant-outer", Screens[screen],
            rot: Quaternion.Euler(0, 0,
                display == ConstantDisplay.Right || display == ConstantDisplay.InsideVert || display == ConstantDisplay.InsideVertLeft || display == ConstantDisplay.InsideVertRight ? 270 :
                display == ConstantDisplay.Below ? 180 :
                display == ConstantDisplay.Left ? 90 : 0),
            scale: new Vector3(.125f, .125f, .125f));
        var inner = createObject(screen, "Constant-inner", outer,
            pos: new Vector3(0,
                display == ConstantDisplay.InsideHorizUp || display == ConstantDisplay.InsideVertRight ? .5f :
                display == ConstantDisplay.InsideHorizDown || display == ConstantDisplay.InsideVertLeft ? -.5f :
                display == ConstantDisplay.InsideHoriz || display == ConstantDisplay.InsideVert ? 0 : 2.9f,
                0));

        var base4 = "";
        while (cnst > 0)
        {
            base4 = (cnst % 4) + base4;
            cnst /= 4;
        }
        if (base4.Length < 2)
            base4.PadLeft(2, '0');

        var totalWidth = base4.Sum(ch => ch < '2' ? .5f : 1f);
        var x = -totalWidth * .5f;
        for (var i = 0; i < base4.Length; i++)
        {
            var tw = base4[i] < '2' ? .5f : 1f;
            createGraphic(screen, "Symbol_" + (base4[i] - '0') + "_4", inner, x: x + tw * .5f, y: 0, w: 2, h: 2);
            x += tw;
        }
    }

    private GameObject createObject(int screen, string name, GameObject parent, Vector3? pos = null, Quaternion? rot = null, Vector3? scale = null)
    {
        var obj = new GameObject(name);
        obj.transform.parent = parent.transform;
        obj.transform.localPosition = pos ?? new Vector3(0, 0, 0);
        obj.transform.localRotation = rot ?? Quaternion.identity;
        obj.transform.localScale = scale ?? new Vector3(1, 1, 1);
        dyn[screen].Add(obj);
        return obj;
    }

    private void createGraphic(int screen, string symbolStr, GameObject parent, float x, float y, float w, float h)
    {
        var graphic = createObject(screen, symbolStr, parent, new Vector3(x, y, 0), Quaternion.identity, new Vector3(w, h, 1));
        var mf = graphic.AddComponent<MeshFilter>();
        mf.sharedMesh = Quad;
        var mr = graphic.AddComponent<MeshRenderer>();
        mr.sharedMaterial = SymbolMaterial;
        mr.material.mainTexture = Symbols.First(s => s.name == symbolStr);
        dyn[screen].Add(graphic);
    }

    private void assertValues(Clue clue, int expectedNumValues)
    {
        if (clue.Values.Length != expectedNumValues)
            Debug.LogFormat(@"[Proper Password #{0}] Clue {1} has {2} values instead of expected {3}. Please report this bug to Timwi.", _moduleId, clue.Type, clue.Values.Length, expectedNumValues);
    }

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command == "m")
        {
            foreach (var del in dyn[1])
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

    void GeneratePuzzle(int seed)
    {
        var words = Data.AllWords;
        var rnd = new System.Random(seed);

        var privilegedGroups = newArray(
            ClueType.Between, ClueType.Outside, ClueType.NotDivisible, ClueType.LessThanConstant, ClueType.GreaterThanConstant,
            ClueType.NotPresent, ClueType.NotLargest, ClueType.NotPrime, ClueType.NotSmallest, ClueType.NotSquare,
            ClueType.ConcatenationNotDivisible, ClueType.LessThan, ClueType.Prime);

        var startTime = DateTime.UtcNow;
        var solutionWord = words[rnd.Next(0, words.Length)];
        var solution = solutionWord.Select(ch => ch - 'A' + 1).ToArray();
        var n = solution.Length;
        var numAttempts = 0;
        tryAgain:
        numAttempts++;
        var allConstraints = new List<Clue>();

        // Relations between two numbers (symmetric)
        for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
            {
                for (var m = 2; m <= 7; m++)
                    if (solution[i] % m == solution[j] % m)
                        allConstraints.Add(Clue.ModuloDiff2(i, j, m));

                allConstraints.Add(Clue.Sum2(i, j, solution[i] + solution[j]));
                allConstraints.Add(Clue.Product2(i, j, solution[i] * solution[j]));
                allConstraints.Add(Clue.Difference2(i, j, Math.Abs(solution[i] - solution[j])));
                if (solution[j] != 0 && solution[i] % solution[j] == 0 && solution[i] / solution[j] < 8)
                    allConstraints.Add(Clue.Quotient2(i, j, solution[i] / solution[j]));
                if (solution[i] != 0 && solution[j] % solution[i] == 0 && solution[j] / solution[i] < 8)
                    allConstraints.Add(Clue.Quotient2(i, j, solution[j] / solution[i]));

                for (var value = Math.Min(solution[i], solution[j]) + 1; value < Math.Max(solution[i], solution[j]); value++)
                    allConstraints.Add(Clue.Between2(i, j, value));
                allConstraints.Add(Clue.HasSum(solution[i] + solution[j]));
            }

        // Relations between two numbers (asymmetric)
        var concatenationModulos = new[] { 3, 4, 6, 7, 8, 9, 11 };
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                if (i != j)
                {
                    if (solution[i] < solution[j])
                        allConstraints.Add(Clue.LessThan(i, j));
                    var concat = int.Parse(solution[i].ToString() + solution[j].ToString());
                    foreach (var m in concatenationModulos)   // beware lambdas
                        allConstraints.Add(concat % m == 0 ? Clue.ConcatenationDivisible(i, j, m) : Clue.ConcatenationNotDivisible(i, j, m));
                    if (solution[j] != 0)
                        allConstraints.Add(Clue.Modulo2(i, j, solution[i] % solution[j]));
                }

        // Relations between three numbers
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                if (j != i)
                    for (var k = 0; k < n; k++)
                        if (k != i && k != j)
                        {
                            if (j > i && solution[i] + solution[j] == solution[k])
                                allConstraints.Add(Clue.Sum3(i, j, k));
                            if (j > i && solution[i] * solution[j] == solution[k])
                                allConstraints.Add(Clue.Product3(i, j, k));
                            if (solution[j] != 0 && solution[i] % solution[j] == solution[k])
                                allConstraints.Add(Clue.Modulo3(i, j, k));
                        }

        var minVal = solution.Min();
        var maxVal = solution.Max();

        // Value relation constraints
        for (var i = 0; i < n; i++)
        {
            foreach (var v in Enumerable.Range(_min, _max - _min + 1)) // don’t use ‘for’ loop because the variable is captured by lambdas
                if (solution[i] < v)
                    allConstraints.Add(Clue.LessThanConstant(i, v));
                else if (solution[i] > v)
                    allConstraints.Add(Clue.GreaterThanConstant(i, v));
            allConstraints.Add(solution[i] == minVal ? Clue.Smallest(i) : Clue.NotSmallest(i));
            allConstraints.Add(solution[i] == maxVal ? Clue.Largest(i) : Clue.NotLargest(i));
        }

        // Position constraints
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                if (i < j)
                    allConstraints.Add(Clue.LeftOfPosition(j, solution[i]));
                else if (i > j)
                    allConstraints.Add(Clue.RightOfPosition(j, solution[i]));

        // Numerical properties of a single value
        for (var i = 0; i < n; i++)
        {
            allConstraints.Add(Data.Primes.Contains(solution[i]) ? Clue.Prime(i) : Clue.NotPrime(i));
            allConstraints.Add(Data.Squares.Contains(solution[i]) ? Clue.Square(i) : Clue.NotSquare(i));
            for (var m = 2; m <= 5; m++)
                allConstraints.Add(solution[i] % m == 0 ? Clue.Divisible(i, m) : Clue.NotDivisible(i, m));
        }

        // Presence and absence of values
        foreach (var v in Enumerable.Range(_min, _max - _min + 1)) // don’t use ‘for’ loop because the value is captured by lambdas
            if (!solution.Contains(v))
                allConstraints.Add(Clue.NotPresent(v));

        for (var low = _min; low <= _max; low++)
            for (var high = low + 1; high <= _max; high++)
            {
                if (solution.Any(v => v > low && v < high))
                    allConstraints.Add(Clue.Between(low, high));
                if (solution.Any(v => v < low || v > high))
                    allConstraints.Add(Clue.Outside(low, high));
            }

        for (var v1 = _min; v1 <= _max; v1++)
            for (var v2 = v1 + 1; v2 <= _max; v2++)
            {
                if ((solution.Contains(v1) && !solution.Contains(v2)) || (solution.Contains(v2) && !solution.Contains(v1)))
                    allConstraints.Add(Clue.HasXor(v1, v2));
                if ((solution.Contains(v1) && solution.Contains(v2)) || (!solution.Contains(v1) && !solution.Contains(v2)))
                    allConstraints.Add(Clue.HasXnor(v1, v2));
            }

        // Group the constraints
        var constraintGroups = allConstraints.GroupBy(c => c.Type).Select(gr => gr.ToList()).ToList();

        // Choose one constraint from each group
        var constraints = new List<Clue>();
        foreach (var gr in constraintGroups)
        {
            var ix = rnd.Next(0, gr.Count);
            constraints.Add(gr[ix]);
            gr.RemoveAt(ix);
        }
        var constraintDic = constraintGroups.Where(gr => gr.Count > 0 && privilegedGroups.Contains(gr[0].Type)).ToDictionary(gr => gr[0].Type);

        // Add more constraints if this is not unique
        var addedCount = 0;
        int solutionCount;
        while ((solutionCount = judgeConstraints(constraints)) > 1)
        {
            var any = false;
            foreach (var kvp in constraintDic)
            {
                if (kvp.Value.Count == 0)
                    continue;
                any = true;
                addedCount++;
                var ix = rnd.Next(0, kvp.Value.Count);
                constraints.Add(kvp.Value[ix]);
                kvp.Value.RemoveAt(ix);
            }
            if (!any)
                goto tryAgain;
        }

        if (solutionCount == 0) // No solution: pretty bad bug
        {
            Debug.LogFormat(@"[Proper Password #{0}] Fatal error: no solution!", _moduleId);
            Module.HandlePass();
            throw new InvalidOperationException();
        }

        // Reduce the set of constraints again
        var req = Ut.ReduceRequiredSet(
             constraints.Shuffle(rnd),
             set => judgeConstraints(set.SetToTest) < 2).ToArray();

        if (req.Count(c => c.GetScreenType() == ScreenType.Wide) > 2 || req.Count(c => c.GetScreenType() == ScreenType.Narrow) > 12)
            goto tryAgain;

        Thread.Sleep(2000);

        _puzzle = req;
        _solution = solutionWord;
        _threadReady = true;
    }

    private int judgeConstraints(IEnumerable<Clue> constraints)
    {
        return new Puzzle(_numLetters, _min, _max, constraints.Select(c => c.Constraint)).Solve().Take(2).Count();
    }
}
