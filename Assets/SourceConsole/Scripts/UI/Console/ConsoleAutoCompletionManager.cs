using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0414 //disable warnings about private members not being used
#pragma warning disable CS0649 //disable warnings about private members not being used

namespace SourceConsole.UI
{
    public class ConsoleAutoCompletionManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject topMoreCommands;
        [SerializeField]
        private Transform template;
        [SerializeField]
        private GameObject bottomMoreCommands;
        [SerializeField]
        private Image backgroundImage;

        private RectTransform templateParentRect;

        private int selectedAutoCompleteIndex = -1;
        private int verticalScrollIndex = 0;
        private int matchingConObjectsCount;

        private int showMaxAutocompletes = 5;

        private string typedInputString;

        private List<ConsoleAutoCompletionController> currentControllers = new List<ConsoleAutoCompletionController>();

        private void Awake()
        {
            topMoreCommands.SetActive(false);
            template.gameObject.SetActive(false);
            bottomMoreCommands.SetActive(false);

            templateParentRect = template.parent.GetComponent<RectTransform>();

            backgroundImage.color = new Color(0, 0, 0, 0);
        }

        public string GetAutoCompleteString()
        {
            if (currentControllers.Count > 0)
            {
                if (selectedAutoCompleteIndex >= 0 && selectedAutoCompleteIndex < currentControllers.Count)
                {
                    return currentControllers[selectedAutoCompleteIndex].GetObject().GetName();
                }
            }

            return "";
        }

        private void Update()
        {
            if (currentControllers.Count > 0)
            {
                bool upArrow = Input.GetKeyDown(KeyCode.UpArrow);
                bool downArrow = Input.GetKeyDown(KeyCode.DownArrow);

                if (selectedAutoCompleteIndex == -1)
                {
                    if (upArrow || downArrow)
                    {
                        selectedAutoCompleteIndex = 0;
                    }
                }
                else
                {
                    if (upArrow) selectedAutoCompleteIndex--;
                    if (downArrow) selectedAutoCompleteIndex++;

                    if (selectedAutoCompleteIndex < 0)
                    {
                        if (topMoreCommands.activeSelf && upArrow)
                        {
                            verticalScrollIndex--;
                            ShowAutoCompletionTips(typedInputString);
                        }

                        selectedAutoCompleteIndex = 0;
                    }
                    if (selectedAutoCompleteIndex >= currentControllers.Count)
                    {
                        if (bottomMoreCommands.activeSelf && downArrow)
                        {
                            verticalScrollIndex++;
                            ShowAutoCompletionTips(typedInputString);
                        }

                        selectedAutoCompleteIndex = currentControllers.Count - 1;
                    }
                }

                if (upArrow || downArrow)
                {
                    UpdateSelectedAutoComplete();
                }
            }
        }

        private void ClearTemplates()
        {
            currentControllers.Clear();

            foreach (Transform oldTemplate in template.parent)
            {
                if (oldTemplate.gameObject.activeSelf && oldTemplate.gameObject != topMoreCommands && oldTemplate.gameObject != bottomMoreCommands)
                {
                    Destroy(oldTemplate.gameObject);
                }
            }
        }

        private void AddTemplate(ConObject command)
        {
            ConsoleAutoCompletionController controller = Instantiate(template, template.parent).GetComponent<ConsoleAutoCompletionController>();

            currentControllers.Add(controller);

            topMoreCommands.transform.SetAsFirstSibling();
            bottomMoreCommands.transform.SetAsLastSibling();

            controller.Setup(command);
        }

        private void Show()
        {
            backgroundImage.color = new Color(0.5843137f, 0.5843137f, 0.5843137f, 0.9f);
        }

        private void Hide()
        {
            selectedAutoCompleteIndex = -1;
            verticalScrollIndex = 0;
            backgroundImage.color = new Color(0, 0, 0, 0);
        }

        public void ShowAutoCompletionTips(string typedInput)
        {
            typedInputString = typedInput;

            ClearTemplates();

            List<ConObject> matchingConObjects = SourceConsole.GetAllConObjectsThatMatch(typedInputString);

            matchingConObjectsCount = matchingConObjects.Count;
            if (matchingConObjects.Count > 0)
            {
                for (int i = verticalScrollIndex; i < Mathf.Min(matchingConObjects.Count, verticalScrollIndex + showMaxAutocompletes); i++)
                {
                    var command = matchingConObjects[i];

                    if (command is ConCommand)
                    {
                        AddTemplate((ConCommand)command);
                    }
                    else
                    {
                        AddTemplate((ConVar)command);
                    }
                }

                Show();
                UpdateSelectedAutoComplete();
            }
            else
            {
                Hide();
            }
        }

        private void UpdateSelectedAutoComplete()
        {
            topMoreCommands.SetActive(verticalScrollIndex != 0);
            bottomMoreCommands.SetActive(matchingConObjectsCount > currentControllers.Count && verticalScrollIndex < matchingConObjectsCount - showMaxAutocompletes);

            for (int i = 0; i < currentControllers.Count; i++)
            {
                if (i == selectedAutoCompleteIndex)
                {
                    currentControllers[i].Select();
                }
                else
                {
                    currentControllers[i].Deselect();
                }
            }
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(templateParentRect);
        }
    }
}