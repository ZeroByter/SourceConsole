using SourceConsole.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SourceConsole
{
    public interface ConObject
    {
        int GetParametersLength();
        string GetName();
        string GetDescription();
    }

    public interface IConCommandAttribute
    {
        MethodInfo MethodInfo { get; set; }
    }

    public class ConCommand : Attribute, IConCommandAttribute, ConObject
    {
        public string CustomName { get; private set; }
        /// <summary>
        /// The description rendered next to the command name when calling the List command
        /// </summary>
        public string Description { get; private set; }
        public MethodInfo MethodInfo { get; set; }

        public string GetName()
        {
            if (CustomName == "")
            {
                return MethodInfo.Name.ToLower();
            }
            else
            {
                return CustomName.ToLower();
            }
        }

        public int GetParametersLength()
        {
            return MethodInfo.GetParameters().Length;
        }

        public string GetDescription()
        {
            return Description;
        }

        public ConCommand(string name = "", string description = "")
        {
            CustomName = name;
            Description = description;
        }
    }

    public interface IConVarAttribute
    {
        PropertyInfo PropertyInfo { get; set; }
    }

    public class ConVar : Attribute, IConVarAttribute, ConObject
    {
        public string CustomName { get; private set; }
        public string Description { get; private set; }
        public PropertyInfo PropertyInfo { get; set; }

        public string GetName()
        {
            if (CustomName == "")
            {
                return PropertyInfo.Name.ToLower();
            }
            else
            {
                return CustomName.ToLower();
            }
        }

        public int GetParametersLength()
        {
            if(PropertyInfo.GetSetMethod(false) != null)
            {
                return 1;
            }

            return 0;
        }

        public string GetDescription()
        {
            return Description;
        }

        public ConVar(string customName = "", string description = "")
        {
            CustomName = customName;
            Description = description;
        }
    }

    public static class SourceConsole
    {
        private class DuplicateCommandException : Exception
        {
            public DuplicateCommandException(string message) : base(message) { }
        }

        private static List<string> CommandNamesCache = new List<string>();
        private static List<ConCommand> Commands;
        private static List<ConVar> Convars;

        [ConVar("", "Should the console show the full error stack when an error occurs? Or simply the exception message?")]
        public static bool ShowFullErrorStack
        {
            get; set;
        }

        //Ignoring C# name convention to allow for conveniant and fast typing when needing to debug stuff quickly
        [ConCommand]
        public static void print(object str)
        {
            ConsolePanelController.print(str.ToString());
        }

        //Ignoring C# name convention to allow for conveniant and fast typing when needing to debug stuff quickly
        [ConCommand]
        public static void warn(object str)
        {
            ConsolePanelController.print($"<color=#eda128>{str}</color>");
        }

        //Ignoring C# name convention to allow for conveniant and fast typing when needing to debug stuff quickly
        [ConCommand]
        public static void error(object str)
        {
            ConsolePanelController.print($"<color=#d12323>{str}</color>");
        }

        //Ignoring C# name convention to allow for conveniant and fast typing when needing to debug stuff quickly
        [ConCommand]
        public static void success(object str)
        {
            ConsolePanelController.print($"<color=#0f0>{str}</color>");
        }

        [ConCommand]
        public static void Quit()
        {
            Application.Quit();
        }

        [ConCommand("", "Used to test the console's performance when handling a lot of new lines")]
        public static void Spam(int amount = 100)
        {
            for(int i = 0; i < amount; i++)
            {
                print("Line #" + i);
            }
        }

        public static int RefreshCommands()
        {
            return RefreshCommands(new Assembly[] { Assembly.GetAssembly(typeof(SourceConsole)) });
        }

        public static int RefreshCommands(Assembly[] assemblies)
        {
            float startTime = Time.realtimeSinceStartup;

            CommandNamesCache.Clear();

            Commands = new List<ConCommand>();
            Convars = new List<ConVar>();

            foreach(var command in SourceConsoleHelper.FindMethodAttributes<ConCommand>(assemblies))
            {
                string name = command.GetName();

                if (CommandNamesCache.Contains(name))
                {
                    error($"Failed to load command '{name}'! A command/convar already exists with that name!");
                    throw new DuplicateCommandException($"Failed to load command '{name}'! A command/convar already exists with that name!");
                }

                CommandNamesCache.Add(name);
                Commands.Add(command);
            }
            Commands = Commands.OrderBy(c => c.GetName()).ToList();

            foreach (var convar in SourceConsoleHelper.FindPropertyAttributes<ConVar>(assemblies))
            {
                string name = convar.GetName();

                if (CommandNamesCache.Contains(name))
                {
                    error($"Failed to load convar '{name}'! A command/convar already exists with that name!");
                    throw new DuplicateCommandException($"Failed to load convar '{name}'! A command/convar already exists with that name!");
                }

                CommandNamesCache.Add(name);
                Convars.Add(convar);
            }
            Convars = Convars.OrderBy(c => c.GetName()).ToList();

            print("Refreshed Commands in " + (Time.realtimeSinceStartup - startTime).ToString("0.000") + " seconds");
            return Commands.Count + Convars.Count;
        }

        public static List<ConObject> GetAllConObjectsThatMatch(string input)
        {
            List<ConObject> objects = new List<ConObject>();

            if (input.Trim() == "") return objects;
            input = input.ToLower();

            foreach (var command in Commands)
            {
                string cleanName = command.GetName().Trim();

                if(cleanName.StartsWith(input) || SourceConsoleHelper.LevenshteinDistance(cleanName, input) < 2 && cleanName != input)
                {
                    objects.Add(command);
                }
            }

            foreach (var convar in Convars)
            {
                string cleanName = convar.GetName().Trim();

                if (cleanName.StartsWith(input) || SourceConsoleHelper.LevenshteinDistance(cleanName, input) < 2 && cleanName != input)
                {
                    objects.Add(convar);
                }
            }

            objects = objects.OrderBy(c => c.GetName()).ToList();

            return objects;
        }

        public static ConCommand GetConCommandByName(string name)
        {
            foreach(var command in Commands)
            {
                if(command.GetName().Trim() == name.ToLower())
                {
                    return command;
                }
            }

            return null;
        }

        public static ConVar GetConVarByName(string name)
        {
            foreach (var convar in Convars)
            {
                if (convar.GetName().Trim() == name.ToLower())
                {
                    return convar;
                }
            }

            return null;
        }

        public static ConObject GetConObjectByName(string name)
        {
            ConCommand command = GetConCommandByName(name);
            if (command != null) return command;
            ConVar convar = GetConVarByName(name);
            if (convar != null) return convar;

            return null;
        }

        public static object ExecuteCommand(ConCommand command, params object[] args)
        {
            //This code is for automatically putting in default parameter values if they exist and weren't specified explicitly by the user
            var parameters = command.MethodInfo.GetParameters();
            if (args == null || args.Length < parameters.Length)
            {
                object[] newArgs = new object[parameters.Length];

                for (int i = 0; i < newArgs.Length; i++)
                {
                    //if we have inputted args and current i is less than the length of original user-inputted args
                    if (args != null && i <= args.Length)
                    {
                        //Copy old args to new args array
                        newArgs[i] = args[i];
                    }
                    else //if args is null or we have passed the length of original user-inputted args
                    {
                        if (parameters[i].HasDefaultValue) newArgs[i] = parameters[i].DefaultValue;
                    }
                }
                args = newArgs;
            }

            try
            {
                if (command.MethodInfo.ReturnType == typeof(void))
                {
                    command.MethodInfo.Invoke(null, args);
                }
                else
                {
                    return command.MethodInfo.Invoke(null, args);
                }
            }catch(Exception e)
            {
                if (ShowFullErrorStack)
                {
                    error(e);
                }
                else
                {
                    error($"{e.GetType().Name}: {e.Message}");
                }
            }

            return null;
        }

        public static object ExecuteConvar(ConVar convar, object arg)
        {
            if(arg == null)
            {
                return convar.PropertyInfo.GetMethod.Invoke(null, null);
            }
            else
            {
                //if user has inputted multiple arguments into the console input string, we just use the first one
                object singleArg = null;
                if(arg is object[])
                {
                    if(((object[])arg).Length > 0)
                    {
                        singleArg = ((object[])arg)[0];
                    }
                }
                else
                {
                    singleArg = arg;
                }

                try
                {
                    convar.PropertyInfo.SetValue(null, singleArg);
                }
                catch (Exception e)
                {
                    if (ShowFullErrorStack)
                    {
                        error(e);
                    }
                    else
                    {
                        error($"{e.GetType().Name}: {e.Message}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Given a string, executes it normally as if it were a console-input
        /// </summary>
        /// <param name="input"></param>
        public static void ExecuteString(string input)
        {
            if (input == "") return;

            string[] parts = input.Split(' '); //split the input string into parts
            string commandName = parts[0]; //get just the command name

            var command = GetConObjectByName(commandName);
            if (command != null) //if the command exists
            {
                string[] cleanParts = SourceConsoleHelper.CleanArgumentsArray(parts, command.GetParametersLength());

                if (command is ConCommand)
                {
                    ExecuteCommand((ConCommand)command, SourceConsoleHelper.CastParameters(cleanParts));
                }
                else //if no command, then convar
                {
                    ExecuteConvar((ConVar)command, SourceConsoleHelper.CastParameters(cleanParts));
                }
            }
        }
    }
}
