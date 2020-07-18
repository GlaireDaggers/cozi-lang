namespace Cozi.IL
{
    public struct FuncRef
    {
        public ILModule InModule;
        public string FunctionName;

        public int GetFuncId()
        {
            return InModule.GetFuncId(FunctionName);
        }

        public FuncInfo GetFuncInfo()
        {
            return InModule.Functions[GetFuncId()];
        }
    }
}