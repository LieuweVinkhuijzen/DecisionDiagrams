namespace DecisionDiagrams {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    // <summary>
    // compile a set of constraints, given as DDs
    // <typeparam name="T"> the type of decision diagram node (BDD, CBDD, etc.)
    // </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Compiler<T> where T : IDDNode, IEquatable<T> {
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        private HashSet<DD> constraints;
        private DDManager<T> manager;

        public CompilationStrategy compilationStrategy;

        public enum CompilationStrategy {
            linear,
            random,
            smallestFirst,
            fractalCompilation,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Compiler{T}"/> class.
        /// </summary>
        public Compiler(DDManager<T> manager) {
            this.manager = manager;
            this.constraints = null;
            constraints = new HashSet<DD>();
            compilationStrategy = CompilationStrategy.linear;
        }

        /// <summary>
        /// adds a constraint to the problem.
        /// </summary>
        public void addConstraint(DD constraint) {
            constraints.Add(constraint);
        }

        public DD Compile() {
            switch (this.compilationStrategy) {
                case CompilationStrategy.linear: return CompileLinear();
                case CompilationStrategy.smallestFirst: return CompileSmallestFirst();
                case CompilationStrategy.fractalCompilation: return CompileFractal();
                default: return manager.False();
            }
        }

        /// <summary>
        ///  Compile the set of constraints.
        /// </summary>
        public DD CompileLinear() {
            Console.WriteLine("Compiling with strategy 'linear'");
            // Heuristically, repeatedly conjoin the two smallest DDs, until only one DD is left
            DD conjunction = manager.True();
            int nProcessedConstraints = 0;
            foreach (DD constraint in constraints) {
                // Console.WriteLine($"Compiling constraint {nProcessedConstraints + 1}; size = {manager.NodeCount(conjunction)}");
                conjunction = manager.And(conjunction, constraint);
                nProcessedConstraints++;
            }
            return conjunction;
        }

        public DD getSmallest(Dictionary<DD, int> dds) {
            KeyValuePair<DD, int> smallest = new KeyValuePair<DD, int>(null, -1);
            foreach (KeyValuePair<DD, int> keyValue in dds) {
                if (smallest.Value == -1) {
                    smallest = keyValue;
                } else {
                    if (keyValue.Value < smallest.Value) {
                        smallest = keyValue;
                    }
                }
            }
            return smallest.Key;
        }

        public DD CompileSmallestFirst() {
            Console.WriteLine("Compiling with strategy 'smallest first'");
            if (constraints.Count == 0)
                return manager.True();
            if (constraints.Count == 1)
                foreach (DD constraint in constraints)
                    return constraint;
            List<KeyValuePair<DD, int>> unProcessedConstraints = new List<KeyValuePair<DD, int>>();
            int size;
            foreach (DD constraint in constraints) {
                size = manager.NodeCount(constraint);
                unProcessedConstraints.Add(new KeyValuePair<DD, int>(constraint, size));
            }
            // Conjoin all the BDDs in the set
            while (unProcessedConstraints.Count > 1) {
                Console.WriteLine($"There are {unProcessedConstraints.Count} constraints left.");
                // Sort the DDs descending
                unProcessedConstraints.Sort((x, y) => x.Value.CompareTo(y.Value));
                for (int i = 0; i < unProcessedConstraints.Count; i++) {
                    // Console.Write($"{unProcessedConstraints[i].Value}, ");
                }
                // Console.WriteLine();
                // pick the two smallest DDs in the set
                KeyValuePair<DD, int> small0 = unProcessedConstraints[0];
                KeyValuePair<DD, int> small1 = unProcessedConstraints[1];
                Console.WriteLine($"Conjoining BDDs of size {small0.Value} and {small1.Value}...");
                DD conjunction = manager.And(small0.Key, small1.Key);
                // Console.WriteLine("Succes!");
                int conjunctionSize = manager.NodeCount(conjunction);
                // Console.WriteLine($"The resulting BDD has size {conjunctionSize}");
                unProcessedConstraints.Remove(small0);
                unProcessedConstraints.Remove(small1);
                unProcessedConstraints.Add(new KeyValuePair<DD, int>(conjunction, conjunctionSize));
            }
            return manager.False();
        }

        private DD CompileFractal() {
            Console.WriteLine("Compiling with strategy fractal.");
            List<DD> constr = constraints.ToList();
            if (constr.Count % 2 == 1) {
                constr[0] = manager.And(constr[0], constr[constr.Count - 1]);
            }
            for (int jump = 1; jump < constr.Count; jump *= 2) {
                for (int i = 0; i + jump < constr.Count; i += jump) {
                    constr[i] = manager.And(constr[i], constr[i + jump]);
                }
            }
            return constr[0];
        }
    }
}