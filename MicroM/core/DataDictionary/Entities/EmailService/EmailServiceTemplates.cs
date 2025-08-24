using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Defines the data structure for storing email templates.
    /// </summary>
    public class EmailServiceTemplatesDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailServiceTemplatesDef"/> class.
        /// </summary>
        public EmailServiceTemplatesDef() : base("eqt", nameof(EmailServiceTemplates)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        /// <summary>
        /// Unique identifier for the email template.
        /// </summary>
        public readonly Column<string> c_email_template_id = Column<string>.PK();

        /// <summary>
        /// Subject line used for the email template.
        /// </summary>
        public readonly Column<string> vc_template_subject = Column<string>.Text();

        /// <summary>
        /// Body content of the email template.
        /// </summary>
        public readonly Column<string> vc_template_body = Column<string>.Text(size: 0);

        /// <summary>
        /// Standard view definition including the template identifier.
        /// </summary>
        public readonly ViewDefinition eqt_brwStandard = new(nameof(c_email_template_id));
    }

    /// <summary>
    /// Represents an email service template entity.
    /// </summary>
    public class EmailServiceTemplates : Entity<EmailServiceTemplatesDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailServiceTemplates"/> class.
        /// </summary>
        public EmailServiceTemplates() : base() { }

        /// <summary>
        /// Initializes a new instance with the specified entity client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client for data operations.</param>
        /// <param name="encryptor">Optional encryption service.</param>
        public EmailServiceTemplates(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
