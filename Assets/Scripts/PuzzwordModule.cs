using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Puzzword;
using PuzzleSolvers;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Puzzword
/// Created by Timwi
/// </summary>
public class PuzzwordModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public GameObject[] Screens;
    public MeshRenderer StatusScreen;
    public GameObject StatusSquare;
    public MeshRenderer[] ScreenBacks;
    public TextMesh[] InputLetters;
    public TextMesh WaitMessage;
    public Mesh Quad;
    public Material SymbolMaterial;
    public Material ScreenNormal;
    public Material ScreenInput;
    public Material ScreenWrong;
    public Material ScreenSolved;
    public Material StatusScreenNormal;
    public Material StatusScreenInput;
    public Material StatusScreenWrong;
    public Material StatusScreenSolved;
    public Texture[] Symbols;
    public KMSelectable NextButton;
    public KMSelectable[] InputButtons;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
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
    private Coroutine _delayedReset;
    private readonly char?[] _curSubmission = new char?[6];
    private char _lastLetter = 'A';

    private static readonly Dictionary<LayoutType, ClueType[]> _layouts = new Dictionary<LayoutType, ClueType[]>
    {
        /* Full */ { LayoutType._1Constant, new[] { ClueType.NotPresent, ClueType.HasSum } },
        /* Room for 2 more */ { LayoutType._1Symbol_1Subsymbol, new[] { ClueType.Smallest, ClueType.NotSmallest, ClueType.Largest, ClueType.NotLargest, ClueType.Prime, ClueType.NotPrime, ClueType.Square, ClueType.NotSquare } },
        /* Full */ { LayoutType._1Symbol_1Constant, new[] { ClueType.GreaterThanConstant, ClueType.RightOfPosition, ClueType.LessThanConstant, ClueType.LeftOfPosition, ClueType.Divisible, ClueType.NotDivisible } },
        /* Full */ { LayoutType._2USymbols_1Constant, new[] { ClueType.Sum2, ClueType.Difference2, ClueType.Product2,  ClueType.Quotient2, ClueType.Between2, ClueType.ModuloDiff2 } },
        /* Full */ { LayoutType._2OSymbols, new[] { ClueType.LessThan } },
        /* Room for 1 more */ { LayoutType._2OSymbols_1Constant, new[] { ClueType.ConcatenationDivisible, ClueType.Modulo2, ClueType.ConcatenationNotDivisible } },
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

        foreach (var scr in ScreenBacks)
            scr.sharedMaterial = ScreenNormal;
        StatusScreen.sharedMaterial = StatusScreenNormal;
        StatusSquare.SetActive(false);
        for (var i = 0; i < 6; i++)
            InputLetters[i].gameObject.SetActive(false);
        WaitMessage.text = "Stand by...,Working...,Initializing...,Please wait...,Booting up...,Calculating...,Processing...,Hang on...,Preparing...,Starting...,Loading...,Launching...".Split(',').PickRandom();
        WaitMessage.gameObject.SetActive(true);
        var seed = Rnd.Range(0, int.MaxValue);
        Debug.LogFormat(@"<Puzzword #{0}> Puzzle seed: {1}", _moduleId, seed);
        new Thread(() => GeneratePuzzle(seed)).Start();
        StartCoroutine(waitForThread());
    }

    private KMSelectable.OnInteractHandler InputButtonPress(int i)
    {
        return delegate
        {
            NextButton.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, InputButtons[i].transform);
            if (!_threadReady || _isSolved)
                return false;
            Audio.PlaySoundAtTransform("blip", transform);
            if (_solveSubmit != null)
                StopCoroutine(_solveSubmit);
            if (_curSubmission[i] != _lastLetter)
                _curSubmission[i] = _lastLetter;
            else
                _curSubmission[i] = _lastLetter = (char) ((_lastLetter - 'A' + 1) % 26 + 'A');
            InputLetters[i].gameObject.SetActive(true);
            InputLetters[i].text = _curSubmission[i].ToString();
            ScreenBacks[i + 1].sharedMaterial = ScreenInput;
            StatusScreen.sharedMaterial = StatusScreenInput;
            foreach (var obj in dyn[i + 1])
                Destroy(obj);
            dyn[i + 1].Clear();
            _solveSubmit = StartCoroutine(delayedSubmit());
            return false;
        };
    }

    private IEnumerator delayedSubmit()
    {
        const float duration = 5f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            StatusSquare.transform.localPosition = new Vector3(0, 0, 0);
            StatusSquare.transform.localRotation = Quaternion.identity;
            StatusSquare.transform.localScale = new Vector3(.4f, .825f * (1 - elapsed / duration), 1);
            yield return null;
            elapsed += Time.deltaTime;
        }
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
                InputLetters[i].text = (_curSubmission[i] ?? '?').ToString();
            }
            StatusScreen.sharedMaterial = StatusScreenWrong;
            StatusSquare.SetActive(false);

            Debug.LogFormat(@"[Puzzword #{0}] You entered: {1}. Strike!", _moduleId, input);
            Module.HandleStrike();
            _delayedReset = StartCoroutine(delayedReset());
        }
        else
        {
            foreach (var scr in ScreenBacks)
                scr.sharedMaterial = ScreenSolved;
            StatusScreen.sharedMaterial = StatusScreenSolved;
            StatusSquare.SetActive(false);
            Debug.LogFormat(@"[Puzzword #{0}] Module solved!", _moduleId);
            Audio.PlaySoundAtTransform("blip3", transform);
            Module.HandlePass();
            _isSolved = true;
        }
    }

    private IEnumerator delayedReset()
    {
        yield return new WaitForSeconds(3f);
        _delayedReset = null;
        setPage(0);
    }

    private bool NextButtonPress()
    {
        if (_delayedReset != null)
        {
            StopCoroutine(_delayedReset);
            _delayedReset = null;
        }
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
        foreach (var c in _puzzle.OrderByDescending(p => p.GetScreenType()))
        {
            switch (c.Type)
            {
                case ClueType.Between: Debug.LogFormat("[Puzzword #{0}] Constraint: There is a value between {1} and {2} (exclusive).", _moduleId, c.Values[0], c.Values[1]); break;
                case ClueType.Between2: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is between {2} and {3}.", _moduleId, c.Values[2], (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A')); break;
                case ClueType.ConcatenationDivisible: Debug.LogFormat("[Puzzword #{0}] Constraint: The concatenation of {1}{2} is divisible by {3}.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), c.Values[2]); break;
                case ClueType.ConcatenationNotDivisible: Debug.LogFormat("[Puzzword #{0}] Constraint: The concatenation of {1}{2} is not divisible by {3}.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), c.Values[2]); break;
                case ClueType.Difference2: Debug.LogFormat("[Puzzword #{0}] Constraint: The absolute difference of {1} and {2} is {3}.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), c.Values[2]); break;
                case ClueType.Divisible: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is divisible by {2}.", _moduleId, (char) (c.Values[0] + 'A'), c.Values[1]); break;
                case ClueType.GreaterThanConstant: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is greater than {2}.", _moduleId, (char) (c.Values[0] + 'A'), c.Values[1]); break;
                case ClueType.HasSum: Debug.LogFormat("[Puzzword #{0}] Constraint: There are two values that add up to {1}.", _moduleId, c.Values[0]); break;
                case ClueType.HasXnor: Debug.LogFormat("[Puzzword #{0}] Constraint: There is a {1} and a {2}, or neither.", _moduleId, c.Values[0], c.Values[1]); break;
                case ClueType.HasXor: Debug.LogFormat("[Puzzword #{0}] Constraint: There is a {1} or a {2}, but not both.", _moduleId, c.Values[0], c.Values[1]); break;
                case ClueType.Largest: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} has the largest value.", _moduleId, (char) (c.Values[0] + 'A')); break;
                case ClueType.LeftOfPosition: Debug.LogFormat("[Puzzword #{0}] Constraint: There is a {1} further left than {2}.", _moduleId, c.Values[1], (char) (c.Values[0] + 'A')); break;
                case ClueType.LessThan: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is less than {2}.", _moduleId, (char) (c.Values[1] + 'A'), (char) (c.Values[0] + 'A')); break;
                case ClueType.LessThanConstant: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is less than {2}.", _moduleId, (char) (c.Values[0] + 'A'), c.Values[1]); break;
                case ClueType.Modulo2: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} modulo {2} = {3}.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), c.Values[2]); break;
                case ClueType.Modulo3: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} modulo {2} = {3}", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), (char) (c.Values[2] + 'A')); break;
                case ClueType.ModuloDiff2: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is a multiple of {2} away from {3}.", _moduleId, (char) (c.Values[0] + 'A'), c.Values[2], (char) (c.Values[1] + 'A')); break;
                case ClueType.NotDivisible: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is not divisible by {2}.", _moduleId, (char) (c.Values[0] + 'A'), c.Values[1]); break;
                case ClueType.NotLargest: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} does not have the largest value.", _moduleId, (char) (c.Values[0] + 'A')); break;
                case ClueType.NotPresent: Debug.LogFormat("[Puzzword #{0}] Constraint: There is no {1}.", _moduleId, c.Values[0]); break;
                case ClueType.NotPrime: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is not a prime number.", _moduleId, (char) (c.Values[0] + 'A')); break;
                case ClueType.NotSmallest: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} does not have the smallest value.", _moduleId, (char) (c.Values[0] + 'A')); break;
                case ClueType.NotSquare: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is not a square number.", _moduleId, (char) (c.Values[0] + 'A')); break;
                case ClueType.Outside: Debug.LogFormat("[Puzzword #{0}] Constraint: There is a value outside of {1} to {2} (exclusive).", _moduleId, c.Values[0], c.Values[1]); break;
                case ClueType.Prime: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is a prime number.", _moduleId, (char) (c.Values[0] + 'A')); break;
                case ClueType.Product2: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} × {2} = {3}.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), c.Values[2]); break;
                case ClueType.Product3: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} × {2} = {3}.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), (char) (c.Values[2] + 'A')); break;
                case ClueType.Quotient2: Debug.LogFormat("[Puzzword #{0}] Constraint: Of {1} and {2}, one is {3} times the other.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), c.Values[2]); break;
                case ClueType.RightOfPosition: Debug.LogFormat("[Puzzword #{0}] Constraint: There is a {1} further right than {2}.", _moduleId, c.Values[1], (char) (c.Values[0] + 'A')); break;
                case ClueType.Smallest: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} has the smallest value.", _moduleId, (char) (c.Values[0] + 'A')); break;
                case ClueType.Square: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} is a square number.", _moduleId, (char) (c.Values[0] + 'A')); break;
                case ClueType.Sum2: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} + {2} = {3}.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), c.Values[2]); break;
                case ClueType.Sum3: Debug.LogFormat("[Puzzword #{0}] Constraint: {1} + {2} = {3}.", _moduleId, (char) (c.Values[0] + 'A'), (char) (c.Values[1] + 'A'), (char) (c.Values[2] + 'A')); break;
                default: throw new InvalidOperationException(@"What is a " + c.Type);
            }
        }
        Debug.LogFormat(@"[Puzzword #{0}] Solution: {1}", _moduleId, _solution);
        setPage(0);
    }

    private void setPage(int page)
    {
        Audio.PlaySoundAtTransform("blip2", transform);
        WaitMessage.gameObject.SetActive(false);
        if (_solveSubmit != null)
            StopCoroutine(_solveSubmit);
        foreach (var scr in ScreenBacks)
            scr.sharedMaterial = ScreenNormal;
        StatusScreen.sharedMaterial = StatusScreenNormal;
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
        StatusSquare.SetActive(true);
        StatusSquare.transform.localPosition = new Vector3(0, numPages == 1 ? 0 : _curPage == 0 ? .225f : -.225f, 0);
        StatusSquare.transform.localRotation = Quaternion.identity;
        StatusSquare.transform.localScale = new Vector3(.4f, numPages == 1 ? .825f : .375f, 1f);

        for (var screen = 0; screen < 7; screen++)
        {
            foreach (var obj in dyn[screen])
                Destroy(obj);
            if (screen == 0 && _curPage >= wideScreenClues.Count)
                continue;
            if (screen > 0 && 6 * _curPage + screen - 1 >= smallScreenClues.Count)
                continue;
            var clue = screen == 0 ? wideScreenClues[_curPage] : smallScreenClues[6 * _curPage + screen - 1];
            //Debug.LogFormat("<Puzzword #{0}> Screen {1}: {2} ({3})", _moduleId, screen, clue.Type, clue.Values.Join(", "));
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
                display == ConstantDisplay.InsideHorizUp || display == ConstantDisplay.InsideVertRight ? 1 :
                display == ConstantDisplay.InsideHorizDown || display == ConstantDisplay.InsideVertLeft ? -1 :
                display == ConstantDisplay.InsideHoriz || display == ConstantDisplay.InsideVert ? 0 : 2.9f,
                0));

        var base4 = "";
        while (cnst > 0)
        {
            base4 = (cnst % 4) + base4;
            cnst /= 4;
        }
        if (base4.Length < 2 && (display == ConstantDisplay.InsideVert))
            base4 = base4.PadLeft(2, '0');

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
            Debug.LogFormat(@"[Puzzword #{0}] Clue {1} has {2} values instead of expected {3}. Please report this bug to Timwi.", _moduleId, clue.Type, clue.Values.Length, expectedNumValues);
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
            Debug.LogFormat(@"[Puzzword #{0}] Fatal error: no solution!", _moduleId);
            Module.HandlePass();
            throw new InvalidOperationException();
        }

        // Reduce the set of constraints again
        var req = Ut.ReduceRequiredSet(
             constraints.Shuffle(rnd),
             set => judgeConstraints(set.SetToTest) < 2).ToArray();

        if (req.Count(c => c.GetScreenType() == ScreenType.Wide) > 2 || req.Count(c => c.GetScreenType() == ScreenType.Narrow) > 12)
            goto tryAgain;

        _puzzle = req;
        _solution = solutionWord;
        _threadReady = true;
    }

    private int judgeConstraints(IEnumerable<Clue> constraints)
    {
        return new Puzzle(_numLetters, _min, _max, constraints.Select(c => c.Constraint)).Solve().Take(2).Count();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit puzzle [submit the answer PUZZLE] | !{0} toggle [switch to the other screen, if any]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*(toggle|sw|switch|screen|page|flip|right|left|next|prev|previous)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return new List<KMSelectable> { NextButton };
            yield break;
        }

        var m = Regex.Match(command, @"^\s*(?:submit|enter|input|go)\s+([a-z]{6})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return "solve";
        yield return "strike";
        yield return TpButtonsForWord(m.Groups[1].Value.ToUpperInvariant());
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        foreach (var btn in TpButtonsForWord(_solution))
        {
            btn.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    private List<KMSelectable> TpButtonsForWord(string word)
    {
        var btns = new List<KMSelectable> { NextButton };
        var infs = Enumerable.Range(0, 6).Select(i => new { Char = word[i], Index = i }).OrderBy(inf => inf.Char).ToArray();
        var last = 'A';
        for (var i = 0; i < infs.Length; i++)
        {
            btns.AddRange(Enumerable.Repeat(InputButtons[infs[i].Index], infs[i].Char - last + 1));
            last = infs[i].Char;
        }

        return btns;
    }
}
