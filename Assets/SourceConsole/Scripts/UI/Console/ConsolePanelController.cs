using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0414 //disable warnings about private members not being used
#pragma warning disable CS0649 //disable warnings about private members not being used

namespace SourceConsole.UI
{
    public class ConsolePanelController : MonoBehaviour
    {
        private static ConsolePanelController Singleton;
        private static List<string> StringBuffer = new List<string>();
        /// <summary>
        /// A list of strings that we attempted to print but did it before the console initialized (pre-awake)
        /// </summary>
        private static List<string> PreInitialStrings = new List<string>();

        [Header("Text template")]
        [SerializeField]
        private TMP_Text textOutput;
        [Header("General panel elements")]
        [SerializeField]
        private TMP_InputField consoleInput;
        [SerializeField]
        private ConsoleAutoCompletionManager autoCompletionManager;
        [SerializeField]
        private RectTransform consoleOutputRect;
        [SerializeField]
        private Scrollbar verticalScrollbar;

        private Canvas testCanvas;
        private CanvasScaler canvasScaler;
        private RectTransform rectTransform;
        private Rect rect;

        private bool isDragging = false;
        private bool isResizing = false;

        private Vector2 mouseStartClickPanelPosition;
        private Vector2 mouseStartClickPosition;
        private Vector2 mouseStartClickPanelSize;
        private Vector2 mouseStartClickPanelDifference;

        private bool autoScrollToBottom = true;

        private int commandsHistoryIndex = 0;
        private List<string> commandsHistory = new List<string>();

        //console input text that the user actually typed manually
        private string typedText = "";

        private float textOutputWidth;
        private float textOutputHeight;

        [ConVar("console_showmethodreturns")]
        public static bool ShowMethodReturns { get; set; }
        [ConVar("console_textlimit")]
        public static int TextLimit { get; set; }

        [ConCommand]
        public static void Clear()
        {
            StringBuffer.Clear();

            if (Singleton == null) return;

            foreach (Transform oldTemplate in Singleton.textOutput.transform)
            {
                if (oldTemplate.gameObject.activeSelf)
                {
                    Destroy(oldTemplate);
                }
            }

            Singleton.UpdateVerticalScrollbarSize();
            Singleton.UpdateVisibleText();
        }

        private void Awake()
        {
            if (Singleton == null) Singleton = this;

            rectTransform = GetComponent<RectTransform>();
            rect = rectTransform.rect;

            testCanvas = GetComponentInParent<Canvas>();
            canvasScaler = GetComponentInParent<CanvasScaler>();

            textOutput.lineSpacing = -textOutput.fontSize;

            StartCoroutine(GetStartupSize());
            StartCoroutine(ProcessPreInitialPrints());
        }

        private IEnumerator GetStartupSize()
        {
            yield return new WaitForEndOfFrame();

            var pixelRect = RectTransformUtility.PixelAdjustRect(consoleOutputRect, testCanvas);

            textOutputWidth = pixelRect.width;
            textOutputHeight = pixelRect.height;
        }

        private IEnumerator ProcessPreInitialPrints()
        {
            yield return new WaitUntil(() => textOutputWidth + textOutputHeight > 0);

            foreach(var str in PreInitialStrings)
            {
                print(str);
            }

            PreInitialStrings.Clear();
        }

        public void VerticalScrollRectValueChanged(float newValue)
        {
            if (Input.GetMouseButton(0)) //if we are currently holding down the left mouse button
            {
                //these two if-statements are to check if where we started clicking the left mouse button was inside the area of the vertical scroll bar...
                //kind of a hacky solution, but it's the only one i could come up with that works reliably
                if (mouseStartClickPanelDifference.x > rectTransform.sizeDelta.x - 40.8f && mouseStartClickPanelDifference.x < rectTransform.sizeDelta.x - 20.9f)
                {
                    if (mouseStartClickPanelDifference.y > 39.2f && mouseStartClickPanelDifference.y < rectTransform.sizeDelta.y - 54.6f)
                    {
                        autoScrollToBottom = 1 - newValue < 0.002f;

                        UpdateVisibleText(newValue);
                    }
                }
            }
        }
        
        private void UpdateVisibleText(float newValue = -1)
        {
            if(newValue == -1)
            {
                newValue = verticalScrollbar.value;
            }

            float fontSize = textOutput.fontSize;

            float totalHeight = GetTotalTextHeight() * fontSize;

            newValue = Mathf.Lerp(0, 1 - (textOutputHeight - (textOutput.margin.y + textOutput.margin.w)) / totalHeight, newValue);

            float start = newValue * totalHeight;
            float end = newValue * totalHeight + textOutputHeight - (textOutput.margin.y + textOutput.margin.w);
            
            textOutput.text = "";
            for (int i = 0; i < StringBuffer.Count; i++)
            {
                string str = StringBuffer[i];

                int wrapLines = GetTextOverflowHeight(str);
                float wrapLinesHeight = wrapLines * fontSize;

                float height = i * wrapLinesHeight;
                height += wrapLinesHeight / 2; //center offset

                if (height >= start && height - fontSize / 2 <= end)
                {
                    textOutput.text += str + "\n";
                }
                if(height > end) break;
            }
        }

