using System;
using System.Text.RegularExpressions;

namespace Fusion.Resources.Domain
{
    /// <summary>
    /// Can either be a contract number or the unique id from the org chart.
    /// </summary>
    public struct OrgContractId
    {
        public OrgContractId(string identifier)
        {
            OriginalIdentifier = identifier;

            if (Guid.TryParse(identifier, out Guid id))
            {
                UniqueId = id;
                ContractNumber = null;
                Type = IdentifierType.UniqueId;
            }
            else
            {
                if (!Regex.IsMatch(identifier, @"\d+"))
                    throw new ArgumentException("Contract number can only consist of digits");

                UniqueId = null;
                ContractNumber = identifier;
                Type = IdentifierType.Number;
            }
        }

        public OrgContractId(Guid uniqueId)
        {
            OriginalIdentifier = $"{uniqueId}";
            UniqueId = uniqueId;
            ContractNumber = null;
            Type = IdentifierType.UniqueId;
        }

        public Guid? UniqueId { get; set; }
        public string OriginalIdentifier { get; set; }
        public string? ContractNumber { get; set; }
        public IdentifierType Type { get; set; }

        public enum IdentifierType { UniqueId, Number }


        public static implicit operator OrgContractId(string identifier)
        {
            return new OrgContractId(identifier);
        }
        public static implicit operator OrgContractId(Guid uniqueId)
        {
            return new OrgContractId(uniqueId);
        }
    }
}
