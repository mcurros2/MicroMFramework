using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    public class EmailServiceTemplatesDef : EntityDefinition
    {
        public EmailServiceTemplatesDef() : base("eqt", nameof(EmailServiceTemplates)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        public readonly Column<string> c_email_template_id = Column<string>.PK();

        public readonly Column<string> vc_template_subject = Column<string>.Text();
        public readonly Column<string> vc_template_body = Column<string>.Text(size: 0);

        public readonly ViewDefinition eqt_brwStandard = new(nameof(c_email_template_id));
    }

    public class EmailServiceTemplates : Entity<EmailServiceTemplatesDef>
    {
        public EmailServiceTemplates() : base() { }
        public EmailServiceTemplates(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
