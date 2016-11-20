using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentEmail.Core.Defaults;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;

namespace FluentEmail.Core
{
    public class Email : IFluentEmail
    {
        public EmailData Data { get; set; }
        public ITemplateRenderer Renderer { get; set; }
        public ISender Sender { get; set; }

        public static ITemplateRenderer DefaultRenderer = new ReplaceRenderer();
        public static ISender DefaultSender = new SaveToDiskSender("/");

        /// <summary>
        /// Creates a new email instance with default settings.
        /// </summary>
        /// <param name="client">Smtp client to send from</param>
        public Email() : this(DefaultRenderer, DefaultSender) { }

        /// <summary>
        /// Creates a new email instance with overrides for the rendering and sending engines.
        /// </summary>
        /// <param name="renderer">The template rendering engine</param>
        /// <param name="sender">The email sending implementation</param>
        public Email(ITemplateRenderer renderer, ISender sender)
            : this(renderer, sender, null, null)
        {
        }

        /// <summary>
        ///  Creates a new Email instance with default settings, from a specific mailing address.
        /// </summary>
        /// <param name="emailAddress">Email address to send from</param>
        /// <param name="name">Name to send from</param>
        public Email(string emailAddress, string name = "") 
            : this(DefaultRenderer, DefaultSender, emailAddress, name) { }

        /// <summary>
        ///  Creates a new Email instance using the given engines and mailing address.
        /// </summary>
        /// <param name="renderer">The template rendering engine</param>
        /// <param name="sender">The email sending implementation</param>
        /// <param name="emailAddress">Email address to send from</param>
        /// <param name="name">Name to send from</param>
        public Email(ITemplateRenderer renderer, ISender sender, string emailAddress, string name = "")
        {
            Data = new EmailData()
            {
                FromAddress = new Address() {EmailAddress = emailAddress, Name = name}
            };
            Renderer = renderer;
            Sender = sender;
        }

        /// <summary>
        /// Creates a new Email instance and sets the from property
        /// </summary>
        /// <param name="emailAddress">Email address to send from</param>
        /// <param name="name">Name to send from</param>
        /// <returns>Instance of the Email class</returns>
        public static IFluentEmail From(string emailAddress, string name = "")
        {
            var email = new Email
            {
                Data = {FromAddress = new Address(emailAddress, name)}
            };

            return email;
        }

        /// <summary>
        /// Adds a reciepient to the email, Splits name and address on ';'
        /// </summary>
        /// <param name="emailAddress">Email address of recipeient</param>
        /// <param name="name">Name of recipient</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail To(string emailAddress, string name)
        {
            if (emailAddress.Contains(";"))
            {
                //email address has semi-colon, try split
                var nameSplit = name.Split(';');
                var addressSplit = emailAddress.Split(';');
                for (int i = 0; i < addressSplit.Length; i++)
                {
                    var currentName = string.Empty;
                    if ((nameSplit.Length - 1) >= i)
                    {
                        currentName = nameSplit[i];
                    }
                    Data.ToAddresses.Add(new Address(addressSplit[i], currentName));
                }
            }
            else
            {
                Data.ToAddresses.Add(new Address(emailAddress, name));
            }
            return this;
        }

        /// <summary>
        /// Adds a reciepient to the email
        /// </summary>
        /// <param name="emailAddress">Email address of recipeient (allows multiple splitting on ';')</param>
        /// <returns></returns>
        public IFluentEmail To(string emailAddress)
        {
            if (emailAddress.Contains(";"))
            {
                foreach (string address in emailAddress.Split(';'))
                {
                    Data.ToAddresses.Add(new Address(address));
                }
            }
            else
            {
                Data.ToAddresses.Add(new Address(emailAddress));
            }

            return this;
        }

        /// <summary>
        /// Adds all reciepients in list to email
        /// </summary>
        /// <param name="mailAddresses">List of recipients</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail To(IList<Address> mailAddresses)
        {
            foreach (var address in mailAddresses)
            {
                Data.ToAddresses.Add(address);
            }
            return this;
        }

        /// <summary>
        /// Adds a Carbon Copy to the email
        /// </summary>
        /// <param name="emailAddress">Email address to cc</param>
        /// <param name="name">Name to cc</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail CC(string emailAddress, string name = "")
        {
            Data.CcAddresses.Add(new Address(emailAddress, name));
            return this;
        }

        /// <summary>
        /// Adds all Carbon Copy in list to an email
        /// </summary>
        /// <param name="mailAddresses">List of recipients to CC</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail CC(IList<Address> mailAddresses)
        {
            foreach (var address in mailAddresses)
            {
                Data.CcAddresses.Add(address);
            }
            return this;
        }

        /// <summary>
        /// Adds a blind carbon copy to the email
        /// </summary>
        /// <param name="emailAddress">Email address of bcc</param>
        /// <param name="name">Name of bcc</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail BCC(string emailAddress, string name = "")
        {
            Data.BccAddresses.Add(new Address(emailAddress, name));
            return this;
        }

        /// <summary>
        /// Adds all blind carbon copy in list to an email
        /// </summary>
        /// <param name="mailAddresses">List of recipients to BCC</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail BCC(IList<Address> mailAddresses)
        {
            foreach (var address in mailAddresses)
            {
                Data.BccAddresses.Add(address);
            }
            return this;
        }

        /// <summary>
        /// Sets the ReplyTo address on the email
        /// </summary>
        /// <param name="address">The ReplyTo Address</param>
        /// <returns></returns>
        public IFluentEmail ReplyTo(string address)
        {
            Data.ReplyToAddresses.Add(new Address(address));

            return this;
        }

