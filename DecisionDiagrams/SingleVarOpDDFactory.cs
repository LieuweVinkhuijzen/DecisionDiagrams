// <copyright file="DDManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace DecisionDiagrams
{
    class SingleVarOpDDFactory : IDDNodeFactory<SingleVarOpDDNode>
    {
        public DDManager<SingleVarOpDDNode> Manager;

        public long MaxVariables;
        public SingleVarOpDDFactory()
        {
            //
        }

        DDManager<SingleVarOpDDNode> IDDNodeFactory<SingleVarOpDDNode>.Manager { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        long IDDNodeFactory<SingleVarOpDDNode>.MaxVariables { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        /// <summary>
        /// So the thing is that normally the variable index is just the next variable + 1, right
        /// but here, ... I don't know, actually.
        /// TODO figure this out
        /// </summary>
        /// <param name="xid"></param>
        /// <param name="yid"></param>
        /// <returns></returns>
        private int getOpVariableIndex(SingleVarOpDDNode x, SingleVarOpDDNode y) {
            return Math.Max(x.Variable, y.Variable) + 1;
        }
            // TODO I used to think that this would be faster
            // if (a.Variable == b.Variable) {
            //     // Can be summarized as if a.low == 0 then result.low == 0 and a.high == 0 implies result.high == 0
            //     resultVariable = a.Variable;
            //     // Get low node
            //     if (a.Low.IsZero() || b.Low.IsZero()) {
            //         resultLow = DDIndex.False;
            //     } else if (a.Low.IsOne()) {
            //         resultLow = b.Low;
            //     } else if (b.Low.IsOne()) {
            //         resultLow = a.Low;
            //     } else {
            //         // both a.low, b.low are not constant
            //         resultLow = this.Manager.Apply(a.Low, b.Low, DDOperation.And);
            //     }

            //     // Get high node
            //     if (a.High.IsZero() || b.High.IsZero()) {
            //         resultHigh = DDIndex.False;
            //     } else if (a.High.IsOne()) {
            //         resultHigh = b.High;
            //     } else if (b.High.IsOne()) {
            //         resultHigh = a.High;
            //     } else {
            //         resultHigh = this.Manager.Apply(a.High, b.High, DDOperation.And);
            //     }
            //     result = this.Manager.Allocate(new SingleVarOpDDNode(resultVariable, resultLow, resultHigh));
            // }
            // HashSet<int> impliedLiterals = new HashSet<int>(); // TODO this is part of a future optimization
            // addImpliedLiteralsToSet(a, impliedLiterals);
            // addImpliedLiteralsToSet(b, impliedLiterals);


        static List<int> FixedValues = new List<int>();

        /// <summary>
        /// Computes the conjunction of a and b; specifically, constructs a DDIndex representing a AND b
        /// TODO use a cache
        /// Note: this procedure does NOT use the Manager. Instead, it takes on the responsibility of cache itself
        /// </summary>
        /// <param name="aEdge"></param>
        /// <param name="a"></param>
        /// <param name="bEdge"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public DDIndex And(DDIndex aEdge, DDIndex bEdge) {
            if (aEdge.IsComplemented() || bEdge.IsComplemented()) {
                Console.WriteLine("Unsupported: complemented edges.");
            }
            const int unset = -1;
            SingleVarOpDDNode a = this.Manager.getNodeFromIndex(aEdge);
            SingleVarOpDDNode b = this.Manager.getNodeFromIndex(bEdge);
            DDIndex resultLow, resultHigh;
            int resultVariable;
            DDIndex resultEdge;
            SingleVarOpDDNode resultNode;
            KeyValuePair<int, int> assignment;
            /// Base cases
            if (aEdge.IsZero()) return DDIndex.False;
            if (bEdge.IsZero()) return DDIndex.False;
            if (aEdge.IsOne()) return bEdge;
            if (bEdge.IsOne()) return aEdge;
            if (aEdge.Equals(bEdge)) return aEdge;
            if (aEdge.isComplementOf(bEdge)) return DDIndex.False;
            if (a.Variable == b.Variable) {
                resultLow = And(a.Low, b.Low);
                resultHigh = And(a.High, b.High);
                resultVariable = a.Variable;
            }
            // Treat the case where a is an AND node
            else if (a.isVariableAnd() || b.isVariableAnd()) {
                // get the highest implied node
                if (a.isPositiveVariableAnd() && b.isPositiveVariableAnd()) {
                    // One of the variables is higher
                    resultLow = DDIndex.False;
                    resultVariable = Math.Max(a.Variable, b.Variable);
                    if (a.Variable > b.Variable) {
                        FixedValues[resultVariable] = 1;
                        resultHigh = this.And(a.High, bEdge);
                        FixedValues[resultVariable] = unset;
                    } else if (a.Variable < b.Variable) {
                        FixedValues[resultVariable] = 1;
                        resultHigh = this.And(aEdge, b.High);
                        FixedValues[resultVariable] = unset;
                    } else {  // TODO this case is subsumed by the above
                        resultHigh = And(a.High, b.High);
                    }
                } else if (a.isPositiveVariableAnd()) {
                    // Then we demand that a.Variable < b.Variable
                    if (!(a.Variable < b.Variable)) {
                        Console.Write("ERR1: variable index was {a.Variable} >= {b.Variable}");
                    }
                    resultVariable = a.Variable;
                    resultLow = DDIndex.False;
                    FixedValues[resultVariable] = 1;
                    resultHigh = this.And(a.High, bEdge);
                    FixedValues[resultVariable] = unset;
                } else if (b.isPositiveVariableAnd()) {
                    // b is a positive variable. We maintain the invariant that b.Variable < a.Variable
                    resultVariable = b.Variable;
                    resultLow = DDIndex.False;
                    FixedValues[resultVariable] = 1;
                    resultHigh = this.And(aEdge, b.High);
                    FixedValues[resultVariable] = unset;
                } else {
                    Console.WriteLine("Unsupported: Negative Variable And");
                }
            } else {
                // Both nodes are Shannon nodes
                DDIndex activeEdge   = (a.Variable > b.Variable) ? aEdge : bEdge;
                DDIndex passiveEdge  = (a.Variable > b.Variable) ? bEdge : aEdge;
                SingleVarOpDDNode activeNode, passiveNode;
                activeNode  = (a.Variable > b.Variable) ? a : b;
                passiveNode = (a.Variable > b.Variable) ? b : a;
                int targetVariable = Math.Max(a.Variable, b.Variable);
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
            resultEdge = ReduceAndAllocate(resultNode);
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
            for (int i=FixedValues.Count; i < this.Manager.NumVariables; i++) {
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
        /// Makes sure that if f implies x, then x is one of the first variables that is branched on
        /// </summary>
        /// <param name="node"></param>
        /// <param name="result"></param>
        /// <returns>if a reduction occurred</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public void ReduceAndAllocate(SingleVarOpDDNode node, out DDIndex result)
        {
            SingleVarOpDDNode lowNode = this.Manager.getNodeFromIndex(node.Low);
            SingleVarOpDDNode highNode = this.Manager.getNodeFromIndex(node.High);
            SingleVarOpDDNode resultNode = new SingleVarOpDDNode();
            // if node.low = x AND g   and   node.high = x AND then node should imply x
            if ( lowNode.isPositiveVariableAnd() && highNode.isPositiveVariableAnd() && lowNode.Variable == highNode.Variable ) {
                // TODO keep track of complement bits
                resultNode.Low = DDIndex.False;
                resultNode.Variable = lowNode.Variable;
                SingleVarOpDDNode resultHighNode = new SingleVarOpDDNode();
                resultHighNode.Low = lowNode.High;
                resultHighNode.High = highNode.High;
                resultHighNode.Variable = node.Variable;
                resultNode.High = this.Manager.Allocate(resultHighNode);
            }
            result = this.Manager.Allocate(resultNode);
            throw new System.NotImplementedException();
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
        /// TODO does not take into account complement bits
        /// </summary>
        /// <param name="v"></param>
        /// <param name="impliedLiterals"></param>
        public void addImpliedLiteralsToSet(SingleVarOpDDNode v, HashSet<int> impliedLiterals) {
            SingleVarOpDDNode u = v;
            bool complement = false;

            while (u.isVariableAnd()) {
                if (u.isPositiveVariableAnd()) {
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
        /// TODO should we return a node, or an index?
        /// </summary>
        /// <returns></returns>
        public SingleVarOpDDNode getFirstNontrivialDescendant(SingleVarOpDDNode v) {
            while (!v.isNontrivialFunction()) {
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
        ///    (this will result in fewer function calls)
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
        /// use a negative integer to indicate a negated variable, e.g., -3 will translate to Not x3
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