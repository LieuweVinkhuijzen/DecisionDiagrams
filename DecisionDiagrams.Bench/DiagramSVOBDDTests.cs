using System;
using DecisionDiagrams;
namespace DecisionDiagrams {
    /// <summary>
    /// Tests for SBOBDD.
    /// </summary>
    public class DiagramSVOBDDTests {
        /// <summary>
        /// Run all the tests.
        /// </summary>
        public static void RunTests() {
            Console.WriteLine("Running all tests");
            TestSingleVariable();
        }

        /// <summary>
        /// Test whether a single variable can be set and retrieved.
        /// </summary>
        public static void TestSingleVariable() {
            Console.WriteLine("Hello, Test!");
            DDManager<SingleVarOpDDNode> manager = new DDManager<SingleVarOpDDNode>();
        }
    }
}