        private Vector2 GetMouseDifference(Vector2 mousePosition)
        {
            Vector2 mouseDifference = ((Vector2)rectTransform.position - mousePosition) / canvasScaler.transform.localScale.x;
            mouseDifference.x *= -1;

            return mouseDifference;
        }

        private IEnumerator MoveToEndOfInput()
        {
            yield return new WaitForEndOfFrame();

            consoleInput.MoveToEndOfLine(false, false);
        }

        private void LateUpdate()
        {
            if (!ConsoleCanvasController.IsVisible()) return;

            bool upArrow = Input.GetKeyDown(KeyCode.UpArrow);
            bool downArrow = Input.GetKeyDown(KeyCode.DownArrow);

            if (upArrow || downArrow)
            {
                if (typedText == "" && commandsHistory.Count > 0)
                {
                    if (upArrow) commandsHistoryIndex++; //auto get last
                    if (downArrow) commandsHistoryIndex--; //auto get first

                    if (commandsHistoryIndex < 0) commandsHistoryIndex = commandsHistory.Count - 1;
                    if (commandsHistoryIndex >= commandsHistory.Count) commandsHistoryIndex = 0;

                    consoleInput.text = commandsHistory[commandsHistoryIndex];
                    StartCoroutine(MoveToEndOfInput());
                }
                else
                {
                    string autocomplete = autoCompletionManager.GetAutoCompleteString();

                    if (autocomplete != "") consoleInput.text = autocomplete + " ";
                    StartCoroutine(MoveToEndOfInput());
                }
            }

            #region Handling dragging and resizing console window
            if (Input.GetMouseButtonDown(0))
            {
                mouseStartClickPanelPosition = transform.position;
                mouseStartClickPosition = Input.mousePosition;
                mouseStartClickPanelSize = rectTransform.sizeDelta;
                mouseStartClickPanelDifference = GetMouseDifference(mouseStartClickPosition);

                if (mouseStartClickPanelDifference.y < 34)
                {
                    isDragging = true;
                }

                if (mouseStartClickPanelDifference.x > rectTransform.sizeDelta.x - 18 &&
                        mouseStartClickPanelDifference.y > rectTransform.sizeDelta.y - 18)
                {
                    isResizing = true;
                }
            }
            else
            {
                if (isDragging) isDragging = Input.GetMouseButton(0);
                if (isResizing) isResizing = Input.GetMouseButton(0);
            }

            if (isDragging)
            {
                Vector2 movePosition = mouseStartClickPanelPosition + ((Vector2)Input.mousePosition - mouseStartClickPosition); ;

                if (movePosition.x < 0) movePosition.x = 0;
                if (movePosition.x / canvasScaler.transform.localScale.x > Screen.width / canvasScaler.transform.localScale.x - rectTransform.sizeDelta.x) movePosition.x = (Screen.width / canvasScaler.transform.localScale.x - rectTransform.sizeDelta.x) * canvasScaler.transform.localScale.x;
                if (movePosition.y > Screen.height) movePosition.y = Screen.height;
                if (movePosition.y / canvasScaler.transform.localScale.x < rectTransform.sizeDelta.y) movePosition.y = rectTransform.sizeDelta.y * canvasScaler.transform.localScale.x;

                transform.position = movePosition;
            }

            if (isResizing)
            {
                Vector2 currentMousePosition = Input.mousePosition;

                if(currentMousePosition.x > Screen.width) currentMousePosition.x = Screen.width;
                if(currentMousePosition.y < 6) currentMousePosition.y = 6;

                float newWidth = Mathf.Max(400, mouseStartClickPanelSize.x + (currentMousePosition.x - mouseStartClickPosition.x) / canvasScaler.transform.localScale.x);
                float newHeight = Mathf.Max(300, mouseStartClickPanelSize.y + (currentMousePosition.y - mouseStartClickPosition.y) / -canvasScaler.transform.localScale.x);

                rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

                var pixelRect = RectTransformUtility.PixelAdjustRect(consoleOutputRect, testCanvas);

                textOutputWidth = pixelRect.width;
                textOutputHeight = pixelRect.height;

                UpdateVerticalScrollbarSize();
                UpdateVisibleText();
            }
            #endregion
        }

        private void CreateNewTemplate(string text)
        {
            var newTemplate = Instantiate(textOutput, textOutput.transform.parent);
            newTemplate.text = text;
            newTemplate.gameObject.SetActive(true);
        }

