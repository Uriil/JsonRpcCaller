using System;

namespace JsonRpcCallerLibrary
{
    public class JsonRpcCallerException : Exception
    {
        public int Code { get; set; }
        public object ExtraData { get; set; }

        public JsonRpcCallerException() : base() { }

        public JsonRpcCallerException(string message)
            : base(message)
        {
        }

        public JsonRpcCallerException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}