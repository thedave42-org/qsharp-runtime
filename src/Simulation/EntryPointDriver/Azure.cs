﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.CsharpGeneration.EntryPointDriver
{
    /// <summary>
    /// Provides entry point submission to Azure Quantum.
    /// </summary>
    internal static class Azure
    {
        /// <summary>
        /// Submits the entry point to Azure Quantum.
        /// </summary>
        /// <param name="entryPoint">The entry point.</param>
        /// <param name="parseResult">The command-line parsing result.</param>
        /// <param name="settings">The submission settings.</param>
        /// <typeparam name="TIn">The entry point's argument type.</typeparam>
        /// <typeparam name="TOut">The entry point's return type.</typeparam>
        internal static async Task<int> Submit<TIn, TOut>(
            IEntryPoint<TIn, TOut> entryPoint, ParseResult parseResult, AzureSettings settings)
        {
            var machine = CreateMachine(settings);
            if (machine is null)
            {
                DisplayUnknownTargetError(settings.Target);
                return 1;
            }

            var input = entryPoint.CreateArgument(parseResult);
            if (settings.DryRun)
            {
                var (isValid, message) = machine.Validate(entryPoint.Info, input);
                Console.WriteLine(isValid ? "✔️  The program is valid!" : "❌  The program is invalid.");
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine(message);
                }
                return isValid ? 0 : 1;
            }
            else
            {
                // TODO: Specify the number of shots.
                var job = await machine.SubmitAsync(entryPoint.Info, input);
                DisplayJob(job, settings.Output);
                return 0;
            }
        }

        /// <summary>
        /// Displays the job using the output format.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="format">The output format.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the output format is invalid.</exception>
        private static void DisplayJob(IQuantumMachineJob job, OutputFormat format)
        {
            switch (format)
            {
                case OutputFormat.FriendlyUri:
                    // TODO: Show the friendly URI. The friendly URI is not yet available from the job.
                case OutputFormat.Id:
                    Console.WriteLine(job.Id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid output format '{format}'.");
            }
        }

        /// <summary>
        /// Creates a quantum machine based on the Azure Quantum submission settings.
        /// </summary>
        /// <param name="settings">The Azure Quantum submission settings.</param>
        /// <returns>A quantum machine.</returns>
        private static IQuantumMachine? CreateMachine(AzureSettings settings) =>
            settings.Target == "nothing"
                ? new NothingMachine()
                : QuantumMachineFactory.CreateMachine(settings.CreateWorkspace(), settings.Target, settings.Storage);

        /// <summary>
        /// Displays an error message for attempting to use an unknown target machine.
        /// </summary>
        /// <param name="target">The target machine.</param>
        private static void DisplayUnknownTargetError(string? target)
        {
            var originalForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"The target '{target}' was not recognized.");
            Console.ForegroundColor = originalForeground;
        }
    }

    /// <summary>
    /// The information to show in the output after the job is submitted.
    /// </summary>
    internal enum OutputFormat
    {
        /// <summary>
        /// Show a friendly message with a URI that can be used to see the job results.
        /// </summary>
        FriendlyUri,
        
        /// <summary>
        /// Show only the job ID.
        /// </summary>
        Id
    }
    
    /// <summary>
    /// Settings for a submission to Azure Quantum.
    /// </summary>
    internal sealed class AzureSettings
    {
        /// <summary>
        /// The target device ID.
        /// </summary>
        public string? Target { get; set; }
        
        /// <summary>
        /// The storage account connection string.
        /// </summary>
        public string? Storage { get; set; }
        
        /// <summary>
        /// The subscription ID.
        /// </summary>
        public string? Subscription { get; set; }
        
        /// <summary>
        /// The resource group name.
        /// </summary>
        public string? ResourceGroup { get; set; }
        
        /// <summary>
        /// The workspace name.
        /// </summary>
        public string? Workspace { get; set; }
        
        /// <summary>
        /// The Azure Active Directory authentication token.
        /// </summary>
        public string? AadToken { get; set; }
        
        /// <summary>
        /// The base URI of the Azure Quantum endpoint.
        /// </summary>
        public Uri? BaseUri { get; set; }
        
        /// <summary>
        /// The number of times the program is executed on the target machine.
        /// </summary>
        public int Shots { get; set; }

        /// <summary>
        /// The information to show in the output after the job is submitted.
        /// </summary>
        public OutputFormat Output { get; set; }

        /// <summary>
        /// Validate the program and options, but do not submit to Azure Quantum.
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// Creates a <see cref="Workspace"/> based on the settings.
        /// </summary>
        /// <returns>The <see cref="Workspace"/> based on the settings.</returns>
        internal Workspace CreateWorkspace() =>
            AadToken is null
                ? new Workspace(Subscription, ResourceGroup, Workspace, baseUri: BaseUri)
                : new Workspace(Subscription, ResourceGroup, Workspace, AadToken, BaseUri);
    }
}
