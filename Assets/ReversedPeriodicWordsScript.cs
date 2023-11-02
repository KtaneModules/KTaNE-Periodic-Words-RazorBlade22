using PeriodicWords;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ReversedPeriodicWordsScript : MonoBehaviour
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

    private KeyCode[] TypableKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M,
        KeyCode.Return, KeyCode.Backspace
    };

    IEnumerable<string> DecomposeString(string sofar, string txt, string remainder)
    {
        if (txt.Length == 0)
            yield return sofar;
        foreach (var elem in Data.Elements)
            if (txt.StartsWith(elem.Key) && elem.Value.ToString("000").StartsWith(remainder))
            {
                var results = DecomposeString(sofar + elem.Value.ToString("000"), txt.Substring(elem.Key.Length), "");
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
            Debug.LogFormat("[Reversed Periodic Words #{0}] You inputted \"{1}\".", _moduleID, InputWord);
            List<string> numbers = DecomposeString("", InputWord, "").ToList();

            if (InputWord == Words[Stage])
            {
                Audio.PlaySoundAtTransform("stage", Buttons[pos].transform);
                Stage++;
                if (Stage == 3)
                {
                    Debug.LogFormat("[Reversed Periodic Words #{0}] The input matches the display. Module Solved.", _moduleID);
                    Module.HandlePass();
                    Texts[0].text = "MODULE SOLVED";
                    Texts[1].text = "";
                    Texts[2].text = "GG";
                    Texts[3].text = "";
                    Solved = true;
                }
                else
                {
                    Debug.LogFormat("[Reversed Periodic Words #{0}] The input matches the display. Progressing the stage.", _moduleID);
                    InputWord = "";
                    Texts[0].text = "";
                    Calculate(Stage);
                    Texts[2].text = (Stage + 1).ToString();
                    Debug.LogFormat("[Reversed Periodic Words #{0}] The displayed numbers for stage {1} are {2}.", _moduleID, Stage + 1, Texts[1].text);
                    Debug.LogFormat("[Reversed Periodic Words #{0}] The word for stage {1} is {2}.", _moduleID, Stage + 1, Words[Stage]);
                }
            }
            else
            {
                Strike("[Reversed Periodic Words #{0}] The input does not match the display. Strike!");
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
        var sols = DecomposeString("", Words[Stage], "").ToList();
        //Debug.Log(sols.Join(", "));
        var nums1 = sols[Rnd.Range(0, sols.Count())];
        //Debug.Log(nums1.Join(", "));
        var nums2 = new List<string>();
        for (int i = 0; i < nums1.Length / 3; i++)
        {
            nums2.Add(nums1.Substring(i * 3, 3));
        }
        Texts[1].text = nums2.Join("").Wrap(27);
        Texts[3].text = nums2.Join("").Replace("\n", "").Select((x, ix) => ((ix / 3) % 2) == 0 ? x : '-').Join("").Wrap(27).Replace("-", " ");
        Debug.LogFormat("[Reversed Periodic Words #{0}] The displayed numbers for stage {1} are {2}.", _moduleID, Stage + 1, Texts[1].text);
        Debug.LogFormat("[Reversed Periodic Words #{0}] The word for stage {1} is {2}.", _moduleID, Stage + 1, Words[Stage]);
    }

    void Strike(string message)
    {
        Debug.LogFormat(message, _moduleID);
        Module.HandleStrike();
        InputWord = "";
        Texts[0].text = "";
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} type <word>' to type the specified word, 'sub' to press the submit button, and 'clr' to press the clear button.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        var keyboard = "qwertyuiopasdfghjklzxcvbnm";
        var commandArray = command.Split(' ');

        if (commandArray.Length == 0 || commandArray.Length > 3)
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        else if (!new[] { "type", "sub", "clr" }.Contains(commandArray[0]))
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        else if (commandArray[0] == "type" && !new[] { 2, 3 }.Contains(commandArray.Length) || commandArray[0] != "type" && commandArray.Length != 1)
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        else if (commandArray[0] == "type" && (commandArray[1].Length > 16 || (commandArray.Length == 3 && commandArray[2] != "sub")))
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }

        yield return null;

        if (commandArray[0] == "sub")
        {
            Buttons[26].OnInteract();
        }
        else if (commandArray[0] == "clr")
        {
            Buttons[27].OnInteract();
        }
        else
        {
            foreach (var letter in commandArray[1])
            {
                Buttons[keyboard.IndexOf(letter)].OnInteract();
                float timer = 0;
                while (timer < 0.1f)
                {
                    yield return "trycancel Reversed Periodic Words: Command cancelled.";
                    timer += Time.deltaTime;
                }
            }
            if (commandArray.Length == 3 && commandArray[2] == "sub")
            {
                Buttons[26].OnInteract();
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
