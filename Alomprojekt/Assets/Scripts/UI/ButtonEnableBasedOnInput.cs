using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonEnableBasedOnInput : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Button backButton;

    private void Start()
    {
        backButton.interactable = false;

        inputField.onValueChanged.AddListener(CheckInput);
    }

    void CheckInput(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            backButton.interactable = true;
        }
        else
        {
            backButton.interactable = false;
        }
    }
}
