﻿// <copyright file="RandomCbddTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams.Tests
{
    using System.Diagnostics.CodeAnalysis;
    using DecisionDiagrams;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests based on building random formulas to evaluate.
    /// Includes all the generated tests that caused bugs before.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class RandomCbddTests : RandomTests<CBDDNode>
    {
        /// <summary>
        /// Initialize the test class.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.BaseInitialize();
        }
    }
}
