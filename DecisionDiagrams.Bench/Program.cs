// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams.Bench
{
    using System;
    using DecisionDiagrams;

    /// <summary>
    /// Main program for the benchmarks.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            GenerateCNF.generateCNFs(18, 4 * 18, 30);

            // var manager = new DDManager<CBDDNode>();
            // var q = new Queens<CBDDNode>(manager, 13);

            // var timer = System.Diagnostics.Stopwatch.StartNew();
            // q.RunCompile();
            // Console.WriteLine($"Time elapsed: {timer.ElapsedMilliseconds} ms");
        }
    }
}
