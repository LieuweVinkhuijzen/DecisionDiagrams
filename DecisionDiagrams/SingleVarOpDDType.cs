#pragma warning disable SA1505 // Opening braces should not be followed by blank line
namespace DecisionDiagrams
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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