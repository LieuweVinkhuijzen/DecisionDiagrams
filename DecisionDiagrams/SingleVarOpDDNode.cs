using System;
using System.Net.WebSockets;

#pragma warning disable SA1505 // Opening braces should not be followed by blank line
namespace DecisionDiagrams
{
#pragma warning restore SA1505 // Opening braces should not be followed by blank line
    /// <summary>
    /// A node which is either a Shannon node, or an Operator
    /// If it is an operator, then it has type either And, Or, or XOR
    /// and the High edge points to a single variable (more specifically, it points to a SingleVarOpDDNode which depends on a single variable)
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

    /// <summary>
    /// A helped bit to be used by algorithms that operate on the DD
    /// Indicates whether this node has been visited by that algorithm
    /// This prevents the need for the algorithm to store a dynamically allocated HashSet<Node>, for example,
    /// which significantly speeds up computation
    /// </summary>
    public bool Visited;

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
    public bool isVariableAnd() {
        // If this node is a constant, then return false
        return isPositiveVariableAnd() || isNegativeVariableAnd();
    }

    /// <summary>
    /// Returns whether this function is of the form f = x AND g.
    /// </summary>
    /// <returns></returns>
    public bool isPositiveVariableAnd() {
        return Low.IsZero() && !High.IsZero();
    }

    /// <summary>
    /// Returns whether this function is of the form f = (Not x) AND g.
    /// </summary>
    /// <returns></returns>
    public bool isNegativeVariableAnd() {
        return !Low.IsZero() && High.IsZero();
    }

    public bool isVariableAnd(int var) {
        return this.Variable == var && isVariableAnd();
    }

    public bool isNotVariableAnd(int var) {
        return this.Variable == var && isNegativeVariableAnd();
    }

    /// <summary>
    /// This case can't happen in a reduced diagram, but ok
    /// </summary>
    /// <returns></returns>
    public bool isVariableOr() {
        return isPositiveVariableOr() || isNegativeVariableOr();
    }

    public bool isPositiveVariableOr() {
        return High.IsOne() && !Low.IsOne();
    }

    public bool isNegativeVariableOr() {
        return Low.IsOne() && !High.IsOne();
    }

    public bool isOrNode(int var) {
        return this.Variable == var && isVariableOr();
    }

    public bool isNotOrNode(int var) {
        return this.Variable == var && isNegativeVariableOr();
    }

    /// <summary>
    /// Returns whether the function can be written as f = x XOR g
    /// where x is the top variable, and g is some function independent of x
    /// </summary>
    /// <returns></returns>
    public bool isXorNode() {
        if (Low.GetPosition() == High.GetPosition())
            return Low.IsComplemented() && !High.IsComplemented() || !Low.IsComplemented() && High.IsComplemented();
        return false;
    }

    public bool isXorNode(int var) {
        return this.Variable == var && isXorNode();
    }

    /// <summary>
    /// Returns whether the function depends on exactly one variable.
    /// </summary>
    /// <returns></returns>
    public bool isLiteral() {
        return isPositiveLiteral() || isNegativeLiteral();
    }

    public bool isPositiveLiteral() {
        return Low.IsZero() && High.IsOne();
    }

    public bool isNegativeLiteral() {
        return Low.IsOne() && High.IsZero();
    }

    public bool isSingleVariableOperation() {
        return isVariableAnd() || isVariableOr() || isVariableAnd();
    }

    public bool isNontrivialFunction() {
        return !isSingleVariableOperation();
    }

    public bool isConstant() {
        return Low.IsZero() && High.IsZero() || Low.IsOne() && High.IsOne();
    }

}