namespace Cozi.IL
{
    public struct ILFuncSignature
    {
        public TypeInfo[] ArgTypes;

        public bool Compare(ILFuncSignature other)
        {
            if(ArgTypes.Length != other.ArgTypes.Length)
                return false;

            for(int i = 0; i < other.ArgTypes.Length; i++)
            {
                if(!ArgTypes[i].Equals(other.ArgTypes[i]))
                    return false;
            }

            return true;
        }
    }
}