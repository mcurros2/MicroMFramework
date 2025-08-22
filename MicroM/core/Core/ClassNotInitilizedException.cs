namespace MicroM.Core
{
    class ClassNotInitilizedException : Exception
    {
        public ClassNotInitilizedException() : base("The class is not initilized. Initialize the class before its use by calling Init() method.") { }
        public ClassNotInitilizedException(string message) : base(message) { }

    }
}
