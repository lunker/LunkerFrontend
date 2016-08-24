using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class NoMessageException : Exception
{

    public NoMessageException()
    {
    }

    public NoMessageException(string message) : base(message)
    {

            

    }

    public NoMessageException(string message, Exception inner) : base(message, inner)
    {


    }
}

