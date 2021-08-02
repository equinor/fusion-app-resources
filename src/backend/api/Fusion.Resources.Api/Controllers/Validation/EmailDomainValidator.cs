using FluentValidation.Validators;
using System;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    /// <summary>
    /// Validator for invalid mail domains for preferred contact mails. 
    /// This is an attempt to weed out most of the private mail addresses to try and force usage of company mails only. 
    /// As we cannot cover all domains this is only a best effort (or maybe just 'an attempt' :p) approach.
    /// </summary>
    public class EmailDomainValidator : PropertyValidator, IPropertyValidator
    {
        private static string[] InvalidDomains = new[]
        {
            "gmail.com",
            "hotmail.com",
            "aol.com",
            "ymail.com",
            "yahoo.no",
            "yahoo.com",
            "outlook.com",
            "zohomail.eu",
            "mail.com",
            "protonmail.com",
            "icloud.com",
            "gmx.com",
            "gmx.us",
            "yandex.com"
        };

        protected override bool IsValid(PropertyValidatorContext context)
        {
            var value = context.PropertyValue as string;

            if (!string.IsNullOrEmpty(value) && value.Contains("@")) 
            {
                var domain = value.Split("@").Last();
                if (InvalidDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
        
    }

    

}
