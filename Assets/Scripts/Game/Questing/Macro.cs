// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2019 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: TheLacus
// Contributors:    
// 
// Notes:
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Questing
{
    /// <summary>
    /// A text replacement macro that allows to reuse task chains inside QBN blocks.
    /// </summary>
	/// <remarks>
	/// A macro is a piece of QBN block that is inserted with the following rules:
    /// - Begis with `macro MacroName pattern` and ends with `end macro`.
    /// - `$` placeholder is replaced by the macro "instance" identifier; all tasks inside a macro
    /// must be named `_$_` or `_$.name_` with unique names. No headless entry point.
    /// - Parameters defined as `&amp;param` inside the pattern are replaced by the given values.
    /// - External macros must be imported from the preamble with `import macro Name from FileName.macro.txt`.
    /// - Local macros must be defined in the QBN before they are used.
    /// - Macros can be inserted with `macro name: MacroName pattern` where `name` is unique inside the QBN.
	/// </remarks>
	/// <example>
	/// -- A basic example where a macro is used only once.
	/// macro SayMessages &amp;message1 &amp;message2
	///     _$_ task:
	///         say &amp;message1
	///         say &amp;message2
	///         clear _$_
	/// end macro
	/// 
	/// macro sayMessages: SayMessages 1001 1002
	/// 
	/// _begin_ task:
	///     start task _sayMessages_
	/// </example>
	/// <example>
	/// -- Another example where the macro above is imported from an external file.
	/// import SayMessages from Example.macros.txt
	/// 
	/// QRC:
	/// 
	/// QBN:
	/// 
	/// macro sayMessages: SayMessages 1001 1002
	/// 
	/// _begin_ task:
	///     start task _sayMessages_
	/// </example>
    public class Macro
    {
        #region Types

        private struct InsertedMacro
        {
            public Macro Macro;
            public string[] signature;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Tasks inside a macro must include this symbol in their name.
        /// This placeholder is replaced with an unique id that allows to insert a macro multiple times.
        /// </summary>
        const string instancePlaceholer = "$";

        /// <summary>
        /// Words inside the macro signature with this prefix are parameters.
        /// </summary>
        const string parameterPrefix = "&";

        static readonly char[] definitionSplit = new char[] { ' ' };
        static readonly char[] insertionSplit = new char[] { ' ', ':' };

        string[] signature;
        List<string> content;

        private List<InsertedMacro> InsertedMacros = new List<InsertedMacro>();

        #endregion

        #region  Properties

        public string Name
        {
            get { return signature[1]; }
        }

        public string FullName { get; private set; }

        public bool IsEmpty
        {
            get { return content == null; }
        }

        #endregion

        #region  Constructors

        /// <summary>
        /// Parses a macro whose definition starts at the given line.
        /// </summary>
        /// <param name="lines">A block of lines containing the macro.</param>
        /// <param name="currentLine">A line in the block of lines</param>
        public Macro(List<string> linesIn, ref int currentLine, Dictionary<string, Macro> macros, string parentName = null)
        {
            content = new List<string>();

            signature = linesIn[currentLine].Trim().Split(definitionSplit);
            currentLine++;
            if (signature.Length < 2)
                return;

            //FullName = GetFullName(parentName, Name);

            while (currentLine < linesIn.Count)
            {
                string line = linesIn[currentLine].Trim();

                if (line.StartsWith("macro", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.Contains(":"))
                    {
                        var macroInsertionPattern = line.Split(insertionSplit);
                        if (macroInsertionPattern.Length < 3)
                            return;

                        //string macroFullName = GetFullName(FullName, macroInsertionPattern[3]);
                        Macro insertedMacro;
                        if (!macros.TryGetValue(macroInsertionPattern[3], out insertedMacro) || insertedMacro.content == null)
                        {
                            Debug.LogError("macro not found");
                            return;
                        }

                        InsertedMacros.Add(new InsertedMacro()
                        {
                            Macro = insertedMacro,
                            signature = macroInsertionPattern
                        });
                        currentLine++;
                    }
                    else
                    {
                        var nested = new Macro(linesIn, ref currentLine, macros, null);
                        macros.Add(nested.Name, nested);
                    }

                    continue;
                }

                if (line.StartsWith("end macro", StringComparison.OrdinalIgnoreCase))
                    break;

                content.Add(linesIn[currentLine]);
                currentLine++;
            }
        }

        #endregion

        #region Private Methods
        
        private void Flatten(List<string> linesOut, string[] macroInsertionPattern, string parentName = null)
        {
            if (content == null)
                return;

            string fullName = GetFullName(parentName, macroInsertionPattern[1]);

            foreach (string line in content)
            {
                string contentLine = line.Replace(instancePlaceholer, fullName);
                for (int j = 2; j < signature.Length && j + 2 < macroInsertionPattern.Length; j++)
                {
                    if (signature[j].StartsWith(parameterPrefix))
                        contentLine = contentLine.Replace(signature[j], macroInsertionPattern[j + 2]);
                }

                linesOut.Add(contentLine.Trim());
            }

            foreach (InsertedMacro insertedMacro in InsertedMacros)
                insertedMacro.Macro.Flatten(linesOut, insertedMacro.signature, fullName);
        }

        #endregion

        #region Public Helpers

        /// <summary>
        /// Parses an external macro import and try to retrieve it.
        /// </summary>
        /// <param name="line">The line in the preamble where the macro is imported.</param>
        /// <param name="globalMacros">Cache for macros imported from external resources.</param>
        /// <returns>Imported macro or null if not found.</returns>
        public static Macro Import(string line, Dictionary<string, Macro> globalMacros)
        {
            string[] parts = line.Split(definitionSplit);
            if (parts.Length == 5)
            {
                string macroName = parts[2];
                string relPath = parts[5];
                string fullName = string.Format("{0}.{1}", relPath, macroName);

                Macro macro;
                if (globalMacros.TryGetValue(fullName, out macro))
                    return macro;

                string path = Path.Combine(Path.Combine(Application.streamingAssetsPath, "QuestPacks"), relPath);
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(relPath);
                    string definition = string.Format("macro {0}", macroName);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].TrimStart().StartsWith(definition, StringComparison.OrdinalIgnoreCase))
                        {
                            globalMacros.Add(fullName, macro = new Macro(lines.ToList(), ref i, globalMacros));
                            return macro;
                        }
                    }
                }
            }

            return null;
        }

        public static List<string> ResolveMacro(string line, Dictionary<string, Macro> macros)
        {
            Debug.LogError("resolve macro");
            Macro macro;
            var macroInsertionPattern = line.Split(insertionSplit);
            if (macroInsertionPattern.Length >= 3 && macros.TryGetValue(macroInsertionPattern[3], out macro))
            {
                var lines = new List<string>();
                macro.Flatten(lines, macroInsertionPattern);
                return lines;
            }

            return null;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Handles nested macros with dotted names.
        /// This ensures all names are different when macros are resolved and tasks flattened.
        /// </summary>
        /// <param name="parentName">Name of the parent or null.</param>
        /// <param name="name">Name for current scope.</param>
        /// <returns>A full name.</returns>
        private static string GetFullName(string parentName, string name)
        {
            return parentName == null ? name : string.Format("{0}.{1}", parentName, name);
        }

        #endregion
    }
}