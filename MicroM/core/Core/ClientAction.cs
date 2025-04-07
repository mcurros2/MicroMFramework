namespace MicroM.Core
{
    public abstract class BaseAction
    {
        public string Name = null!;
        public string MenuText = null!;

        private readonly Dictionary<string, object> _arguments = new(StringComparer.OrdinalIgnoreCase);

        protected void AddParameter<T>(string name, T parm)
        {
            _arguments[name] = parm!;
        }

        protected T GetParameter<T>(string name)
        {
            return (T)_arguments[name];
        }

        public abstract Task<Dictionary<string, object>> Action();

    }
}
