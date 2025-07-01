using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    public class EmailServiceConfigurationDef : EntityDefinition
    {
        public EmailServiceConfigurationDef() : base("eqc", nameof(EmailServiceConfiguration)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        public readonly Column<string> c_email_configuration_id = Column<string>.PK();
        public readonly Column<string> vc_smtp_host = Column<string>.Text(size: 2048);
        public readonly Column<int> i_smtp_port = new();
        public readonly Column<string> vc_user_name = Column<string>.Text();
        public readonly Column<string> vc_password = Column<string>.Text(size: 2048, encrypted: true); // Encrypted columns store base64 encoded strings
        public readonly Column<bool?> bt_use_ssl = new(nullable: true);

        public readonly Column<string?> vc_default_sender_email = Column<string?>.Text(nullable: true);
        public readonly Column<string?> vc_default_sender_name = Column<string?>.Text(nullable: true);

        public readonly Column<string?> vc_template_subject = Column<string?>.Text(fake: true, nullable: true);
        public readonly Column<string?> vc_template_body = Column<string?>.Text(size: 0, fake: true, nullable: true);

        public readonly ViewDefinition eqc_brwStandard = new(nameof(c_email_configuration_id));
    }

    public class EmailServiceConfiguration : Entity<EmailServiceConfigurationDef>
    {
        public EmailServiceConfiguration() : base() { }
        public EmailServiceConfiguration(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
