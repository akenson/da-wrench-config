/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Inventor;
using Autodesk.Forge.DesignAutomation.Inventor.Utils;
using System.Collections.Generic;
using Newtonsoft.Json;

using File = System.IO.File;
using Path = System.IO.Path;
using System.IO.Compression;

namespace DaWrenchConfigPlugin
{
    [ComVisible(true)]
    public class SampleAutomation
    {
        private readonly InventorServer inventorApplication;

        public SampleAutomation(InventorServer inventorApp)
        {
            inventorApplication = inventorApp;
        }

        public void Run(Document doc)
        {
            LogTrace("Run called with {0}", doc.DisplayName);
        }

        public void RunWithArguments(Document doc, NameValueMap map)
        {
            LogTrace("Processing " + doc.FullFileName);

            try
            {
                if (doc.DocumentType == DocumentTypeEnum.kPartDocumentObject)
                {
                    LogTrace("We don't yet process parts in this example");
                    using (new HeartBeat())
                    {
                        // TODO: handle the Inventor part here
                    }
                }
                else if (doc.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject) // Assembly.
                {
                    using (new HeartBeat())
                    {
                        LogTrace("We have an assembly file!");

                        // JSON File with params is the first thing passed in
                        string paramFile = (string)map.Value[$"_1"];
                        LogTrace($"Reading param file {paramFile}");
                        string json = File.ReadAllText(paramFile);

                        // Loop through params and set each one
                        Dictionary<string, string> parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                        foreach (KeyValuePair<string, string> entry in parameters)
                        {
                            var paramName = entry.Key;
                            var paramValue = entry.Value;
                            LogTrace($" params: {paramName}, {paramValue}");
                            ChangeParam((AssemblyDocument)doc, paramName, paramValue);
                        }
                    }

                    // Get the full name to use in zipping up the output assembly
                    LogTrace($"Getting full file name of assembly");
                    var docDir = Path.GetDirectoryName(doc.FullFileName);
                    var pathName = doc.FullFileName;
                    doc.Update2(true); ;

                    LogTrace($"Saving updated assembly");
                    doc.Save2(true);
                    doc.Close(true);

                    // Zip up the output assembly
                    //
                    // assembly lives in own folder under WorkingDir. Get the WorkingDir
                    var workingDir = Path.GetDirectoryName(docDir);
                    var fileName = Path.Combine(workingDir, "result.zip"); // the name must be in sync with OutputIam localName in Activity
                    LogTrace($"Zipping up {fileName}");

                    if (File.Exists(fileName)) File.Delete(fileName);

                    // start HeartBeat around ZipFile, it could be a long operation
                    using (new HeartBeat())
                    {
                        ZipFile.CreateFromDirectory(Path.GetDirectoryName(pathName), fileName, CompressionLevel.Fastest, false);
                    }

                    LogTrace($"Saved as {fileName}");
                }
            }
            catch (Exception e)
            {
                LogError("Processing failed. " + e.ToString());
            }
        }

        public void ChangeParam(AssemblyDocument doc, string paramName, string paramValue)
        {
            using (new HeartBeat())
            {
                AssemblyComponentDefinition assemblyComponentDef = doc.ComponentDefinition;
                Parameters docParams = assemblyComponentDef.Parameters;
                UserParameters userParams = docParams.UserParameters;
                try
                {
                    LogTrace($"Setting {paramName} to {paramValue}");
                    UserParameter userParam = userParams[paramName];
                    userParam.Expression = paramValue;
                }
                catch (Exception e)
                {
                    LogError("Cannot update '{0}' parameter. ({1})", paramName, e.Message);
                }
            }
        }

        #region Logging utilities

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string message)
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string message)
        {
            Trace.TraceError(message);
        }

        #endregion
    }
}