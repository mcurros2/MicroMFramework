namespace MicroM.Core
{
    /// <summary>
    /// Interface to implement lazy initialization in a class
    /// </summary>
    public interface IInit
    {
        /// <summary>
        /// Determines is the class has been initialized
        /// </summary>
        public bool IsInitialized { get; }

        /// <summary>
        /// Method that will init the class, here you should check that all needed properties are set
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Checks if the object is initialized and throws and exeption if not. Call this from within methods that need the class initialized.
        /// </summary>
        protected virtual void CheckInit()
        {
            if (!IsInitialized) throw new ClassNotInitilizedException();
        }


    }
}
