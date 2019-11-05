using System.Collections.Generic;
using System.IO;
using Autodesk.Forge.DesignAutomation.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Interaction
{
    /// <summary>
    /// Customizable part of Publisher class.
    /// </summary>
    internal partial class Publisher
    {
        /// <summary>
        /// Constants.
        /// </summary>
        private static class Constants
        {
            private const int EngineVersion = 24;
            public static readonly string Engine = $"Autodesk.Inventor+{EngineVersion}";

            public const string Description = "PUT DESCRIPTION HERE";

            internal static class Bundle
            {
                public static readonly string Id = "DaWrenchConfig";
                public const string Label = "alpha";

                public static readonly AppBundle Definition = new AppBundle
                {
                    Engine = Engine,
                    Id = Id,
                    Description = Description
                };
            }

            internal static class Activity
            {
                public static readonly string Id = Bundle.Id;
                public const string Label = Bundle.Label;
            }

            internal static class Parameters
            {
                public const string InventorDoc = nameof(InventorDoc);
                public const string DocumentParams = nameof(DocumentParams);
                public const string OutputZip = nameof(OutputZip);
            }
        }


        /// <summary>
        /// Get command line for activity.
        /// </summary>
        private static List<string> GetActivityCommandLine()
        {
            return new List<string> { $"$(engine.path)\\InventorCoreConsole.exe /al $(appbundles[{Constants.Activity.Id}].path) $(args[{Constants.Parameters.DocumentParams}].path) /i $(args[{Constants.Parameters.InventorDoc}].path)" };
        }

        /// <summary>
        /// Get activity parameters.
        /// </summary>
        private static Dictionary<string, Parameter> GetActivityParams()
        {
            return new Dictionary<string, Parameter>
                    {
                        {
                            Constants.Parameters.InventorDoc,
                            new Parameter
                            {
                                Verb = Verb.Get,
                                Zip = true,
                                LocalName = "Wrench",
                                Description = "Assembly Zip"
                            }
                        },
                        {
                        Constants.Parameters.DocumentParams,
                            new Parameter
                            {
                                Verb = Verb.Get,
                                Description = "Json file containing User Parameters",
                                LocalName = "documentParams.json"
                            }
                        },
                        {
                            Constants.Parameters.OutputZip,
                            new Parameter
                            {
                                Verb = Verb.Put,
                                LocalName = "result.zip",
                                Description = "Resulting assembly"
                            }
                        }
                    };
        }

        /// <summary>
        /// Get arguments for workitem.
        /// </summary>
        private static Dictionary<string, IArgument> GetWorkItemArgs(string bucketKey, string inputName, string paramFile, string outputName, string token)
        {
            string jsonPath = paramFile;
            JObject inputJson = JObject.Parse(File.ReadAllText(jsonPath));
            string inputJsonStr = inputJson.ToString(Newtonsoft.Json.Formatting.None);

            // TODO: update the URLs below with real values
            return new Dictionary<string, IArgument>
            {
                {
                    Constants.Parameters.InventorDoc,
                    new XrefTreeArgument
                    {
                        Verb = Verb.Get,
                        PathInZip = "Wrench.iam",
                        Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketKey, inputName),
                        Headers = new Dictionary<string, string>()
                        {
                            { "Authorization", "Bearer " + token }
                        }
                    }
                },
                {
                    Constants.Parameters.DocumentParams,
                    new XrefTreeArgument
                    {
                        Verb = Verb.Get,
                        Url = "data:application/json, " + inputJsonStr
                    }
                },
                {
                    Constants.Parameters.OutputZip,
                    new XrefTreeArgument
                    {
                        Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketKey, outputName),
                        Verb = Verb.Put,
                        Headers = new Dictionary<string, string>()
                        {
                            { "Authorization", "Bearer " + token }
                        }
                    }
                }
            };
        }
    }
}
