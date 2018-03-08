using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Blood : MonoBehaviour
{

	public GameObject host;
	public int maxValue;
	public int currValue;

	public Text textReborn;
	public Text textInfo;

	void Start ()
	{
		Text[] texts = GetComponentsInChildren<Text> ();
		foreach (Text text in texts) {
			if (text.gameObject.name =="TextReborn") {
				textReborn = text;
			} else if (text.gameObject.name == "TextInfo") {
				textInfo = text;
			}
		}
	}

	void OnGUI ()
	{

	}

	void Update()
	{
		transform.position = host.transform.position + new Vector3 (0, 3.0f, 0);

		Slider slider = GetComponentInChildren<Slider> ();
		slider.maxValue = maxValue;
		slider.value = currValue;
	}

}
