using PeriodicWords;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class PeriodicWordsScript : MonoBehaviour
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
    private string Input = "";
    private bool Active, Solved;
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
        Texts[0].text = "PERIODIC WDS.";
        Texts[1].text = "";
        Texts[2].text = "0";
        for (int i = 0; i < Buttons.Length; i++)
        {
            int x = i;
            Buttons[x].OnInteract += delegate { if (Active && !Solved) ButtonPress(x); return false; };
        }
        Module.OnActivate += delegate { Active = true; Calculate(); Texts[2].text = "1"; };
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

        if (pos < 10 && (Input == null ? "" : Input).Length < 55)
        {
            Input = (Input + Buttons[pos].GetComponentInChildren<TextMesh>().text).Wrap(27);
            Texts[1].text = Input;
        }
        else if (pos == 10)
        {
            Debug.LogFormat("[Periodic Words #{0}] You inputted \"{1}\".", _moduleID, Input.Replace("\n", "").Select((x, index) => (index % 3 == 2 ? x + " " : x.ToString())).Join("").Trim());
            List<int> numbers = Input.Replace("\n", "").Select((x, index) => (index % 3 == 2 ? x + " " : x.ToString())).Join("").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(y => int.Parse(y)).ToList();

            //Input is not nothing and is the correct format
            if (Input != null && Input.Replace("\n", "").Length % 3 == 0)
            {
                //Number is not found in the Periodic Table
                if (numbers.Where(x => x > 118).Count() > 0 || numbers.Contains(0))
                {
                    Strike("[Periodic Words #{0}] At least one of the inputted atomic numbers was not found on the periodic table. Strike!");
                }
                //Numbers are found in the Periodic Table
                else
                {
                    Debug.LogFormat("[Periodic Words #{0}] The atomic numbers spell out the word \"{1}\".", _moduleID, numbers.Select(x => Data.Elements.FirstOrDefault(y => y.Value == x).Key.ToUpper()).Join(""));
                    //Input matches the display
                    if (numbers.Select(x => Data.Elements.FirstOrDefault(y => y.Value == x).Key.ToUpper()).Join("") == Texts[0].text)
                    {
                        Audio.PlaySoundAtTransform("stage", Buttons[pos].transform);
                        Stage++;
                        if (Stage == 3)
                        {
                            Debug.LogFormat("[Periodic Words #{0}] The input matches the display. Module Solved.", _moduleID);
                            Module.HandlePass();
                            Texts[0].text = "MODULE SOLVED";
                            Texts[1].text = "";
                            Texts[2].text = "GG";
                            Solved = true;
                        }
                        else
                        {
                            Debug.LogFormat("[Periodic Words #{0}] The input matches the display. Progressing the stage.", _moduleID);
                            Input = "";
                            Texts[0].text = Words[Stage];
                            Texts[1].text = "";
                            Texts[2].text = (Stage + 1).ToString();
                            Debug.LogFormat("[Periodic Words #{0}] The displayed word for stage {1} is {2}.", _moduleID, Stage + 1, Words[Stage]);
                        }
                    }
                    //Input does not match the display
                    else
                    {
                        Strike("[Periodic Words #{0}] The input does not match the display. Strike!");
                    }
                }
            }
            //Input is nothing or in the wrong format
            else
            {
                Strike("[Periodic Words #{0}] At least one of the inputted numbers was not formatted correctly. Strike!");
            }
        }
        else if (pos == 11)
        {
            Input = "";
            Texts[1].text = "";
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

    void Calculate()
    {
        Texts[0].text = Words[0];
        Debug.LogFormat("[Periodic Words #{0}] The displayed word for stage 1 is {1}.", _moduleID, Words[0]);
    }

    void Strike(string message)
    {
        Debug.LogFormat(message, _moduleID);
        Module.HandleStrike();
        Input = "";
        Texts[1].text = "";
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} 0123456789' to press the specified numbered buttons, 's' to press the submit button, 'c' to press the clear button.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        var validCommands = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 's', 'c' };

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
            Buttons[Array.IndexOf(validCommands, command[i])].OnInteract();

            float timer = 0;

            while (timer < 0.1f)
            {
                yield return "trycancel Periodic Words: Command cancelled.";
                timer += Time.deltaTime;
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        while (!Solved)
        {
            if (Input != "")
            {
                Buttons[11].OnInteract();
                yield return true;
            }

            string digits = DecomposeString(Input.Substring(0, (Input.Length / 3) * 3)
                , Words[Stage].Substring(Input.Length / 3),
                Input.Substring((Input.Length / 3) * 3, Input.Length % 3)).Last();

            foreach (char digit in digits)
            {
                if (digit == '-')
                {
                    Buttons[11].OnInteract();
                    yield return true;
                }
                else
                {
                    var charNumbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };


                    Buttons[Array.IndexOf(charNumbers, digit)].OnInteract();
                    yield return true;
                }
            }

            Buttons[10].OnInteract();
            yield return true;
        }


    }
}
