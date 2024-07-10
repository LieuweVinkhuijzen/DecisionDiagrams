#pragma warning disable SA1505 // Opening braces should not be followed by blank line
namespace DecisionDiagrams
{
    public partial struct SingleVarOpDDNode
    {
        public enum SingleVarOpDDType {
        ShannonNode,
        And,
        Or,
        Xor,
    }
    }
}