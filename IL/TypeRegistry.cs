using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cozi.IL
{
    public class TypeRegistry
    {
        public string ModuleName => Owner?.Name ?? "";
        public readonly ILModule Owner;

        private Dictionary<string, TypeInfo> _typeCache = new Dictionary<string, TypeInfo>();
        private Dictionary<int, TypeInfo> _idCache = new Dictionary<int, TypeInfo>();
        private int _nextID = 0;

        public TypeRegistry(ILModule owner)
        {
            Owner = owner;
        }

        public bool TryGetTypeById(int id, out TypeInfo type)
        {
            return _idCache.TryGetValue(id, out type);
        }

        public void DefineType(TypeInfo type)
        {
            type.Owner = this;
            type.ID = _nextID++;

            _typeCache.Add(type.Name, type);
            _idCache.Add(type.ID, type);
        }

        public TypeInfo GetType(string typename)
        {
            if(_typeCache.TryGetValue(typename, out var val))
                return val;

            return null;
        }

        public bool TryGetType(string typename, out TypeInfo type)
        {
            return _typeCache.TryGetValue(typename, out type);
        }

        public override string ToString()
        {
            return string.Join( "\n", _typeCache.Select( x => x.Value.ToString() ).ToArray() );
        }

        public void Serialize(BinaryWriter outStream)
        {
            // serialize number of types
            outStream.Write(_typeCache.Count);

            // serialize each type
            foreach(var t in _typeCache.Values)
            {
                t.Serialize(outStream);
            }
        }

        public void Deserialize(BinaryReader inStream)
        {
            int count = inStream.ReadInt32();

            _typeCache.Clear();
            for(int i = 0; i < count; i++)
            {
                DefineType( TypeInfo.Deserialize(inStream) );
            }
        }
    }
}