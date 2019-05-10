using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SourceConsole
{
    public static class SourceConsoleHelper
    {
        public static bool CastParameter(string parameter, out object result)
        {
            //Bool
            bool parsedBool;
            bool parsed = bool.TryParse(parameter, out parsedBool);
            if (parsed)
            {
                result = parsedBool;
                return true;
            }

            //Int
            int parsedInt;
            parsed = int.TryParse(parameter, out parsedInt);
            if (parsed)
            {
                result = parsedInt;
                return true;
            }

            //Float
            float parsedFloat;
            parsed = float.TryParse(parameter, out parsedFloat);
            if (parsed)
            {
                result = parsedFloat;
                return true;
            }

            //String
            result = parameter;
            return false;
        }

        public static object[] CastParameters(string[] parameters)
        {
            if (parameters.Length == 0)
            {
                return null;
            }

            object[] result = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                CastParameter(parameters[i], out result[i]);
            }
            return result;
        }

        public static AttributeType[] FindMethodAttributes<AttributeType>(Assembly[] assemblies, bool staticOnly = true) where AttributeType : Attribute, IConCommandAttribute
        {
            List<AttributeType> result = new List<AttributeType>();

            for (int current = 0; current < assemblies.Length; current++)
            {
                MethodInfo[] methods = assemblies[current].GetTypes()
                  .SelectMany(t => t.GetMethods())
                  .Where(m => m.GetCustomAttributes(typeof(AttributeType), true).Length > 0)
                  .ToArray();

                for (int i = 0; i < methods.Length; i++)
                {
                    if (staticOnly && !methods[i].IsStatic)
                    {
                        continue;
                    }

                    AttributeType attribute = ExtractAttribute<AttributeType>(methods[i]);
                    attribute.MethodInfo = methods[i];
                    result.Add(attribute);
                }
            }

            return result.ToArray();
        }

        public static AttributeType[] FindPropertyAttributes<AttributeType>(Assembly[] assemblies) where AttributeType : Attribute, IConVarAttribute
        {
            List<AttributeType> result = new List<AttributeType>();

            for (int current = 0; current < assemblies.Length; current++)
            {
                PropertyInfo[] properties = assemblies[current].GetTypes()
                  .SelectMany(t => t.GetProperties())
                  .Where(m => m.GetCustomAttributes(typeof(AttributeType), true).Length > 0)
                  .ToArray();

                for (int i = 0; i < properties.Length; i++)
                {
                    AttributeType attribute = ExtractAttribute<AttributeType>(properties[i]);
                    attribute.PropertyInfo = properties[i];
                    result.Add(attribute);
                }
            }

            return result.ToArray();
        }

        public static T ExtractAttribute<T>(MethodInfo method) where T : Attribute
        {
            object[] attributes = method.GetCustomAttributes(typeof(T), true);
            for (int a = 0; a < attributes.Length; a++)
            {
                if (attributes[a].GetType() == typeof(T))
                {
                    return (T)attributes[a];
                }
            }
            return default(T);
        }

        public static T ExtractAttribute<T>(PropertyInfo method) where T : Attribute
        {
            object[] attributes = method.GetCustomAttributes(typeof(T), true);
            for (int a = 0; a < attributes.Length; a++)
            {
                if (attributes[a].GetType() == typeof(T))
                {
                    return (T)attributes[a];
                }
            }
            return default(T);
        }

        public static string[] GetArgumentPartsOnly(string[] parts)
        {
            if (parts.Length <= 1) return new string[0]; //if no args

            string[] argParts = new string[parts.Length - 1];

            for (int i = 1; i < parts.Length; i++)
            {
                argParts[i - 1] = parts[i];
            }

            return argParts;
        }

        /// <summary>
        /// removes the first element (the command name), and splits string by space unless it's in quotes and returns the result array
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        public static string[] CleanArgumentsArray(string[] parts, int maxParameters)
        {
            string str = string.Join(" ", GetArgumentPartsOnly(parts));

            var newPartsArray = str.Split('"')
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : new string[] { element })  // Keep the entire item
                     .SelectMany(element => element).ToList();

            if (newPartsArray.Count > maxParameters)
            {
                newPartsArray.RemoveRange(newPartsArray.Count - (newPartsArray.Count - maxParameters), newPartsArray.Count - maxParameters);
            }

            return newPartsArray.ToArray();
        }

        //Taken from/credit to: https://www.csharpstar.com/csharp-string-distance-algorithm/
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
