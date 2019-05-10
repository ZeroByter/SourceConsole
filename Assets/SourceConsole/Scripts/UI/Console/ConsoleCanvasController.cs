using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable CS0414 //disable warnings about private members not being used
#pragma warning disable CS0649 //disable warnings about private members not being used

namespace SourceConsole.UI
{
    public class ConsoleCanvasController : MonoBehaviour
    {
        private static ConsoleCanvasController Singleton;

        /// <summary>
        /// You can reference this in your own scripts to decide if to pause the game, block player movement, or whatever
        /// </summary>
        /// <returns></returns>
        public static bool IsVisible()
        {
            return Singleton.isVisible;
        }
        
        [SerializeField]
        private TMP_InputField consoleInput;
        [SerializeField]
        private ConsolePanelController consolePanelController;

        private Canvas canvas;
        private GraphicRaycaster graphicRaycaster;

        private int commandsHistoryIndex = 0;
        private List<string> commandsHistory = new List<string>();
        
        private bool isVisible;

        private void Awake()
        {
            if (Singleton != null)
            {
                Destroy(gameObject);
                return;
            }

            Singleton = this;
            DontDestroyOnLoad(gameObject);

            canvas = GetComponent<Canvas>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();

            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void SceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (isVisible)
            {
                StartCoroutine(SelectConsoleInputWithDelay());
            }
        }

        private void Start()
        {
            SourceConsole.RefreshCommands();
            
            Hide();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote)) Toggle();
        }

        private IEnumerator SelectConsoleInputWithDelay()
        {
            yield return new WaitForFixedUpdate();

            consoleInput.OnSelect(null);
        }

        private bool AllowOpen()
        {
            /*
             TODO: Insert your own custom logic for deciding if the console can be opened.
             For example, you may choose to only allow the console to open if the current player is a developer, or something.
             
             Example:
             return DeveloperController.IsDeveloper(SteamUser.GetSteamID());
             */

            return true;
        }

        public void Toggle()
        {
            if (isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            if (!AllowOpen()) return;

            consolePanelController.ResetCommandHistoryIndex();

            isVisible = true;

            canvas.enabled = isVisible;
            graphicRaycaster.enabled = isVisible;

            StartCoroutine(SelectConsoleInputWithDelay());

            consoleInput.text = "";
        }

        public void Hide()
        {
            isVisible = false;

            canvas.enabled = isVisible;
            graphicRaycaster.enabled = isVisible;

            consoleInput.OnDeselect(null);
        }
    }
}