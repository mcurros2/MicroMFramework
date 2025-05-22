namespace MicroM.Core
{
    /// <summary>
    /// Class to implement lazy initialization
    /// </summary>
    public abstract class InitBase
    {
        /// <summary>
        /// Determines is the class has been initialized
        /// </summary>
        public bool IsInitialized { get; protected set; }

        /// <summary>
        /// Checks if the object is initialized and throws and exeption if not. Call this from within methods that need the class initialized.
        /// </summary>
        protected virtual void CheckInit()
        {
            if (!IsInitialized) throw new ClassNotInitilizedException();
        }


    }
}
