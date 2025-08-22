using MicroM.Core;

namespace MicroM.Database
{
    public record DatabaseSchemaCreationOptions<T>(T EntityInstance, bool create_or_alter = true) where T : EntityBase
    {
        public Type EntityType => EntityInstance.GetType();
        public string Mneo => EntityInstance.Def.Mneo;

        public bool create_or_alter { get; set; } = create_or_alter;
    }
}
