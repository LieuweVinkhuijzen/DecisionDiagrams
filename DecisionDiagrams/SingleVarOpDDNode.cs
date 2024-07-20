using System;
using System.Net.WebSockets;

#pragma warning disable SA1505 // Opening braces should not be followed by blank line
namespace DecisionDiagrams
{
    /// <summary>
    /// A node which is either a Shannon node, or an Operator
    /// If it is an operator, then it has type either And, Or, or XOR
    /// and the High edge points to a single variable (more specifically, it points to a SingleVarOpDDNode which depends on a single variable).
    /// </summary>
    public partial struct SingleVarOpDDNode : IDDNode, IEquatable<SingleVarOpDDNode> {

    /// <summary>
    /// A pointer to the low node, including bit flip.
    /// </summary>
    public DDIndex Low { get; set; }

    /// <summary>
    /// A pointer to the high node, including bit flip.
    /// </summary>
    public DDIndex High { get; set; }

    /// <summary>
    /// The variable on which this node branches.
    /// </summary>
    public int Variable { get; set; }
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public bool Mark { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary>
    /// A helped bit to be used by algorithms that operate on the DD
    /// Indicates whether this node has been visited by that algorithm
    /// This prevents the need for the algorithm to store a dynamically allocated HashSet, for example,
    /// which significantly speeds up computation.
    /// </summary>
    public bool Visited;

    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public SingleVarOpDDNode(int variable, DDIndex lo, DDIndex hi) {
        this.Low = lo;
        this.High = hi;
        this.Variable = variable;
        this.Visited = false;
    }

    public bool Equals(SingleVarOpDDNode node) {
        return Low.Equals(node.Low) && High.Equals(node.High) && Variable == node.Variable;
    }

    /// <summary>
    /// Returns whether this node's function can be written as f = x AND g   or as   f = !x AND g
    /// where x is the top variable, and g is some function
    /// possibly g is constant, so f is just the function f = [!]x.
    /// </summary>
    /// <returns></returns>
    public bool IsVariableAnd() {
        // If this node is a constant, then return false
        return IsPositiveVariableAnd() || IsNegativeVariableAnd();
    }

    /// <summary>
    /// Returns whether this function is of the form f = x AND g.
    /// </summary>
    /// <returns></returns>
    public bool IsPositiveVariableAnd() {
        return Low.IsZero() && !High.IsZero();
    }

    /// <summary>
    /// Returns whether this function is of the form f = (Not x) AND g.
    /// </summary>
    /// <returns></returns>
    public bool IsNegativeVariableAnd() {
        return !Low.IsZero() && High.IsZero();
    }

    public bool IsVariableAnd(int var) {
        return this.Variable == var && IsVariableAnd();
    }

    public bool IsNotVariableAnd(int var) {
        return this.Variable == var && IsNegativeVariableAnd();
    }

    /// <summary>
    /// This case can't happen in a reduced diagram, but ok.
    /// </summary>
    /// <returns></returns>
    public bool IsVariableOr() {
        return IsPositiveVariableOr() || IsNegativeVariableOr();
    }

    public bool IsPositiveVariableOr() {
        return High.IsOne() && !Low.IsOne();
    }

    public bool IsNegativeVariableOr() {
        return Low.IsOne() && !High.IsOne();
    }

    public bool IsOrNode(int var) {
        return this.Variable == var && IsVariableOr();
    }

    public bool IsNotOrNode(int var) {
        return this.Variable == var && IsNegativeVariableOr();
    }

    /// <summary>
    /// Returns whether the function can be written as f = x XOR g
    /// where x is the top variable, and g is some function independent of x.
    /// </summary>
    /// <returns></returns>
    public bool IsXorNode() {
        if (Low.GetPosition() == High.GetPosition())
            return Low.IsComplemented() && !High.IsComplemented() || !Low.IsComplemented() && High.IsComplemented();
        return false;
    }

    public bool IsXorNode(int var) {
        return this.Variable == var && IsXorNode();
    }

    /// <summary>
    /// Returns whether the function depends on exactly one variable.
    /// </summary>
    /// <returns></returns>
    public bool isLiteral() {
        return isPositiveLiteral() || IsNegativeLiteral();
    }

    public bool isPositiveLiteral() {
        return Low.IsZero() && High.IsOne();
    }

    public bool IsNegativeLiteral() {
        return Low.IsOne() && High.IsZero();
    }

    public bool IsSingleVariableOperation() {
        return IsVariableAnd() || IsVariableOr() || IsVariableAnd();
    }

    public bool IsNontrivialFunction() {
        return !IsSingleVariableOperation();
    }

    public bool IsConstant() {
        return Low.IsZero() && High.IsZero() || Low.IsOne() && High.IsOne();
    }
    }
}