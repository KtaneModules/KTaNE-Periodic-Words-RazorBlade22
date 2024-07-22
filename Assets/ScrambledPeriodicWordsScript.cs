using PeriodicWords;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ScrambledPeriodicWordsScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public TextMesh[] Texts;

    private Coroutine[] ButtonAnims;
    private int Stage;
    private float StartPos;
    private string[] Words;
    private string InputWord = "";
    private bool Active, Focused, Solved;
    private List<int> DisplayNumbersSorted = new List<int>();

    private KeyCode[] TypableKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M,
        KeyCode.Return, KeyCode.Backspace
    };

    IEnumerable<List<int>> DecomposeString(List<int> sofar, string txt)
    {
        if (txt.Length == 0)
            yield return sofar;
        foreach (var elem in Data.Elements)
            if (txt.StartsWith(elem.Key))
            {
                var nextSofar = sofar.ToList();
                nextSofar.Add(elem.Value);
                var results = DecomposeString(nextSofar, txt.Substring(elem.Key.Length));
                foreach (var sol in results)
                    yield return sol;
            }
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        Words = Data.WordList.ToArray().Shuffle();

        ButtonAnims = new Coroutine[Buttons.Length];
        StartPos = Buttons[0].transform.localPosition.y;
        Texts[0].text = "PERIODIC WORDS";
        Texts[1].text = "";
        Texts[2].text = "0";
        Texts[3].text = "";
        for (int i = 0; i < Buttons.Length; i++)
        {
            int x = i;
            Buttons[x].OnInteract += delegate { if (Active && !Solved) ButtonPress(x); return false; };
        }
        Module.OnActivate += delegate { Active = true; Calculate(0); Texts[2].text = "1"; Texts[0].text = ""; };
        if (Application.isEditor)
            Focused = true;
        Module.GetComponent<KMSelectable>().OnFocus += delegate { Focused = true; };
        Module.GetComponent<KMSelectable>().OnDefocus += delegate { Focused = false; };
    }

    void Update()
    {
        for (int i = 0; i < TypableKeys.Count(); i++)
        {
            if (Input.GetKeyDown(TypableKeys[i]) && Focused)
            {
                Buttons[i].OnInteract();
            }
        }
    }

    bool CompareSolutions(List<int> first, List<int> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }
        for (int i = 0; i < first.Count; i++)
        {
            if (first[i] != second[i])
            {
                return false;
            }
        }
        return true;
    }

    void ButtonPress(int pos)
    {
        Audio.PlaySoundAtTransform("press", Buttons[pos].transform);
        try
        {
            StopCoroutine(ButtonAnims[pos]);
        }
        catch { }
        ButtonAnims[pos] = StartCoroutine(ButtonAnim(pos));

        if (pos < 26 && (InputWord == null ? "" : InputWord).Length < 16)
        {
            InputWord = (InputWord + Buttons[pos].GetComponentInChildren<TextMesh>().text);
            Texts[0].text = InputWord;
        }
        else if (pos == 26)
        {
            Debug.LogFormat("[Scrambled Periodic Words #{0}] You inputted \"{1}\".", _moduleID, InputWord);
            var numbers = DecomposeString(new List<int>(), InputWord).ToList();
            var sortedSolutions = new List<List<int>>();

            foreach (var sol in numbers)
            {
                sol.Sort();
                sortedSolutions.Add(sol);
            }

            bool isValid = false;

            for (int i = 0; i < sortedSolutions.Count; i++)
            {

                if (CompareSolutions(sortedSolutions[i], DisplayNumbersSorted.ToList()))
                {
                    isValid = true;
                    goto endLoop;
                }
            }

        endLoop:

            if (!isValid)
            {
                Strike("[Scrambled Periodic Words #{0}] The inputted word cannot be made from the displayed atomic numbers. Strike!");
            }
            else if (!Words.Contains(InputWord))
            {
                Strike("[Scrambled Periodic Words #{0}] The inputted word is not a word. Strike!");
                Debug.LogFormat("[Scambled Periodic Words #{0}] If you feel this strike is an error, please contact RazorBlade on Telegram, @RazorBlade_Wolfe.", _moduleID);
                Debug.LogFormat("[Scambled Periodic Words #{0}] Or GhostSalt on Discord, ghostsalt12", _moduleID);
            }
            else
            {
                Audio.PlaySoundAtTransform("stage", Buttons[pos].transform);
                Stage++;
                if (Stage == 3)
                {
                    Debug.LogFormat("[Scambled Periodic Words #{0}] The input was valid. Module Solved.", _moduleID);
                    Module.HandlePass();
                    Texts[0].text = "MODULE SOLVED";
                    Texts[1].text = "";
                    Texts[2].text = "GG";
                    Texts[3].text = "";
                    Solved = true;
                }
                else
                {
                    Debug.LogFormat("[Scrambled Periodic Words #{0}] The input was valid. Progressing the stage.", _moduleID);
                    InputWord = "";
                    Texts[0].text = "";
                    Calculate(Stage);
                    Texts[2].text = (Stage + 1).ToString();
                }
            }

        }

        else if (pos == 27)
        {
            InputWord = "";
            Texts[0].text = "";
        }
    }

    private IEnumerator ButtonAnim(int pos, float duration = 0.075f, float end = 0.012f)
    {
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[pos].transform.localPosition = Vector3.Lerp(new Vector3(Buttons[pos].transform.localPosition.x, StartPos, Buttons[pos].transform.localPosition.z), new Vector3(Buttons[pos].transform.localPosition.x, end, Buttons[pos].transform.localPosition.z), timer * (1 / duration));
        }
        timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[pos].transform.localPosition = Vector3.Lerp(new Vector3(Buttons[pos].transform.localPosition.x, end, Buttons[pos].transform.localPosition.z), new Vector3(Buttons[pos].transform.localPosition.x, StartPos, Buttons[pos].transform.localPosition.z), timer * (1 / duration));
        }
        Buttons[pos].transform.localPosition = new Vector3(Buttons[pos].transform.localPosition.x, StartPos, Buttons[pos].transform.localPosition.z);
    }

    void Calculate(int stage)
    {
        var sols = DecomposeString(new List<int>(), Words[Stage]).ToList();
        //Debug.Log(sols.Join(", "));
        var nums1 = sols[Rnd.Range(0, sols.Count())];
        DisplayNumbersSorted = nums1.ToList();
        DisplayNumbersSorted.Sort();
        //Debug.Log(nums1.Join(", "));
        var nums2 = new List<string>();
        for (int i = 0; i < nums1.Count; i++)
        {
            nums2.Add(nums1[i].ToString("000"));
        }
        nums2.Shuffle();
        Texts[1].text = nums2.Join("").Wrap(27);
        Texts[3].text = nums2.Join("").Replace("\n", "").Select((x, ix) => ((ix / 3) % 2) == 0 ? x : '-').Join("").Wrap(27).Replace("-", " ");
        Debug.LogFormat("[Scrambled Periodic Words #{0}] The displayed numbers for stage {1} are {2}.", _moduleID, Stage + 1, Texts[1].text);
        Debug.LogFormat("[Scrambled Periodic Words #{0}] A possible word for stage {1} is {2}.", _moduleID, Stage + 1, Words[Stage]);
    }

    void Strike(string message)
    {
        Debug.LogFormat(message, _moduleID);
        Module.HandleStrike();
        InputWord = "";
        Texts[0].text = "";
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} WORD' to type specified word, '^' to press the submit button, '<' to press the clear button.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        var validCommands = "qwertyuiopasdfghjklzxcvbnm^<";

        for (int i = 0; i < command.Length; i++)
        {
            if (!validCommands.Contains(command[i]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        }

        yield return null;

        for (int i = 0; i < command.Length; i++)
        {
            Buttons[validCommands.IndexOf(command[i])].OnInteract();

            float timer = 0;

            while (timer < 0.1f)
            {
                yield return "trycancel Scrambled Periodic Words: Command cancelled.";
                timer += Time.deltaTime;
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        while (!Solved)
        {
            if (InputWord != "")
            {
                Buttons[27].OnInteract();
                yield return true;
            }

            foreach (char letter in Words[Stage])
            {
                Buttons["QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(letter)].OnInteract();
                yield return true;
            }

            Buttons[26].OnInteract();
            yield return true;
        }


    }
}
