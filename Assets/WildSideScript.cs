using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class WildSideScript : MonoBehaviour
{
	public KMAudio Audio;
	public KMBombInfo Bomb;
	public KMBombModule Module;
	
	public GameObject[] Buttons;
	public Sprite[] Images;
	public Material[] Colors;
	public MeshRenderer[] HL;
	
	int SolveCount = 0;
	
	//Logging
	static int moduleIdCounter = 1;
	int moduleId;
	private bool ModuleSolved;
	
	List<int> ValidButtons = new List<int>();
	List<int> NumberPressed = new List<int>();
	
	void Awake()
	{
		moduleId = moduleIdCounter++;
		for (int i = 0; i < Buttons.Count(); i++)
		{
			int index = i;
			Buttons[index].GetComponent<KMSelectable>().OnInteract += delegate
			{
				PressButton(index);
				return false;
			};
		}
	}
	
	void Start()
	{
		List<int> ButtonsUsed = new List<int>();
		while (ButtonsUsed.Count() != 16)
		{
			ButtonsUsed = new List<int>();
			ValidButtons = new List<int>();
			int[] StartingCoordinates = {UnityEngine.Random.Range(0,6), UnityEngine.Random.Range(0,6)};
			int Coordinates = UnityEngine.Random.Range(0,2);
			int[] NumbersAdded = Enumerable.Range(0,6).ToArray().Shuffle();
			for (int x = 0; x < 4; x++)
			{
				switch (Coordinates)
				{
					case 0:
						ValidButtons.Add(StartingCoordinates[0]*6 + ((StartingCoordinates[1] + NumbersAdded[x]) % 6));
						ButtonsUsed.Add(StartingCoordinates[0]*6 + ((StartingCoordinates[1] + NumbersAdded[x]) % 6));
						break;
					case 1:
						ValidButtons.Add(((StartingCoordinates[0] + NumbersAdded[x]) % 6)*6 + StartingCoordinates[1]);
						ButtonsUsed.Add(((StartingCoordinates[0] + NumbersAdded[x]) % 6)*6 + StartingCoordinates[1]);
						break;
					default:
						break;
				}
			}
			
			int[] Numbers = Enumerable.Range(0,36).ToArray().Shuffle();
			for (int x = 0; x < Numbers.Length; x++)
			{
				if (new[] {Numbers[x]}.Any(c => ButtonsUsed.Contains(c)))
				{
					continue;
				}
				
				for (int y = 0; y < 2; y++)
				{
					int Count = 0;
					for (int z = 0; z < 6; z++)
					{
						switch (y)
						{
							case 0:
								if (new[] {((Numbers[x]/6)*6 + (((Numbers[x]%6) + z) % 6))}.Any(c => ButtonsUsed.Contains(c)))
								{
									Count++;
								}
								break;
							case 1:
								if (new[] {((((Numbers[x]/6) + z) % 6)*6 + (Numbers[x]%6))}.Any(c => ButtonsUsed.Contains(c)))
								{
									Count++;
								}
								break;
							default:
								break;
						}
					}
					
					if (Count >= 3)
					{
						break;
					}
					
					if (y == 1)
					{
						ButtonsUsed.Add(Numbers[x]);
					}
				}
				
				if (ButtonsUsed.Count() == 16)
				{
					break;
				}
			}
		}
		
		Debug.LogFormat("[Wild Side #{0}] Images shown on each button (in reading order): ", moduleId);
		string Log = "", Log2 = "";
		ButtonsUsed.Shuffle();
		for (int x = 0; x < Buttons.Length; x++)
		{
			Buttons[x].GetComponentInChildren<SpriteRenderer>().sprite = Images[ButtonsUsed[x]];
			Log += x % 4 != 3 ? "Image " + (ButtonsUsed[x] + 1).ToString() + ", " : "Image " + ButtonsUsed[x].ToString();
			if (x % 4 == 3)
			{
				Debug.LogFormat("[Wild Side #{0}] {1}", moduleId, Log);
				Log = "";
			}
		}
		Debug.LogFormat("[Wild Side #{0}] -------------------------------------------------------------", moduleId);
		Debug.LogFormat("[Wild Side #{0}] Correct buttons to press: ", moduleId);
		for (int x = 0; x < 4; x++)
		{
			Log2 += x % 4 != 3 ? "Button " + (Array.IndexOf(ButtonsUsed.ToArray(), ValidButtons[x]) + 1).ToString() + ", " : "Button " + (Array.IndexOf(ButtonsUsed.ToArray(), ValidButtons[x]) + 1).ToString();
		}
		Debug.LogFormat("[Wild Side #{0}] {1}", moduleId, Log2);
		Debug.LogFormat("[Wild Side #{0}] -------------------------------------------------------------", moduleId);
	}
	
	void PressButton(int Press)
	{
		Buttons[Press].GetComponent<KMSelectable>().AddInteractionPunch(0.2f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (!ModuleSolved)
		{
			if (new[] {Press}.Any(c => !NumberPressed.Contains(c)))
			{
				NumberPressed.Add(Press);
				if (new[] {Array.IndexOf(Images, Buttons[Press].GetComponentInChildren<SpriteRenderer>().sprite)}.Any(c => ValidButtons.Contains(c)))
				{
					HL[Press].GetComponentInChildren<MeshRenderer>().material = Colors[0];
					SolveCount++;
					Debug.LogFormat("[Wild Side #{0}] You pressed Button {1}. That was correct.", moduleId, Press+1);
					if (SolveCount == 4)
					{
						Module.HandlePass();
						ModuleSolved = true;
						for (int x = 0; x < HL.Length; x++)
						{
							HL[x].GetComponentInChildren<MeshRenderer>().material = new[] {Array.IndexOf(Images, Buttons[x].GetComponentInChildren<SpriteRenderer>().sprite)}.Any(c => ValidButtons.Contains(c)) ? Colors[0] : Colors[1];
						}
					}
				}
				
				else
				{
					Debug.LogFormat("[Wild Side #{0}] You pressed Button {1}. That was incorrect.", moduleId, Press+1);
					HL[Press].GetComponentInChildren<MeshRenderer>().material = Colors[1];
					Module.HandleStrike();
				}
			}
		}
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To press the button(s) on the module (in reading order), use the command !{0} press [1-16] (This can be performed in a chain)";
    #pragma warning restore 414
	
	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] parameters = command.Split(' ');
		if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			for (int x = 1; x < parameters.Length; x++)
			{
				int Out;
				if (!Int32.TryParse(parameters[x], out Out))
				{
					yield return "sendtochaterror You gave an invalid number. The command was not processed.";
					yield break;
				}
				
				if (Out < 1 || Out > 16)
				{
					yield return "sendtochaterror Number being pressed in not between 1-16. The command was not processed.";
					yield break;
				}
				
				if (new[] {Out - 1}.Any(c => NumberPressed.Contains(c)))
				{
					yield return "sendtochaterror You already press this button number. The command was not processed.";
					yield break;
				}
				Buttons[Out - 1].GetComponent<KMSelectable>().OnInteract();
				yield return new WaitForSecondsRealtime(0.2f);
			}
		}
	}
}