        private void PrintToConsole(string text)
        {
            if (TextLimit > 0 && StringBuffer.Count > TextLimit)
            {
                StringBuffer.RemoveAt(0);
            }

            foreach (var line in text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                StringBuffer.Add(line);
            }

            UpdateVerticalScrollbarSize();

            if (autoScrollToBottom)
            {
                verticalScrollbar.value = 1f;
                LayoutRebuilder.ForceRebuildLayoutImmediate(consoleOutputRect);
            }

            UpdateVisibleText();
        }

        private void UpdateVerticalScrollbarSize()
        {
            float totalTextHeight = GetTotalTextHeight() * textOutput.fontSize;
            if (totalTextHeight >= 1)
            {
                verticalScrollbar.size = textOutputHeight / totalTextHeight;
            }
            else
            {
                verticalScrollbar.size = 1;
            }
        }

        public new static void print(object str)
        {
            if (Singleton == null || Singleton.textOutputHeight + Singleton.textOutputWidth == 0)
            {
                PreInitialStrings.Add(str.ToString());
                return;
            }

            Singleton.PrintToConsole(str.ToString());
        }

        /// <summary>
        /// Called when the console input was changed
        /// </summary>
        /// <param name="input"></param>
        public void CommandInputEdited(string input)
        {
            //fix tidle randomly appearing while trying to open/close console
            if (input == "`")
            {
                consoleInput.text = "";
                input = "";
            }

            if (!Input.GetKeyDown(KeyCode.DownArrow) && !Input.GetKeyDown(KeyCode.UpArrow))
            {
                typedText = input;

                autoCompletionManager.ShowAutoCompletionTips(input.Trim());
            }
        }

        public void CommandEntered(string input)
        {
            if (input == "") return;
            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;

            //reset text and select console input
            consoleInput.text = "";
            consoleInput.OnSelect(null);
            ResetCommandHistoryIndex();

            string[] parts = input.Split(' '); //split the input string into parts
            string commandName = parts[0]; //get just the command name

            //insert command history (regardless of if the command is valid or not)
            if (commandsHistory.Count == 0) //if no command history. add anyways
            {
                commandsHistory.Add(input);
            }
            else
            {
                if (commandsHistory[commandsHistory.Count - 1] != input) commandsHistory.Add(input); //add only if last command isnt identical
            }
            if (commandsHistory.Count > 6) commandsHistory.RemoveAt(0); //make sure history list is not too big

            var command = SourceConsole.GetConObjectByName(commandName); //get either the convar or concommand typed
            if (command != null) //if the command exists
            {
                string[] cleanParts = SourceConsoleHelper.CleanArgumentsArray(parts, command.GetParametersLength());

                SourceConsole.print($"<color=#ffffffaa>{input}</color>");

                if (command is ConCommand)
                {
                    //If you want to display the return value of the function...
                    if (ShowMethodReturns)
                    {
                        object commandResult = SourceConsole.ExecuteCommand((ConCommand)command, SourceConsoleHelper.CastParameters(cleanParts));
                        if (commandResult != null)
                        {
                            print($"commandName = {commandResult}");
                        }
                    }
                    else
                    {
                        SourceConsole.ExecuteCommand((ConCommand)command, SourceConsoleHelper.CastParameters(cleanParts));
                    }
                }
                else //if no command, then convar
                {
                    object result = SourceConsole.ExecuteConvar((ConVar)command, SourceConsoleHelper.CastParameters(cleanParts));
                    if (result != null)
                    {
                        if (command.GetDescription() == "")
                        {
                            SourceConsole.print($"<color=#ffffffaa>{commandName} = {result}</color>");
                        }
                        else
                        {
                            SourceConsole.print($"<color=#ffffffaa>{commandName} = {result}\n\"{command.GetDescription()}\"</color>");
                        }
                    }
                }
            }
            else
            {
                SourceConsole.print($"Command '{commandName}' does not exist!");
            }
        }

        public void ResetCommandHistoryIndex()
        {
            commandsHistoryIndex = commandsHistory.Count;
        }

        private int GetTextOverflowHeight(string text)
        {
            //0.485 is the *approximate* width of a single charachter based off the normal fontSize
            return Mathf.CeilToInt(text.Length * (textOutput.fontSize * 0.485f) / textOutputWidth);
        }

        //test: print "this is a very long line lmao mate this is a very long line lmao mate this is a very long line lmao mate"
        private float GetTextBlockHeight(string text)
        {
            int overflowHeight = GetTextOverflowHeight(text);
            if(overflowHeight >= 1) return overflowHeight;

            return 1;
        }

        private float GetTotalTextHeight()
        {
            float height = 0;

            foreach(var str in StringBuffer)
            {
                height += GetTextBlockHeight(str);
            }

            return height;
        }
    }
}