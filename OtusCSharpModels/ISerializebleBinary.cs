using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtusCSharpModels
{

    public interface ISerializableBinary<T> where T : ISerializableBinary<T>
    {
        void SerializeToBinary(Stream stream);

        static abstract T DeserializeFromBinary(Stream stream);
    }
}