        /// <summary>
        /// Sets the ReplyTo address on the email
        /// </summary>
        /// <param name="address">The ReplyTo Address</param>
        /// <param name="name">The Display Name of the ReplyTo</param>
        /// <returns></returns>
        public IFluentEmail ReplyTo(string address, string name)
        {
            Data.ReplyToAddresses.Add(new Address(address, name));

            return this;
        }

        /// <summary>
        /// Sets the subject of the email
        /// </summary>
        /// <param name="subject">email subject</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail Subject(string subject)
        {
            Data.Subject = subject;
            return this;
        }

        /// <summary>
        /// Adds a Body to the Email
        /// </summary>
        /// <param name="body">The content of the body</param>
        /// <param name="isHtml">True if Body is HTML, false for plain text (Optional)</param>
        public IFluentEmail Body(string body)
        {
            Data.Body = body;
            return this;
        }

        /// <summary>
        /// Marks the email as High Priority
        /// </summary>
        public IFluentEmail HighPriority()
        {
            Data.Priority = Priority.High;
            return this;
        }

        /// <summary>
        /// Marks the email as Low Priority
        /// </summary>
        public IFluentEmail LowPriority()
        {
            Data.Priority = Priority.Low;
            return this;
        }

        /// <summary>
        /// Set the template rendering engine to use, defaults to RazorEngine
        /// </summary>
        public IFluentEmail UsingTemplateEngine(ITemplateRenderer renderer)
        {
            Renderer = renderer;
            return this;
        }

        /// <summary>
        /// Adds template to email from embedded resource
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">Path the the embedded resource eg [YourAssembly].[YourResourceFolder].[YourFilename.txt]</param>
        /// <param name="model">Model for the template</param>
        /// <param name="assembly">The assembly your resource is in. Defaults to calling assembly.</param>
        /// <returns></returns>
        public IFluentEmail UsingTemplateFromEmbedded<T>(string path, T model, Assembly assembly)
        {
            var template = EmbeddedResourceHelper.GetResourceAsString(assembly, path);
            var result = Renderer.Parse(template, model, Data.IsHtml);
            Data.Body = result;

            return this;
        }

        /// <summary>
        /// Adds the template file to the email
        /// </summary>
        /// <param name="filename">The path to the file to load</param>
        /// <param name="isHtml">True if Body is HTML, false for plain text (Optional)</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail UsingTemplateFromFile<T>(string filename, T model)
        {
            var template = "";

            using (var reader = new StreamReader(File.OpenRead(filename)))
            {
                template = reader.ReadToEnd();
            }

            var result = Renderer.Parse(template, model, Data.IsHtml);
            Data.Body = result;

            return this;
        }

        /// <summary>
        /// Adds a culture specific template file to the email
        /// </summary>
        /// <param name="filename">The path to the file to load</param>
        /// /// <param name="model">The razor model</param>
        /// <param name="culture">The culture of the template (Default is the current culture)</param>
        /// <param name="isHtml">True if Body is HTML, false for plain text (Optional)</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail UsingCultureTemplateFromFile<T>(string filename, T model, CultureInfo culture)
        {
            var cultureFile = GetCultureFileName(filename, culture);
            return UsingTemplateFromFile(cultureFile, model);
        }

        /// <summary>
        /// Adds razor template to the email
        /// </summary>
        /// <param name="template">The razor template</param>
        /// <param name="isHtml">True if Body is HTML, false for plain text (Optional)</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail UsingTemplate<T>(string template, T model, bool isHtml = true)
        {
            var result = Renderer.Parse(template, model, isHtml);
            Data.Body = result;

            return this;
        }

        /// <summary>
        /// Adds an Attachment to the Email
        /// </summary>
        /// <param name="attachment">The Attachment to add</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail Attach(Attachment attachment)
        {
            if (!Data.Attachments.Contains(attachment))
            {
                Data.Attachments.Add(attachment);
            }

            return this;
        }

        /// <summary>
        /// Adds Multiple Attachments to the Email
        /// </summary>
        /// <param name="attachments">The List of Attachments to add</param>
        /// <returns>Instance of the Email class</returns>
        public IFluentEmail Attach(IList<Attachment> attachments)
        {
            foreach (var attachment in attachments.Where(attachment => !Data.Attachments.Contains(attachment)))
            {
                Data.Attachments.Add(attachment);
            }
            return this;
        }

        /// <summary>
        /// Sets Message to html (set by default)
        /// </summary>
        public IFluentEmail BodyAsHtml()
        {
            Data.IsHtml = true;
            return this;
        }

        /// <summary>
        /// Sets Message to plain text (set by default)
        /// </summary>
        public IFluentEmail BodyAsPlainText()
        {
            Data.IsHtml = false;
            return this;
        }

        /// <summary>
        /// Sends email synchronously
        /// </summary>
        /// <returns>Instance of the Email class</returns>
        public virtual SendResponse Send(CancellationToken? token = null)
        {
            return Sender.Send(this, token);
        }

        public virtual async Task<SendResponse> SendAsync(CancellationToken? token = null)
        {
            return await Sender.SendAsync(this, token);
        }

        private static string GetCultureFileName(string fileName, CultureInfo culture)
        {
            var extension = Path.GetExtension(fileName);
            var cultureExtension = string.Format("{0}{1}", culture.Name, extension);

            var cultureFile = Path.ChangeExtension(fileName, cultureExtension);
            if (File.Exists(cultureFile))
                return cultureFile;
            else
                return fileName;
        }
    }
}
