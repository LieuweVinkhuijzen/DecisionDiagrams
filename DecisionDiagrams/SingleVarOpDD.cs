using System;
using System.Net.WebSockets;

#pragma warning disable SA1505 // Opening braces should not be followed by blank line
namespace DecisionDiagrams {
#pragma warning restore SA1505 // Opening braces should not be followed by blank line
    /// <summary>
    /// the type of node.
    /// </summary>
    public struct SingleVarOpDD : IDDNode, IEquatable<SingleVarOpDD> {
    public enum SingleVarOpDDType {
        ShannonNode,
        And,
        Or,
        Xor,
    }

    public SingleVarOpDDType type;

    public DDIndex Low { get; set; }

    public DDIndex High { get; set; }

    public int Variable { get; set; }

    public bool Mark { get; set; }

    public SingleVarOpDD(int variable, DDIndex lo, DDIndex hi, bool mark) {
        this.Low = lo;
        this.High = hi;
        this.Variable = variable;
        this.Mark = mark;
        this.type = SingleVarOpDDType.ShannonNode; // by default
    }

    public SingleVarOpDD(int variable, DDIndex lo, DDIndex hi, bool mark, SingleVarOpDDType type) {
        this.Low = lo;
        this.High = hi;
        this.Variable = variable;
        this.Mark = mark;
        this.type = type;
        // if it's a non-Shannon node, then the single variable must be the High node
        if (type != SingleVarOpDDType.ShannonNode) {
            // TODO make sure the High edges points to a single variable
        }
    }

    public bool Equals(SingleVarOpDD node) {
        return Low.Equals(node.Low) && High.Equals(node.High) && type == node.type && Variable == node.Variable && Mark == node.Mark;
    }
    }
}