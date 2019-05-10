using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0414 //disable warnings about private members not being used
#pragma warning disable CS0649 //disable warnings about private members not being used

namespace SourceConsole.UI
{
    public class ConsoleAutoCompletionController : MonoBehaviour
    {
        private Image image;
        [SerializeField]
        private TMP_Text commandName;
        [SerializeField]
        private TMP_Text commandDescription;

        private ConObject command;

        public void Setup(ConObject command)
        {
            if(command == null)
            {
                Destroy(gameObject);
                return;
            }

            this.command = command;

            image = GetComponent<Image>();

            Deselect();

            string parametersString = "";
            if(command is ConCommand)
            {
                foreach (var param in ((ConCommand)command).MethodInfo.GetParameters())
                {
                    if (param.HasDefaultValue)
                    {
                        parametersString += $"<{param.Name} : {param.ParameterType.Name} = {param.DefaultValue}> ";
                    }
                    else
                    {
                        parametersString += $"<{param.Name} : {param.ParameterType.Name}> ";
                    }
                }
            }
            else
            {
                parametersString += $": {((ConVar)command).PropertyInfo.PropertyType.Name}";
            }

            commandName.text = $"{command.GetName()} {parametersString}";
            commandDescription.text = command.GetDescription();

            gameObject.SetActive(true);
        }

        public void Select()
        {
            commandName.color = new Color(0, 0, 0, 1);
            commandDescription.color = commandName.color;

            image.color = new Color(1, 1, 1, 0.75f);
        }

        public void Deselect()
        {
            commandName.color = new Color(1, 1, 1, 1);
            commandDescription.color = commandName.color;

            image.color = new Color(1, 1, 1, 0);
        }

        public ConObject GetObject()
        {
            return command;
        }
    }
}