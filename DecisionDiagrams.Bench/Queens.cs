// <copyright file="Queens.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DecisionDiagrams.Bench
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using DecisionDiagrams;

    /// <summary>
    /// Benchmark for the n queens problem.
    /// </summary>
    public class Queens<T> where T : IDDNode, IEquatable<T>
    {
        private DDManager<T> manager;

        private int boardSize;

        private DD[,] boardConstraints;

        private VarBool<T>[,] variables;

        private DD problemEncoding;

        /// <summary>
        /// Creates a new instance of the <see cref="Queens{T}"/> class.
        /// Satcount when n=12:  14200.
        /// Satcount when n=11:  4921.
        /// </summary>
        /// <param name="manager">The manager object.</param>
        /// <param name="boardSize">The size of the board.</param>
        public Queens(DDManager<T> manager, int boardSize)
        {
            this.manager = manager;
            this.problemEncoding = manager.True();
            this.boardSize = boardSize;
            this.boardConstraints = new DD[boardSize, boardSize];
            this.variables = new VarBool<T>[boardSize, boardSize];

            for (int i = 0; i < this.boardSize; i++)
            {
                for (int j = 0; j < this.boardSize; j++)
                {
                    this.variables[i, j] = manager.CreateBool();
                    this.boardConstraints[i, j] = this.variables[i, j].Id();
                }
            }
        }

        /// <summary>
        /// Run the benchmark.
        /// </summary>
        public void Run()
        {
            PlaceQueenInEachRow();

            Console.WriteLine($"After placing queen in each row, DD contains {this.manager.NodeCount(problemEncoding)} nodes.");

            for (int i = 0; i < this.boardSize; i++)
            {
                for (int j = 0; j < this.boardSize; j++)
                {
                    // System.Console.WriteLine(GC.GetTotalMemory(true) / 1000 / 1000);
                    Console.WriteLine($"Adding position {i}, {j}  -- currently at {this.manager.NodeCount(problemEncoding)} nodes.");
                    Build(i, j);
                }
            }

            Console.WriteLine($"\n // The number of ways to put {this.boardSize} queens on the board");
            Console.WriteLine($"Satcount: {this.manager.SatCount(problemEncoding)}");

            /* var assignment = this.manager.Sat(this.problemEncoding);

            for (int i = 0; i < this.boardSize; i++)
            {
                for (int j = 0; j < this.boardSize; j++)
                {
                    Console.WriteLine($"[{i}, {j}] = {assignment.Get(this.variables[i, j])}");
                }
            } */
        }

        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void RunCompile() {
            Compiler<T> compiler = new (manager);
            // Add the constraints to the compiler
            PlaceQueenInEachRowAddConstraints(compiler);
            for (int i = 0; i < boardSize; i++) {
                for (int j = 0; j < boardSize; j++) {
                    addQueenConstraints(compiler, i, j);
                }
            }
            // compiler.compilationStrategy = Compiler<T>.CompilationStrategy.linear;
            compiler.compilationStrategy = Compiler<T>.CompilationStrategy.fractalCompilation;
            DD queens = compiler.Compile();
            Console.WriteLine($"\n // The number of ways to put {this.boardSize} queens on the board");
            Console.WriteLine($"Satcount: {this.manager.SatCount(queens)}");
        }

        /// <summary>
        /// Adds diagonal constraints for position i, j.
        /// </summary>
        /// <param name="i">The row.</param>
        /// <param name="j">The column.</param>
        private void Build(int i, int j)
        {
            DD a = manager.True();
            DD b = manager.True();
            DD c = manager.True();
            DD d = manager.True();

            // no other queens in same column.
            for (int l = 0; l < this.boardSize; l++)
            {
                if (l != j)
                {
                    a = manager.And(a, manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[i, l])));
                }
            }

            // no other queens in same row.
            for (int k = 0; k < this.boardSize; k++)
            {
                if (k != i)
                {
                    b = manager.And(b, manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[k, j])));
                }
            }

            // no other queens in same up right diagonal
            for (int k = 0; k < this.boardSize; k++)
            {
                int ll = k - i + j;
                if (ll >= 0 && ll < this.boardSize)
                {
                    if (k != i)
                    {
                        c = manager.And(c, manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[k, ll])));
                    }
                }
            }

            // no other queens in same down right diagonal
            for (int k = 0; k < this.boardSize; k++)
            {
                int ll = i + j - k;
                if (ll >= 0 && ll < this.boardSize)
                {
                    if (k != i)
                    {
                        d = manager.And(d, manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[k, ll])));
                    }
                }
            }

            this.problemEncoding = manager.And(this.problemEncoding, manager.And(a, manager.And(b, manager.And(c, d))));
        }

        private void addQueenConstraints(Compiler<T> compiler, int i, int j) {
            // no other queens in the same column
            for (int l = 0; l < this.boardSize; l++) {
                if (l != j) {
                    compiler.addConstraint(manager.Implies(boardConstraints[i, j], manager.Not(boardConstraints[i, l])));
                }
            }

            // no other queens in the same row
            for (int k = 0; k < this.boardSize; k++) {
                if (k != i) {
                    compiler.addConstraint(manager.Implies(boardConstraints[i, j], manager.Not(boardConstraints[k, j])));
                }
            }

            // no other queens in the same up right diagonal
            for (int k = 0; k < this.boardSize; k++) {
                int ll = k - i + j;
                if (ll >= 0 && ll < this.boardSize) {
                    if (k != i) {
                        compiler.addConstraint(manager.Implies(boardConstraints[i, j], manager.Not(boardConstraints[k, ll])));
                    }
                }
            }

            // no other queens in the same down right diagonal
            for (int k = 0; k < this.boardSize; k++)
            {
                int ll = i + j - k;
                if (ll >= 0 && ll < this.boardSize) {
                    if (k != i) {
                        compiler.addConstraint(manager.Implies(this.boardConstraints[i, j], manager.Not(this.boardConstraints[k, ll])));
                    }
                }
            }
        }

        /// <summary>
        /// Place a queen in each row.
        /// More precisely, adds to this.problemEncoding the constraint that in each row, there is at least one queen
        /// This is a conjunction of disjunctions.
        /// </summary>
        private void PlaceQueenInEachRow()
        {
            for (int i = 0; i < this.boardSize; i++)
            {
                DD e = manager.False();
                for (int j = 0; j < this.boardSize; j++)
                {
                    e = manager.Or(e, this.boardConstraints[i, j]);
                }

                this.problemEncoding = manager.And(this.problemEncoding, e);
            }
        }

        private void PlaceQueenInEachRowAddConstraints(Compiler<T> compiler) {
            DD row;
            for (int i = 0; i < this.boardSize; i++) {
                row = manager.False();
                for (int j = 0; j < this.boardSize; j++) {
                    row = manager.Or(row, this.boardConstraints[i, j]);
                }
                compiler.addConstraint(row);
            }
        }
    }
}
