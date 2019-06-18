using System;
using System.Runtime.Serialization;

namespace Grok
{
    [Serializable]
    public class GrokException : Exception
    {
        public GrokException()
        {
        }

        public GrokException(string message) : base(message)
        {
        }

        public GrokException(string message, Exception inner) : base(message, inner)
        {
        }

        protected GrokException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}