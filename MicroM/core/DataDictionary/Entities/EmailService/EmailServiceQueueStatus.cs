using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    public class EmailServiceQueueStatusDef : EntityDefinition
    {
        public EmailServiceQueueStatusDef() : base("emqs", nameof(EmailServiceQueueStatus)) { }

        public readonly Column<string> c_email_queue_id = Column<string>.PK();
        public readonly Column<string> c_status_id = Column<string>.PK();
        public readonly Column<string> c_statusvalue_id = Column<string>.FK();

        public readonly ViewDefinition emqs_brwStandard = new(nameof(c_email_queue_id));

        public readonly EntityForeignKey<EmailServiceQueue, EmailServiceQueueStatus> FKQueue = new();

    }

    public class EmailServiceQueueStatus : Entity<EmailServiceQueueStatusDef>
    {
        public EmailServiceQueueStatus() : base() { }
        public EmailServiceQueueStatus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
