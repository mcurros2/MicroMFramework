namespace MicroM.AutomatedTests;

public abstract class BaseSeedData
{
    public BaseSeedData() { }

    public virtual object[] GetSeedData() => [];
}

public class BaseSeedData<T> : BaseSeedData where T : class
{
    public BaseSeedData() { }

    public override T[] GetSeedData() => [];

}

