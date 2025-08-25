using System;

namespace MicroM.Core
{
    /// <summary>
    /// Exception thrown when an object is used before it is initialized.
    /// </summary>
    internal class ClassNotInitilizedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassNotInitilizedException"/> class with a default message.
        /// </summary>
        public ClassNotInitilizedException() : base("The class is not initilized. Initialize the class before its use by calling Init() method.") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassNotInitilizedException"/> class with a custom message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ClassNotInitilizedException(string message) : base(message) { }
    }
}
