using System;
using System.Collections.Generic;
using System.Text;

namespace Task1
{
    public class ContainerException : System.Exception
    {
        public ContainerException()
        {
        }

        public ContainerException(string message) : base(message)
        {
        }

        public ContainerException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
