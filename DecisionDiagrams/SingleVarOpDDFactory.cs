// <copyright file="DDManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq.Expressions;

namespace DecisionDiagrams
{
    #pragma warning disable SA1501 // Statement should not be on a single line
    class SingleVarOpDDFactory : IDDNodeFactory<SingleVarOpDDNode>
    {
        public DDManager<SingleVarOpDDNode> Manager;

        public long MaxVariables;

        private static readonly int unset = -1; // the value we assign to a variable that has not been set to 0 or 1
        public SingleVarOpDDFactory()
        {
            //
            MaxVariables = 128;
            Manager = new DDManager<SingleVarOpDDNode>();
        }

        DDManager<SingleVarOpDDNode> IDDNodeFactory<SingleVarOpDDNode>.Manager { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        long IDDNodeFactory<SingleVarOpDDNode>.MaxVariables { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        /// <summary>
        /// Follow an edge to a node, applying the negation bit to its children.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns>a node.</returns>.
        private SingleVarOpDDNode FollowEdge(DDIndex edge) {
            SingleVarOpDDNode node = this.Manager.getNodeFromIndex(edge);
            if (edge.IsComplemented()) {
                node.Low = node.Low.Flip();
                node.High = node.High.Flip();
            }
            return node;
        }
        // Suppose you have two nodes for different variables, and they are OR and AND, so
        // a = x2 * a'   b = x3 | b'
        // a*b = (x2*a') * (x3 | b') = x2*x3*a' | x2*a'*b'
        // Can a node have both b = x3 | b' and b = x2 * b''? No, that can't happen, by Ashenhurst's Theorem.
        // Short proof just for good form:
        // If b implies x2, and is implied by x3, consider the point (x2=0, x3=1). Then we have x3 implies b, so b=1. But b implies x2, and x2=0, so b=0. QED.
        // Conclusion: Always just switch on the highest-index variable
        // a = x2 * a'   b = x3 + b'
        // a*b = x2*a'*x3 + x2*a'*b' = !x3*x2*a'*b' | x3*x2*a'*!b'.

        static List<int> FixedValues = new List<int>();

        /// <summary>
        /// Computes the conjunction of a and b; specifically, constructs a DDIndex representing a AND b
        /// TODO use a cache
        /// Note: this procedure does NOT use the Manager. Instead, it takes on the responsibility of cache itself.
        /// </summary>
        /// <param name="aEdge"></param>
        /// <param name="bEdge"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public DDIndex And(DDIndex aEdge, DDIndex bEdge) {
            if (aEdge.IsComplemented() || bEdge.IsComplemented()) {
                Console.WriteLine("Unsupported: complemented edges.");
            }
            SingleVarOpDDNode a = FollowEdge(aEdge);
            SingleVarOpDDNode b = FollowEdge(bEdge);
            DDIndex resultLow, resultHigh;
            int resultVariable;
            SingleVarOpDDNode resultNode;
            // Base cases
            if (aEdge.IsZero()) return DDIndex.False;
            if (bEdge.IsZero()) return DDIndex.False;
            if (aEdge.IsOne()) return bEdge;
            if (bEdge.IsOne()) return aEdge;
            if (aEdge.Equals(bEdge)) return aEdge;
            if (aEdge.isComplementOf(bEdge)) return DDIndex.False;
            if (a.Variable == b.Variable) {
                Debug.Assert(FixedValues[a.Variable] == unset, "ERR2: fixed value of {a.Variable} to {FixedValues[a.Variable]} before branching on both node.");
                resultLow = And(a.Low, b.Low);
                resultHigh = And(a.High, b.High);
                resultVariable = a.Variable;
            }
            else if (a.IsVariableAnd() || b.IsVariableAnd()) {
                // get the highest implied node
                SingleVarOpDDNode activeNode  = a.Variable > b.Variable ? a : b;
                SingleVarOpDDNode passiveNode = a.Variable > b.Variable ? b : a;
                DDIndex passiveEdge           = a.Variable > b.Variable ? bEdge : aEdge;
                Debug.Assert(activeNode.Variable > passiveNode.Variable, "ERR1: active variable < passive.variable");
                Debug.Assert(activeNode.IsVariableAnd(), "ERROR 3: active node is not a Variable And, i.e., is not of the form x AND f");
                resultVariable = activeNode.Variable;
                if (activeNode.IsPositiveVariableAnd()) {
                        resultLow = DDIndex.False;
                        FixedValues[resultVariable] = 1;
                        resultHigh = this.And(activeNode.High, passiveEdge);
                        FixedValues[resultVariable] = unset;
                } else {
                    Debug.Assert(activeNode.IsNegativeVariableAnd(), "ERROR 4: active node is not negative variable AND");
                    resultHigh = DDIndex.False;
                    FixedValues[resultVariable] = 0;
                    resultLow = this.And(activeNode.Low, passiveEdge);
                    FixedValues[resultVariable] = unset;
                }
            } else {
                // Both nodes are Shannon nodes
                DDIndex activeEdge   = (a.Variable > b.Variable) ? aEdge : bEdge;
                DDIndex passiveEdge  = (a.Variable > b.Variable) ? bEdge : aEdge;
                SingleVarOpDDNode activeNode, passiveNode;
                activeNode  = (a.Variable > b.Variable) ? a : b;
                passiveNode = (a.Variable > b.Variable) ? b : a;
                int targetVariable = activeNode.Variable;
                resultVariable = targetVariable;
                if (FixedValues[targetVariable] == unset) {
                    resultLow  = And(activeNode.Low,  passiveEdge);
                    resultHigh = And(activeNode.High, passiveEdge);
                } else if (FixedValues[targetVariable] == 0) {
                    resultLow = DDIndex.False;
                    resultHigh = And(activeNode.High, passiveEdge);
                } else { // is 1
                    resultLow  = And(activeNode.Low,  passiveEdge);
                    resultHigh = DDIndex.False;
                }
                // if (a.Variable < b.Variable)  // OLD CODE - keep for reference for a while
                // {
                //     if (FixedValues[b.Variable] == unset) {
                //         resultLow  = this.Manager.Apply(a.Low, bEdge, DDOperation.And);
                //         resultHigh = this.Manager.Apply(a.High, bEdge, DDOperation.And);
                //         return this.Manager.Allocate(new SingleVarOpDDNode(a.Variable, xlow, xhigh));
                //     } else if (FixedValues[b.Variable] == 0) {
                //         resultLow  = DDIndex.False;
                //         resultHigh = And(aEdge, b.High);
                //     } else { // is 1
                //         resultLow  = And(aEdge, b.Low);
                //         resultHigh = DDIndex.False;
                //     }
                //     return this.Manager.Allocate(new SingleVarOpDDNode(b.Variable, resultLow, resultHigh));
                // }
                // else if (b.Variable < a.Variable)
                // {
                //     var ylow = this.Manager.Apply(b.Low, aEdge, DDOperation.And);
                //     var yhigh = this.Manager.Apply(b.High, aEdge, DDOperation.And);
                //     return this.Manager.Allocate(new SingleVarOpDDNode(b.Variable, ylow, yhigh));
                // }
                // else
                // {
                //     var low = this.Manager.Apply(a.Low, b.Low, DDOperation.And);
                //     var high = this.Manager.Apply(a.High, b.High, DDOperation.And);
                //     return this.Manager.Allocate(new SingleVarOpDDNode(a.Variable, low, high));
                // }
            }
            resultNode = new SingleVarOpDDNode(resultVariable, resultLow, resultHigh);
            DDIndex resultEdge;
            ReduceAndAllocate(resultNode, out resultEdge);
            return resultEdge;
        }

        public DDIndex Or(DDIndex a, DDIndex b) {
            DDIndex notA = a.Flip();
            DDIndex notB = b.Flip();
            DDIndex notAandNotB = And(notA, notB);
            return notAandNotB.Flip();
        }

        public DDIndex Apply(DDIndex xid, SingleVarOpDDNode x, DDIndex yid, SingleVarOpDDNode y, DDOperation operation)
        {
            for (int i = FixedValues.Count; i < this.Manager.NumVariables; i++) {
                FixedValues.Add(-1);
            }
            if (operation == DDOperation.And)
                return And(xid, yid);
            throw new System.NotImplementedException();
        }

        public string Display(SingleVarOpDDNode node, bool negated)
        {
            throw new System.NotImplementedException();
        }

        public DDIndex Exists(DDIndex xid, SingleVarOpDDNode x, VariableSet<SingleVarOpDDNode> variables)
        {
            throw new System.NotImplementedException();
        }

        public SingleVarOpDDNode Flip(SingleVarOpDDNode node)
        {
            throw new System.NotImplementedException();
        }

        public SingleVarOpDDNode Id(int variable)
        {
            throw new System.NotImplementedException();
        }

        public DDIndex Ite(DDIndex fid, SingleVarOpDDNode f, DDIndex gid, SingleVarOpDDNode g, DDIndex hid, SingleVarOpDDNode h)
        {
            throw new System.NotImplementedException();
        }

        public int Level(DDIndex idx, SingleVarOpDDNode node)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Makes sure that if f implies x, then x is one of the first variables that is branched on.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="result"></param>
        /// <returns>if a reduction occurred.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool ReduceAndAllocate(SingleVarOpDDNode node, out DDIndex result)
        {
            // if node.low = x AND g   and   node.high = x AND then node should imply x
            bool flipped = false;
            if (node.Low.Equals(node.High)) {
                result = node.Low;
                return false;
            }
            if (node.Low.Equals(DDIndex.True)) {
                node.Low = node.Low.Flip();
                node.High = node.High.Flip();
                flipped = true;
            }
            result = this.Manager.Allocate(node);
            if (flipped) {
                result.Flip();
            }
            // TODO
            SingleVarOpDDNode lowNode = this.Manager.getNodeFromIndex(node.Low);
            SingleVarOpDDNode highNode = this.Manager.getNodeFromIndex(node.High);
            if (lowNode.IsPositiveVariableAnd() && highNode.IsPositiveVariableAnd() && lowNode.Variable == highNode.Variable) {
                // TODO pull it up
            }
            return false;
        }

        public DDIndex Replace(DDIndex xid, SingleVarOpDDNode x, VariableMap<SingleVarOpDDNode> variableMap)
        {
            throw new System.NotImplementedException();
        }

        public void Sat(SingleVarOpDDNode node, bool hi, Dictionary<int, bool> assignment)
        {
            throw new System.NotImplementedException();
        }

        public double SatCount(SingleVarOpDDNode node)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// For a given function which implies literals  f ==> x * !y * z,
        /// adds the variables x, -y, z to the set
        /// TODO does not take into account complement bits.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="impliedLiterals"></param>
        public void addImpliedLiteralsToSet(SingleVarOpDDNode v, HashSet<int> impliedLiterals) {
            SingleVarOpDDNode u = v;
            bool complement = false;

            while (u.IsVariableAnd()) {
                if (u.IsPositiveVariableAnd()) {
                    if (complement)
                        impliedLiterals.Add(-u.Variable);
                    else
                        impliedLiterals.Add(u.Variable);
                    if (u.High.IsComplemented())
                        complement ^= true;
                } else {
                    if (complement)
                        impliedLiterals.Add(-u.Variable);
                    else
                        impliedLiterals.Add(u.Variable);
                    if (u.Low.IsComplemented())
                        complement ^= true;
                }
                u = getNonConstantChild(u);
            }
        }
        
        /// <summary>
        /// Returns the first encountered non-trivial descendant in a walk from this node to the leaf
        /// A descendant is trivial if its function is non-constant, and is not of the form f(x,y) = x * g(y)
        /// TODO should we return a node, or an index?.
        /// </summary>
        /// <returns></returns>
        public SingleVarOpDDNode getFirstNontrivialDescendant(SingleVarOpDDNode v) {
            while (!v.IsNontrivialFunction()) {
                v = getNonConstantChild(v);
            }
            return v;
        }

        public SingleVarOpDDNode getNonConstantChild(SingleVarOpDDNode v) {
            if (v.Low.IsConstant())
                return this.Manager.getNodeFromIndex(v.High);
            return this.Manager.getNodeFromIndex(v.Low);
        }

        /// <summary>
        /// Traverses the DD to find out whether the function depends on the variable x.
        /// TODO can be optimized by taking variable order into account
        /// TODO can be optimized by checking in advance whether low, high == Constant
        ///    (this will result in fewer function calls).
        /// </summary>
        /// <param name="v"></param>
        /// <param name="x"></param>
        /// <returns>Whether [v] depends on x.</returns>
        public bool DependsOn(DDIndex v, int x) {
            SingleVarOpDDNode node = Manager.getNodeFromIndex(v);
            if (node.Visited == true)
                return false;
            node.Visited = true;
            if (node.Variable == x)
                return true;
            if (node.Low.IsConstant() && node.High.IsConstant())
                return false;
            return DependsOn(node.Low, x) || DependsOn(node.High, x);
        }

        public void clearVisited(DDIndex v) {
        }

        /// <summary>
        /// constructs a BDD for the disjunction var1 OR var2 OR var3
        /// use a negative integer to indicate a negated variable, e.g., -3 will translate to Not x3.
        /// </summary>
        /// <param name="var1"></param>
        /// <param name="var2"></param>
        /// <param name="var3"></param>
        /// <returns></returns>
        public DDIndex getDisjunctionDD(int var1, int var2, int var3) {
            // let's assume the variables are sorted var1 < var2 < var3
            DDIndex dd1, dd2, dd3;
            dd1 = this.Manager.Allocate(new SingleVarOpDDNode(Math.Abs(var1), DDIndex.False, DDIndex.True));
            if (var1 > 0) {
                dd1 = dd1.Flip();
            }
            if (var2 > 0) {
                dd2 = this.Manager.Allocate(new SingleVarOpDDNode(Math.Abs(var2), DDIndex.False, dd1));
            } else {
                dd2 = this.Manager.Allocate(new SingleVarOpDDNode(Math.Abs(var2), dd1, DDIndex.False));
            }
            if (var3 > 0) {
                dd3 = this.Manager.Allocate(new SingleVarOpDDNode(Math.Abs(var3), DDIndex.False, dd2));
            } else {
                dd3 = this.Manager.Allocate(new SingleVarOpDDNode(Math.Abs(var3), dd2, DDIndex.False));
            }
            return dd3;
        }
    }
}