using System;


namespace OtusCSharpModels
{
    [AttributeUsage (AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class GenerateBinarySerializerAttribute : Attribute 
    {
    }
}
